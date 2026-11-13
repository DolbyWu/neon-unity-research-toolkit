# Automatic five-point validation

**Latest branch follow-up:** automatic two-round progression is now enabled in the trial. After round one, sufficient saved per-point coverage/direction evidence triggers fitting and a 3-second continuously fresh-signal countdown into round two, without another fit/point click. Insufficient evidence displays the exact blocked points and **Retry round 1 — all five points**. Stream loss or long updates restart the countdown; abort, skip or changed session/head/calibration cancels automatic start. Operator approval remains separate. This is a follow-up commit on the timestamped research branch; see `automatic-two-round-transition.md` for checks and limits.

**2026-11-13 update:** the two-round flow now uses the **same fixed world target centres**, including the exact centre, with NEW measurement samples in round two. It no longer deliberately offsets the second-round pattern or reanchors it when the wearer moves their head. Repositioning is available only before the first round; to change the layout discard the correction and collect again. Reports include `target_geometry_id` and `geometry_policy`. This repeat-layout validation is narrower than a held-out-position/generalization test and does not establish accuracy across the entire scene.

The trial default project tolerance is now **3 degrees mean error per target**, with **70% valid coverage still required**. Neither the old 2-degree starting cutoff nor the new 3-degree policy is a mandatory Pupil Labs acceptance standard. Raising the cutoff does not improve measured precision. At two metres, 3 degrees corresponds to about 10.5 cm lateral separation, so small or neighboring AOIs remain sensitive to error. Historical reports retain their original thresholds and results. Fresh unverified geometric observations remain separately labeled low precision; invalid/stale samples never become valid zeroes.

Latest fitting behavior supersedes earlier statements below that no correction is fitted: the explicit fit action applies a bounded head-local direction correction after round one, then collects new round-two samples. A passing report and explicit approval remain necessary for accepted AOI hits; failed checks do not block gameplay or separately labeled low-precision exports.

**Latest layout:** five-point validation is in front; the grabbing tutorial is held hidden throughout validation (including abort/retry), then appears **180 degrees behind the entry heading**. Turn around, not right. This supersedes the earlier 90-degree right-side layout described below. Four updated synthetic layout/visibility checks passed in the open trial Editor; the new layout still needs headset confirmation. Rendering visibility is restored after the check; colliders and original grab objectives are not altered.

Local experimental update, 2026-11-13. Applies to the independent trial that references this package checkout. No original project, participant recording, `main` or store submission is changed. This includes mapping **validation** and optional bounded head-local direction fitting; it is not a validated scientific protocol or a replacement for official mount calibration.

## Operator and wearer workflow

1. Start the independent trial and complete participant/recording confirmation. Play enters the first tutorial room, even if Room 1 was open in the Editor. The automatic sequence is now **five-point validation → grabbing tutorial → LocomotionTutorial → Room 1**.
2. The **Gaze validation** computer panel opens automatically. The trial binds the official provider to this scene's actual tracked head and uses a hash of the local mount configuration as a version label (not a claim of calibration accuracy). **Automatic five points** is enabled.
3. The system waits for a live worn signal, then shows five fixed dots in front: centre, left, right, up and down. The moving gaze cursor stays hidden. There is no extra **Show five targets** click at this automatic first-room entry. Use that button only for a later retry.
4. Ask the wearer to confirm the dots are visible and that they are ready. If placement is unsuitable, use **Reposition the unmeasured preview** before starting. Do not start with unseen/occluded targets.
5. Click **one point on the computer panel's five-point map**. That selects the first target and starts the entire automatic sequence. The other targets then follow in authored order (centre, left, right, up, down, excluding the selected first point).
6. The selected point pulses by scaling around its fixed centre. The wearer looks at its centre. Default timing: **1 second settling + 2 seconds measured gaze**. It then disappears. Remaining dots stay static.
7. After a **2-second gap**, the next point pulses automatically. No further point-confirmation clicks are required. After round one, the automatic flow checks saved collection quality. If sufficient, it fits correction and starts a 3-second fresh-signal countdown into round two at the SAME fixed target centres. Otherwise it names the blocked points and waits for an explicit retry or skip. After completed round two, the grabbing tutorial activates **180 degrees behind** the captured entry heading, pass or fail. Turn around to practise grabbing. The station stays world-fixed instead of following the head back over the test. Original narration, grab objectives, Next gate and later scene transitions are retained.

An aborted check does not silently advance: retry, or explicitly choose **Skip validation and continue to grabbing (AOI stays unverified)**. A completed failed measurement may continue to the tutorial but is still failed/unapproved. A skipped check cannot be counted as a pass. After a scene/head-provider replacement the same-session saved report can be restored **for review only**; operator approval is cleared. No approval is automatically transferred to a new tracked-head binding. Repeat validation if placement/mount changes or accuracy is uncertain.

The point does not move/jump in space while pulsing. This avoids changing the reference direction used for angular error. Only the measured part contributes samples and coverage; settling and inter-target gaps are not measurement time. Actual timing follows Unity's frame clock, so intervals can be longer by a frame or under load. Late frames do not fast-forward through unseen targets.

Abort if a target cannot be seen or the wearer is unable to follow. Missing/stale/unworn samples remain invalid; automatic advancement never creates valid zero-error samples. Finishing the sequence is not a pass. Each target must meet the configured error/coverage policy. **Approve this mapping for AOI** remains a separate human action after a saved passing report; fitting alone does not approve mapping. New second-round measurement samples are required.

## Optional manual mode

Disable **Automatic five points** to retain the earlier per-target confirmation workflow, with optional nine targets. Advanced settings expose target distance, settling/measurement/gap durations and engineering thresholds. Defaults are starting values for testing, not validated scientific cutoffs.

## Traceability

Each run still writes a new immutable `Calibration/<verification-id>/validation.json` and `validation_samples.csv`. Existing files and schemas remain readable. Additive report fields include presentation mode, selected target order, gap duration, sequence ordinal, start method and an explicit abort reason.

Only the first automatic target is marked operator-confirmed; later targets are explicitly marked automatic. The software does not claim that each later target's visibility was individually confirmed. Target start/end and gap events are recorded separately. Reports preserve missing error as null, not zero. Existing AOI/Replay/sample-builder paths continue to use the verification ID and quality status; this presentation change does not alter raw gaze or physical time alignment.

## Checks and limitations

The sequence has deterministic synthetic timing tests for arbitrary first-point order, exactly-once completion, two-second gaps, invalid parameters, no restart and no late-frame skipping. A Unity synthetic runtime test checks all five actual markers appear, centres do not move, completed targets hide, gaps produce no measurement rows, later targets need no new clicks and a run with no measurement samples cannot pass.

Local checks on 2026-11-13: independent trial import/compilation succeeded; 43 core tests and 3 trial layout tests passed. Layout checks cover right-side placement, head-pitch independence and deterministic fallback heading. These checks do not exercise real tracking, scene transitions or recording.

Real Neon + Quest first-room visibility, station reachability, pulse comfort, timing and end-to-end recording still require a fresh device test. This update has not been pushed or released automatically.
