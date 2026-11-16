# Windows AOI integration · experimental branch

Branch: `feature/neon-xr-aoi-integration`. Package versions remain `0.2.0-exp.1`; install by a fixed commit for reproducibility. This is source for further validation, not a release or store submission. `main` and the submitted store branch are unchanged.

## What this update contains

- Optional official Neon XR Core adapter: WebSocket freshness uses actual receive-event sequence numbers when the pinned official client does not provide device timestamps. Missing device timestamps export as blank, never fabricated PC timestamps. The latest spatial ray and packet metadata are not guaranteed to be an atomic pair.
- Research targets retain existing names, stable IDs, groups, explicit legacy event aliases and declared behavior kinds. Prefab templates fail closed until the host registers a unique spawn-site/instance identity through `StudyTargetSpawnIdentity.Register`.
- `gaze_targets.csv` adds object names, behavior metadata and hit geometry; `target_catalog.csv` records configured objects separately from actual gaze evidence, including inactive loaded objects. Unloaded/uninstantiated assets are not automatically a complete project inventory.
- Nearest solid colliders occlude targets behind them. Existing trigger AOI volumes require explicit opt-in and export as `authored_trigger_volume`, not visible surfaces. Gameplay colliders and physics settings are not changed. Query-buffer saturation fails closed.
- HTML review adds names, IDs, groups and explicit interaction filters, target/event seeking, filtered target CSV and full interval bundles with the complete target stream, catalogue and separately identified filtered rows. Video, eye traces and multiple manual annotations remain available.
- The optional adapter provides **Tools > GazeFlow > Neon XR > Gaze Pointer**. During Play, **Hide gaze pointer / Show gaze pointer** controls the official visualizer's dot and ray only. Hiding does not disable acquisition, AOI logging, calibration or the core Gaze Monitor. The package window opens manually; a host-specific automatic window is not required.

Ray-hit callback counts, observed visits and conservative same-target coverage are descriptive observations, not fixation, attention, understanding or cognitive-state estimates. Gaps and invalid data are excluded from covered duration. Unity callback time, device time and estimated media offsets remain distinct.

## Install without touching an existing experiment

1. Make an independent copy of your working Unity project; preserve `.meta` files and create a separate technical-test output root. Keep original recordings unchanged.
2. Use Unity `6000.3.23f1`. Clone this branch outside `Assets`; record the commit shown by `git rev-parse HEAD`.
3. In Package Manager, install the core from `Packages/com.dolbywu.neon-research/package.json` on disk. Do not install alongside the overlapping Asset Store edition or copied legacy core scripts.
4. Install the official dependency using the pinned URL:

   ```text
   https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a
   ```

5. Follow official Addressables and prefab setup, then install `Packages/com.binyanxu.gazeflow.neonxr/package.json` from disk. Confirm official Core `2.0.0` resolves to that commit. No official source/native binaries are redistributed.
6. Assign the real tracked XR head Transform, not the rig root. Keep mapping unverified until an operator confirms known targets after mount/calibration adjustments. A visual pointer or network connection alone is not validation.
7. Configure stable target IDs, groups, collision masks and explicit behavior links. Use the [detailed setup guide](../Packages/com.dolbywu.neon-research/Documentation~/neon-xr-targets.md). Re-run known-target, head-motion, occlusion, disconnect/reconnect, session-isolation and media checks in your own project.

## Scope and validation limits

The local Windows trial exercised actual Neon + Quest acquisition, spatial hits, behavior recording, stream recovery and HTML review. Alignment acceptance was an operator declaration, not a quantified spatial-error or hardware-synchronization measurement. A project-wide all-object behavioral/target ownership audit is not complete; an object's configured status does not mean it was physically tested.

The reusable package changes are included here. The existing host experiment's scenes, assets, custom event bridges, audit inventories, participant archives, device IP configuration, calibration files, API keys, raw CSV, audio and video are deliberately excluded. This branch is not a backup of the entire experiment. Host-only transcription tools remain in the project copy; this push does not add automatic cloud transcription to the toolkit.

The new portable pointer window is a packaging adaptation of the host display switch. It needs a real-headset hide/show check; it must not be represented as already physically validated. The original Windows execution [handoff](windows-neon-xr-handoff.md) is historical; this update and the current setup guide supersede its trigger-only limitations and branch selection.

Repeatable software checks for this push are recorded in [the branch verification note](windows-aoi-verification.md). No participant records or private acceptance reports are uploaded.
