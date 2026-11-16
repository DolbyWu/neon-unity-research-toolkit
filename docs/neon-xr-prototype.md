# Neon XR research-target prototype · 0.2.0-exp.1

Original prototype branch: `feature/neon-xr-research-targets`. The Windows integration continuation is `feature/neon-xr-aoi-integration`: see [the current update](windows-aoi-integration.md) and [verification note](windows-aoi-verification.md). The submitted store snapshot remains on `store/0.1.0-preview.6`; this work does not change or resubmit it. The checks below are the original prototype history, not the latest Windows status.

See the [complete setup, architecture, timing limitations and acceptance checklist](../Packages/com.dolbywu.neon-research/Documentation~/neon-xr-targets.md).

The next trial connects three authored research targets with available official spatial gaze, explicit Unity task events, and HTML review/reflection. It extends the existing participant/session and annotation workflows. It is a technical prototype, not a validated quantitative protocol.

The core development version is `0.2.0-exp.1`. The separate optional adapter is `com.binyanxu.gazeflow.neonxr`; the core remains usable without it or the official SDK. The store builder is also marked experimental on this branch to avoid rebuilding modified code under preview.6's version. Do not submit the development profile as an update before the pending acceptance checks pass.

Windows project-copy, plugin replacement and real-device acceptance: [Codex execution handoff](windows-neon-xr-handoff.md).

## Checks

- PASS: static C# compilation of the full core Runtime, Editor and test sources using actual Unity 6000.3.23f1 reference assemblies. This is not a Unity project import or runtime test.
- PASS: six cases exercising the actual shipped metrics function: gaps, invalid samples, target transitions, calibration changes and half-open selections.
- PASS: DOM summary/filter/seek-to-annotation JSON, target CSV and interval bundle exports, inert untrusted labels, empty/invalid intervals and legacy sessions. Canvas/media are substitutes; visual/video behavior is not asserted.
- PENDING: seven new Unity EditMode tests and original runtime/serialization regression tests. Editor startup is blocked by missing license entitlements on this Mac.
- PENDING: dependency resolution and compilation/loading of the optional adapter with official SDK/native plugins in Unity.
- PENDING: real browser rendering/video regression. Native browser automation is blocked by the current process sandbox.
- PENDING: new target-mapping/disconnect/reconnect/clock acceptance on Neon + Quest. Earlier successful hardware recording tests remain valid for their tested preview; they do not establish acceptance of this extension.

## Commands

On macOS, static compilation can be reproduced with `python3 Tools/Compile-UnitySources.py --unity /path/to/Unity.app/Contents --cache /path/to/disposable-host/Library/PackageCache`. Add `--sdk-runtime-dll /path/to/Library/ScriptAssemblies/PupilLabs.NeonXR.Core.Runtime.dll` after the official SDK has actually compiled in Unity to check the optional adapter. These checks do not run Unity or establish native-plugin compatibility.

From the repository root, install test-only Node dependencies in a scratch directory with `npm install --prefix work/target-checks playwright@1.58.2 jsdom@26.1.0`. Keep these out of UPM artifacts.

```sh
STUDY_METRICS_ONLY=1 STUDY_PLAYWRIGHT="$PWD/work/target-checks/node_modules/playwright" node Packages/com.dolbywu.neon-research/Verification/Test-GazeTargets.cjs
STUDY_JSDOM="$PWD/work/target-checks/node_modules/jsdom" node Packages/com.dolbywu.neon-research/Verification/Test-GazeTargetsDom.cjs
```

On a machine that permits browser launch, run `Test-GazeTargets.cjs` without `STUDY_METRICS_ONLY` and the existing `Test-OfflineReplay.cjs` / `Test-ReplayAnnotations.cjs`. Their default browser is Microsoft Edge; the target test also accepts `STUDY_BROWSER_EXECUTABLE`. Run Unity tests only in a licensed disposable host, with the package listed in `testables`; follow the existing isolated-scene guard for save/reopen tests.
