# One yellow-dot baseline, three object captures and NEW object verification

**Superseded as the default entry:** see [official-first workflow](official-first-workflow.md). The following describes the earlier experimental opt-in sequence, not a required recording or tutorial gate. Extra correction is now off by default, and failed/unperformed spatial QA does not invalidate usable official rays.

**Hierarchy organization:** generated cues are nested under `[GazeFlow] Calibration cues (temporary)` and are cleaned after each check, including owned resources and orphan Editor tests. This parent stays world-fixed rather than following the head. Older generated previews in the trial's unsaved working scene were grouped into `[GazeFlow] Archived test previews (inactive)` with Undo support; no gameplay object or scene asset was deleted/saved.

**Latest visual choice:** banana visuals have been disabled in the integrated trial. Both baseline dots and object cues are yellow spheres with a **32 mm base diameter**, and their existing pulse scales proportionally. Reference positions, one-baseline/three-object flow and acceptance policies are unchanged. The banana implementation remains optional unused source, not the active test appearance.

Local experimental update, 2026-11-14. This supersedes the integrated trial's earlier automatic two-five-point sequence. Original project, historical research data, `main` and store edition are unchanged. No hardware recording or upload is started by this update.

**Visibility feedback update:** authored-object yellow cues now have a base diameter of **32 mm**, four times the original 8 mm; their pulsing scale is increased by the same factor. The fixed aim points, collider queries, angular/coverage/hit-fraction policies and object identities are unchanged. Banana cues now have a stronger crescent curve, a narrow brown stem and a dark end tip rendered with separate material sections (not unsupported vertex-color assumptions). Their reference centre remains fixed. The 62 core synthetic tests passed after these visual changes; headset readability still needs a new wearer check.

## Operator workflow

1. Start the independent trial with the usual participant and media consent. The first tutorial room shows **five yellow round dots, 32 mm base diameter**. Look at each dot's centre. Select ONE starting point on the computer. The five baseline measurements advance automatically. This is one baseline run, not a fitting round followed by another five-point run.
2. The original tutorial station appears on the opposite side. Turn around. The three calibration roles are **Chess → Card → Box**. Default actual scene owners are `ChessPiece`, the flat teaching object `Visuals` under `Left Diary - Tutorial`, and `BoxLid`. The panel shows full object names/IDs; bindings can be inspected/changed explicitly if the intended card is a different object. Existing objects are not renamed or replaced.
3. **ROUND 1:** the player looks at the small cue on each named real object. The researcher clicks **Capture [object] — wearer is looking at the cue** once per object. Default collection is 1 second settling plus 2 seconds measurement. Do not grab/move the objects yet. Each capture still checks freshness; a click alone is not data.
4. After all three, sufficient finite head-local directions and valid coverage allow an explicit **yaw/pitch-only** correction, with roll fixed at zero. Keep the head reasonably steady and use eye movement between the cues. This avoids estimating roll from the tutorial's narrow target pattern. Equal-target-weight chord error is minimized; no origin/depth/time correction is fitted. Directions must remain in the forward head-local field and span at least 1 degree. The default collection policy remains 70% coverage and at least 12 valid direction pairs per target. Corrections over 12 degrees are rejected, not clamped. Earlier files remain saved on retry. The separate five-point/full-rotation fitter is unchanged.
5. **ROUND 2:** after 3 continuously fresh seconds, the same real-object aim points pulse in sequence automatically, starting at Chess. The player looks again at Chess, Card, Box. No additional capture clicks are required. Missing/stale input resets the countdown; moved objects/changed context abort the check.
6. Review the NEW verification. If it passes, click **Approve measured mapping & continue tutorial**. This is a final operator review, not another capture round. Alternatively explicitly continue without approved AOI; raw and clearly labeled low-precision records remain separate.
7. Original grabbing narration/objectives/Next resume after approval or explicit skip. Then continue to the original movement tutorial and main task. The workflow never fabricates grab completion.

Open the desktop panel using **Tools > GazeFlow > Tutorial Object Calibration**. The old five-point panel redirects to it once the authored-object phase begins.

The panel's latest measured ray shows query status and the collider hierarchy path (with sibling indices). New validation sample CSVs append `query_collider_path`. This identifies non-target obstructions without disabling collisions or expanding AOIs. Paths describe the hit collider, not stable research identity. Historical files are not rewritten. No new live acceptance is implied by the 65 passing core synthetic checks.

## What the verification actually checks

The second round uses NEW samples with the applied correction, not fitting samples replayed as if independently measured. Every target must meet the configured mean angular error and coverage policy. In addition, at least the configured fraction of valid ray-query callbacks must hit the expected stable target ID (default **80%**). The query is the SAME nearest-collider/explicit-trigger policy used by the target logger, with the actual recorder's collision mask and range. Front occlusion, duplicate identity, no-hit and wrong-target observations cannot count as correct hits.

The 3-degree/70%/80% values are configurable engineering policies, not universal scientific standards. Callback match fraction is not fixation accuracy or a guaranteed confidence interval. Only three objects/positions and the current head binding are tested; this does not establish accuracy for every object, depth or later scene. Later head/scene changes still require explicit mapping review.

No gameplay collider, rigidbody, object name or physical interaction is changed to obtain a hit. The banana/cue colliders are disabled. Small aim cues identify fixed known points; their appearance does not enlarge accepted AOIs. Original raw gaze remains unchanged.

## Saved evidence and HTML

Each baseline/collection/verification has its own `Calibration/<id>/validation.json` and `validation_samples.csv`; fitted correction has a separate `correction.json`. Target results include expected names, IDs/groups, mean/p95 error, coverage, valid target-query checks, correct hits/fraction and a stated limited verification scope. Validation sample CSV adds expected/observed IDs, query status and hit geometry. Invalid fields stay missing.

New HTML reports show the authored object names, IDs, correct-hit fraction and limited scope alongside validation results. Existing video, timeline, live observations, manual annotations and accepted/unverified target separation remain. Accepted AOI statistics still require actual operator approval and valid hit records; neither completion nor a saved correction alone fabricates accepted rows.

## Software checks

- **62 core tests passed**, including finite banana geometry/disabled cue colliders, bounded explicit three-target fit and a test where zero angular error plus full coverage STILL fails because a front occluder prevents the correct target hit.
- **17 trial integration checks passed**, including three captures/fitting/automatic verification/explicit approval, quality rejection/skip, identity/motion rejection, tutorial hold behavior and real-scene enabled collider-owner bindings. These checks do not enter Play or start hardware/media recording.
- Existing real-browser target/evidence suites passed. Fresh Neon + Quest testing remains necessary for target visibility, the correct flat teaching object, station stability and end-to-end capture/replay.

The source-only host integration reference is kept separately from portable packages. This local change has not automatically been pushed or signed.
