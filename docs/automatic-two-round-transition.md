# Automatic two-round transition — local follow-up, 2026-11-13

This implementation follows the initial Research Pipeline snapshot and is published as a follow-up commit on `feature/research-pipeline-20261113-1338-est`. It changes the experimental package checkout and independent trial integration only, not the original project, historical recordings, `main` or submitted store package. It is not a new signed release or hardware acceptance result.

## Researcher workflow

1. Start a consented session as before. Confirm all five dots are visible and choose ONE starting point for round one.
2. After all five, the provider checks saved collection evidence. Each point needs the configured valid coverage (default 70%), at least 12 valid head-local direction pairs and finite non-degenerate directions. High angular error before correction does not itself prohibit fitting.
3. If sufficient, fit once automatically and show the SAME fixed world dots. A 3-second continuously fresh-signal countdown starts round two from the centre; no fit button or second point click is needed.
4. If insufficient, **ROUND 2 NOT STARTED** lists the exact point names, coverage and direction problems. Check wearing/connection, then **Retry round 1 — all five points**. Separate attempts are retained; successful points from older runs are not spliced into a new report.
5. Round-two completion allows the integrated trial to continue to grabbing, pass or fail. AOI approval remains a separate review/action; a failed or skipped check is not promoted to accepted hits. Raw and clearly labeled low-precision records remain independent.

Actual UI uses spaced labels such as **ROUND 2 NOT STARTED** and **Retry round 1 — all five points**. Manual mode retains explicit fit/visibility controls. **Pause automatic round-two transition**, Abort and the explicit Skip option remain available. Stop if the wearer cannot see the targets; automatic progression is not a new visibility confirmation.

## Safeguards and traceability

- Missing/stale/not-worn signal or an update gap over 0.25 seconds resets the countdown. No old ray or absent sample counts as new validation data.
- Abort, Skip, disabled automatic flow, or changed session/head/calibration/correction cancels automatic progression.
- Restored reports do not start fitting automatically. Missing saved round-one evidence or a fitting/save error is shown rather than silently advancing.
- Round two must reuse the first round's world geometry and receive fresh corrected samples before automatic start.
- Events record blocked fitting, queued countdown and automatic start. Target results say `automatic_verification_after_countdown` and do NOT claim `operator_confirmed_visible` for automatic round-two points.
- No runtime/headset recording is started by software checks, and no historical report is rewritten.

## Verification

- Isolated Unity core suite: **58 passed, 0 failed**. Includes five new readiness tests and a runtime automatic-start provenance test.
- Actual independent-trial EditMode subset: **12 passed, 0 failed**, comprising nine automatic-transition tests plus existing correction/binding checks. Tests cover insufficient coverage, adequate collection despite high raw error, fitting once, continuous countdown, signal-loss reset, cancellation/abort/skip, changed context and restored/manual safeguards.
- Test inputs are synthetic; no hardware/media acquisition is started. XML/logs remain in ignored local `Artifacts/`.
- A fresh real Neon + Quest run is still needed for presentation comfort/visibility and end-to-end acceptance. Do not treat software checks as measured full-scene eye-tracking accuracy.
