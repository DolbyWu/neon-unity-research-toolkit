# AOI flow code check — 2026-11-13

Scope: independent experimental trial, never the original project/main/store submission or historical participant data. No upload.

## Findings and changes

- Validation markers own their materials and colliders; removing them does not remove gameplay objects. Marker colliders are disabled and they are not research AOIs.
- AOI ray queries still stop at the nearest solid non-target occluder. No gameplay collider/layer/physics changes were introduced for detection.
- Gaze target CSV remains open for the same session across recorder replacements. Scene/provider disable and stale input create explicit gap/exit states, not sustained target coverage.
- Found a real workflow risk: scene/head replacement intentionally cleared mapping approval but did not proactively open the review panel. It now opens automatically and warns that unapproved rays cannot become valid AOI hits. Raw gaze/video continue independently.
- Restored same-session measurement evidence remains distinct from a new measurement. New head bindings never inherit approval automatically. Restored rows explicitly identify prior-binding evidence and whether the current binding has been operator-reconfirmed.
- Approval now requires an explicit operator action, saved passing same-session report, matching calibration/head and fresh worn signal. It logs the confirmation and stage. A changed configuration requires revalidation; it is not silently assigned the old calibration label.
- Freshness monitoring now works outside an active validation run, including after evidence restoration. Restoring a report resets freshness. Not-worn input invalidates it.
- Computer review checkbox is reset on provider/head replacement.

## Automated verification

- Core tests: **45 passed, 0 failed** (`Artifacts/aoi-flow-regression-final-20261113.xml`).
- Trial binding/layout tests: **4 passed, 0 failed** (`Artifacts/aoi-binding-regression-final-20261113.xml`).
- Synthetic regression proves game collider and two valid target-hit rows survive validation-marker cleanup and appear in generated HTML.
- Restored evidence requires a new live sample, rejects another session, and cannot auto-approve the adapter. Explicit approval rejects missing freshness and changed calibration.
- Existing core occlusion, missingness, watchdog, session separation and identity tests remain passing.

These are synthetic tests, not headset acceptance.

## Real-device checklist — not yet completed

1. New local technical session; confirm recording consent and participant/session identity.
2. In first tutorial room: all five dots visible, one start click, automatic sequence; inspect actual saved report rather than assuming completion is passing.
3. Review and explicitly approve a passing mapping for the unchanged wearing/setup; otherwise correct setup and repeat.
4. Turn around (180 degrees) for the grabbing objectives; check raw gaze, target hits where configured and behavior events.
5. On LocomotionTutorial and Room 1 entry, review the automatically opened binding warning. Reconfirm only if actual head mapping and mount/setup remain correct; otherwise rerun validation. Until approval, AOI rows are intentionally mapping_unverified.
6. Known target, head motion and wall occlusion; brief stream disconnect/reconnect; no gap coverage accumulation.
7. Stop normally; check closed session, video/audio, gaze_targets.csv target IDs/names/groups and game events; open generated HTML to check AOI records, seeking, annotations and exports.

No new hardware pass or complete recording acceptance is claimed by this report.
