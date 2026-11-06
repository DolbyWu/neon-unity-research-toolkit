# Features, setup and Neon acceptance checklist

**Unofficial Windows Unity Editor preview, 0.1.0-preview.2.** Separate verification host on Unity 6000.3.23f1; not yet validated with a real Neon/headset in the extracted version. Keep the working EscapeRoom unchanged until device validation passes. No automatic cognitive-state or gaze-to-VR-object claims.

## What the plugin does

| Feature | Scope / operator action |
| --- | --- |
| Anonymous participants | Pre-Play intake; explicit returning-participant selection; separate sessions and immutable profile snapshots |
| Head pose | Assigned head/camera Transform, or Main Camera fallback; Unity-world position and quaternion CSV, not a new head trace in the HTML |
| Grabs and game events | Explicit adapters/API for grab start/end, hand, stable object ID, attempts/success/failure/custom events; does not auto-discover arbitrary games |
| Regions | Generic collider/ID components; overlapping and unconfigured areas remain Ambiguous/Unknown |
| Neon reception | Managed outbound WebSocket gaze transport, RTP/RTCP and estimated Time Echo alignment; raw payload retention, gaze/pupil fields when actually provided, quality/drop/unsupported counts |
| Live monitor | Recent samples/quality, connection, events, gaze/pupil traces; opening the window itself does not start acquisition |
| Offline review | Embedded event/region/eye timeline; load a separate finalized local video; estimated/manual offset and bidirectional seeking |
| Manual annotation | Optional point/interval codes; annotator, source, quotation, observation and interpretation; JSON save/import, CSV for analysis |
| Optional media | Separate operator-installed Python/FFmpeg, configured video source/microphone and per-session confirmation; not a proven full-duration hardware recorder |

No game assets, Meta/OpenAI/MCP SDK, Python/FFmpeg executable, live IP or participant recording is bundled. A real VR game still requires its own XR/headset setup. This package does not configure Quest Link or make the blank verification host into an escape-room game.

## A. First installation

Safest route: download this repository's ZIP, extract it to a local writable folder, and add/open **VerificationProject** in Unity Hub with **6000.3.23f1**. No Git client is needed for this route. Unity still needs access to its registry to restore declared dependencies on first import.

For another disposable Unity project, choose Package Manager **Install/Add package from disk** and select `Packages/com.dolbywu.neon-research/package.json`. Do not open the package folder itself as a Unity project. A release `.tgz` is an alternative package-from-tarball input; installation from that archive is not independently covered by the current local-source compile test.

Git URL installation is also supported by Unity's Package Manager for repositories accessible to the user:

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.dolbywu.neon-research
```

This repository is private; other people need repository access and suitable Git authentication before that route works. Do not change visibility to public before code-rights/privacy review.

## B. Configure before Play

Open **Tools > Neon Research > Setup and Recording Settings**.

1. Choose a fresh independent **Output folder**, preferably on a local non-cloud-synced drive (for example, a new `C:\NeonResearchTests` folder). The default `NeonResearchData` is relative to this host project and is suitable for isolated synthetic checks. Do not initially select the working `C:\VRStudyData\PlayTests` archive. Cloud placeholders, file sharing and synchronization are additional risks for live acquisition.
2. Enable **Record on Unity Play (opt-in)**. Installation alone leaves recording off.
3. Enter the experimental condition. Leave Neon and media disabled for the first event-only smoke test.
4. Assign **Head Pose Source** to the real head/camera Transform. The recorded orientation is quaternion/world pose; verify project units, coordinate space and the actual head source.
5. Add **Study Region** components/colliders and explicit region IDs where needed.
6. Wire **Interaction Adapter** grab/release methods to actual XRI/Meta/custom interaction events. Use a stable object ID, or integrate the `StudyEvents` API. Without wiring, a grab not being logged is expected.
7. Save, then open **Tools > Neon Research > Open Live Monitor**. Configuration is read for a new Play; stop before changing connection/recording settings.
8. Click Unity Play, choose a new/returning anonymous participant, fill optional notes and continue. If media was configured, a separate recording-consent confirmation follows. Cancel starts no recording.

Settings are stored in **this host's UserSettings/NeonResearch/neon-study.json**, not in the package or old EscapeRoom settings.

## C. Connect Neon

1. Connect Neon to its Companion phone and confirm it functions in the phone app. Use an institution-approved network that permits direct communication between the phone and PC. The same Wi-Fi name alone does not exclude client isolation. Quest USB/Link does not carry the phone's Neon stream.
2. In the Companion's current streaming information, find the phone IPv4 and available ports. Enter only the IPv4 in **Companion IPv4**: no `http://`, no `:8080` suffix.
3. **Companion API port** defaults to `8080`, but use the actual port if changed. **Companion WebSocket port** defaults to `8686` in this preview: it must be the gaze sensor's WEBSOCKET stream port, not the API/status WebSocket or scene-video port. Ports are configurable; the preview does not perform automatic discovery. The legacy `rtspPort=8086` field is not the receiving path.
4. Enable real-time **Compute Eye State** in the Companion if pupil/eye-state values are required. Gaze may arrive without pupil fields. Missing fields remain unavailable, never valid zero-valued pupils. Processing capability/phone model can affect performance with Eye State enabled.
5. Enable **Receive Neon gaze (opt-in)** in Setup and save before Play. The managed receiver uses the documented wire layout, not the original SDK's converted representation; actual compatibility and pupil magnitude need a separate real-device check.
6. Optionally start a separate Companion recording for backup. The plugin does not start/stop or automatically merge that phone recording. A black scene-camera view inside VR does not by itself establish whether gaze/eye state is present; check the actual numerical stream. VR scene video must come from the separately configured PC/headset video source.

