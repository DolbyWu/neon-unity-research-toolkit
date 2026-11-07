# Quick Start

This guide takes you from a fresh download to your first saved session. All menu and button names below are in English.

**Supported verification environment:** Windows and Unity **6000.3.23f1**. Public preview **0.1.0-preview.4** includes the component-serialization fix and a tested signed tarball. Neon + Quest 3 core-function tests passed. Configure your own hardware and check the selected data sources before collecting participant data. See the [release notes](RELEASE_NOTES.md).

## 1. What to prepare

For a first data-only test you need Unity Hub, Unity 6000.3.23f1, a writable local folder and repository access. You do **not** need Git, Neon, a headset, Python, FFmpeg or an API key.

For eye data, additionally prepare Neon, its supported Companion phone/app and a network permitting direct phone-to-PC communication. For video/audio, separately prepare Python, FFmpeg, a video source and a microphone. Those tools/devices are not installed or configured by this plugin.

## 2. Download and open the test project — no Git needed

1. On GitHub, select **Code → Download ZIP**.
2. Extract the ZIP fully to a local writable folder. Do not open files from inside the ZIP.
   On Windows, keep the project and data paths short (for example, a dedicated folder directly under C:) to avoid long-path errors in Unity/Mono. Do not debug first installation inside a deeply nested synced archive.
3. In Unity Hub, select **Add/Open project from disk** and choose the extracted **VerificationProject** folder.
4. Open it using **6000.3.23f1**. Allow the first import to finish and Unity to resolve its declared registry dependencies.
5. Confirm that **Tools → Neon Research** appears in the Unity menu.

The included test project is deliberately minimal. It is not your experiment and does not include headset configuration or interaction gameplay. The research monitor can record a test observation even in a blank scene.

If you already have a Unity project, back it up first. Open **Window → Package Manager**, choose **Install/Add package from disk** (wording varies), and select `Packages/com.dolbywu.neon-research/package.json` inside the downloaded repository. Do not copy the package's Runtime files into Assets as a second installation.

## 3. Configure a data-only first test

Open **Tools → Neon Research → Setup and Recording Settings** while not in Play.

1. Click **Choose data folder**. Create/select a fresh folder, for example `C:\NeonResearchTests`. This is your data root, not the package installation folder. Prefer local, non-cloud-synced storage with adequate free space.
2. Enable **Record on Unity Play (opt-in)**.
3. Leave **Receive Neon gaze (opt-in)** **off**.
4. Leave **Enable optional media capture** **off**.
5. Enter an **Experimental condition**, for example `practice`.
6. Leave **Open folder + HTML after Stop** enabled.
7. Click **Save project settings**.

Installing the package or opening the monitor does not start recording. Settings are saved in this Unity project's `UserSettings/NeonResearch/neon-study.json`. Stop Play before changing settings for the next session.

## 4. Record your first 60-second session

1. Open **Tools → Neon Research → Open Live Monitor**.
2. Press Unity **Play**. The participant form intercepts entry before recording starts.
3. Select **New participant**. Use an anonymous ID, for example `P001`; do not put names/contact details in the ID. Optional background fields should only contain consented information.
4. Click **Save participant and continue** to enter Play. Cancel leaves acquisition unstarted.
5. In the monitor, click **Mark observation** a few times. Run for about 60 seconds.
6. Press Unity **Stop** normally. Wait until Unity returns to Edit mode and the export completes.

For a camera-pose test, create/use a Camera tagged **MainCamera** and move/rotate it during Play. In your experiment, use the actual tracked head/camera rather than an unrelated scene camera. See the [integration guide](Packages/com.dolbywu.neon-research/Documentation~/workflow.md) for explicit head, region and interaction setup.

**Expected:** the monitor shows an active session, observations appear as events, and the folder/HTML open after Stop. With Neon off, an empty eye timeline is expected. The plugin never creates synthetic eye samples to fill it.

## 5. Find your files

Using the example output root above, the normal participant workflow creates:

```text
C:\NeonResearchTests\
  Participants\
    P001\
      participant.json
      participant.txt
      Sessions\
        <unique-session-id>\
          participant_snapshot.json
          participant_summary.txt
          export_manifest.json
          game_events.csv
          behavior_events.csv
          region_intervals.csv
          head_pose.csv
          clock_anchors.csv
          Replay\
            study-replay.html
          Neon-<id>\              # only when Neon recording starts
            NeonGaze.csv
            raw_payloads.csv
            session.json
          Audio\                 # media files only when capture succeeds
            think-aloud.wav
          Video\
            quest-view.mp4
```

