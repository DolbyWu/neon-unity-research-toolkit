# GazeFlow Neon XR Adapter · experimental

Optional spatial ray adapter for official Pupil Labs Neon XR Core 2.0.0. GazeFlow remains unofficial. This package adds no simulated research data, video capture or automatic session start.

Install the GazeFlow development core `0.2.0-exp.1`, then official Neon XR Core, then this adapter. Assign the official provider and actual tracked head Transform; retain an explicit calibration ID and keep mapping unverified until a known-target trial succeeds.

The full instructions, timing limitations and acceptance checklist are in the core package's `Documentation~/neon-xr-targets.md` and the repository's `docs/neon-xr-prototype.md`.

This is an additive target-ray path. It does not replace the existing raw gaze/pupil recorder. The pinned official WS client omits device timestamps: this adapter uses actual receive-event freshness, exports blank device time and discloses non-atomic ray/metadata pairing. Direct/native timestamps retain monotonic checks.

During Play, **Tools > GazeFlow > Neon XR > Gaze Pointer** opens a single **Hide gaze pointer / Show gaze pointer** button. It controls display only, not acquisition, calibration or AOI logging. The portable window still requires a headset hide/show check.

See the repository's `docs/windows-aoi-integration.md` and `docs/windows-aoi-verification.md` for the Windows trial scope and current verification. No Pupil Labs source code or native binaries are included. Install only in an independent project copy first; do not combine the core with the overlapping Asset Store package. Full project-wide AOI coverage and quantified accuracy/synchronization are not established.
