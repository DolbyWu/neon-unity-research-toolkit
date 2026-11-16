"""No microphone or desktop is opened: integration uses only generated bars and sine audio."""
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import study_capture
from study_archive import atomic_json
from study_capture import build_commands, run, ManifestReadWatch, publish_status


class CaptureTests(unittest.TestCase):
    def test_manifest_read_contention_recovers_without_stopping(self):
        watch = ManifestReadWatch("test-session")
        with patch("study_capture.read_json", side_effect=[PermissionError("sharing"),
                {"sessionId": "test-session", "closed": False},
                {"sessionId": "test-session", "closed": True}]):
            self.assertIsNone(watch.poll(Path("synthetic.json")))
            self.assertIsNone(watch.poll(Path("synthetic.json")))
            self.assertEqual(watch.poll(Path("synthetic.json")), "session_manifest_closed")
        self.assertEqual(watch.failures, 1)
        self.assertEqual(watch.recoveries, 1)

    def test_missing_manifest_cannot_extend_capture_indefinitely(self):
        current = [0.0]
        watch = ManifestReadWatch("test-session", clock=lambda: current[0])
        with patch("study_capture.read_json", side_effect=PermissionError("sharing")):
            self.assertIsNone(watch.poll(Path("synthetic.json")))
            current[0] = 29.9
            self.assertIsNone(watch.poll(Path("synthetic.json")))
            current[0] = 30.0
            self.assertEqual(watch.poll(Path("synthetic.json")), "manifest_unavailable_safety_stop")

    def test_wrong_session_closed_flag_is_not_a_valid_stop_signal(self):
        with patch("study_capture.read_json", return_value={"sessionId": "other", "closed": True}):
            watch = ManifestReadWatch("test-session")
            self.assertIsNone(watch.poll(Path("synthetic.json")))
            self.assertEqual(watch.failures, 1)

    def setUp(self):
        self.base = Path(__file__).resolve().parents[4] / "Artifacts/python-media-unit"
        self.base.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="capture-synthetic-", dir=self.base)
        self.root = Path(self.temp.name)
        self.ffmpeg = os.environ.get("STUDY_FFMPEG", "")
        self.config = {"outputRoot": str(self.root), "mediaFfmpegExecutable": self.ffmpeg,
            "mediaRecordVideo": True, "mediaRecordMicrophone": True,
            "mediaMicrophoneDevice": "Synthetic device; never opened in these tests",
            "mediaWindowTitle": "Oculus Mirror", "mediaVideoSource": "desktop", "mediaAllowDesktopRecording": True,
            "mediaVideoWidth": 2560, "mediaVideoHeight": 1440}

    def tearDown(self):
        assert Path(self.temp.name).resolve().is_relative_to(self.base.resolve())
        self.temp.cleanup()

    def test_desktop_requires_explicit_configuration_and_never_uses_shell(self):
        if not self.ffmpeg:
            self.skipTest("Set STUDY_FFMPEG")
        commands = build_commands(self.config, self.root)
        self.assertIn("desktop", commands["video"])
        self.assertIn("2560x1440", commands["video"])
        self.assertFalse(any(value.startswith("title=") for value in commands["video"]))
        self.assertEqual(commands["video"][commands["video"].index("-offset_x") + 1], "0")
        self.assertEqual(commands["video"][commands["video"].index("-offset_y") + 1], "0")
        self.assertIn("-n", commands["video"])
        self.assertNotIn("-y", commands["video"])
        self.config["mediaAllowDesktopRecording"] = False
        with self.assertRaises(ValueError):
            build_commands(self.config, self.root)

    def test_missing_microphone_and_invalid_dimensions_are_not_silent_fallbacks(self):
        if not self.ffmpeg:
            self.skipTest("Set STUDY_FFMPEG")
        self.config["mediaMicrophoneDevice"] = ""
        with self.assertRaises(ValueError):
            build_commands(self.config, self.root)
        self.config["mediaMicrophoneDevice"] = "Synthetic"
        self.config["mediaVideoWidth"] = 1001
        with self.assertRaises(ValueError):
            build_commands(self.config, self.root)

    def test_synthetic_full_resolution_video_audio_stop_and_mp4_finalization(self):
        if not self.ffmpeg:
            self.skipTest("Set STUDY_FFMPEG")
        folder = self.root / "20260101T120000000Z-1234abcd"
        atomic_json(folder / "export_manifest.json", {"sessionId": folder.name, "participantId": "synthetic-media-test", "closed": False})
        result = run(folder, self.config, synthetic=True)
        self.assertEqual(result["status"], "finalized", result)
        self.assertEqual(result["errors"], [])
        self.assertTrue(result["audio"]["signalDetected"])
        self.assertGreater(result["audio"]["durationSeconds"], 0)
        self.assertTrue(result["video"]["includesMicrophoneTrack"])
        video = next(s for s in result["video"]["streams"] if s["codec_type"] == "video")
        self.assertEqual((video["width"], video["height"]), (2560, 1440))
        self.assertEqual(result["captureScope"], "entire_configured_desktop_rectangle_including_visible_windows")
        self.assertEqual(result["desktopRectangle"], {"x": 0, "y": 0, "width": 2560, "height": 1440})
        self.assertIn("NOT_first_frame", result["timestampMethod"])
        self.assertTrue((folder / "Video/quest-view.mp4").is_file())
        self.assertTrue((folder / "Audio/think-aloud.wav").is_file())
        with self.assertRaises(FileExistsError):
            run(folder, self.config, synthetic=True)

    def test_synthetic_capture_survives_manifest_and_finalizing_status_contention(self):
        if not self.ffmpeg:
            self.skipTest("Set STUDY_FFMPEG")
        folder = self.root / "20260101T120000000Z-1234abcd"
        atomic_json(folder / "export_manifest.json", {"sessionId": folder.name,
            "participantId": "synthetic-media-test", "closed": False})
        real_read, real_write = study_capture.read_json, study_capture.atomic_json
        reads = [0]
        def flaky_read(file, default=None):
            if Path(file).name == "export_manifest.json":
                reads[0] += 1
                if reads[0] in (2, 3, 5):
                    raise PermissionError("Synthetic Unity sharing conflict")
            return real_read(file, default)
        def flaky_write(file, value):
            if value.get("status") == "finalizing":
                raise PermissionError("Synthetic status reader lock")
            return real_write(file, value)
        config = dict(self.config, mediaRecordVideo=False)
        with patch("study_capture.read_json", side_effect=flaky_read), patch("study_capture.atomic_json", side_effect=flaky_write):
            result = run(folder, config, synthetic=True)
        self.assertEqual(result["stopReason"], "synthetic_test_end")
        self.assertEqual(result["status"], "finalized", result)
        self.assertEqual(result["manifestReadDiagnostics"]["unavailableReads"], 3)
        self.assertGreaterEqual(result["manifestReadDiagnostics"]["recoveries"], 1)
        self.assertEqual(result["statusPublishFailures"], 1)
        self.assertEqual(result["channels"]["audio"]["exitCode"], 0)
        self.assertGreater(result["audio"]["durationSeconds"], 2)

    def test_final_metadata_falls_back_only_after_capture_is_done(self):
        from study_archive import read_media_capture
        file = self.root / "media_capture.json"
        real_write = study_capture.atomic_json
        def locked_primary(target, value):
            if target == file:
                raise PermissionError("Synthetic permanent primary lock")
            return real_write(target, value)
        metadata = {"sessionId": "synthetic-session", "status": "finalized", "finalizedUtcMs": 100, "errors": []}
        with patch("study_capture.atomic_json", side_effect=locked_primary):
            self.assertFalse(publish_status(file, metadata, final=True))
        recovered = read_media_capture(self.root, "synthetic-session")
        self.assertEqual(recovered["status"], "partial_or_failed")
        self.assertEqual(recovered["metadataRecoveryFile"], "media_capture.finalization.json")


if __name__ == "__main__":
    unittest.main()
