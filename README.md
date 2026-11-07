# Neon Unity Research Toolkit

Record participant sessions in Unity, receive Pupil Labs Neon eye data, and review your study locally with video, timelines and manual annotations.

**Version:** 0.1.0-preview.2 · **Tested environment:** Windows, Unity 6000.3.23f1 · **Status:** development preview, real-device validation pending.

New here? Start with [the step-by-step Quick Start](QUICKSTART.md). You can complete your first recording **without Neon, a headset, Git, Python or FFmpeg**. Add hardware and media after the basic recording works.

## What you can record

| Channel | What is saved | What you need to do |
| --- | --- | --- |
| Participants | Anonymous IDs, optional background information, per-session profile snapshots | Confirm the participant before Play |
| Head / camera pose | World position and quaternion rotation in CSV | Provide the correct camera/head Transform; Main Camera is the fallback |
| Interactions and events | Grab/release, object IDs, task outcomes and custom events | Connect your application's actual events using adapters or the API |
| Regions | Time spent in explicitly configured areas | Configure region colliders and IDs |
| Neon eye data | Available gaze/pupil fields, timestamps, quality flags and raw payloads | Connect a compatible Neon Companion stream and verify the data |
| Optional media | Local video and continuous microphone audio | Configure your own Python/FFmpeg and approve capture |
| Offline review | HTML timeline, linked local video, optional manual labels | Open the finished session HTML in Edge or Chrome |

The plugin does not automatically detect every interaction, map gaze to Unity objects, classify cognitive states, or calculate fixations/saccades. Pupil availability depends on the actual eye-state stream. Missing data is not replaced with valid zeros.

## Your first session

1. Download this repository using **Code → Download ZIP**, then extract it to a writable folder.
2. In Unity Hub, add the extracted **VerificationProject** folder and open it with **6000.3.23f1**. This is a minimal test project, not a finished experiment.
3. Open **Tools → Neon Research → Setup and Recording Settings**.
4. Choose a new output folder, enable **Record on Unity Play (opt-in)**, leave Neon/media off, and click **Save project settings**.
5. Open **Tools → Neon Research → Open Live Monitor**. Press Unity **Play**, choose **New participant**, and confirm an anonymous ID such as `P001`.
6. Run for about 60 seconds. Click **Mark observation** in the monitor to create a test event. If your scene has a tagged Main Camera, its pose can also be recorded.
7. Press **Stop** normally. With **Open folder + HTML after Stop** enabled, the session folder and offline replay open automatically.

**Success looks like:** a separate participant/session folder, a closed export manifest, event rows from your test, and a working replay HTML. No eye data is expected until Neon reception is enabled and receiving valid samples.

For exact settings, file locations and troubleshooting, follow [Quick Start](QUICKSTART.md).

## Install in your own Unity project

First back up your project. In Unity's Package Manager, choose **Install/Add package from disk** and select:

```text
Packages/com.dolbywu.neon-research/package.json
```

Select the file inside the extracted repository. Do not open the package directory as a Unity project. Git is not needed for this route. Unity needs registry access on first import to resolve dependencies.

The repository is currently **private**: other users need repository access to download it. Git-URL installation is an optional advanced route described in the guide. No public distribution license has been granted.

## Review and label a recording

The replay places **eye timeline → Manual annotations → interval export → Game log** beside a local video panel on desktop.

Load the finished video, pause or drag to a frame, click **Pause & mark this frame**, select a label or enter a description, then **Add annotation**. Both video and experiment times are shown. Use **Add another annotation here** for more notes. Point and interval annotations are optional.

**Export annotations JSON before closing the page.** Import that JSON to resume later. CSV is for analysis, not re-import. The HTML does not silently save edits back to itself. Video/audio and annotation JSON remain separate files: copy the complete session folder when moving computers.

## Documentation

- [Quick Start: installation, first recording, Neon, media and troubleshooting](QUICKSTART.md)
- [Integration guide: head pose, regions, events and annotation workflow](Packages/com.dolbywu.neon-research/Documentation~/workflow.md)
- [Validation: measured checks and remaining limitations](VALIDATION.md)
- [Changelog](Packages/com.dolbywu.neon-research/CHANGELOG.md)
- [License](Packages/com.dolbywu.neon-research/LICENSE.md) and [dependency notices](<Packages/com.dolbywu.neon-research/Third Party Notices.md>)

## Before collecting research data

Run a short hardware trial and a full task-length trial. Verify recent eye samples, actual pupil availability, head source, event wiring, timing estimates, audio intelligibility, video coverage and normal Stop. Automated tests use synthetic fixtures; they do not certify a physical Neon setup.

All acquisition is **off by default**. Settings belong to each host project under `UserSettings/NeonResearch`; the default data root is `NeonResearchData` under that project. Choose a dedicated local folder for research data, obtain consent, use anonymous IDs and keep archives private. Whole-desktop capture can include sensitive windows.

This is unofficial software, not a Unity or Pupil Labs product. Real-device compatibility, physical pupil units and full-duration capture remain validation requirements. Other Unity versions, platforms and standalone/Quest builds are unverified. No automatic cloud upload or speech transcription is included.
