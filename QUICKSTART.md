# Quick Start

**GazeFlow 0.2.0-beta.1 candidate · Windows Unity Editor 6000.3.23f1.** Follow this in a backed-up independent project copy. See the [illustrated controls](docs/illustrated-quickstart.md) and [validation scope](VALIDATION.md).

## 1. Install — no Git needed

Download this candidate branch using **Code → Download ZIP**. Extract into a permanent folder. In **Window > Package Manager**, use **Install/Add package from disk** and select `Packages/com.dolbywu.neon-research/package.json`. Do not install beside another copy/store edition.

For spatial gaze, install official Neon XR Core at the commit listed in the [README](README.md), configure its XR objects/mount, then install the matching adapter. The core-only recording/observation/HTML workflow does not require a headset SDK.

## 2. Open Start Here

Choose **Tools > GazeFlow > Start Here**. Open **Recording and media settings**.

- Choose a private, reasonably short data root outside Git, such as `C:/ResearchData`.
- Enable **Record on Unity Play** only when ready. For a first test leave Neon and media disabled.
- Optionally enable **Open monitor + observer on Play** and **Open folder + HTML after Stop**.
- Save settings. No recording has started.

## 3. First 60-second test

1. Supply an explicit **Head Pose Source** on the camera/head you intend to record, or use the tagged Main Camera fallback for core-only head logging. Spatial binding in the adapter never uses a camera-name fallback.
2. Press Play and use **New participant** or **Returning participant** in the configured intake flow.
3. Enter an anonymous ID and choose **Save participant and continue**. Confirm media/recording if requested. Cancel if the location or participant is wrong.
4. Record about 60 seconds of your own task; open **Tools > GazeFlow > Live Observation Marker**, start/end an interval and save an observer note.
5. Stop normally; inspect the separate session folder and HTML. With Neon disabled, no eye samples are expected.

## 4. Connect the real Neon stream

Enable Companion Streaming, and the relevant Eye State / **Compute Eye State** option when available in your Companion version. Confirm your current device options rather than assuming every version uses identical wording.

Enter the current **Companion IPv4**, API and WebSocket ports in recording settings; enable **Receive Neon gaze** and save. Phone and PC must permit local device-to-device traffic. **NO RECENT EYE DATA** means inspect current signal, wearing, address/ports and network access; a Connected label alone is insufficient.

Check written samples, queue drops, quality flags, available pupils and missing-data gaps. Do not interpret missing values as zero pupil size.

## 5. Optional 3D gaze and manual adjustment

Follow [Generic Neon XR setup](docs/universal-neon-xr-setup.md). Explicitly select the actual tracked head and official provider. Use a stable mount configuration ID. Author Research Targets and wire the events you need; installation does not automatically discover your study's semantics.

Create a visual reference Transform on a visible point of any chosen object. In **Desktop Gaze Adjustment**, assign the reference, show it and the cyan gaze pointer, nudge, **Save gaze adjustment**, then hide the pointer/reference. Default step is 0.10°. A separate optional accuracy check is not a recording gate.

## 6. Optional local media

Install Python and FFmpeg separately. Enable the desired video/microphone channels, select actual executables and input device, and save. For whole-monitor capture choose **Desktop**, explicitly allow it and enter the actual monitor rectangle. The Mirror window is not required for Desktop capture.

Obtain consent and close private windows. Do a short spoken test, stop and listen to the actual output. A microphone channel does not automatically include game/system sound.

## 7. Live observation

The yellow button remains bright while locked. **RECORDING REQUIRED** means no recording session is active; it is not an error. Once recording, click **START OBSERVATION**, then **END OBSERVATION**, write a note and repeat (up to 10 episodes). Note-writing time is not included. Observations do not diagnose cognition.

## 8. Review, annotate and save

Stop and wait for media finalization. Open the generated HTML. Use **Load session video** or choose the finalized local video, verify its timing offset, pause/scrub and mark a frame or interval.

Add multiple annotations with researcher identity/evidence source and the participant's explanation. Optional 1–10 intensity and 1–5 confusion are different fields. **Export annotations JSON** before closing; import that JSON to resume. CSV is for analysis, not annotation restore.

The left-column offset report describes applied research-ray angles, not measured accuracy. At zero or a signal gap it can say Not available despite a later save receipt. Original X/Y/pupil data are not redrawn by the offset.

To update a finished static report, preserve any old HTML/annotation JSON you need, then use **Tools > Neon Research > Regenerate Finished Session HTML**. Missing historical fields cannot be reconstructed.

## 9. Connect behavior

Use the Interaction Adapter with your own UnityEvents, or the documented `StudyEvents.Record` API for custom task events. Keep stable target IDs consistent. Do not assume grabbing/puzzle events are automatic merely because gaze works.

Keep complete session folders and private raw records. No participant research data, connection settings or API keys belong in GitHub.
