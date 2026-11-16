"""Per-session local capture. Desktop capture requires explicit configuration/operator consent.
Unity only launches this agent and signals stop; it never waits for encoder shutdown.
"""
import argparse
import array
import ctypes
import datetime as dt
import json
import math
import os
from pathlib import Path
import subprocess
import time
import wave
from study_archive import atomic_json, read_json


def now_ms():
    return int(time.time() * 1000)


def parent_alive(pid):
    if os.name != "nt":
        return False
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    handle = kernel.OpenProcess(0x1000, False, pid)
    if not handle:
        return False  # Fail closed if Unity cannot be checked.
    try:
        code = ctypes.c_ulong()
        return bool(kernel.GetExitCodeProcess(handle, ctypes.byref(code))) and code.value == 259
    finally:
        kernel.CloseHandle(handle)


class ManifestReadWatch:
    """Advisory metadata read: tolerate brief contention, never wait forever."""
    def __init__(self, session_id, grace_seconds=30, clock=None):
        self.session_id = session_id
        self.grace = grace_seconds
        self.clock = clock or time.monotonic
        self.unavailable_since = None
        self.failures = self.recoveries = 0
        self.last_failure = ""

    def poll(self, file):
        try:
            value = read_json(file)
            if (not isinstance(value, dict) or value.get("sessionId") != self.session_id
                    or type(value.get("closed")) is not bool):
                raise ValueError("Missing or mismatched session manifest")
        except (OSError, ValueError) as exc:
            self.failures += 1
            self.last_failure = type(exc).__name__
            if self.unavailable_since is None:
                self.unavailable_since = self.clock()
            if self.clock() - self.unavailable_since >= self.grace:
                return "manifest_unavailable_safety_stop"
            return None
        if self.unavailable_since is not None:
            self.recoveries += 1
        self.unavailable_since = None
        return "session_manifest_closed" if value["closed"] else None

    def summary(self):
        return {"unavailableReads": self.failures, "recoveries": self.recoveries,
                "lastFailureType": self.last_failure, "graceSeconds": self.grace,
                "currentlyUnavailable": self.unavailable_since is not None,
                "policy": "Continue during brief metadata contention; explicit stop, Unity exit, encoder exit and duration cap remain active"}


def publish_status(file, metadata, final=False):
    """Status-file contention must never prevent FFmpeg child cleanup."""
    try:
        atomic_json(file, metadata)
        return True
    except (OSError, ValueError) as exc:
        metadata["statusPublishFailures"] = metadata.get("statusPublishFailures", 0) + 1
        metadata["lastStatusPublishError"] = type(exc).__name__
        if final:
            metadata["status"] = "partial_or_failed"
            metadata["errors"].append("Final capture-status publication failed; recovery metadata retained: " + type(exc).__name__)
            # Only after children have stopped: never leave an encoder running
            # because its ordinary status file was held open by another reader.
            atomic_json(file.with_name("media_capture.finalization.json"), metadata)
        return False


