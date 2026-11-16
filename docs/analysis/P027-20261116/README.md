# Multimodal pilot case P027 — aggregate results

Date: **16 November 2026**, America/New_York. One participant, one session. These deidentified aggregate results are published at the user's request. This is an exploratory example of combining eye/pupil, object-contact, head/view, task-event and human-review evidence. It is not a cognitive-state classifier, a measured spatial-accuracy pass, a new plugin release or a population inference.

## What was analyzed

| Private input | Role in analysis |
|---|---|
| Native Neon gaze/pupil CSV (335,015 full-session rows) | Aligned bilateral pupil features and scene-image XY diagnostics; rows are not independent trials |
| Mapped ray callbacks (38,095 full-session callbacks) | Validity, gaps, object/surface contacts and consecutive-callback transitions |
| Corrected canonical object hits (16,136 full-session hits; 12,726 Study hits) | Explicit raw-child-to-object identity, without inventing precision approval |
| Head/camera pose CSV (133,329 full-session rows) | Descriptive view-motion features; world discontinuities screened |
| Game events and normalized target-status events | Grab counts and original insertion verdicts; raw/normalized duplicates not double-counted |
| Logical puzzle logs, region intervals and clock anchors | Task context, progression references, original session-clock scope and timing provenance |
| Three saved draft labels and player intensity ratings (7, 10, 8) | Post-hoc human context and exact episode comparison, never K-means training targets |

Study begins at first recorded Pre-room entry, **426.7381221 s**. Earlier Tutorial is excluded from these analyses but retained in private recordings. Unaligned rows remain unavailable. Native eye aligned time, Unity callback time and capture launch estimates are distinct; no hardware synchronization is asserted. Historical blackboard/whiteboard contacts remain separate unconfigured surfaces, not newly fabricated AOI hits. Mapping-verified callbacks in this pilot: **0**.

## Algorithms actually run

1. Gap-aware bilateral pupil availability checks and an explicit rapid-change heuristic, without interpolation or zero filling. Screen sensitivity uses MAD multipliers 4, 6 and 8 alongside availability-only estimates. This is not validated blink detection; visual luminance was not measured.
2. Nonoverlapping **5-second** multimodal windows. **251** full, finite, quality-eligible windows enter the primary model. Low-coverage/boundary/partial windows are preserved locally but not imputed into the model.
3. Median/IQR scaling (standard-deviation fallback for zero-IQR columns), normalized-feature clipping, deterministic NumPy **K-means++ / Lloyd**, k=2..5 and descriptive silhouette selection with minimum group-size rules. **PCA** is SVD for visualization, not a cognitive axis.
4. Within-region **penalized multivariate mean-shift dynamic programming**, minimum 20-second segments, no bridging region/quality gaps, penalty sensitivity at 0.5/1/2 times D·log(N).
5. Exact labelled-episode features and equal-duration same-region **unlabelled** reference periods, excluding annotations and a 10-second guard. These references are not confirmed calm/non-confused controls.
6. Pupil around original insertion-verdict events: baseline −2 to −0.5 s, post +0.5 to +3 s, 0.5-second bins, coverage checks and a separately reported isolated-event subset.

The features are pupil regional deviation/variability, head/view angular speed, configured-target contact fraction, observed target transition rate, grab rate, insertion-verdict rates and historical board-contact fraction. Human labels/ratings and identities were not model features. Raw native scene-image XY dispersion is diagnostic, not a VR fixation measure. No fixation/saccade detection was added.

## Findings

- Two neutral recorded-feature groups were selected at 5, 10 and 20-second window sizes. This does not establish two cognitive states or reliable label separation.
- Post-hoc overlap: the entire 6.47-second Confusion interval and about 87.5% of the model-eligible Stuck duration belong to global pattern P01. The unsupervised model did not separate those two human labels into distinct groups. This overlap is not supervised accuracy, and the short episode spans partially labelled five-second windows.
- A within-Hub change occurs near **29:51.738** original experiment time and is also found without pupil features. In the final annotated interval, before/after that boundary there are **17/2** versus **6/8** wrong/right insertion-status records. It is an offline feature boundary using task events too, not an early prediction or precise onset of insight. It is a useful replay/interview prompt.
- Stuck screened pupil median: **4.632783 mm**. Median across its two equal-duration same-region unlabelled references: **4.632866 mm**. This pilot does not support a simple larger-pupil Stuck detector.
- Confusion has five matched unlabelled reference periods. The much longer Hub interval has no equal-duration unlabelled same-region reference; none was fabricated.
- All-event correct/wrong pupil neighborhoods overlap and correct verdicts cluster late in the task. After the isolation rule, **5 eligible isolated WrongMove events and 0 RightMove events** remain. No independent correct-versus-wrong physiological effect is established.

See [episode aggregates](episode-summary.csv), [model diagnostics](model-summary.json), [change-point aggregate](change-point-summary.csv) and [event aggregate](event-analysis-summary.csv). Decimal precision is for reproducibility, not physiological or psychological certainty.

## What is deliberately not public

No raw native eye/pupil samples, five-second personal trajectories, original annotation IDs/notes or investigator identities, participant profiles/demographics, audio/video, full study assets, absolute machine paths, live endpoints or keys are published here. Detailed deidentified derived trajectories stay in the authorized private study repository. Original source data remain in their private archive.

[Frozen local reproduction recipe](../../../Tools/PilotAnalysis/README.md) requires an authorized private session and creates new local outputs. No input dataset is distributed with it. Before adapting it to another experiment, review time conventions, human labels, task-specific event semantics and quality thresholds. The standalone Unity toolkit does not require this case-specific recipe.

## How to use the results

Review the change point with the player; ask whether a strategy or understanding changed rather than assigning “insight” automatically. Add confirmed smooth-progress and difficult comparison episodes in similar visual/task contexts. Confusion and inability to proceed may co-occur; do not force exclusive classes. Multiple independent labelled episodes/participants and participant/session-aware validation are needed before evaluating supervised state prediction. Native sample count is not the number of independent training examples.

Pupil interpretation needs preprocessing and visual-context controls: [Mathot et al., baseline correction](https://pmc.ncbi.nlm.nih.gov/articles/PMC5809553/) and [Pan et al., luminance/pupil interaction](https://www.nature.com/articles/s41598-022-05280-1). [K-means overview](https://scikit-learn.org/stable/modules/clustering.html#k-means) describes the clustering family; the frozen recipe implements the needed calculations with NumPy rather than requiring scikit-learn.
