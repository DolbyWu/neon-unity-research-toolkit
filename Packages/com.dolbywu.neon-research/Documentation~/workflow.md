# Integration and experimental workflow

## Install and configure

New to Unity research recording? Start with a 60-second data-only test before connecting hardware. Use the repository's Quick Start for ZIP download, Unity Hub and beginner troubleshooting. Nothing requires a completed game or headset for the first event-only recording.

Use Package Manager's local package-from-disk function and select this package's `package.json`. Supported verification target: Windows Editor, Unity 6000.3.23f1. Other platforms, standalone player builds and older Unity versions are untested. Core/runtime and Editor assemblies are separate; only Unity JSON is a registry dependency. Python/FFmpeg are not required to compile, collect basic events/head data or review the offline HTML.

Open Setup from **Tools > Neon Research**. New project defaults disable Record on Play, Neon reception and media. Choose a fresh output folder. Enable Record on Play, save and then use Unity Play. Participant intake is only intercepted when recording is explicitly armed. Profiles and sessions use anonymous IDs with immutable per-session snapshots; a returning participant must be explicitly selected. Never use real names as IDs or treat optional background fields as inferred ethnicity/cognitive state.

## Head, regions and interactions

Assign a **Head Pose Source** component to the head/camera Transform. Otherwise a tagged Main Camera is used. `head_pose.csv` stores Unity WORLD position in project distance units and quaternion rotation; verify the host's metre scale and coordinate conventions. A head movement is not an eye fixation. Head pose is raw CSV, not a newly added head chart in the replay.

Add **Study Region** components with colliders and explicit IDs. Multiple overlapping matches are labelled Ambiguous. Unconfigured areas are Unknown/In-between, not attention targets.

Add **Interaction Adapter** and wire GrabLeft/ReleaseLeft/GrabRight/ReleaseRight to the host framework's actual grab/release events. Set a stable object ID or use the provided fallback identity. Alternatively call on Unity's main thread:

```csharp
using NeonResearch;
StudyEvents.Record("attempt", "door_lock", "failure", "Observed game event only", "TaskA");
StudyEvents.Grab("Left", objectTransform, "stable_object_id");
StudyEvents.Release("Left");
```

These calls do nothing outside an active session and do not start recording. They do not automatically discover every action in every game. Never run two independent recorders against the same participant archive.

## Neon

Enable reception separately and configure the Companion IPv4, API and WEBSOCKET gaze stream port on an approved device-to-device local network. Enter only the IPv4 in the address field; enter the API and gaze stream ports separately. The default ports are configuration defaults, not guaranteed discovery results. The gaze stream is not the API/status WebSocket. Native RTSP/Live555 and the Pupil Labs Unity SDK are not bundled or started. Managed RTP/RTCP over the Companion WebSocket and Time Echo provide the receiving/timing path. The documented-wire decoder retains raw base64 payloads, quality flags and queue/unsupported counts. Pupil fields use the documented raw wire unit (mm); real-device and physical-unit validation remain required.

Check recent samples, data availability, supported payloads, quality and clock alignment; Connected alone is not proof of a usable eye stream. Real-time Eye State must be available for pupil fields. Missing values remain unavailable. No automatic phone-side backup recording is started. No gaze-object raycast, fixation/saccade classification, cognitive-state classifier or hardware-verified synchronization is provided. Refer to the official [Real-Time API](https://docs.pupil-labs.com/neon/real-time-api/), [protocol details](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/) and [data streams](https://docs.pupil-labs.com/neon/data-collection/data-streams/) for device-side behavior; vendor documentation does not certify this implementation.

## Optional media

Python/FFmpeg executables and microphone/window configuration must be separately installed/configured. Nothing is downloaded, launched or recorded by installing this package. Per-session operator confirmation remains required before capture. Whole-desktop recording has a separate explicit opt-in. The existing Quest Mirror preview helper uses the conventional Windows Oculus installation location; other installs require operator adaptation. Generic video-window capture can use another configured window title.

Capture helpers tolerate brief metadata contention and retain finalization evidence. This is not a full task-length hardware validation; complete media coverage, microphone intelligibility and DPI/cropping must be verified. Missing speech or video cannot be reconstructed. External-process capture/redistribution is not certified for Asset Store submission.

## Offline review and annotation

After successful raw finalization, Stop opens the session folder and generated HTML when enabled. The independent media agent may still be muxing: wait for its finalized file. The HTML can load `../Video/quest-view.mp4` or a manually selected local video and use a capture/manual offset. Video and session time seek together. Verify the offset using an actual marker; it is not hardware-synchronized. Out-of-coverage video is explicitly marked.

The right-column order is **eye-data timeline → Manual annotations → interval export → Game log**. The video stays visible beside annotations on desktop; smaller screens stack panels. All interface strings are English.

Pause the video or drag its position slider, then click **Pause & mark this frame** (or **Mark current frame / time**). Both **Video** and **Experiment** times are displayed; annotation time is always experiment/session time. Choose a quick/custom label OR enter a description, supply an anonymous annotator ID and add the annotation. For the next note, find another frame and click **Add another annotation here**; previously saved records remain unchanged. Explicit **Edit** updates an existing record; the next-annotation button exits edit mode after confirming any unapplied draft. Waiting for a decoded frame temporarily disables frame marking. Advanced fields retain interval times, quotations and observed evidence. Timeline interval annotation also remains available.

**Export annotations JSON** is required to retain work; **Import annotations JSON** resumes it, validating the session/participant and asking before replacement. Import replaces the list, not appends; add multiple notes in the page before exporting. Older JSON without optional frame provenance still imports. Point notes marked from video retain the frame time/offset in JSON/CSV. CSV is for analysis only. Downloaded JSON and video remain separate files; copy the whole session folder. No browser cache, cloud upload or local web server is needed for this HTML. No raw records are modified by annotation. Newly generated HTML uses this template automatically; already exported pages do not silently update.

The separately callable Participant Archive Python service is optional and not automatically opened after Stop. The normal workflow opens the finished session folder and offline HTML when enabled. Profiles live under `Participants/<ID>` and each run has its own `Sessions/<session-ID>` folder. To review later, use **Tools > Neon Research > Regenerate Finished Session HTML** and select one completed session containing `export_manifest.json`. Export any open-page annotations to JSON before rebuilding HTML.
