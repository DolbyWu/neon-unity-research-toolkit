# Desktop gaze adjustment (experimental)

This optional Unity Editor tool keeps official Neon XR reception/mapping and adds a reversible local direction adjustment for research rays. It is **not** the official Companion Offset Correction, a replacement for mount calibration, a fixation detector or an automatic accuracy guarantee. The phone still streams; no phone interaction is required for these local controls.

## Open and use

1. Work in the isolated study copy, not the original or store edition. Press Play, confirm the archive participant ID and recording consent normally.
2. Open **Tools > GazeFlow > Desktop Gaze Adjustment**. The study copy's existing **Tools > GazeFlow > Gaze Pointer** entry opens the same tool. It never opens automatically on Play.
3. Choose **Show gaze pointer**. The cyan cursor displays the same callback ray used by research AOI logging. In the integrated host's first tutorial room, a fixed **yellow 32 mm disc** appears on the lower visible solid surface of the uniquely bound chess object during preparation. Ask the wearer to keep looking at the yellow centre, **not** to follow the cyan cursor. The yellow reference stays anchored to the object rather than following head motion, is depth-occluded normally, has no collider/Rigidbody/AOI, and does not approve mapping. It hides during an optional accuracy check and after **Start tutorial**. If no unobstructed lower surface is found, the panel reports that instead of moving geometry or drawing through walls.
4. Choose a step (default 0.10 degrees) and click **LEFT / RIGHT / UP / DOWN**. Positive horizontal moves gaze right; positive vertical moves it up, in tracked-head-local axes. A new immutable correction revision is saved per click. No target, head pose or collider is moved.
5. Click **Save gaze adjustment**. The panel shows **Adjustment saved — you can now hide the gaze pointer.** This writes a separate immutable operator-confirmation receipt, referencing the selected correction. Repeated clicks for the unchanged binding return the same receipt. This is not a measured accuracy pass and does not hide the cursor or approve AOI quality.
6. Click **Hide gaze pointer** when ready. In the integrated study's first tutorial room, click **Start tutorial** at the top of this panel to begin original tutorial narration. Adjustment/checks are optional: Start also works without them and without a live gaze signal. Tutorial narration waits for this explicit click; eye/AOI, video and microphone recording continue when enabled. No new popup is opened. Preparation grabs remain raw behavior records but do not prematurely complete teaching objectives.
7. **Reset - use official ray without extra correction** removes the local correction immediately, cancels an active check and clears accuracy approval. Prior files remain intact. It does not undo any offset already applied on the phone.

The saved confirmation is invalidated after another nudge, reset, restored model, head rebind, configuration or session change. Hiding/showing the cursor does not invalidate it. Each nudge's correction JSON is already persisted for provenance; the Save button adds an explicit operator acknowledgement for the current selection. It does not change the mount configuration or prove spatial accuracy.

The combined rotation is bounded to 12 degrees as an engineering safety limit, not a scientific acceptance criterion. Different local fitted/manual corrections are not stacked. Reset a fitted model before using arrows. The exact incoming official ray is preserved alongside the adjusted ray; this baseline already includes any existing official mount or phone-side processing, not unprocessed sensor truth.

## Measure separately, without automatic fitting

**Show five check targets - no automatic fitting** begins one optional fresh-sample measurement. Wait for live worn gaze, ask the wearer whether all five dots are visible, then click **Targets visible - start one five-point check**. Five 32 mm dots are anchored at fixed world positions in front of the current head when valid input arrives. They are not AOIs or colliders. The cursor is hidden during the check so it is not another object for the wearer to follow.

Defaults are 1 second settling and 2 seconds measuring per point, with 2-second gaps. The check reports per-point mean angular error, p95 and conservative valid coverage. Existing project tolerances (3 degrees mean, 70% coverage by default) remain explicit engineering choices, not a Neon-certified standard. This does not establish full-scene/depth accuracy or fixation/attention.

