# GazeFlow — Neon Research Toolkit for Unity

**0.2.0-beta.2 candidate · unofficial · Windows Unity Editor 6000.3.23f1.**

A reusable Configure → Record → Observe → Review workflow for researchers using Pupil Labs Neon and their own Unity experiment. No specific game, scene or object is required. Installation does not start recording.

## Start

Open **Tools > GazeFlow > Start Here**. Choose a private output folder, opt into recording, confirm an anonymous participant and run a short test. The yellow Live Observation Marker records start/end intervals and observer notes. After Stop, review the generated offline HTML, video and timeline; add multiple human annotations, export JSON to save and import it to resume.

Original eye/pupil fields, head pose, connected events, optional media and offsets remain separate evidence streams. Missing data remains missing. No automatic fixation, attention or cognitive-state inference is claimed.

## Optional spatial gaze

Install official Neon XR Core and matching `com.binyanxu.gazeflow.neonxr` beta. Its **Neon XR Setup** binds your actual tracked head and existing official provider, creates an independent research recorder, and optionally configures explicit cross-scene sources. Author research targets and connect behavior yourself.

Desktop Gaze Adjustment adds a reversible local direction offset and optional visual reference on any authored Transform. Save is not measured accuracy or official Companion calibration. Image-space X/Y and pupil traces are not rewritten.

## Documentation

- [Installation and Quick Start](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/QUICKSTART.md)
- [Generic XR setup](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/docs/universal-neon-xr-setup.md)
- [Illustrated controls](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/docs/illustrated-quickstart.md)
- [Validation and limits](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/VALIDATION.md)

Core data-only checks do not require a headset SDK. Python/FFmpeg for optional local media must be installed separately; capture needs explicit consent. Standalone player/mobile/macOS/Linux support and quantitative synchronization/physical accuracy are not established by this beta.

This UPM line does not replace existing signed preview archives or the separate Asset Store submission. MIT; see LICENSE.md and Third Party Notices.md.
