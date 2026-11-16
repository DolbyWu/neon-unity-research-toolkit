# Local scene-continuity and entry UI checks — 2026-11-14

Follow-up preference: Live Observation Marker now opens automatically alongside the monitor. The original monitor-only result below records the earlier UI preference, superseded by the current [workflow](official-first-workflow.md). Observation intervals still require explicit start/end clicks; calibration remains optional.

Experimental source update only; no publication, original-project edit or historical-data migration.

## Implemented

- One dedicated session target recorder persists across host scene transitions. Reloaded initialization scenes reuse it; recognized duplicates are disabled at runtime, not deleted. Unknown/ambiguous recorder configurations report an error.
- Core recorder follows late assignment/replacement of its provider. Official adapter follows replacement/re-enable of its official source. Repeated binding does not add duplicate listeners; detached sources cannot produce new accepted callbacks.
- Rebinding ends the prior target interval and writes a waiting/gap boundary. No cached ray is replayed; no accuracy approval or recording session is created by binding.
- The desktop participant archive/details selection is the sole participant ID. No separate auto-numbering system was added. Existing participant confirmation and media consent are preserved.
- Legacy VR ID menu/keypad and welcome audio are hidden at tutorial scene load. Original grabbing instructions, objectives, Next and movement/main-task sequence remain.
- Only the live monitor opens automatically on Play. Observation Marker, Pre-room Gaze Validation, Tutorial Object Calibration and pointer windows remain manual Tools-menu options. Participant/consent forms and explicit observation-note editing are excluded from cleanup.

## Results

| Check | Result |
|---|---|
| Isolated core EditMode suite | PASS: 67/67 |
| Host targeted integration checks | PASS: 22/22 |
| HTML target review/filter/seek/annotation/CSV/bundle checks | PASS |
| Documentation check | PASS: 33 tracked Markdown files, 59 local links |
| Unity host compilation and post-import test execution | PASS |
| New real-device cross-scene recording after this change | NOT TESTED |
| New wearer entry/audio/panel check after this change | NOT TESTED |

Synthetic lifecycle/event tests exercise recorder reuse, a destroyed old provider, replacement-source subscriptions, duplicate listeners, disable/re-enable and session isolation. They do not establish real-device continuity, spatial accuracy or headset visibility.

## Short device recheck

1. Press Play, select/confirm the anonymous archive participant ID and recording consent. Confirm there is no VR ID keypad or legacy welcome audio and only the live monitor opens automatically.
2. Complete grabbing and movement, enter the main task, and look at/interact with several configured targets in each scene.
3. End normally. Confirm `gaze_targets.csv` continues after the main-task phase/head binding, target identities match those objects, stale intervals are gaps, and HTML/media/archive remain finalized and aligned within their documented time-source limits.
4. Open optional observation/calibration tools manually only if desired. Skipping optional spatial QA must retain usable official rays without relabeling them verified.

All recorded participant material stays local. This update has not been committed or pushed.
