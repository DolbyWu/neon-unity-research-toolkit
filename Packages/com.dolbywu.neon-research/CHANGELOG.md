# Changelog

## Research Pipeline follow-up — automatic two-round transition (2026-11-13)

- Default integrated flow now automatically checks saved round-one collection, fits if sufficient and starts a 3-second fresh-signal countdown into corrected round two at the same fixed world centres.
- Insufficient collection explicitly names each coverage/pair/direction problem and offers a whole-round retry. High pre-correction angular error is not itself a fitting blocker. Historical reports and all attempts are preserved separately.
- Countdown resets on stale input/long updates, cancels on abort/skip/context change and never approves mapping. Automatic round-two start provenance does not claim a new operator visibility confirmation.
- Manual fit mode remains available. This follow-up is published on the same timestamped Research Pipeline branch as a separate commit; it is not a new signed release or store update.

## Research Pipeline snapshot — 2026-11-13 13:38 EST / 18:38 UTC

Branch `feature/research-pipeline-20261113-1338-est`; package identity remains `0.2.0-exp.1`, not a new signed release or store update.

- Added five-point mapping validation and bounded head-local direction correction with NEW round-two samples at the SAME fixed world target centres. Default trial tolerance is 3 degrees per-target mean error, with 70% coverage retained; these are project policy, not a universal accuracy standard.
- Preserved invalid/stale data states and separate low-precision unverified candidate exports without promoting them to accepted AOI hits.
- Added two-click live researcher observations, end-time-first note popup, 10-interval session limit and interrupted-session handling.
- Integrated observer segments with HTML video/timeline review, linked participant explanations and optional distinct 1–5 retrospective confusion ratings, JSON save/resume and CSV export.
- Added evidence-aware annotation revisions and offline quality-filtered sample curation; packaged its Python source without external executables.
- Improved optional media manifest-contention handling and retained raw/archive workflows. Audio capture is not automatic transcription.
- Added an English research-pipeline README, tutorial, source-only existing-study integration reference and synthetic checks. Newest presentation/observer changes still need a fresh real-device end-to-end acceptance run.

## 0.2.0-exp.1 — 2026-11-09 · Experimental development

- Added an optional main-thread spatial gaze interface, authored research targets, nearest-collider target records and a missing-data watchdog. Existing raw Neon schemas are unchanged.
- Added a separate optional official Neon XR adapter package; the core has no new SDK dependency. The adapter is additive, not a complete raw-receiver replacement.
- Added offline HTML target summaries, filtering, time seeking, selected target exports and annotation linkage; legacy sessions remain supported.
- Added configuration checks, a three-object sample, conservative metrics/DOM verification and Unity regression cases.
- This version has static core compilation and metrics/DOM evidence only. Unity execution, full adapter compilation, real-browser/media and Neon + Quest target mapping acceptance remain pending. Submitted store preview.6 is unchanged.

## 0.1.0-preview.5 — 2026-11-07

- Unified package author, copyright holder and current documentation under the Publisher spelling Binyan Michelle Xu.
- Added unityRelease 23f1 to match the recorded 6000.3.23f1 environment.
- Metadata/documentation update only; runtime code is unchanged from preview.4. This newly built candidate is not the previously tested Unity-signed archive and requires its own signing/import validation before store submission.

## 0.1.0-preview.4 — Unreleased

- Fixed scene serialization of Head Pose Source, Study Region and Interaction Adapter by placing each MonoBehaviour in its own matching source file with a stable metadata GUID. Namespaces, public fields and event APIs are unchanged.
- Added three scene save/reopen regression cases, including head/collider references and region/object IDs.
- Declared Unity JSON serialization, physics and IMGUI modules explicitly so a minimal host does not depend on another package pulling them in.
- Fixed the tarball whitelist to include root-document and Tools/StudyArchive folder metadata; added an archive-stage check for every Unity-imported asset/folder so immutable-package imports cannot silently ignore missing-meta assets.
- Existing Missing Script components from earlier previews are not automatically repaired. Back up the scene, remove only the broken component, re-add the matching component and restore its fields/callbacks.

## 0.1.0-preview.3 — 2026-11-06

- Added matching repository and package MIT license texts for the plugin software.
- Set package license metadata to MIT and removed private-package metadata.
- Clarified third-party license preservation and exclusions for demonstration media and research data.
- Corrected outdated private/unlicensed documentation. Acquisition and recording behavior are unchanged; historical device validation status is unchanged.

## 0.1.0-preview.2 — 2026-11-06

- Added a beginner-first installation, recording, Neon connection, media and review guide; clarified project-local Setup/monitor wording without changing recording behavior.
- Updated the offline review workflow without adding a game dependency or changing acquisition defaults.
- Video pause is authoritative; scrubbing stays paused. Mark a decoded frame with paired video/experiment clocks and retain its timing provenance in annotation JSON/CSV.
- Added quick labels, description-only notes and Add another annotation here. Consecutive additions retain existing records; editing an existing record remains explicit.
- Placed Manual annotations directly below the eye-data timeline, interval exports next, and Game log last. The video stays beside the annotation form on desktop.
- Kept legacy annotation JSON import, interval annotations, English-only UI, offline operation and the standalone decoder's pupil-mm columns/validation warnings.
- Expanded synthetic browser regression coverage. No real-device acquisition or cloud transcription is added by this update.

## 0.1.0-preview.1 — 2026-11-06

- Introduced Runtime/Editor assemblies with project-local settings and acquisition off by default.
- Added generic region/head/interaction components and explicit event APIs.
- Retained participant snapshots, monitor, exports and the offline video/manual-annotation workflow.
- Added a documented-wire managed Neon decoder without SDK/native library redistribution. Real-device validation is pending.
- Retained optional consent-gated Python/FFmpeg helpers; no executables, data or live configuration bundled.
- Added a blank verification host, synthetic Unity/browser/media tests and source-preservation checks.