There is no automatic second fitting round, tutorial hold or automatic approval. An aborted/failed/skipped check never stops usable gaze/AOI, media or the task. An explicit review checkbox plus **Approve measured mapping for this head binding** is required after a saved passing check and fresh worn signal. A manual revision, reset, head binding, configuration change or session change clears prior approval. A restored manual model needs a NEW check through this tool before approval.

## Preview versus recording

- Preview and AOI share the research callback ray, nearest-collider query, recorder collision mask, maximum distance and freshness policy. A non-target wall blocks the target behind it. Geometry/identity errors do not become accepted AOI.
- With no collider, the cursor may appear at a 2 m visual fallback distance. That is display only, not a recorded target hit. Invalid/non-worn/stale samples hide the cursor; they are never filled with a valid zero ray.
- The preview owns a temporary cyan sphere with its collider disabled on Ignore Raycast. It never invokes gameplay hit callbacks, writes target rows or starts/stops recording. Hiding/closing display controls does not reset the correction.
- The official visualizer's visible cursor/ray is hidden when this preview is used, to avoid confusing raw and adjusted cursors. Official raycast/gameplay callbacks are otherwise left unchanged. A separate gameplay gaze-interaction system is not silently recalibrated by this research tool.

## Files and session isolation

- `Calibration/manual-offset-<id>/correction.json`: kind, horizontal/vertical degrees, bounded quaternion, session/configuration identity, apply time and method/limitations. Each adjustment gets a new ID; no older revision is overwritten.
- `Calibration/gaze-confirmations/gaze-save-<id>.json`: operator save receipt, exact correction reference (or official/no-extra-correction mode), head/session/configuration, angles, save time and explicit non-approval limitation. `game_events.csv` records `gaze_adjustment_saved`. Neither the receipt nor the button grants accuracy approval or starts/stops recording.
- Host tutorial events distinguish `tutorial_preparation_waiting`, `tutorial_start_requested` and `grabbing_tutorial_started`. The original intro delay begins after Start, not during preparation; completing the original actual-grab objectives still unlocks Next.
- `game_events.csv`: `gaze_manual_offset_applied`, head-rebind provenance and `official_gaze_mode_selected` reset events. Adjustment/reset boundaries also write explicit waiting/gap rows, not cached hits.
- `gaze_targets.csv`: `correction_id`, adjusted `ray_*`, unmodified official `raw_ray_*` and separate mapping-quality fields. Existing raw Neon payload/eye/pupil streams, head poses, archive layout, video/audio and HTML/annotation exports remain unchanged.
- The HTML can review the existing ray rows and associated events. Keep the complete session folder, including `Calibration/`, with exported data to retain model provenance.

In the integrated host, a manual head-local adjustment is retained across its known scene-head replacements **only in the same recording session and mount-configuration ID**. Approval is cleared and the previous report is marked as prior-binding evidence. A new session or configuration does not inherit the offset. The software cannot detect re-wearing or phone-side offset edits: explicitly reset/recheck after either, and use a new session when changing participants. No phone profile, official package file, persistent mount configuration or historical participant file is edited by this tool.

## Local verification

Synthetic checks cover signs/limits, immutable revisions, raw versus adjusted exports, occlusion, independent approval, reset/restore, source replacement, scene/session isolation and preview freshness/cleanup. Local results on 2026-11-14: 70/70 isolated core tests and 42/42 integrated-host targeted tests passed, including explicit save receipts, manual tutorial narration start and the new lower reference disc. An instantiated copy of the existing authored chess prefab also passed the lower-visible-surface/no-collider-edit check. These are not real-device spatial-accuracy, visual-placement or headset-audio acceptance. A new Neon + Quest run is required to confirm the perceived arrow direction, cursor alignment, cue visibility, scene continuation, narration timing and measured errors in the current wearing setup. No new device recording was started during implementation.
