# Research Pipeline snapshot — 2026-11-13

Snapshot identifier: **2026-11-13 13:38 EST / 18:38 UTC**. This is the branch creation timestamp, not a hardware recording timestamp.

Branch: `feature/research-pipeline-20261113-1338-est`

Base source commit: `e820338244bc4e5dc503dd30ac605b96fa23d0a8`

Core and adapter package identity: `0.2.0-exp.1`

Reference Editor: Unity `6000.3.23f1` on Windows

Official Neon XR Core: `2.0.0`, upstream commit `f4dee47b9690a059b1c4660628596d7303ade59a`

This is a source snapshot of the research pipeline, not a new release/tag, signed tarball, default-branch update or Asset Store submission. Record the final Git commit when using it in a study; a branch name alone is not an immutable version.

## Included changes

- Existing raw eye/pupil, head pose, event, archive, optional media and offline HTML paths are retained.
- Optional official spatial adapter and AOI recording preserve stable names/IDs/groups and nearest-collider occlusion semantics.
- Five-point validation, bounded direction fitting and NEW second-round samples now share the same fixed world target centres. Trial policy is 3 degrees mean error per target and 70% valid coverage. Historical reports are not revised.
- Fresh unverified ray candidates remain available in distinctly labeled low-precision records. Invalid/stale/unworn input is not promoted into usable hits or zero values.
- Live observer intervals use a desktop start/end toggle, end-time-first note popup, 10-interval limit and interrupted-interval handling.
- HTML integrates original observer intervals, bounded video replay, separate player retrospective explanations, optional 1–5 confusion ratings, annotation linkage, JSON save/resume and CSV exports.
- Offline sample curation preserves the new rating/provenance/link independently from legacy 0–10 intensity, includes audit/missingness evidence and does not train a model.
- Media finalization handles temporary session-manifest access contention without treating it as an immediate normal stop. Synthetic checks do not establish actual microphone or full-monitor framing.
- [Source-only host integration reference](../Integrations~/ExistingStudyTrial/README.md) records bridge/helper code and patches without bundling an entire study/game or its scenes, assets and local configuration.

## Verification in this publication pass

All test inputs in this pass are synthetic. No participant files are used, no new real-device recording is started and no recording is uploaded.

| Check | Result | What this does and does not show |
|---|---|---|
| Isolated Unity core EditMode suite | **52 passed, 0 failed** | Core serialization, ray/target missingness, validation/correction and observer persistence cases; not headset perception |
| Offline browser replay | **Passed** | Data-only load, precision, gaps, half-open selection, downloads and no network |
| Browser AOI/target suite | **Passed** | Six conservative metric cases plus real-browser target filtering, seeking, CSV/bundles and legacy data |
| Evidence-review browser suite | **Passed** | Annotation v2, revisions, conflicts/merge, deletion audit, legacy migration and calibration report review |
| Synthetic video/annotation browser suite | **Passed** | Pause/scrub, multiple annotations, two observer intervals, player rating/link persistence, bounded interval replay, JSON reload and CSV |
| Archive and synthetic media tests | **14 passed** | Local archive/session boundaries, consent/config guards, generated video/audio and finalization/contention; no live mic/screen capture |
| Reviewed-sample curation tests | **9 passed** | Determinism, identity/quality rejection, input preservation and distinct 1–5 rating provenance/export |
| Legacy jsdom target-DOM script | **Not run successfully** | The local optional `jsdom` dependency was unavailable; the real-browser AOI suite above passed. Do not report this old script as passed |
| Documentation links and publication inventory | Checked before commit | English beginner guidance; source-only changed inventory, no new participant/raw media or credentials |

Local XML/logs and synthetic screenshots remain in ignored `Artifacts/`, not the public commit. The final package/source inventory is available in Git. Package whitelist and source-packaging helper include the offline `Tools~` builder; this pass does not generate or sign a release archive.

## Device acceptance boundaries

Earlier independent study sessions exercised raw acquisition, behavior, archive/media and spatial data paths. Their private recordings remain local. They are not proof that the latest UI/geometry changes are accepted.

The newest fixed-target geometry/tolerance, live observer popup and linked player-response workflow need a **new** end-to-end Neon + Quest run. Check raw acquisition, adapter, accepted AOI, low-precision candidates, occlusion, disconnect/reconnect, session isolation, media/archive, HTML and exports separately as **pass / fail / not tested**. See the [tutorial acceptance checklist](research-pipeline-tutorial.md#11-your-first-acceptance-checklist).

No assertion of automatic fixation detection, attention/understanding classification, calibrated pupil physiology or hardware-precise synchronization is introduced. Repeat-layout angular checks do not establish full-scene gaze accuracy. Research labels and ratings remain human interpretations/reports with provenance.

## Reproduction and protected material

1. Use this branch in a new project copy, not a production study in place.
2. Follow the [beginner tutorial](research-pipeline-tutorial.md). A generic host must configure its own XR rig, scene transitions, target identity and real interaction callbacks.
3. Preserve the resolved toolkit/upstream commits and your study-specific configuration privately. Keep generated archives outside Git.
4. Run software checks with synthetic fixtures, then a consented short real-device session.

The default `main` branch and submitted store edition are not changed. No original Unity project or historical research data is overwritten. New study recordings, participant metadata, transcripts, annotation files, live device addresses, mount/private configuration, API secrets and external executable binaries are excluded. Existing repository history and historical demo material are not rewritten by this branch.
