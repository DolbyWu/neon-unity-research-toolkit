# Changelog

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
