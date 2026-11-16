"""Inspect actual beta tarballs without importing Unity or reading study recordings."""
import hashlib
import json
from pathlib import Path
import tarfile

root = Path(__file__).resolve().parent.parent
version = json.loads((root/'Packages/com.dolbywu.neon-research/package.json').read_text())['version']
for name in ('com.dolbywu.neon-research', 'com.binyanxu.gazeflow.neonxr'):
    archive = root/'Artifacts'/f'{name}-{version}.tgz'
    with tarfile.open(archive, 'r:gz') as package:
        members = package.getmembers()
        for entry in members:
            parts = Path(entry.name).parts
            assert parts[0] == 'package' and '..' not in parts and not entry.name.startswith('/'), entry.name
            assert not entry.issym() and not entry.islnk(), entry.name
            assert not set(parts) & {'Participants','UserSettings','NeonConfig','Library','Logs','Temp','__pycache__','Verification'}, entry.name
            assert Path(entry.name).suffix.lower() not in {'.exe','.dll','.csv','.mp4','.wav','.pyc'}, entry.name
        names = {entry.name.rstrip('/') for entry in members}
        manifest = json.load(package.extractfile('package/package.json'))
        assert manifest['name'] == name and manifest['version'] == version
        for entry in members:
            if entry.name.endswith('.meta') or any(part.endswith('~') for part in Path(entry.name).parts):
                continue
            assert entry.name.rstrip('/') == 'package' or entry.name.rstrip('/')+'.meta' in names, entry.name
        if name.endswith('neon-research'):
            assert 'package/Runtime/Resources/NeonResearchOfflineReplay.txt' in names
            assert 'package/Editor/LiveObservationWindow.cs' in names
            assert 'package/Editor/GazeFlowWelcomeWindow.cs' in names
            for source in ('StudyObjectHitExport.cs','StudySessionSegments.cs'):
                assert 'package/Runtime/'+source in names
            assert 'package/Editor/StudyObjectOwnershipReplay.cs' in names
        else:
            assert manifest['dependencies']['com.dolbywu.neon-research'] == version
            for source in ('StudyNeonXRBindings.cs','StudyGazeReferencePoint.cs'):
                assert 'package/Runtime/'+source in names
        digest = hashlib.sha256(archive.read_bytes()).hexdigest()
        assert digest in (root/'Artifacts/SHA256SUMS.txt').read_text()
        print(f'PASS: {archive.name}: {len(members)} members, metadata, dependency and privacy inventory.')
