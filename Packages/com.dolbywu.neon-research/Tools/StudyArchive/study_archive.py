"""Private, loopback-only study archive. SQLite is an index; raw session files stay intact."""
import argparse
from contextlib import contextmanager
import datetime as dt
import hashlib
import http.cookies
import json
import os
from pathlib import Path
import re
import secrets
import sqlite3
import threading
import time
import urllib.parse
import urllib.request
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

PARTICIPANT = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_-]{0,47}$")
SESSION = re.compile(r"^\d{8}T\d{9}Z-[a-f0-9]{8}$")


def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def read_json(path, default=None):
    path = Path(path)
    for attempt in range(5):
        try:
            return json.loads(path.read_text(encoding="utf-8-sig"))
        except FileNotFoundError:
            return default
        except (OSError, json.JSONDecodeError):
            if attempt == 4:
                raise
            # Unity replaces this file while publishing. A brief Windows sharing
            # conflict or partial legacy write is not immediately a fatal read.
            time.sleep(0.01 * 2 ** attempt)


def atomic_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + "." + secrets.token_hex(4) + ".tmp")
    try:
        with temporary.open("x", encoding="utf-8") as stream:
            json.dump(value, stream, ensure_ascii=False, indent=2, allow_nan=False)
            stream.flush()
            os.fsync(stream.fileno())
        for attempt in range(5):
            try:
                os.replace(temporary, path)
                break
            except PermissionError:
                if attempt == 4:
                    raise
                time.sleep(0.01 * 2 ** attempt)
    finally:
        temporary.unlink(missing_ok=True)


def read_media_capture(directory, sid, default=None):
    primary = read_json(directory / "media_capture.json", default or {})
    recovery = read_json(directory / "media_capture.finalization.json", {})
    if not isinstance(primary, dict) or not isinstance(recovery, dict):
        raise ValueError("Invalid capture metadata")
    if (recovery.get("sessionId") == sid and recovery.get("status") in ("finalized", "partial_or_failed")
            and isinstance(recovery.get("finalizedUtcMs"), (int, float))
            and recovery["finalizedUtcMs"] >= primary.get("finalizedUtcMs", 0)):
        return dict(recovery, metadataRecoveryFile="media_capture.finalization.json")
    return primary