Authoritative references: [Realtime API](https://docs.pupil-labs.com/neon/real-time-api/), [connection troubleshooting](https://pupil-labs.github.io/pl-realtime-api/dev/troubleshooting/), [wire/API details](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/), [eye-state/data streams](https://docs.pupil-labs.com/neon/data-collection/data-streams/). Device documentation describes the protocol; it does not certify this implementation.

### A 60-second acceptance check

- With the device worn normally, verify **RECEIVING EYE DATA**, increasing samples, nonzero recent counts, appropriate basic-valid counts, alignment status and zero queue drops. Connected or a visible eye-camera video alone is insufficient.
- If pupils are required, verify both are available with plausible values and compare the stream/units against the device's official tools. This decoder labels the documented wire values as mm but has not been physically validated. Do not reinterpret old SDK-unit EscapeRoom files as mm.
- Do real grab/release actions and head movements. Confirm new event/pose rows belong to the selected participant/session. The plugin does not infer unhooked events.
- Stop normally. Inspect `export_manifest.json`, the current `Neon-*/session.json`, `NeonGaze.csv`, raw payloads and `head_pose.csv`. Require closed status, actual rows, no unexplained error/drop/unsupported-payload counts and no pending queue. Review validity/coverage, not only total counts.
- Then conduct a task-length trial and a controlled reconnect/Stop check before formal participants. Estimated alignment is not hardware synchronization, and pupil changes do not prove a cognitive state.

## D. Optional video and continuous microphone

Enable optional media only after configuring separately installed Python and FFmpeg, exact microphone device name, window/desktop source, dimensions and duration cap. Desktop capture requires an explicit opt-in and can capture private windows. Keep the intended monitor/game visible and make a spoken-marker short test. Listen to the WAV and MP4 and verify their coverage before a full run. The plugin does not change Windows microphone/output/security settings.

The existing Quest Mirror preview helper assumes the conventional Windows Oculus installation path. Other installations/sources require operator adaptation. The optional external capture and store-distribution/licensing have not been certified. It is safe to test data/HTML without media enabled and later load an independently recorded local video with a checked offset.

## E. Finish, review and annotate (optional)

After raw export and return to idle Edit mode, the enabled auto-open workflow opens this session's folder and HTML, not the archive dashboard. Independent MP4 finalization may still be running; wait for the finalized file.

1. Load the local session video and verify/adjust offset: `session seconds = video seconds + offset`. Keep the complete folder if moving computers.
2. Choose a point or interval, label it, identify the annotator/evidence source, and separate participant quotations from observations/interpretation. Click **Add annotation** or save edits.
3. **Export annotations JSON** is required to persist labels. Browser downloads are separate files, not silent changes to HTML/raw data. JSON import resumes editing and asks before replacing an existing list. CSV is for analysis only.
4. Backup the entire session, including raw data, video/audio, JSON annotations and metadata. No automatic cloud upload or automatic cognitive-state classifier is provided.
