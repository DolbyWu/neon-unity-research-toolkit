# Official Neon XR first: recording without mandatory extra calibration

Local experimental update, 2026-11-14. This supersedes the previously mandatory five-point and three-object entry sequence. Original host project, historical data, main and store edition are unchanged. No participant data is uploaded.

## Default operation

1. Press Play and use the existing desktop participant archive/details form and recording consent. Its selected/suggested anonymous ID is the sole session identity. There is no second automatic numbering system or VR ID keypad.
2. Bind the actual tracked head to both the official visualizer and research provider. Use the official Neon XR mount configuration and gaze calculation.
3. Enter the original grabbing room directly. Tutorial narration waits while you optionally adjust gaze. Open **Tools > GazeFlow > Desktop Gaze Adjustment**, optionally adjust and **Save gaze adjustment**, hide the pointer, then click **Start tutorial** at the top of that same panel. Start does not require gaze, a correction or a passed check. The original intro delay, instructions, objectives and Next remain intact, followed by movement tutorial and main task. Legacy VR ID menu/welcome audio stay hidden. No extra gaze test or correction starts automatically; data/media recording are not paused by this narration-only wait.
4. Record official rays, ray/target queries, raw eye/pupil availability, head pose, behavior, media and archive/HTML using the existing pipeline. Live observations and later manual annotations remain available.

The provider converts the official `GazeDataProvider.GazeRay` through the actual head transform once. With no extra correction, the recorded world ray uses the same origin and direction calculation as the official visualizer. Optional fitted/manual corrections have explicit IDs; original official world origin/direction remain alongside them. Fitted corrections clear at a new trial scene-head binding. Desktop manual offsets may remain only within the same recording session and mount-configuration ID; accuracy approval always clears. A mismatched optional correction falls back to official rays without granting mapping approval.

## Validity is not accuracy

`ray_valid` means a usable fresh worn ray with finite nonzero geometry. It does not mean a fixation, attention, known spatial accuracy or successful verification. Missing head, not-worn, missing/new-packet checks and stale-stream watchdog still reject/expose unavailable data. Missing values are not valid zeros.

`mapping_verified` is separate and defaults to false. `spatial_accuracy_status` distinguishes not measured for this binding, checking, measured fail, aborted, measured pass awaiting approval and operator verified. Passing/approval is not fabricated when the optional check is skipped. Geometry status still independently distinguishes target hit, non-target collider, no hit and invalid data. `query_collider_path` describes the actual queried collider, not stable research identity.

Usable unverified rays now produce primary `gaze_targets.csv` rows and normal observed target enter/exit events. Existing historical candidate files remain unchanged; their prior exclusions are not retroactively relabeled.

## Optional tools

The live monitor and Live Observation Marker open automatically on Play, after the existing participant/recording gate. Opening the marker does not start an observation interval: click START, then END and write a note when desired. Pre-room Gaze Validation, Tutorial Object Calibration and gaze-pointer panels remain manual Tools-menu options; previously restored copies close on entry. The desktop participant/consent forms and an explicitly opened observation note are not treated as optional-panel clutter.

The observation marker uses a large, persistently yellow raised button with an interval timer. Its action changes from START OBSERVATION to END OBSERVATION to START NEXT OBSERVATION without graying the face. Before recording, RECORDING REQUIRED is visibly locked; reaching ten intervals prevents a new start but not ending the tenth interval. Notes still open separately after the end timestamp is saved. Button appearance is not an indication that data collection has started.

- Tools > GazeFlow > Gaze Pointer: display the official pointer for a visual check.
- Tools > GazeFlow > Desktop Gaze Adjustment: [computer-only optional local nudges, reset, research-ray preview and independent measurement](desktop-gaze-adjustment.md). Gaze Pointer menu entries now open this same panel; the displayed cursor follows the ray actually used by research AOI.
- Tools > GazeFlow > Pre-room Gaze Validation: opt-in measurement only.
- Tools > GazeFlow > Tutorial Object Calibration: opt-in extra correction and independent verification at the tutorial objects. Keep objects still; manual checks do not suspend original gameplay automatically.
- Use official Neon XR ray — no extra correction: stop optional measurement, clear extra correction and keep recording. It does not approve accuracy.
- Tools > GazeFlow > Live Observation Marker: unchanged start/end interval and notes.

If accuracy is poor, inspect official mount settings, head binding, actual target geometry and occlusion. Do not increase colliders, turn off walls or silently loosen verification thresholds to manufacture a pass.

## Offline HTML and export

The default **All usable ray observations** view includes usable rays without extra QA. **Verified / accepted AOI only** counts only explicitly verified usable rows. Both views retain the conservative consecutive-callback rule: gaps over 0.1 seconds, invalid data and changes of source, calibration, correction or verification quality break covered time. These are descriptive ray-hit observations, not fixations or inferred cognition.

Selected CSV respects the chosen view. Interval JSON retains complete original rows and separately filtered rows plus the quality selection. Unverified rows cannot enter verified-only statistics. Legacy rows without explicit verification metadata are not upgraded to verified. Video, timeline, annotation JSON/CSV save/resume and behavioral filtering remain available. Official WS device timestamps remain unavailable in the pinned SDK path; Unity callback time is not hardware synchronization.

## Cross-scene AOI continuity

The isolated host keeps one dedicated session recorder across its tutorial, movement and main-task scenes. Initialization reloads reuse it and disable only recognized duplicate initialization recorders at runtime. Each scene resolves the surviving official provider and binds the actual new head. Late source assignments and source replacement update subscriptions; old-source callbacks and repeated binding do not duplicate hits. Unrelated/ambiguous recorders produce an explicit error rather than being deleted.

Rebinding records a waiting/gap status and ends the prior target interval. It never replays a cached ray, approves mapping or starts a session. Freshness, invalid-ray and occlusion checks remain unchanged. Files from past sessions are not rewritten.

## Local software checks

- Core synthetic tests: 70 passed, including late assignment, source rebind, disable/re-enable, duplicate-callback avoidance, session isolation, observation-control labels/timer guards and bounded manual-offset math/provenance.
- Independent host targeted integration tests cover official-source replacement, recorder reuse, direct grabbing, optional-panel behavior and local desktop adjustment/preview/export/approval isolation. See the current local check report for the result count.
- Browser: observed/verified separation, filters, seek-to-annotation, selected CSV, complete/filtered interval bundle, invalid intervals, legacy session and no network requests passed.
- A new wearer/device run is still required. These checks are not proof of spatial accuracy or full-scene AOI acceptance.
