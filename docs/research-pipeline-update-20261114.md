# Research pipeline update — 2026-11-14

Branch: `feature/research-pipeline-20261113-1338-est`. Package version remains `0.2.0-exp.1`; identify this update by its commit. This is not a new signed release or Asset Store submission, and it does not update `main`.

## Current operator workflow

1. Press Play in the independent study copy. Confirm the existing participant archive ID and recording consent; there is no second VR ID keypad or welcome narration.
2. The original grabbing station appears, but tutorial narration waits. Configured eye/pupil, head, behavior, video and microphone channels continue recording during this preparation.
3. Open **Tools > GazeFlow > Desktop Gaze Adjustment**. Optionally show the cyan gaze pointer. The integrated first room supplies a fixed yellow 32 mm disc on the lower visible solid surface of its uniquely identified chess object. Ask the wearer to look at yellow, not chase cyan.
4. Optionally nudge **LEFT / RIGHT / UP / DOWN** in tracked-head-local degrees. Click **Save gaze adjustment** and read the confirmation. Then **Hide gaze pointer**. Save is an operator acknowledgement, not measured spatial approval; resetting or changing the adjustment/binding/session needs a new confirmation.
5. Click **Start tutorial** at the top of that same panel. The original intro delay and narration begin, followed by actual-grab objectives, original Next, movement tutorial and main task. Adjustment and extra accuracy checks can be skipped; Start does not require live gaze or an accuracy pass. The yellow reference hides after Start and during optional measurements.
6. Use the bright yellow **Live Observation Marker** during the task: start, end, then write the observer note. Recording and observation timing are separate controls.
7. Stop normally and inspect the closed session's files and offline HTML. Review video/time intervals with the participant, add explanations/labels and optional scores, then export annotation JSON to save/resume and CSV for analysis.

The yellow disc is a visual-only host integration aid, not a portable scene installer. It has no collider/Rigidbody/AOI, does not follow the head and does not change gameplay geometry or names. A missing/occluded lower surface produces a status warning, not a through-wall fallback. See [desktop controls and saved evidence](desktop-gaze-adjustment.md).

## Recording and quality

- Official Neon XR mapping/reception remains primary. Usable rays and observed object hits are recorded without mandatory extra fitting or a passed five-point check. Invalid, not-worn, stale and disconnected data remain explicitly unavailable.
- A persistent target recorder and refreshed provider/source subscriptions preserve recording across known scene-head replacements without duplicate listeners or cached-ray replay.
- Manual offsets may carry only within the same session/configuration across known head replacements. Mapping approval and save confirmation do not carry to a new binding; participants/sessions do not inherit a prior person's adjustment.
- `gaze_targets.csv` retains raw official versus adjusted rays, correction IDs, target names/IDs/groups, geometry/collider context and separate mapping-quality flags. Save receipts and correction revisions are additive local files.
- HTML offers observed versus verified-only statistics. Save, visual alignment and ray hits do not establish fixation, attention, cognition or full-scene spatial accuracy. Invalid data and callback gaps over 0.1 seconds never add covered time.

## Replay presentation and labels

- **Research target hits · experimental** starts collapsed and has a native disclosure triangle; its expanded statistics table scrolls internally.
- Observation review/play controls and saved annotation time/seek buttons use the same yellow as the live observer control. Local video pause, scrub and bounded interval replay remain available.
- New optional participant intensity uses **1–10 integer buttons**, with no default; clear saves missing, not 1 or 0. JSON/CSV and curated outputs preserve `intensity_scale` and `intensity_input_method`.
- Imported legacy 0–10 values, including zero/decimals, remain unchanged. Revisions retain previous values and scale. The separate 1–5 retrospective confusion measure is not converted or merged.
- Export JSON before closing/reloading an edited page. Old HTML is a snapshot: preserve it and saved annotation JSON, generate a new derived HTML for the same session, then import that JSON. No raw files are overwritten by the offline page.

Number buttons do not automatically make ratings or machine-learning predictions more accurate. Missingness, question/scale consistency, independent coding and participant/session-aware validation remain essential.

## Software evidence and remaining checks

| Check | Result |
|---|---|
| Isolated Unity core EditMode suite | PASS, 70/70 |
| Targeted integrated-host EditMode suite | PASS, 42/42 |
| Existing authored chess prefab lower-surface placement, no collider edits | PASS, included above |
| Browser target metrics/filter/CSV/bundle checks | PASS |
| Browser annotation/rating/history/JSON/CSV checks | PASS |
| Synthetic local video pause/scrub and consecutive/observer-interval replay | PASS |
| Offline reviewed-sample builder | PASS, 11/11 |
| New wearer/device visual appearance, manual alignment and narration timing | NOT TESTED by these automated checks |
| Full-scene measured spatial accuracy of this update | NOT ESTABLISHED |

Existing successful device sessions are not relabeled as acceptance of new code. Refer to [current default behavior](official-first-workflow.md), [replay presentation checks](replay-presentation-checks-20261114.md) and the [beginner tutorial](research-pipeline-tutorial.md). Older experimental fitting workflows remain optional source, explicitly superseded as the default.

## Distribution boundary

The portable packages contain generic recording/review and Neon XR adapter code. `Integrations~/ExistingStudyTrial` contains source-only host changes and narrow baseline patches, not the authored study, scenes, prefabs, media, complete target catalogue or live device configuration. Review its dependencies and exact-path safety guards before deliberate integration into another saved project copy.

No participant archives, raw eye/head/event data, video/audio, transcripts, annotations, live mount/device settings, network addresses or API keys accompany this update. Original project and Unity submission edition remain unchanged. No cloud transcription feature or new hardware pass is claimed.
