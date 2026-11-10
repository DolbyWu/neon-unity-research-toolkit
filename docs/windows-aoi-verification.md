# Branch verification · 2026-11-10

Scope: `feature/neon-xr-aoi-integration`, reusable source changes only. No participant recordings, private reports, calibration files or device configuration accompany this note.

| Check | Result | Scope / limitation |
|---|---|---|
| Unity core EditMode tests | PASS, 37/37 | Unity 6000.3.23f1, separate core-only host, synthetic fixtures; includes target/occlusion/trigger/spawn/catalogue/session tests, not official SDK hardware execution |
| Target HTML / metrics | PASS | `Test-GazeTargets.cjs`: six conservative coverage cases; names, groups, interaction/target filters, seek, target CSV, complete/filtered interval bundle, inert labels, invalid/empty ranges, legacy sessions; no network requests |
| Video + annotations | PASS | `Test-ReplayAnnotations.cjs`: synthetic local video, authoritative pause and scrub, video/session clocks, multiple additions, JSON reload, CSV escaping and validation; no network requests |
| Existing replay regression | PASS | `Test-OfflineReplay.cjs`: pupil display precision without changing raw values, aligned half-open extraction, missing data, region selection, playback, JSON/CSV and escaped text; no network requests |
| Optional adapter and pointer Editor sources | PASS, static compilation only | Actual installed official Core 2.0.0 runtime assembly plus Unity 6000.3.23f1 modules; not a new clean-project import or native-loading test |
| Portable pointer window in a headset | NOT TESTED for this packaging revision | Display switch only; verify hide/show in the target project before use |
| Every relevant object / behavior mapping | INCOMPLETE | Host-specific ownership, unused prefabs, dynamic factories and full physical exercise need a project-level audit |
| Quantified spatial accuracy / hardware synchronization | NOT ESTABLISHED | Operator acceptance, callback time and estimated media offsets are not measured spatial or clock accuracy |

The earlier local Neon + Quest trial is summarized in [the integration update](windows-aoi-integration.md). Software checks above were repeated for this push; they do not convert that trial into universal hardware acceptance.

## Repeat the software checks

Use a licensed disposable Unity host with the core package in `testables`, no participants and no automatic recording:

```text
Unity.exe -batchmode -nographics -runTests -testPlatform EditMode -projectPath <disposable-core-host> -testResults <private-results.xml> -logFile <private-test.log>
```

For the browser tests, set `STUDY_PLAYWRIGHT` to an installed Playwright module and `STUDY_FFMPEG` to an installed FFmpeg executable. The annotation and replay checks use an installed Microsoft Edge browser. From the repository root run:

```text
node Packages/com.dolbywu.neon-research/Verification/Test-GazeTargets.cjs
node Packages/com.dolbywu.neon-research/Verification/Test-ReplayAnnotations.cjs
node Packages/com.dolbywu.neon-research/Verification/Test-OfflineReplay.cjs
```

These scripts create synthetic outputs under ignored `Artifacts/`; they are not research records. Keep actual recordings and private validation evidence outside this public repository. Do not publish keys or machine-specific configuration.