def build_commands(config, folder, synthetic=False):
    ffmpeg = Path(config.get("mediaFfmpegExecutable", ""))
    if not ffmpeg.is_file():
        raise ValueError("Configured FFmpeg executable is missing")
    commands = {}
    prefix = [str(ffmpeg), "-hide_banner", "-loglevel", "warning", "-nostats", "-n"]
    if config.get("mediaRecordVideo"):
        title = config.get("mediaWindowTitle", "")
        source = config.get("mediaVideoSource", "window")
        if source not in ("window", "desktop"):
            raise ValueError("Unsupported video capture source")
        if source == "desktop" and not config.get("mediaAllowDesktopRecording"):
            raise ValueError("Desktop capture was not explicitly enabled")
        if source == "window" and (not title or any(x in title for x in ("\r", "\n"))):
            raise ValueError("A specific Quest Mirror window title is required")
        width, height = int(config.get("mediaVideoWidth", 1280)), int(config.get("mediaVideoHeight", 720))
        if not 64 <= width <= 7680 or not 64 <= height <= 4320 or width % 2 or height % 2:
            raise ValueError("Invalid even video dimensions")
        video_input = ["-thread_queue_size", "512", "-f", "gdigrab", "-framerate", "30", "-draw_mouse", "1"]
        if source == "desktop":
            video_input += ["-offset_x", str(int(config.get("mediaScreenX", 0))), "-offset_y", str(int(config.get("mediaScreenY", 0))),
                            "-video_size", f"{width}x{height}", "-i", "desktop"]
        else:
            video_input += ["-i", "title=" + title]
        if synthetic:
            video_input = ["-re", "-f", "lavfi", "-i", "testsrc2=size=640x360:rate=30"]
        commands["video"] = prefix + video_input + ["-vf", f"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2",
            "-an", "-c:v", "libx264", "-preset", "ultrafast", "-crf", "23", "-pix_fmt", "yuv420p",
            str(folder / "Video/quest-view-recovery.mkv")]
    if config.get("mediaRecordMicrophone"):
        microphone = config.get("mediaMicrophoneDevice", "")
        if not microphone or any(x in microphone for x in ("\r", "\n")):
            raise ValueError("Select a specific microphone before recording")
        audio_input = ["-re", "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000"] if synthetic else [
            "-thread_queue_size", "512", "-f", "dshow", "-i", "audio=" + microphone]
        commands["audio"] = prefix + audio_input + ["-vn", "-ar", "48000", "-ac", "1", "-c:a", "pcm_s16le", str(folder / "Audio/think-aloud.wav")]
    if not commands:
        raise ValueError("No recording channels selected")
    return commands


def audio_metrics(file):
    with wave.open(str(file), "rb") as sound:
        if sound.getsampwidth() != 2 or sound.getnframes() == 0:
            raise ValueError("WAV has no PCM16 audio frames")
        result = {"durationSeconds": sound.getnframes() / sound.getframerate(), "sampleRate": sound.getframerate(),
                  "channels": sound.getnchannels(), "frames": sound.getnframes()}
        squares = peak = count = 0
        while True:
            data = sound.readframes(48000)
            if not data:
                break
            samples = array.array("h", data)
            for value in samples[::16]:
                squares += value * value
                peak = max(peak, abs(value))
                count += 1
        result["sampledRms"] = math.sqrt(squares / max(1, count)) / 32768
        result["sampledPeak"] = peak / 32768
        result["signalDetected"] = result["sampledRms"] > 0.00001
        result["signalAssessment"] = "nonzero_audio_energy_only_NOT_verified_speech_or_intelligibility"
        return result


