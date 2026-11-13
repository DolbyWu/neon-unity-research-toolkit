# Live observer intervals and retrospective review

Local experimental update, 2026-11-13. Only the independent trial and experimental package checkout are changed. No participant recordings are rewritten or uploaded.

## During play

The **Observation Marker** desktop Editor window opens on Play. Reopen it using **Tools > GazeFlow > Live Observation Marker**. It does not start recording; first finish the normal participant and recording confirmation. This interface is Editor-only, not a headset overlay or a standalone-build interface.

1. Enter an anonymous observer ID.
2. Click **START OBSERVATION** when an episode begins.
3. Click the same button, now **END OBSERVATION & WRITE NOTE**, when it ends. The end timestamp is fixed immediately.
4. The **Observer note** popup opens. Enter an observation label and notes, then **Save observer note**. Typing is not included in the interval and does not pause the game. Prefer visible behavior plus explicitly provisional interpretation, not an asserted cognitive diagnosis.
5. Repeat, up to **10 intervals per recording session**. Earlier completed notes can be edited during recording. Closing the popup without saving leaves the interval saved; use its Write / edit button later. Save notes before stopping Play. The popup cannot save after the recording has ended.

Session-root files: `live_observations.json` and `live_observations.csv`. Starts, ends and note saves also produce game events. An unfinished interval is closed as `interrupted`, with `session_ended_before_second_click`, not a normal operator-ended observation. New sessions have independent lists. Observation recording does not require gaze mapping to pass.

## After play

New HTML reports include **Live researcher observations** inside Manual annotations, immediately below eye data. Orange timeline marks show intervals. Original observer notes are retained separately from subsequent participant responses.

- **Review segment / add player response** selects its experiment-time range and seeks the video if that time is covered. Ask what happened and what the player was thinking. Enter their explanation and an optional integer **confusion rating 1–5** (1 not confused; 5 very confused), then **Add annotation**. This creates a separate retrospective-interview annotation linked by `related_observation_id`.
- **Play this interval** plays from the start and pauses at the end. Missing video coverage is not filled with invented footage. Existing video-offset limitations still apply: software launch timing is not hardware synchronization.
- **Export annotations JSON** preserves the annotation list, rating, observation link, revisions and embedded original observer intervals. Import this JSON into the same session replay to resume. An offline page cannot silently overwrite its original HTML or raw recording files.
- **Export annotations CSV** includes `confusion_rating_1_5`, its participant-retrospective source, and the observation link. The legacy optional 0–10 intensity field remains a separate measure; values are never automatically converted.
- Selected-interval JSON bundles include overlapping observer intervals and currently saved manual annotations. Original observer records remain available as full session JSON/CSV.

An observer-selected episode is not ground-truth confusion. Keep observer hypotheses, participant quotations, ratings, eye-quality flags and behavior evidence distinct in analysis.

## Verification

Isolated Unity core suite: **52 passed, 0 failed**. Added tests cover two-click timing, note persistence, 10-interval limit, unfinished termination, session isolation, and common fixed world targets across validation rounds. Synthetic offline browser tests cover two observer intervals, inert note text, interval video stop, linked participant responses, 1–5 validation, JSON restoration and CSV fields. These automated checks are not a new Neon + Quest device acceptance run.