Some channels can have only headers or no media file when disabled/unavailable. Their presence alone is not proof of valid data. Optional capture also writes status/timing metadata.

For a new person, choose **New participant** and a new ID. For the same person returning, choose **Returning participant** and explicitly select their profile. Every run gets a separate session folder. Never reuse one recorder's active archive from another running Unity project.

If auto-open is disabled, use **Open data folder** in the monitor. To rebuild a finished replay later, use **Tools → Neon Research → Regenerate Finished Session HTML** and choose the folder containing `export_manifest.json`, not the participant or archive root. This regenerates the HTML; export in-tab annotations to JSON first.

## 6. Add Neon eye data

Complete the data-only test first. Then Stop Play and reopen Setup.

1. Connect Neon to its Companion phone. Confirm that the device works in the Companion app.
2. Put the phone and PC on an approved network that permits device-to-device traffic. Sharing a Wi-Fi name does not guarantee this; institutional guest networks may isolate clients. Do not disable security controls to bypass network policy.
3. Open the Companion's **Streaming** information. Find its current IPv4 address and API port. The official API details are documented by [Pupil Labs](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/).
4. Enter the address in **Companion IPv4 (no port)**. Example: if the phone shows `192.168.1.25:8080`, enter `192.168.1.25` as IPv4 and `8080` as **Companion API port**. This is an example, not a preset address.
5. Set **Companion WebSocket port** to the **gaze sensor's WEBSOCKET stream port**. The plugin default is `8686`, not a guaranteed value for your device. It is different from the API/status WebSocket port. This preview does not discover the correct stream automatically; verify the gaze stream connection details in the Companion/API status before proceeding.
6. If pupil diameter is required, enable the Companion's real-time eye-state processing (**Compute Eye State**, where available). Gaze may be received without pupil fields. Real-time eye-state processing can affect throughput on some phones; see [Pupil Labs data streams](https://docs.pupil-labs.com/neon/data-collection/data-streams/).
7. Enable **Receive Neon gaze (opt-in)**, click **Save project settings**, then start a new test session through Play/intake.

To check the stream details on the PC, open `http://<phone-ip>:<api-port>/api/status` in a browser, replacing the placeholders with the phone's actual values. Find the gaze sensor's WEBSOCKET connection information and use its advertised stream port, not the status endpoint's port. If this page cannot be reached, resolve device/network access first. If the expected stream details are absent or different, do not guess ports: consult your device documentation or support before collecting data.

The receiver uses a managed documented-wire decoder and outbound WebSocket connection. No native Pupil Labs Unity SDK is required. Supported payload lengths are 9/17/25/65/89/105 bytes; unknown layouts are flagged. Pupil columns use documented wire millimetres, but this implementation's physical units/device compatibility remain unverified. Do not assume a successful connection means usable eye data.

### Your eye-data acceptance checklist

- The monitor says **RECEIVING EYE DATA**, not only Connected.
- **Samples written** and **Last 5s** counts increase.
- Basic-valid and clock-aligned counts are appropriate; investigate drops, errors, gaps and unsupported packets.
- If pupils are needed, both are actually available with plausible values and independently checked against the device tools. `N/A` is not zero.
- After Stop, inspect actual rows in `NeonGaze.csv`, quality columns, `raw_payloads.csv` and `session.json`. Check closed status, errors, queue counts and coverage.
- Run a full-duration trial and controlled reconnect/Stop test before formal data collection.

Gaze coordinates are scene-camera pixels, not automatic Unity object hits. This plugin does not save eye-camera video or automatically start/merge phone-side recordings. A headset link is not the phone's Neon data connection. For a backup, start a separate Companion recording yourself.

## 7. Add optional video and microphone audio

You can skip this step and annotate the data timeline without video.

1. Separately install Python and FFmpeg from trusted sources; keep their executable paths.
2. In Setup, enable **Enable optional media capture** and enter **Python executable** and **FFmpeg executable**.
3. Select **Record video** and/or **Record microphone**. Enter the exact **Microphone device name** for microphone recording. Windows device enumeration can be checked with FFmpeg's DirectShow device-listing function; ask your technical support if unsure.
4. For **Video source → Window**, enter the exact visible **Window title**. The default `Oculus Mirror` only applies when that application is actually running; it is not a generic camera source.
5. For **Desktop**, explicitly enable **Allow desktop (explicit opt-in)** and configure the intended monitor area's left/top coordinates and capture width/height. Close private windows first. Multiple monitors and DPI scaling require a short crop test.
6. Set **Maximum capture minutes** long enough for the planned task, save settings, then start a fresh session. Approve the separate per-session capture confirmation only with consent.
7. Speak a test phrase and make a visible/spoken synchronization marker. Stop normally and wait for media finalization.
8. Listen to `Audio/think-aloud.wav` and the MP4 where applicable; check audibility, picture, crop and full coverage. A microphone WAV is separate from video; do not assume every MP4 has a usable microphone track.

