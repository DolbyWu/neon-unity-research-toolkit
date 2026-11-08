# GazeFlow: Democratizing VR Eye-Tracking Research with Neon and Quest 3

Free, unofficial research toolkit for **Pupil Labs Neon and Meta Quest 3**, designed to democratize VR eye-tracking research. It technically integrates quantitative eye-data collection with qualitative video-based inquiry, participant reflection and human annotation, lowering barriers to hardware integration, multimodal data alignment and interpretation.

This Asset Store edition records eye/pupil data when available, Unity head pose, explicitly connected behavioral events, participant/session snapshots and configured region intervals. It generates independent HTML review with local-video import, point/interval annotation and JSON/CSV export.

## Setup

1. Use Windows and Unity **6000.3.23f1**. Configure your own XR host and headset first.
2. Open **Tools > GazeFlow > Setup and Recording Settings**. Select a dedicated output folder and explicitly enable Record on Unity Play. Installation does not start acquisition.
3. For a first data-only trial, keep Neon reception off. Provide a tagged Main Camera or assign Head Pose Source, press Play, confirm an anonymous participant, mark an observation in Live Monitor and Stop normally.
4. Enable Neon and enter the current Companion IPv4/API/WebSocket ports. Check fresh samples, field availability, quality, gaps and timing status. Connect actual interaction events through the adapter or public StudyEvents API.
5. Open the finished session HTML. Choose an existing local video; inspect/adjust its offset before interpreting timing. Pause/mark a frame or select an interval, add labels and evidence sources, then export annotation JSON before closing. Import JSON to resume; CSV is for analysis.

## Edition scope

This edition does not launch external recording programs, capture video/microphone audio, run Python, invoke FFmpeg or start a local archive server. No such executable dependency is required. Supply an existing video if you need video-linked review. Participant intake and per-session folder archives remain available locally in Unity.

Settings are stored under the host's `UserSettings/GazeFlow`. Do not install this edition together with the original GitHub package in the same project: they share namespaces/assemblies. Use a separate project or remove the other package first after backing up scenes.

Head pose is not raw IMU. No gaze-to-object mapping, fixation/saccade classifier or cognitive-state inference is included. Clock/video alignment is estimated, not a hardware synchronization guarantee. The preview.4 full-edition device results are a baseline; this store edition requires its own Validator/import/recording acceptance. Other platforms/standalone players are not validated.

Read [the workflow](Documentation~/workflow.md), [license](LICENSE.md) and [third-party notices](<Third Party Notices.md>). Support/source: https://github.com/DolbyWu/neon-unity-research-toolkit . Independent project, not endorsed by Pupil Labs, Meta or Unity.