class Archive:
    def __init__(self, root, config=None):
        self.root = Path(root).resolve()
        self.config = Path(config).resolve() if config else None
        self.lock = threading.RLock()
        self.root.mkdir(parents=True, exist_ok=True)
        (self.root / "Participants").mkdir(exist_ok=True)
        (self.root / "ReferenceTests").mkdir(exist_ok=True)
        self.database = self.root / "archive.sqlite"
        with self.connect() as db:
            db.executescript("""
                CREATE TABLE IF NOT EXISTS participants(id TEXT PRIMARY KEY, profile TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS sessions(id TEXT PRIMARY KEY, relative_path TEXT UNIQUE NOT NULL,
                    participant TEXT NOT NULL, kind TEXT NOT NULL, summary TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS notes(session TEXT PRIMARY KEY, version INTEGER NOT NULL, body TEXT NOT NULL);
            """)
        self.refresh()

    @contextmanager
    def connect(self):
        db = sqlite3.connect(self.database, timeout=5)
        db.row_factory = sqlite3.Row
        try:
            with db:
                yield db
        finally:
            db.close()

    def safe(self, candidate):
        candidate = Path(candidate)
        resolved = candidate.resolve()
        if not resolved.is_relative_to(self.root):
            raise ValueError("Path escapes archive")
        # Do not follow junctions/symlinks to another data collection.
        walk = candidate
        while walk != self.root and walk.is_relative_to(self.root):
            if walk.is_symlink() or (hasattr(walk, "is_junction") and walk.is_junction()):
                raise ValueError("Linked archive paths are not supported")
            walk = walk.parent
        return resolved

    def refresh(self):
        errors = []
        with self.lock, self.connect() as db:
            for profile_file in (self.root / "Participants").glob("*/participant.json"):
                try:
                    profile_file = self.safe(profile_file)
                    profile = read_json(profile_file)
                    pid = profile_file.parent.name
                    if not PARTICIPANT.fullmatch(pid) or profile.get("participantId") != pid:
                        raise ValueError("Participant ID mismatch")
                    db.execute("INSERT OR REPLACE INTO participants VALUES (?,?)", (pid, json.dumps(profile, ensure_ascii=False)))
                except (ValueError, OSError) as exc:
                    errors.append(f"{profile_file.name}: {exc}")
            candidates = list((self.root / "Participants").glob("*/Sessions/*/export_manifest.json"))
            candidates += list((self.root / "ReferenceTests").glob("*/export_manifest.json"))
            for manifest_file in candidates:
                try:
                    manifest_file = self.safe(manifest_file)
                    folder = manifest_file.parent
                    sid = folder.name
                    if not SESSION.fullmatch(sid):
                        raise ValueError("Invalid session directory")
                    manifest = read_json(manifest_file)
                    if manifest.get("sessionId") != sid:
                        raise ValueError("Session ID mismatch")
                    reference = folder.parent.name == "ReferenceTests"
                    pid = "technical-reference" if reference else folder.parent.parent.name
                    if not reference and manifest.get("participantId") != pid:
                        raise ValueError("Manifest participant differs from parent folder")
                    neon = [read_json(self.safe(p / "session.json"), {}) for p in folder.glob("Neon-*") if p.is_dir()]
                    summary = dict(manifest)
                    self.safe(folder / "media_capture.json")
                    self.safe(folder / "media_capture.finalization.json")
                    summary.update({"neon": neon, "replayExists": (folder / "Replay/study-replay.html").is_file(),
                                    "folder": str(folder), "kind": "reference" if reference else "participant",
                                    "mediaCapture": read_media_capture(folder, sid)})
                    existing = db.execute("SELECT relative_path FROM sessions WHERE id=?", (sid,)).fetchone()
                    relative = str(folder.relative_to(self.root))
                    if existing and existing[0] != relative:
                        raise ValueError("Duplicate session ID at a different location")
                    db.execute("INSERT OR REPLACE INTO sessions VALUES (?,?,?,?,?)", (sid, relative, pid,
                               summary["kind"], json.dumps(summary, ensure_ascii=False)))
                    notes = read_json(folder / "interview_observations.json")
                    if notes:
                        db.execute("INSERT OR REPLACE INTO notes VALUES (?,?,?)", (sid, notes["version"], json.dumps(notes, ensure_ascii=False)))
                except (ValueError, OSError, KeyError) as exc:
                    errors.append(f"{manifest_file.parent.name}: {exc}")
        self.scan_errors = errors

    def listing(self):
        self.refresh()
        with self.connect() as db:
            participants = [json.loads(r[0]) for r in db.execute("SELECT profile FROM participants ORDER BY id")]
            sessions = [dict(json.loads(r["summary"]), archiveParticipantId=r["participant"])
                        for r in db.execute("SELECT * FROM sessions ORDER BY id DESC")]
        config = read_json(self.config, {}) if self.config else {}
        return {"root": str(self.root), "participants": participants, "sessions": sessions,
                "selectedParticipant": config.get("participantId", ""), "scanErrors": self.scan_errors,
                "recordingSetup": {"enabled": bool(config.get("mediaRecordingEnabled")),
                    "video": bool(config.get("mediaRecordVideo")), "audio": bool(config.get("mediaRecordMicrophone")),
                    "microphone": config.get("mediaMicrophoneDevice", ""), "window": config.get("mediaWindowTitle", ""),
                    "source": config.get("mediaVideoSource", "window"), "width": config.get("mediaVideoWidth", 1280), "height": config.get("mediaVideoHeight", 720),
                    "maxMinutes": config.get("mediaMaxMinutes", 90)}}

    def media_status(self, sid):
        directory = self.session_folder(sid)
        self.safe(directory / "media_capture.json")
        self.safe(directory / "media_capture.finalization.json")
        result = read_media_capture(directory, sid, {"status": "launch_requested_no_status" if (directory / "media-start-request.json").exists() else "not_recorded"})
        result["videoAvailable"] = (directory / "Video/quest-view.mp4").is_file() and result["status"] not in ("starting", "recording", "finalizing")
        result["audioAvailable"] = (directory / "Audio/think-aloud.wav").is_file() and result["status"] not in ("starting", "recording", "finalizing")
        return result

    def stop_media(self, sid):
        directory = self.session_folder(sid)
        status = self.media_status(sid)
        if status["status"] in ("starting", "recording"):
            atomic_json(directory / "media-stop.request", {"requestedUtc": utc(), "source": "archive_stop_button"})
        return {"message": "Media stop requested. Wait for finalization; raw recordings are retained."}

    def participant(self, pid):
        if not isinstance(pid, str) or not PARTICIPANT.fullmatch(pid):
            raise ValueError("Use an anonymous ID such as P001; letters, numbers, _ and - only")
        return self.safe(self.root / "Participants" / pid)

    def save_participant(self, data):
        pid = data.get("participantId", "")
        directory = self.participant(pid)
        background = data.get("background", "")
        if not isinstance(background, str) or len(background) > 50000:
            raise ValueError("Background notes too long")
        with self.lock:
            file = self.safe(directory / "participant.json")
            current = read_json(file, {})
            if current and not data.get("update", False):
                raise ValueError("Participant already exists; select it or explicitly save edited background")
            profile = dict(current)  # Preserve structured details collected by Unity's pre-Play form.
            profile.update({"participantId": pid, "createdUtc": current.get("createdUtc", utc()),
                       "updatedUtc": utc(), "background": background,
                       "privacyNote": "Anonymous ID. Voluntary self-report only; store contact/consent identity mapping separately."})
            atomic_json(file, profile)
            (directory / "Sessions").mkdir(exist_ok=True)
            self.refresh()
        return profile

    def select_participant(self, pid):
        directory = self.participant(pid)
        if not (directory / "participant.json").is_file():
            raise ValueError("Create participant first")
        if not self.config:
            raise ValueError("Unity configuration not attached to this archive service")
        with self.lock:
            self.refresh()
            # Never silently change the identity of a running recording.
            with self.connect() as db:
                summaries = [json.loads(r[0]) for r in db.execute("SELECT summary FROM sessions WHERE kind='participant'")]
            if any(not s.get("closed", False) for s in summaries):
                raise ValueError("A session is still marked open. Stop/finalize it before switching participant; do not fake its closed flag.")
            config = read_json(self.config, {})
            if Path(config.get("outputRoot", "")).resolve() != self.root:
                raise ValueError("Unity output root does not match this archive")
            config["participantId"] = pid
            atomic_json(self.config, config)
        return {"selectedParticipant": pid, "message": "Selected for NEXT Play; stop any existing Play first."}

    def session_folder(self, sid):
        if not isinstance(sid, str) or not SESSION.fullmatch(sid):
            raise ValueError("Invalid session ID")
        with self.connect() as db:
            record = db.execute("SELECT relative_path FROM sessions WHERE id=?", (sid,)).fetchone()
        if not record:
            raise ValueError("Session not found")
        directory = self.safe(self.root / record[0])
        if not directory.is_dir():
            raise ValueError("Session directory is missing")
        return directory

    def notes(self, sid):
        directory = self.session_folder(sid)
        return read_json(self.safe(directory / "interview_observations.json"), {"sessionId": sid, "version": 0,
            "interview": "", "observations": "", "updatedUtc": ""})

    def save_notes(self, data):
        sid = data.get("sessionId")
        with self.lock:
            directory = self.session_folder(sid)
            current = self.notes(sid)
            if data.get("version") != current["version"]:
                raise ValueError("Notes changed in another window. Reload before saving; your text has not been overwritten.")
            interview, observations = data.get("interview", ""), data.get("observations", "")
            if any(not isinstance(x, str) or len(x) > 200000 for x in (interview, observations)):
                raise ValueError("Notes must be text, at most 200000 characters per field")
            result = {"sessionId": sid, "version": current["version"] + 1, "interview": interview,
                      "observations": observations, "updatedUtc": utc()}
            atomic_json(directory / "interview_observations.json", result)
            with self.connect() as db:
                db.execute("INSERT OR REPLACE INTO notes VALUES (?,?,?)", (sid, result["version"], json.dumps(result, ensure_ascii=False)))
            return result


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass  # Do not copy identifiers or interview text into access logs.

    def host_ok(self):
        return self.headers.get("Host") == f"127.0.0.1:{self.server.server_port}"

    def authorized(self):
        cookie = http.cookies.SimpleCookie()
        try:
            cookie.load(self.headers.get("Cookie", ""))
            value = cookie.get("study_archive")
            return bool(value) and secrets.compare_digest(value.value, self.server.key)
        except http.cookies.CookieError:
            return False

    def send(self, code, value, content_type="application/json; charset=utf-8", extra=None):
        body = json.dumps(value, ensure_ascii=False).encode() if content_type.startswith("application/json") else value
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("X-Frame-Options", "DENY")
        for key, val in (extra or {}).items():
            self.send_header(key, val)
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if not self.host_ok():
            return self.send(403, {"error": "Invalid Host"})
        url = urllib.parse.urlparse(self.path)
        query = urllib.parse.parse_qs(url.query)
        if url.path == "/health":
            supplied = self.headers.get("X-Archive-Key", "")
            if not secrets.compare_digest(supplied, self.server.key):
                return self.send(403, {"error": "Unauthorized"})
            return self.send(200, {"service": "neon-study-archive-v1"})
        if url.path == "/" and "launch" in query:
            if not secrets.compare_digest(query["launch"][0], self.server.key):
                return self.send(403, {"error": "Invalid launch key"})
            sid = query.get("session", [""])[0]
            target = "/" + ("?session=" + urllib.parse.quote(sid) if SESSION.fullmatch(sid) else "")
            return self.send(303, b"", "text/plain", {"Location": target,
                "Set-Cookie": f"study_archive={self.server.key}; HttpOnly; SameSite=Strict; Path=/"})
        if url.path == "/":
            return self.send(200, (Path(__file__).parent / "archive.html").read_bytes(), "text/html; charset=utf-8",
                {"Content-Security-Policy": "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; frame-src 'self'; media-src 'self'; img-src data:; base-uri 'none'; form-action 'none'"})
        if not self.authorized():
            return self.send(403, {"error": "Open the archive through its local launcher first"})
        try:
            if url.path == "/api/library":
                return self.send(200, self.server.archive.listing())
            if url.path == "/api/notes":
                return self.send(200, self.server.archive.notes(query.get("session", [""])[0]))
            if url.path == "/api/media":
                return self.send(200, self.server.archive.media_status(query.get("session", [""])[0]))
            if url.path.startswith("/media/"):
                parts = url.path.split("/")
                if len(parts) != 4 or parts[3] not in ("video", "audio"):
                    raise ValueError("Invalid media resource")
                folder = self.server.archive.session_folder(parts[2])
                relative, mime = ("Video/quest-view.mp4", "video/mp4") if parts[3] == "video" else ("Audio/think-aloud.wav", "audio/wav")
                return self.send_file(self.server.archive.safe(folder / relative), mime)
            if url.path.startswith("/replay/"):
                folder = self.server.archive.session_folder(url.path.removeprefix("/replay/"))
                replay = self.server.archive.safe(folder / "Replay/study-replay.html")
                if not read_json(folder / "export_manifest.json", {}).get("closed"):
                    raise ValueError("Wait for the recording to close")
                if not replay.is_file():
                    raise ValueError("Replay has not been generated; raw files are retained")
                return self.send(200, replay.read_bytes(), "text/html; charset=utf-8")
            return self.send(404, {"error": "Not found"})
        except (ValueError, OSError, sqlite3.Error) as exc:
            return self.send(400, {"error": str(exc)})

    def do_POST(self):
        expected_origin = f"http://127.0.0.1:{self.server.server_port}"
        if not self.host_ok() or not self.authorized() or self.headers.get("Origin") != expected_origin or self.headers.get("X-Study-Archive") != "1":
            return self.send(403, {"error": "Unauthorized or cross-origin request"})
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if length < 1 or length > 1000000:
                raise ValueError("Invalid request size")
            data = json.loads(self.rfile.read(length))
            if not isinstance(data, dict):
                raise ValueError("Expected an object")
            if self.path == "/api/participant":
                return self.send(200, self.server.archive.save_participant(data))
            if self.path == "/api/select":
                return self.send(200, self.server.archive.select_participant(data.get("participantId")))
            if self.path == "/api/notes":
                return self.send(200, self.server.archive.save_notes(data))
            if self.path == "/api/media/stop":
                return self.send(200, self.server.archive.stop_media(data.get("sessionId")))
            if self.path == "/api/open-folder":
                directory = self.server.archive.session_folder(data.get("sessionId"))
                if os.name == "nt":
                    os.startfile(str(directory))
                return self.send(200, {"folder": str(directory)})
            return self.send(404, {"error": "Not found"})
        except (ValueError, OSError, sqlite3.Error) as exc:
            return self.send(400, {"error": str(exc)})

    def send_file(self, file, mime):
        size = file.stat().st_size
        start, end = 0, size - 1
        requested = self.headers.get("Range")
        if requested:
            match = re.fullmatch(r"bytes=(\d+)-(\d*)", requested)
            if not match:
                return self.send(416, {"error": "Unsupported range"})
            start = int(match[1])
            end = min(int(match[2]) if match[2] else end, size - 1)
            if start > end or start >= size:
                return self.send(416, {"error": "Range outside file"})
        self.send_response(206 if requested else 200)
        self.send_header("Content-Type", mime)
        self.send_header("Content-Length", str(end - start + 1))
        self.send_header("Accept-Ranges", "bytes")
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        if requested:
            self.send_header("Content-Range", f"bytes {start}-{end}/{size}")
        self.end_headers()
        with file.open("rb") as stream:
            stream.seek(start)
            remaining = end - start + 1
            while remaining:
                block = stream.read(min(1024 * 1024, remaining))
                if not block:
                    break
                self.wfile.write(block)
                remaining -= len(block)