These helpers do not change Windows input/output settings, install tools, or guarantee full-duration capture. You may instead record a local video with another tool, choose it in the HTML, and verify its offset manually.

## 8. Review and optionally annotate

1. Open `Replay/study-replay.html` in Edge or Chrome.
2. Click **Load session video**, or **Choose local video** for a manually recorded/finalized file. Keep the video with the session folder.
3. Verify timing with your marker: **experiment time = video time + offset**. The capture offset is an estimate, not hardware synchronization.
4. Pause or drag the video to a frame, then click **Pause & mark this frame**. The page shows both **Video** and **Experiment** time.
5. Under **Manual annotations**, choose a label OR enter a description, enter an anonymous **Annotator ID**, select the evidence source, then **Add annotation**.
6. Find another frame and use **Add another annotation here** to add another independent note. Use **Edit** only to change an existing note. Interval annotation remains available through **Use selected interval**.
7. Click **Export annotations JSON (save / resume)** before closing the page. Save the download in the session folder. Import that JSON when reopening to resume; import replaces the current list after confirmation, not appends.
8. Optionally export annotation CSV for analysis. CSV is not a resume format.

Manual annotation is **optional**. Without it, the session still retains its recorded channels. Labels are human judgments; they do not make a validated cognitive-state classifier.

The desktop layout is **eye timeline → Manual annotations → interval export → Game log**, beside a sticky video panel. Small screens stack panels. HTML embeds the timeline data, not the video bytes or future label edits. Copy/back up the complete session folder and exported annotation JSON.

## 9. Troubleshooting

| What you see | What to check |
| --- | --- |
| No Neon Research menu | Wait for import; inspect Unity Console compile errors; confirm you installed the package, not only downloaded the ZIP |
| Play does not ask for a participant | Enable Record on Unity Play, save settings and retry from Edit mode |
| No data folder after Play | Confirm intake completed, recording was armed, output root is writable and the monitor has an active session; inspect Console |
| NO RECENT EYE DATA | Verify reception enabled, actual IPv4/API/gaze stream ports, device state and approved network reachability; Connected alone is insufficient |
| Gaze arrives, pupils are N/A | Check real-time Eye State and actual payload support; do not convert missing values into zeros |
| Head pose absent or from wrong camera | Verify MainCamera tag or the explicit head Transform |
| Grabs are absent | Wire the actual interaction callbacks; installation does not detect arbitrary grabs |
| No sound | Confirm exact input device, per-session consent, microphone enabled and non-silent WAV; inspect capture status and duration |
| Video will not load | Wait for finalization; choose the file manually; verify codec/browser support and that the file exists |
| Video and event times disagree | Check offset against a visible/spoken marker; estimated launch time is not first-frame synchronization |
| Labels disappear after closing | Import the JSON you exported; unsaved in-tab changes do not persist in the HTML |

Do not use participant data to diagnose installation problems: use an anonymous practice session and share redacted errors only.

## 10. Optional installation methods and next steps

**Git URL (advanced):** requires a Git client. This repository is publicly accessible. In Package Manager choose install from Git URL:

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.dolbywu.neon-research
```

**Tarball:** if you have a matching `.tgz`, choose Package Manager's install-from-tarball option. Latest source on main and older Release assets may differ; check the version/changelog. Installation from tarball is not separately validated by the local-source tests.

Next, follow the [integration guide](Packages/com.dolbywu.neon-research/Documentation~/workflow.md) for your own application's event wiring. Read [Validation](VALIDATION.md), the [license](Packages/com.dolbywu.neon-research/LICENSE.md) and [dependency notices](<Packages/com.dolbywu.neon-research/Third Party Notices.md>) before research deployment or redistribution. Plugin code and software documentation authored and owned by Michelle Binyan Xu are MIT-licensed; third-party materials retain their own licenses, and demonstration media/research data are excluded. This is a development preview, not an approved Asset Store submission; no vendor endorsement is implied.
