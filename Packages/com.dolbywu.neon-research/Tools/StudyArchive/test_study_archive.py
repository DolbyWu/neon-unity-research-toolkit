"""Synthetic fixtures only. Never scans actual research data."""
import http.client
import json
from pathlib import Path
import tempfile
import threading
import unittest
from study_archive import Archive, atomic_json, make_server

SID = "20260101T120000000Z-1234abcd"


class ArchiveTests(unittest.TestCase):
    def setUp(self):
        self.fixture_base = Path(__file__).resolve().parents[4] / "Artifacts/python-archive-unit"
        self.fixture_base.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="study-archive-unit-", dir=self.fixture_base)
        self.root = Path(self.temp.name) / "Library"
        self.config = Path(self.temp.name) / "config.json"
        atomic_json(self.config, {"outputRoot": str(self.root), "participantId": "", "companionIp": "127.0.0.1"})
        self.archive = Archive(self.root, self.config)

    def tearDown(self):
        assert Path(self.temp.name).resolve().is_relative_to(self.fixture_base.resolve())
        self.temp.cleanup()

    def add_session(self, closed=True):
        self.archive.save_participant({"participantId": "P001", "background": "Synthetic profile"})
        directory = self.root / "Participants/P001/Sessions" / SID
        atomic_json(directory / "export_manifest.json", {"sessionId": SID, "participantId": "P001", "closed": closed})
        (directory / "Replay").mkdir()
        (directory / "Replay/study-replay.html").write_text("<!doctype html><title>Synthetic</title>", encoding="utf8")
        self.archive.refresh()
        return directory

    def test_profile_select_preserves_neon_settings(self):
        self.archive.save_participant({"participantId": "P001", "background": "Voluntary synthetic notes"})
        self.archive.select_participant("P001")
        config = json.loads(self.config.read_text())
        self.assertEqual(config["participantId"], "P001")
        self.assertEqual(config["companionIp"], "127.0.0.1")
        with self.assertRaises(ValueError):
            self.archive.save_participant({"participantId": "P001"})

    def test_traversal_ids_and_external_paths_rejected(self):
        for value in ["../P001", "P001/other", "..", "", "P001\\other", "A B"]:
            with self.assertRaises(ValueError):
                self.archive.participant(value)
        with self.assertRaises(ValueError):
            self.archive.safe(self.root.parent / "outside")
        with self.assertRaises(ValueError):
            self.archive.session_folder("../outside")

    def test_session_index_and_notes_persist_without_touching_raw(self):
        directory = self.add_session()
        raw = directory / "raw.csv"
        raw.write_bytes(b"synthetic,unchanged\n")
        rows = self.archive.listing()["sessions"]
        self.assertEqual(len(rows), 1)
        self.assertTrue(rows[0]["replayExists"])
        result = self.archive.save_notes({"sessionId": SID, "version": 0, "interview": "测试者原话 / synthetic", "observations": "Observed, not inferred"})
        self.assertEqual(result["version"], 1)
        self.assertEqual(Archive(self.root, self.config).notes(SID)["interview"], "测试者原话 / synthetic")
        self.assertEqual(raw.read_bytes(), b"synthetic,unchanged\n")
        with self.assertRaises(ValueError):
            self.archive.save_notes({"sessionId": SID, "version": 0, "interview": "stale overwrite"})

    def test_open_session_blocks_identity_switch(self):
        self.add_session(closed=False)
        with self.assertRaises(ValueError):
            self.archive.select_participant("P001")

    def test_reference_not_counted_as_participant(self):
        directory = self.root / "ReferenceTests" / SID
        atomic_json(directory / "export_manifest.json", {"sessionId": SID, "participantId": "technical-test", "closed": True})
        listing = self.archive.listing()
        self.assertEqual(listing["participants"], [])
        self.assertEqual(listing["sessions"][0]["kind"], "reference")

    def test_http_auth_origin_and_replay_boundaries(self):
        self.add_session()
        server = make_server(self.archive, "synthetic-key")
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        def request(method, target, body=None, headers=None):
            client = http.client.HTTPConnection("127.0.0.1", server.server_port, timeout=3)
            client.request(method, target, body, headers or {})
            response = client.getresponse()
            data = response.read()
            status = response.status
            client.close()
            return status, data
        try:
            self.assertEqual(request("GET", "/api/library")[0], 403)
            cookie = {"Cookie": "study_archive=synthetic-key"}
            self.assertEqual(request("GET", "/api/library", headers=cookie)[0], 200)
            self.assertEqual(request("GET", "/api/library", headers={**cookie, "Host": "evil.example"})[0], 403)
            self.assertEqual(request("GET", "/replay/../../outside", headers=cookie)[0], 400)
            self.assertEqual(request("GET", "/replay/" + SID, headers=cookie)[0], 200)
            note = json.dumps({"sessionId": SID, "version": 0, "interview": "Synthetic HTTP note"})
            self.assertEqual(request("POST", "/api/notes", note, cookie)[0], 403)
            headers = {**cookie, "Content-Type": "application/json", "X-Study-Archive": "1", "Origin": f"http://127.0.0.1:{server.server_port}"}
            self.assertEqual(request("POST", "/api/notes", note, headers)[0], 200)
            self.assertEqual(request("POST", "/api/notes", note, headers)[0], 400)
        finally:
            server.shutdown()
            server.server_close()
            thread.join(3)


if __name__ == "__main__":
    unittest.main()
