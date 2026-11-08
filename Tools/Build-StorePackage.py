#!/usr/bin/env python3
"""Prepare the separate no-external-program store edition; never mutate the full package."""
import hashlib
import json
from pathlib import Path
import re
import shutil
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Packages/com.dolbywu.neon-research'
SUPPORT = ROOT / 'StoreSupport'
NAME = 'individual.binyan-xu.gazeflow'


def replace_once(path, old, new):
    text = path.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise ValueError(f'Unexpected source structure: {path.name}: {old[:60]}')
    path.write_text(text.replace(old, new, 1), encoding='utf-8')


def prepare(destination):
    shutil.copytree(SOURCE, destination, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
    for name in ('Tools', 'Tools.meta', 'Verification', 'Verification.meta', 'Editor/StudyMediaEditor.cs', 'Editor/StudyMediaEditor.cs.meta'):
        target = destination / name
        if target.is_dir():
            shutil.rmtree(target)
        elif target.exists():
            target.unlink()
    manifest_path = destination / 'package.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    manifest.update(name=NAME, displayName='GazeFlow - VR Eye-Tracking Research Toolkit',
                    description='Unofficial Neon + Meta Quest 3 research recording, local-video HTML review and human annotation. No external recording programs. Windows Unity Editor.')
    manifest['files'] = [x for x in manifest['files'] if x not in ('Tools', 'Tools.meta')]
    if manifest['author']['name'] != 'Binyan Michelle xu':
        raise ValueError('Publisher identity mismatch')
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')

    setup = destination / 'Editor/NeonResearchSetup.cs'
    text = setup.read_text(encoding='utf-8')
    start = text.index('                settings.mediaRecordingEnabled=')
    end = text.index('                if(GUILayout.Button("Save project settings"))', start)
    text = text[:start] + '                EditorGUILayout.HelpBox("Import an existing video in the finished HTML replay. This edition does not record video or microphone audio.",MessageType.Info);\n' + text[end:]
    text = text.replace('Leave Neon and media off for your first test.', 'Leave Neon off for your first test.')
    setup.write_text(text, encoding='utf-8')

    archive = destination / 'Editor/StudyArchiveEditor.cs'
    text = archive.read_text(encoding='utf-8').replace('using System.Diagnostics;\n', '')
    text = text.replace('    [MenuItem("Tools/Neon Research/Open Participant Archive")]\n    public static void Open() => Launch(null);\n\n', '')
    start = text.index('    private static string Quote(string text)')
    text = text[:start] + '}\n\n}\n'
    text = text.replace('Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });', 'EditorUtility.RevealInFinder(folder);')
    archive.write_text(text, encoding='utf-8')

    settings = destination / 'Runtime/NeonStudySettings.cs'
    text = settings.read_text(encoding='utf-8')
    start = text.index('    public string archivePythonExecutable')
    end = text.index('    public string participantId', start)
    text = text[:start] + '    // Compatibility readout: this edition never starts media capture.\n    public bool mediaRecordingEnabled => false;\n' + text[end:]
    text = text.replace('"UserSettings", "NeonResearch"', '"UserSettings", "GazeFlow"').replace('"NeonResearchConfig"', '"GazeFlowConfig"')
    settings.write_text(text, encoding='utf-8')
    session = destination / 'Runtime/StudyExportSession.cs'
    replace_once(session, 'requestedVideo = rootOverride == null && config.mediaRecordingEnabled && config.mediaRecordVideo;', 'requestedVideo = false; // Store edition imports existing media only.')
    replace_once(session, 'requestedAudio = rootOverride == null && config.mediaRecordingEnabled && config.mediaRecordMicrophone;', 'requestedAudio = false; // Store edition does not capture microphone audio.')
    tests = destination / 'Tests/Editor/ExtractionTests.cs'
    text = tests.read_text(encoding='utf-8').replace('UserSettings/NeonResearch', 'UserSettings/GazeFlow')
    tests.write_text(text, encoding='utf-8')
    for source in destination.rglob('*.cs'):
        text = source.read_text(encoding='utf-8').replace('Tools/Neon Research/', 'Tools/GazeFlow/').replace('AddComponentMenu("Neon Research/', 'AddComponentMenu("GazeFlow/')
        source.write_text(text, encoding='utf-8')

    shutil.copyfile(SUPPORT / 'README.template.txt', destination / 'README.md')
    shutil.copyfile(SUPPORT / 'workflow.template.txt', destination / 'Documentation~/workflow.md')
    shutil.copyfile(SUPPORT / 'Third-Party-Notices.template.txt', destination / 'Third Party Notices.md')
    sample_readme = destination / 'Samples~/MinimalStudy/README.md'
    sample_readme.write_text('''# Minimal Study\n\nUse a blank scene with Main Camera, or assign a Head Pose Source. Add a box collider and Study Region with a unique region ID. Add Interaction Adapter and explicitly wire real UnityEvents to its public methods. The example methods emit synthetic behavioral events only, never fabricated eye data.\n\nOpen Tools > GazeFlow > Setup and Recording Settings, select a dedicated output folder and opt into recording. For a first trial leave Neon off. Confirm an anonymous participant, mark observations and Stop normally. Inspect CSVs and the finished HTML. Import an existing video for review; this edition does not capture media or run external programs.\n''')
    sample = destination / 'Samples~/MinimalStudy/MinimalStudyScene.cs'
    text = sample.read_text(encoding='utf-8')
    if '\nnamespace ' not in text:
        position = text.index('public sealed class')
        text = text[:position] + 'namespace NeonResearch.Samples\n{\n' + text[position:] + '\n}\n'
        sample.write_text(text, encoding='utf-8')
    return manifest


