# Pre-room validation → evidence review → reproducible samples

**Latest 2026-11-13 policy:** see `automatic-five-point-validation.md` for two-round direction fitting, repeated fixed world targets and the 3-degree project tolerance (coverage remains 70%). These supersede the older manual-mode 2-degree/no-fit description below. See `live-observer-intervals.md` for the two-click researcher marker, note popup and linked player retrospective responses.

**Current layout correction:** the first tutorial room now has five points in front and grabbing **180 degrees behind**, not to the right. Grab models are hidden during validation and restored afterwards. See `automatic-five-point-validation.md` for the latest update; earlier right-side wording below is superseded.

**2026-11-13 presentation update:** the independent trial now starts [automatic five-point validation](automatic-five-point-validation.md) in the FIRST tutorial room, before grabbing: all five dots in front → one starting-point click on the computer → automatic pulses with two-second gaps → turn right for grabbing → LocomotionTutorial → Room 1. The earlier pre-room/per-point instructions below describe the optional manual/recheck mode, not the default entry sequence. No calibration correction is fitted and completion does not automatically approve mapping.

Implemented locally on 2026-11-12 for the independent experimental project. This is an additive experimental workflow, not a new released version, validated cognitive classifier or replacement for official Neon calibration. Original recordings are unchanged. Hardware acceptance of this new workflow is still required.

## 1. Before the main task: validate in the pre-room

Use the existing official mount calibration/configuration. This module does **not** fit an offset or rebuild Neon's eye-estimation algorithm. A calibration identifier is a study-controlled version label: use a new ID when the actual mount/calibration changes. Device connection is not proof of spatial accuracy.