def probe(ffprobe, file):
    completed = subprocess.run([str(ffprobe), "-v", "error", "-show_streams", "-show_format", "-of", "json", str(file)],
                               capture_output=True, timeout=20, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    if completed.returncode:
        raise ValueError("Media probe failed for " + file.name)
    data = json.loads(completed.stdout)
    duration = float(data.get("format", {}).get("duration", 0))
    if duration <= 0:
        raise ValueError("Media has no positive duration")
    return {"durationSeconds": duration, "streams": [{k: s.get(k) for k in ("codec_type", "codec_name", "width", "height", "sample_rate", "channels")} for s in data.get("streams", [])]}


def run(folder, config, synthetic=False):
    folder = Path(folder).resolve()
    root = Path(config.get("outputRoot", "")).resolve()
    if not folder.is_relative_to(root):
        raise ValueError("Capture folder escapes configured archive root")
    manifest = read_json(folder / "export_manifest.json", {})
    request = read_json(folder / "media-start-request.json", {})
    if manifest.get("closed") or manifest.get("sessionId") != folder.name:
        raise ValueError("Capture requires a matching open session")
    if synthetic and manifest.get("participantId") != "synthetic-media-test":
        raise ValueError("Synthetic mode requires an explicitly synthetic fixture")
    if not synthetic and (not request.get("confirmedByOperator") or not request.get("unityProcessId")):
        raise ValueError("Operator confirmation and Unity process ID are required")
    if not synthetic and not config.get("mediaRecordingEnabled"):
        raise ValueError("Media recording is disabled")
    walk = folder
    while walk != root.parent:
        if walk.is_symlink() or (hasattr(walk, "is_junction") and walk.is_junction()):
            raise ValueError("Linked capture folders are not allowed")
        if walk == root:
            break
        walk = walk.parent
    (folder / "Audio").mkdir(exist_ok=True)
    (folder / "Video").mkdir(exist_ok=True)
    status_file = folder / "media_capture.json"
    commands = build_commands(config, folder, synthetic)
    # A persistent marker prevents a second agent overwriting any part of this session.
    with (folder / "media-capture.lock").open("x", encoding="ascii") as marker:
        marker.write(str(os.getpid()))
    metadata = {"schemaVersion": "1", "sessionId": folder.name, "status": "starting", "synthetic": synthetic,
                "requestedVideo": bool(config.get("mediaRecordVideo")), "requestedAudio": bool(config.get("mediaRecordMicrophone")),
                "microphone": config.get("mediaMicrophoneDevice", ""), "windowTitle": config.get("mediaWindowTitle", ""),
                "videoSource": config.get("mediaVideoSource", "window"), "videoWidth": config.get("mediaVideoWidth", 1280), "videoHeight": config.get("mediaVideoHeight", 720),
                "captureScope": "entire_configured_desktop_rectangle_including_visible_windows" if config.get("mediaVideoSource") == "desktop" else "named_window_only",
                "desktopRectangle": {"x": int(config.get("mediaScreenX", 0)), "y": int(config.get("mediaScreenY", 0)),
                                     "width": int(config.get("mediaVideoWidth", 1280)), "height": int(config.get("mediaVideoHeight", 720))} if config.get("mediaVideoSource") == "desktop" else None,
                "timestampMethod": "process_launch_UTC_estimate_NOT_first_frame_or_sample_hardware_sync",
                "sessionStartUtcMsEstimate": request.get("requestUtcMs", now_ms()) - request.get("sessionSeconds", 0) * 1000,
                "startedUtcMs": now_ms(), "channels": {}, "errors": [], "warnings": []}
    atomic_json(status_file, metadata)
    processes, logs = {}, []
    started = time.monotonic()
    limit = min(max(float(config.get("mediaMaxMinutes", 90)), 1), 240) * 60
    stop_reason = "unknown"
    manifest_watch = ManifestReadWatch(folder.name)
    last_status_publish = time.monotonic()
    try:
        for channel, command in commands.items():
            log = (folder / ("Video" if channel == "video" else "Audio") / (channel + "-capture.log")).open("xb")
            logs.append(log)
            launched = now_ms()
            process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=log,
                                       creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
            processes[channel] = process
            metadata["channels"][channel] = {"pid": process.pid, "launchUtcMs": launched,
                "estimatedSessionStartSeconds": (launched - metadata["sessionStartUtcMsEstimate"]) / 1000}
        metadata["status"] = "recording"
        publish_status(status_file, metadata)
        while True:
            if (folder / "media-stop.request").exists():
                stop_reason = "operator_or_Unity_stop"
                break
            reason = manifest_watch.poll(folder / "export_manifest.json")
            metadata["manifestReadDiagnostics"] = manifest_watch.summary()
            if reason:
                stop_reason = reason
                if reason == "manifest_unavailable_safety_stop":
                    metadata["errors"].append("Session manifest remained unavailable/invalid for 30 seconds; safety stop")
                break
            if not synthetic and not parent_alive(int(request["unityProcessId"])):
                stop_reason = "Unity_process_exited_or_unavailable"
                break
            if time.monotonic() - started >= (2.5 if synthetic else limit):
                stop_reason = "synthetic_test_end" if synthetic else "maximum_duration_safety_stop"
                break
            failed = [name for name, process in processes.items() if process.poll() is not None]
            if failed:
                metadata["errors"].append("Capture process ended unexpectedly: " + ", ".join(failed))
                stop_reason = "capture_process_exited"
                break
            if time.monotonic() - last_status_publish >= 2:
                metadata["captureElapsedSeconds"] = time.monotonic() - started
                publish_status(status_file, metadata)
                last_status_publish = time.monotonic()
            time.sleep(0.2)
    except Exception as exc:
        metadata["errors"].append(type(exc).__name__ + ": " + str(exc))
        stop_reason = "agent_error"
    finally:
        metadata["status"] = "finalizing"
        metadata["manifestReadDiagnostics"] = manifest_watch.summary()
        publish_status(status_file, metadata)
        # Send stop to both channels before waiting, to minimize stop-time skew.
        for process in processes.values():
            if process.poll() is None:
                try:
                    process.stdin.write(b"q\n")
                    process.stdin.flush()
                except (OSError, ValueError):
                    pass
        for name, process in processes.items():
            try:
                process.wait(timeout=8)
            except subprocess.TimeoutExpired:
                process.kill()  # Only the exact FFmpeg child created above.
                process.wait(timeout=3)
                metadata["errors"].append(name + " did not stop gracefully; recovery file retained")
            metadata["channels"][name]["exitCode"] = process.returncode
            if process.returncode:
                metadata["errors"].append(name + " encoder returned " + str(process.returncode))
            process.stdin.close()
        for log in logs:
            log.close()
    metadata["stopReason"] = stop_reason
    metadata["captureStoppedUtcMs"] = now_ms()
    wav = folder / "Audio/think-aloud.wav"
    mkv = folder / "Video/quest-view-recovery.mkv"
    mp4 = folder / "Video/quest-view.mp4"
    ffmpeg = Path(config["mediaFfmpegExecutable"])
    ffprobe = ffmpeg.with_name("ffprobe.exe")
    if config.get("mediaRecordMicrophone"):
        try:
            metadata["audio"] = audio_metrics(wav)
            if not metadata["audio"]["signalDetected"]:
                metadata["warnings"].append("Audio is silent or extremely quiet; listen before accepting the recording")
        except (OSError, ValueError, wave.Error) as exc:
            metadata["errors"].append("Audio validation: " + str(exc))
    if config.get("mediaRecordVideo") and mkv.exists():
        try:
            arguments = [str(ffmpeg), "-hide_banner", "-loglevel", "error", "-n", "-i", str(mkv)]
            include_audio = "audio" in metadata
            if include_audio:
                offset = (metadata["channels"]["audio"]["launchUtcMs"] - metadata["channels"]["video"]["launchUtcMs"]) / 1000
                arguments += ["-itsoffset", str(offset), "-i", str(wav), "-map", "0:v:0", "-map", "1:a:0", "-c:a", "aac", "-b:a", "128k"]
            arguments += ["-c:v", "copy", "-movflags", "+faststart", str(mp4)]
            result = subprocess.run(arguments, capture_output=True, timeout=120, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
            if result.returncode:
                raise ValueError("MP4 finalization failed; MKV recovery file retained")
            metadata["video"] = probe(ffprobe, mp4)
            metadata["video"]["includesMicrophoneTrack"] = include_audio
        except (OSError, ValueError, subprocess.TimeoutExpired) as exc:
            metadata["errors"].append("Video finalization: " + str(exc))
    elif config.get("mediaRecordVideo"):
        metadata["errors"].append("No video recovery file was written")
    metadata["status"] = "partial_or_failed" if metadata["errors"] else "finalized"
    metadata["finalizedUtcMs"] = now_ms()
    metadata["playbackVerification"] = "Container checked; operator must verify correct VR scene and intelligible speech"
    publish_status(status_file, metadata, final=True)
    return metadata


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--session", required=True)
    parser.add_argument("--config", required=True)
    parser.add_argument("--synthetic-test", action="store_true")
    args = parser.parse_args()
    run(args.session, read_json(Path(args.config), {}), args.synthetic_test)


if __name__ == "__main__":
    main()