def make_server(archive, key=None, port=0):
    server = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    server.daemon_threads = True
    server.key = key or secrets.token_urlsafe(32)
    server.archive = archive
    return server


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--config")
    parser.add_argument("--open", action="store_true")
    parser.add_argument("--session", default="")
    parser.add_argument("--resume", action="store_true", help="Resume an existing local address/key after a controlled service update")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    root.mkdir(parents=True, exist_ok=True)
    if not args.config:
        args.config = read_json(root / "archive-config.json", {}).get("unityConfig")
    else:
        atomic_json(root / "archive-config.json", {"unityConfig": str(Path(args.config).resolve()),
                    "pythonExecutable": __import__("sys").executable})
    state_file = root / ".archive-server.json"
    state = read_json(state_file, {})
    if state.get("root") == str(root) and not args.resume:
        try:
            base = f"http://127.0.0.1:{int(state['port'])}"
            request = urllib.request.Request(base + "/health", headers={"X-Archive-Key": state["key"]})
            with urllib.request.urlopen(request, timeout=2) as response:
                if json.load(response).get("service") == "neon-study-archive-v1":
                    if args.open:
                        webbrowser.open(base + "/?launch=" + state["key"] + "&session=" + urllib.parse.quote(args.session))
                    return
        except (OSError, ValueError, KeyError):
            pass
    archive = Archive(root, args.config)
    preserve = args.resume and state.get("root") == str(root)
    server = make_server(archive, key=state.get("key") if preserve else None, port=int(state["port"]) if preserve else 0)
    atomic_json(state_file, {"root": str(root), "port": server.server_port, "key": server.key, "pid": os.getpid()})
    if args.open:
        url = f"http://127.0.0.1:{server.server_port}/?launch={server.key}&session=" + urllib.parse.quote(args.session)
        threading.Timer(0.5, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
