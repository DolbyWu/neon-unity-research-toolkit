"""Store-profile acceptance: actual generated payload and isolation, without Unity execution."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('store_builder', Path(__file__).with_name('Build-StorePackage.py'))
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


def fingerprint(root):
    return {str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest()
            for p in root.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix != '.pyc'}


class StoreProfileTests(unittest.TestCase):
    def test_profile_excludes_programs_preserves_core_and_full_source(self):
        before = fingerprint(builder.SOURCE)
        with tempfile.TemporaryDirectory() as directory:
            package = Path(directory) / 'package'
            manifest = builder.prepare(package)
            builder.validate(package)
            self.assertEqual(manifest['name'], 'individual.binyan-xu.gazeflow')
            self.assertEqual(manifest['author']['name'], 'Binyan Michelle xu')
            self.assertEqual(manifest['license'], 'MIT')
            # Acquisition transport/decoder, replay and event algorithms remain identical.
            for name in ['NeonTcpGazeClient.cs','NeonClockSync.cs','NeonWireDecoder.cs','NeonStudyRecorder.cs','StudySequence.cs','StudyOfflineReplay.cs','StudyEpisodes.cs']:
                self.assertEqual((package/'Runtime'/name).read_bytes(), (builder.SOURCE/'Runtime'/name).read_bytes(), name)
            self.assertFalse((package/'Tools').exists())
            self.assertFalse((package/'Editor/StudyMediaEditor.cs').exists())
            archive = (package/'Editor/StudyArchiveEditor.cs').read_text()
            self.assertIn('FinishedReplayPath', archive)
            self.assertNotIn('private static void Launch', archive)
            settings = (package/'Runtime/NeonStudySettings.cs').read_text()
            self.assertIn('public bool mediaRecordingEnabled => false;', settings)
            self.assertNotIn('mediaFfmpegExecutable',settings)
        self.assertEqual(before, fingerprint(builder.SOURCE), 'Full GitHub package must remain unchanged')

    def test_validator_rejects_external_launcher_regression(self):
        with tempfile.TemporaryDirectory() as directory:
            package = Path(directory) / 'package'
            builder.prepare(package)
            source = package/'Editor/StudyArchiveEditor.cs'
            source.write_text(source.read_text()+'\n// Process.Start must not return to this edition\n')
            with self.assertRaisesRegex(ValueError, 'External capture reference'):
                builder.validate(package)


if __name__ == '__main__':
    unittest.main()