def validate(destination):
    guids = set()
    for path in destination.rglob('*'):
        if path.is_symlink():
            raise ValueError(f'Linked package content: {path}')
        if not path.is_file():
            continue
        if path.suffix.lower() in ('.py', '.pyc', '.exe', '.dll', '.apk', '.ps1', '.cjs'):
            raise ValueError(f'External executable/script present: {path.name}')
        if path.suffix == '.cs':
            text = path.read_text(encoding='utf-8')
            for forbidden in ('Process.Start', 'ProcessStartInfo', 'archivePythonExecutable', 'mediaFfmpegExecutable', 'StudyMediaEditor', 'config.mediaRecordVideo', 'config.mediaRecordMicrophone'):
                if forbidden in text:
                    raise ValueError(f'External capture reference {forbidden}: {path}')
        if path.suffix in ('.json', '.asmdef'):
            json.loads(path.read_text(encoding='utf-8'))
        if path.suffix == '.meta':
            match = re.search(r'^guid: ([a-f0-9]{32})\s*$', path.read_text(encoding='utf-8'), re.M)
            if not match or match[1] in guids:
                raise ValueError(f'Invalid/duplicate metadata GUID: {path}')
            guids.add(match[1])
            continue
        relative = path.relative_to(destination)
        if not any(part.endswith('~') for part in relative.parts) and not Path(str(path)+'.meta').exists():
            raise ValueError(f'Missing import metadata: {relative}')
    assert (destination / 'Runtime/Resources/NeonResearchOfflineReplay.txt').is_file()
    assert not (destination / 'Editor/StudyMediaEditor.cs').exists()
    assert 'requestedVideo = false;' in (destination / 'Runtime/StudyExportSession.cs').read_text(encoding='utf-8')
    assert 'requestedAudio = false;' in (destination / 'Runtime/StudyExportSession.cs').read_text(encoding='utf-8')
    assert 'public bool mediaRecordingEnabled => false;' in (destination / 'Runtime/NeonStudySettings.cs').read_text(encoding='utf-8')


def main():
    output = ROOT / 'Artifacts/Store'
    output.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='.stage-', dir=output) as temp:
        package = Path(temp) / 'package'
        manifest = prepare(package)
        validate(package)
        archive = output / f"{NAME}-{manifest['version']}-CANDIDATE.tgz"
        with tarfile.open(archive, 'w:gz') as tar:
            tar.add(package, arcname='package')
        embedded = output / 'StoreValidationProject/Packages' / NAME
        # Only replace the generated package under Artifacts/Store, never host data or source.
        if embedded.exists():
            shutil.rmtree(embedded)
        shutil.copytree(package, embedded)
        project = output / 'StoreValidationProject'
        (project / 'Assets').mkdir(exist_ok=True)
        (project / 'ProjectSettings').mkdir(exist_ok=True)
        shutil.copyfile(ROOT / 'VerificationProject/ProjectSettings/ProjectVersion.txt', project / 'ProjectSettings/ProjectVersion.txt')
        (project / 'Packages/manifest.json').write_text(json.dumps({'dependencies':dict(manifest['dependencies'], **{'com.unity.test-framework':'1.6.0'}),'testables':[NAME]}, indent=2)+'\n', encoding='utf-8')
        evidence = dict(package=NAME, version=manifest['version'], author=manifest['author']['name'], sha256=hashlib.sha256(archive.read_bytes()).hexdigest(), external_capture_programs=False, unity_validator='NOT_RUN', signed=False, store_approval='NOT_SUBMITTED')
        (output / 'candidate-evidence.json').write_text(json.dumps(evidence, indent=2)+'\n', encoding='utf-8')
        print(json.dumps(evidence, indent=2))
        print(archive)


if __name__ == '__main__':
    main()
