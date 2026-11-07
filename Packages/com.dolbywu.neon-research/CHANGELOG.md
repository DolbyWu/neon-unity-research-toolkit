# Changelog

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
