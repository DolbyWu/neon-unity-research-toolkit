# GazeFlow Neon XR Adapter · experimental

Optional spatial ray adapter for official Pupil Labs Neon XR Core 2.0.0. GazeFlow remains unofficial. This package adds no simulated research data, video capture or automatic session start.

Install the GazeFlow development core `0.2.0-exp.1`, then official Neon XR Core, then this adapter. Assign the official provider and actual tracked head Transform; retain an explicit calibration ID and keep mapping unverified until a known-target trial succeeds.

The full instructions, timing limitations and acceptance checklist are in the core package's `Documentation~/neon-xr-targets.md` and the repository's `docs/neon-xr-prototype.md`.

This is an additive target-ray path. It does not replace the existing raw gaze/pupil recorder. Full Unity dependency/adapter compilation and Neon + Quest hardware validation are pending. No Pupil Labs source code or native binaries are included. Install only in a disposable test project first; do not combine the core with the overlapping Asset Store package.
