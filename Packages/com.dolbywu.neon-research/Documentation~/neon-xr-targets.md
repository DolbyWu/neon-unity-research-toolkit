# Experimental Neon XR research targets · 0.2.0-exp.1

This development prototype adds object-level ray-hit review to GazeFlow. The submitted Asset Store preview.6 is unchanged on `store/0.1.0-preview.6`. This branch is not a replacement submission or a validated research release.

## What is implemented

- `StudyGazeRayProvider`: optional main-thread fresh-ray event interface. It never starts acquisition or sessions.
- `StudyResearchTarget`: stable authored target ID, group and label; active duplicate or missing IDs fail closed.
- `StudyGazeTargetRecorder`: one opt-in recorder, nearest-collider raycasting and missing-data watchdog (default 0.1 s).
- Separate `gaze_targets.csv`, plus target enter/exit events in existing `game_events.csv`. Existing raw Neon/pupil CSV schemas are unchanged.
- Optional `com.binyanxu.gazeflow.neonxr` package bridges the official provider. The core package has no official SDK dependency.
- HTML target summary, callback status list, target filter, time seeking, annotation workflow and selected-interval target CSV/bundle export. Old sessions without the new stream still load.
- Configuration-check window and optional three-object sample. They do not measure calibration accuracy or synchronize clocks.

This is an additive spatial adapter, not yet a selectable replacement for the complete raw eye receiver. Keep the existing receiver enabled if native raw eye/pupil export is required. Running both receivers creates two network consumers; bandwidth, field coverage and data-loss behavior need a real-device trial.

## Install in a disposable project

Use Unity 6000.3.23f1. Do not install alongside the Asset Store edition: it shares the existing core namespace/assemblies/GUIDs. Do not overwrite an active experiment installation.

1. Install the development core tarball (or this branch's `Packages/com.dolbywu.neon-research/package.json` from disk).
2. Install official Neon XR Core **before** the optional adapter. The interface inspected was version 2.0.0 at upstream commit `f4dee47b9690a059b1c4660628596d7303ade59a`:

   ```text
   https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a
   ```

3. Follow the official Addressables/import/build and NeonXR prefab setup. The upstream package adds Addressables, Input System and XR Interaction Toolkit dependencies; compatibility with the host's existing XR packages must be checked. No official files or native binaries are redistributed here.
4. Install `com.binyanxu.gazeflow.neonxr-0.2.0-exp.1.tgz` (or its package.json from disk). Its manifest expects the development core and official Core 2.0.0 already installed.
5. Configure official mount calibration and its provider. Add **Neon XR Research Provider (Experimental)** to a GameObject. Assign `neon` to the official `NeonGazeDataProvider`, and `headReference` to the same tracked head/camera Transform used by the official gaze visualizer. Never substitute the XR rig root for the moving head.
6. Give `calibrationId` a versioned study identifier. Leave `mappingVerified` OFF until a known-target trial confirms the mapping for this mounting/setup. The flag records an operator declaration, not measured quality.
7. Add **Research Target** to each object; assign unique IDs and enabled non-trigger colliders. Child colliders can belong to a parent target. Trigger-only AOIs are not supported. Include intervening occluders in the collision mask: excluded layers cannot block the ray.
8. Add exactly one **Gaze Target Recorder**, assign its provider before Play, and configure distance/mask. Multiple enabled recorders are flagged and do not produce target assignments. Runtime provider reassignment requires disabling/re-enabling the recorder.
9. Open **Tools > GazeFlow > Research Target Setup (Experimental)** and check configuration. For a demo, import the **Three research targets** sample and choose **Tools > GazeFlow > Create Three-Target Demo**. It creates synthetic scene objects only, never synthetic research eye samples.
10. Enable the existing recording opt-in, confirm the participant, and run a short session. Mark/grab objects through the existing interaction adapter using the same stable IDs. Stop normally; open `Replay/study-replay.html`, inspect **Research target hits**, click a callback, mark the time and add a reflection/annotation. Export annotation JSON before closing.

Official setup: https://docs.pupil-labs.com/neon/neon-xr/neon-xr-core-package/

## Timing and interpretation

- `session_time_s` is Unity callback/logger time on the existing monotonic session clock. It is **not** Time Echo corrected device time and is independent of the HTML eye-trace time-basis selector.
- Official `GazeRay` is transformed by the configured tracked head reference, matching the official visualizer's coordinate handling. The current head pose is used; historical device-time pose interpolation is not implemented.
- The optional adapter reads the latest public RTSP snapshot for worn status/device timestamp/RTCP flag. The SDK does not expose an atomic ray-and-timestamp payload on its public provider event. These values are not guaranteed to be the exact snapshot used to construct the ray; export `device_time_semantics` explicitly records this limitation.
- Callbacks occur at Unity update rate and can coalesce native samples. They must not be counted as all native eye samples. Official simulation/no-live-client samples are rejected.
- Unverified mapping, missing head reference, not-worn, invalid/duplicate/out-of-order device times, non-finite rays, missing target IDs and stale data remain explicit statuses. A valid ray with no hit differs from invalid data.
- Summary visits are runs of valid same-target callbacks. Same-target covered time sums only consecutive valid, same-source, same-calibration, same-target pairs within 0.1 s. Target transitions receive no time attribution. No extrapolation is made before the first callback, after the last callback or across gaps. Interval selections are half-open and do not borrow pre-selection context. These conservative descriptive metrics are **not fixation counts/durations, attention, understanding or cognitive-state measures**.
- Coverage describes observed time between valid callbacks within the selected interval. It does not measure calibration accuracy or prove hardware synchronization. Target export includes rays/hit coordinates, IDs, source, calibration and timing semantics for inspection.

## Verification and remaining acceptance

Completed on 2026-11-09: core Runtime, Editor and Editor test sources statically compile with Unity 6000.3.23f1's Roslyn compiler and actual Unity assemblies; six exact-function metrics cases and DOM integration checks pass. DOM checks use inert canvas/media implementations and do **not** validate video playback or visual rendering.

Unity EditMode execution is currently blocked on this Mac by the Editor license reporting no UI/headless entitlement. Seven new Unity tests cover opt-in, occlusion, duplicate IDs, missing/invalid data, file closure/session isolation and invalid rays, but have not been executed. The official adapter has been checked against pinned upstream source; full Unity dependency resolution, adapter compilation and native-library loading remain pending. Browser automation launch is blocked by the current macOS process sandbox; the Playwright integration test is provided but not passed here.

Run in a licensed disposable Unity host before an experiment:

- Resolve official dependencies; compile core + optional adapter; run all EditMode tests, including original regression tests and component save/reopen checks.
- Confirm known targets at several head poses; check head-coordinate mapping and collider occlusion. Save calibration/validation evidence.
- Leave mapping unverified, remove head reference, and test off-head/no-data states; none may count as a target hit.
- Disconnect/reconnect Neon; verify explicit gaps, no sustained-hit inflation, new data recovery and device-time behavior.
- Compare target callbacks with raw eye counts and task markers over a task-length trial. Check timing differences rather than assuming hardware sync.
- Move the whole session folder to a second machine; review video, inspect target/event alignment, create point/interval annotations, export/re-import JSON and inspect selected CSV/bundle.

Research setup wizard, selectable raw-provider replacement, validated fixation/AOI algorithms, automatic video overlay, raw IMU ingestion, Neon Player round-trip import and measured synchronization accuracy are future work.
