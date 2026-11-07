# Preview.3 acceptance and next steps

Package: `com.dolbywu.neon-research` `0.1.0-preview.3`. Preparation date: 6 November 2026. Planned operator trial: 7 November 2026.

## What has passed

- GitHub Actions package checks at source commit `f52f2782ba53acbee85eadffbccea725d5b6b9ae`: documentation and Windows UPM archive build passed. No Unity execution occurs in this workflow.
- Documentation check: 12 tracked Markdown files and 30 local links passed before this report was added.
- Downloaded archive: safe package-root paths, no filesystem links, expected directories, valid manifest/asmdef JSON, declared sample paths, C#/asmdef metadata and unique metadata GUIDs passed.
- Every archived file matched the package source after normalizing Windows CRLF/macOS LF line endings. Repository/package MIT license and copyright matched.
- No participant archives, settings, generated data, demo photos/videos or native executable binaries were included in the package.
- Python archive tests: 6 passed, including loopback HTTP authorization/origin boundaries, participant switching and raw-file preservation.
- Python capture-state tests: 4 passed; 4 FFmpeg-dependent command/media tests were skipped. No real microphone, screen capture or Neon device was accessed.

Artifact SHA-256:

```text
c8bd533389244e61a2d79fdfed095de800a1124f3cce23c81918051b1a94ceec  com.dolbywu.neon-research-0.1.0-preview.3.tgz
```

## What has not passed yet

A prepared project is not an executed installation test. The preparation machine only has Unity 2022.3.62f3 and 6000.0.39f1, below the package's declared 6000.3 minimum. No supported Unity Editor import/compile, PlayMode, Git installation, real Neon disconnection, full task-length media or timing-accuracy acceptance has been performed for preview.3 here. Playwright is unavailable in the default local Node environment; browser suites were not rerun.

Historical Windows preview.2 test results remain in [Validation](../VALIDATION.md). The author's successful original-study hardware use does not substitute for this extracted-package installation trial.

## Run the clean-project trial

Use a separate Windows computer/project with Unity **6000.3.23f1**. Do not replace or duplicate recording scripts in your original experiment yet.

### A. Installation

1. Create a blank 3D Unity project through Unity Hub. Registry access is needed for dependencies.
2. Package Manager → Install/Add package from tarball → select the preview.3 `.tgz` attached to this release.
3. Wait for import/compilation. Confirm no package-origin compile errors and that **Tools → Neon Research** appears.
4. Import **Minimal event integration sample** through Package Manager. This sample supplies scripts/instructions, not a ready-made experiment scene.
5. Try attaching **Head Pose Source**, **Study Region** and **Interaction Adapter**. Save and reopen the scene; check for missing scripts/fields. These types currently share `StudyAdapters.cs`; record any attachment/serialization failure as a blocker rather than treating this step as already passed.
6. In another fresh project, test the release's tagged Git-subfolder installation independently. Record the tag/commit and installed version.

The release also includes an optional prepared clean host ZIP. Extract it and add `CleanInstallProject` through Unity Hub. It references the included tarball and intentionally contains no old settings/data/Library. It is a convenience host, not a substitute for testing installation into your own blank project.

### B. One-minute data-only recording

1. Ensure a tagged Main Camera exists and identify the actual head source.
2. Open Setup and Recording Settings. Choose a new dedicated test output root; enable Record on Unity Play; leave Neon and media disabled; save.
3. Open Live Monitor. Press Play, confirm an anonymous technical-test participant and record about 60 seconds.
4. Move the camera in a controlled way and click Mark observation. If using adapters, invoke explicitly wired synthetic task/grab/release events.
5. Stop normally. Inspect `export_manifest.json`, `head_pose.csv`, `behavior_events.csv`, `game_events.csv` and `Replay/study-replay.html`.
6. Confirm the intended event/pose rows, raw closure, no fabricated eye rows and successful replay opening. Start/stop again to check fresh session creation.

### C. Neon and media

1. Enable Neon and enter the current Companion address/ports. Check advancing sample counts, fresh samples, quality/pupil availability and alignment status.
2. Interrupt the approved network/stream briefly, then restore it. Check gap visibility, connection epochs/recovery, continued non-eye recording and normal Stop.
3. Record exact headset, Neon/Companion, Unity, Windows and network versions/configuration. Follow the [hardware notes](neon-hardware-setup.md).
4. Configure optional Python/FFmpeg/FFprobe and the intended video/microphone only after data-only recording works.
5. Run a full task-length session. Verify actual video/audio coverage, finalization and timing against observable markers at the beginning/middle/end.
6. In HTML, add two point annotations and one interval annotation, export JSON, close/reopen and import it. Confirm labels, evidence sources and times survive. Do not use participant data for installation debugging.

## Record the result

| Check | Result / evidence |
|---|---|
| Windows / Unity / package version | Pending |
| Tarball install and compile | Pending |
| Component attach/save/reopen | Pending |
| Tagged Git install in separate project | Pending |
| Data-only CSV and HTML | Pending |
| Repeat Play/Stop | Pending |
| Neon reception / fields | Pending |
| Disconnect / recovery / Stop | Pending |
| Full-duration media and marker alignment | Pending |
| Annotation JSON roundtrip | Pending |
| Remaining blockers / logs | Pending |

On failure, retain Console logs, reproduction steps and synthetic session evidence; do not edit raw completion flags. Share a reproducible issue without participant data. Do not describe pending items as passed.

## After the operator trial

**Pupil Labs:** summarize the project, supported configuration, results, limitations and demo. Ask the `pupil-labs/neon-xr` maintainers whether a community-tool link, integration example or narrowly scoped PR fits their project. Keep this toolkit independently maintained unless upstream agrees otherwise. No upstream outreach has been sent as part of this release.

**Asset Store:** after the trial and feedback, prepare a working sample scene, support/contact information, clean imports, required package metadata and store validation. Review external Python/FFmpeg executable dependencies with Unity before submission; GitHub distribution does not establish Asset Store eligibility. No store enrollment or submission is included in this release.
