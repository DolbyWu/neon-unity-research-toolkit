# Changelog

## 0.1.0-preview.1 — 2026-11-06

- Extracted research-only source into independent Runtime/Editor assemblies with project-local settings and inert defaults.
- Replaced EscapeRoom-specific zone/head/interaction coupling with explicit generic components and APIs.
- Retained participant snapshots, monitor, exports and the offline video/manual-annotation workflow.
- Added a documented-wire managed Neon decoder without SDK/native library redistribution. Real-device validation is pending.
- Retained optional consent-gated Python/FFmpeg helpers; no executables, data or live configuration bundled.
- Added a blank verification host, synthetic Unity/browser/media tests and source-preservation checks.