1. Start the independent experiment as usual and confirm participant/recording details. Remain in the pre-room; do not begin the main task yet.
2. On the computer choose **Tools > GazeFlow > Pre-room Gaze Validation**. The existing provider and tracked-head binding must be ready. Assign a meaningful **Mount calibration ID**, for example `mount-A-config-01` (not a participant's name).
3. Choose five or nine targets. Ensure the target plane is visible and clear of walls; adjust its distance to suit the pre-room. Default settle/measurement durations are 0.75/1.5 seconds per target. Threshold defaults (2° mean angular error per target, 70% consecutive-valid coverage) are configurable engineering starting points, **not** accepted scientific accuracy standards.
4. Click **Start gaze validation**. This clears operator approval and hides the moving gaze dot/line. It now waits for a fresh worn signal before placing targets using the current head pose. Each fixed yellow target remains in preview with NO measurement timer until the wearer confirms it is visible and the operator clicks **Target visible — start measuring this target**. Never confirm an unseen target. The first unmeasured preview can be repositioned in front of the current headset using the window's button. Later targets also wait for confirmation. Targets remain world-fixed after placement; no existing colliders or gameplay physics are modified. If preview is still invisible, abort and inspect placement/rendering rather than treating the resulting angular error as measured calibration accuracy.
5. Review every target's mean error, p95 error and coverage. No data yields missing error, not zero. A failed/aborted check cannot be approved through this window. Adjust wearing/binding and, where necessary, repeat official mount calibration, then validate again. No corrective transform is fitted or silently applied here.
6. Only after reviewing a saved passing report, check the explicit review box and click **Approve this mapping for AOI**. This is a separate operator declaration, not measured clock synchronization. Changing the calibration ID clears this window's approval. Revalidate after wearing, mount or meaningful alignment changes; automatic drift/mount-change detection is not implemented.
7. Keep the pointer hidden for the experiment. **Tools > GazeFlow > Neon XR > Gaze Pointer** can restore it for diagnostic checks. Gaze Monitor and raw recording remain independent.

Completed measurements save new `Calibration/<verification-id>/validation.json` and `validation_samples.csv` under the current session. Without an active session they save under the configured output root's `ValidationChecks/`; they are not automatically attached to a later session. Samples include target positions, world rays, angular error, callback times and available device-time semantics. `gaze_targets.csv` appends `verification_id` and `verification_status` without removing older columns. Invalid/watchdog rows remain explicit; a verification reference is not invented where unavailable.

The validation report appears in newly generated Replay HTML. A missing or corrupt optional report must not prevent raw replay. Validation start/end events identify this non-task phase; the offline builder excludes overlapping windows, including an open-ended check until the session ends. Hiding a pointer does not approve mapping. Fixed-plane targets do not establish accuracy at every depth or throughout the task.

## 2. After the task: participant/researcher review

Open the newly generated `Replay/study-replay.html`, load the corresponding local video if needed, and verify its offset. Pause/scrub, mark a frame or select an interval, add a label or description, and provide an anonymous annotator ID.

- **Evidence source** records self-report, retrospective interview, observation or interpretation separately. A retrospective recollection is not a contemporaneous report.
- **Participant-reported intensity**, optional 0–10, is independent of **Annotator confidence**, Low/Medium/High. Blank intensity stays missing, not zero; a reported zero is retained.
- **Review status** is `draft`, `reviewed`, `disputed` or `excluded`. Reviewed is a human workflow decision, not ground truth.
- Use **Add another annotation here** for new records. Editing retains prior snapshots under `revisions`, with record revision, author, source, timestamps and previous values. The **History** action shows those snapshots. Deleting removes a record from the active list but retains its full record/history in the JSON deletion audit.
- **Export annotations JSON** saves/resumes the complete v2 bundle. Previous exported files are never overwritten by the page. New Replay imports v1 files as draft labels with missing intensity and empty history; old v1-only Replay readers cannot read v2, so keep original files and use the updated Replay.
- Exchange JSON files offline. **Merge another annotator JSON** keeps existing records; a conflicting revision with the same ID becomes a new disputed linked record. Another author's deletion audit is retained but never automatically deletes current records. The current video offset is not replaced on merge.
- Expand **Compare overlapping annotations / reconciliation**. Cross-annotator overlapping intervals show labels side by side. This is basic temporal comparison, not inter-rater reliability estimation or online collaboration. Point annotations compare at the same instant only. **Create linked reconciliation note** makes a NEW record; neither source is rewritten. For sample building, explicitly mark superseded originals excluded and review the reconciled interval. Otherwise active overlapping intervals are conservatively rejected, even if their labels match.

JSON is the authoritative save/resume and provenance format. CSV includes serialized history and links for analysis. Avoid cognitive certainty claims from ray hits or pupil variation alone.

## 3. Build quality-filtered samples locally

After stopping Play and waiting for session/media closure, open **Tools > GazeFlow > Build Reviewed Samples (Offline)**. Choose the finished session folder, reviewed annotation JSON and a NEW output folder. Verify the configured Python executable. Click **Build reviewed samples locally**; no upload, API key, third-party Python dependency or model training is involved. The window can open the output folder when finished. Do not close the window while it is building.

Outputs:

- `curated_intervals.csv`: all supplied intervals with human-label provenance, including non-included ones.
- `samples.csv`: included fixed windows and quality/context summaries.
- `audit.csv`: included/rejected windows with reasons, plus unlabeled time that is **not** treated as a negative class.
- `manifest.json`: versioned schema, full processing policy, input SHA-256 hashes, builder hash, output hashes and limitations. No wall-clock creation time or absolute input paths are added, so the same inputs and parameters reproduce identical output bytes in a second new folder.

Defaults are 2-second, non-overlapping windows anchored at each annotation start; tails shorter than a window are audited/rejected. Only reviewed intervals with a resolved target label are eligible. Points are not silently expanded to intervals. Draft/disputed/excluded or unresolved `Unlabeled`/`Uncertain`/`Uncertainty` labels, active overlaps, validation periods and insufficient quality receive explicit rejection reasons. Labels are configurable human codes; these exclusions are current engineering policy, not universal scientific rules.

Raw eye coverage uses consecutive basic-valid, worn, finite-gaze samples with `time_echo_estimated` aligned session time, the same raw stream/connection epoch and gaps ≤0.1 s. It does not interpolate, borrow boundary context or count unaligned receive times as aligned samples. Missingness is excluded time between callbacks, not a measurement of every lost native sample. Basic quality is not full eye-data validity.

AOI quality is independent and optional. **Also require measured AOI quality** rejects windows without sufficient valid, operator-approved callbacks linked to a matching saved measured-pass report. This does not reinterpret `no_hit` or `non_target_occluder` as invalid rays. By default raw-eye samples may be included despite unusable AOI; AOI metrics/statuses remain explicit. No fixation metrics are produced.

Pupil wire-value means and available counts preserve the existing units; missing/nonpositive pupil values stay unavailable. They are not physically validated diameter estimates. Event/head-pose counts are context summaries, not automatic cognitive features. All intervals use session seconds, but raw aligned time, AOI callback time and estimated media time are not claimed as hardware synchronized.

Advanced CLI, using Python 3.8+:

```text
python Tools/Build-ReviewedSamples.py --session <closed-session> --annotations <reviewed-json> --output <new-folder> --window 2 --min-coverage 0.7
```

Use `--require-aoi` to require measured spatial quality. `--require-video --video-duration <seconds>` additionally requires the saved video offset and an explicitly supplied actual video duration covering each window. Without a supplied duration, video coverage is unknown rather than assumed. These extra video options are CLI-only in the first UI. Saved offset provenance remains explicit; providing duration does not validate synchronization. The bundled implementation is in the core package's `Tools~/Build-ReviewedSamples.py`.

Split training/test data by participant/session/task before tuning; never randomly split adjacent windows. Fit preprocessing on training folds only, consider task/outcome leakage in context features, and retain source annotation IDs. No classifier or automatic confusion/emotion detector is implemented.

## Verification / outstanding

Completed software checks on 2026-11-12:

- PASS: 40/40 isolated Unity EditMode tests, including three synthetic angular-error/no-data/threshold/serialization tests. Current result: `Artifacts/review-validation-complete-20261112.xml`.
- PASS: seven offline builder tests, including byte-identical repeated outputs, source immutability, missing/overlapping/adjacent/trailing intervals, validation-phase exclusion, optional AOI/video quality, legacy drafts, identity checks and mismatched calibration-report session rejection.
- PASS: `Test-EvidenceReview.cjs`, `Test-ReplayAnnotations.cjs`, `Test-GazeTargets.cjs`, `Test-OfflineReplay.cjs`, with synthetic data and no network requests. Existing video pause/scrub/multiple-label/JSON/CSV behavior remains covered.
- PASS: optional adapter and validation Editor static compilation against installed official SDK assemblies. The actual experiment Editor imported the changes; both **Pre-room Gaze Validation** and **Build Reviewed Samples (Offline)** windows were opened without starting Play. A discovered ambiguous PackageInfo reference was fixed; earlier failure logs are not erased.
- NOT TESTED: building a real participant dataset through the new Unity button; its offline implementation was exercised with synthetic inputs only.
- NOT TESTED: the new five/nine-target procedure, operator approval, stream-loss handling and final generated report with Neon + Quest. Existing gameplay remains unmodified, but a fresh end-to-end device regression is still required.

Artifacts and checkpoint backups are local/ignored, not participant evidence. Do not represent the old device trial as acceptance of these new modules. No data, code or documentation from this development step has been pushed automatically.
