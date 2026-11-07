# Neon hardware connection and recording notes

Practical setup notes for the unofficial Neon Unity Research Toolkit. Based on the author's original VR study field notes dated 3 November 2026 and the extracted preview implementation. These observations describe a particular lab deployment, not universal hardware compatibility or timing guarantees.

## Scope and status

The author has used Neon successfully in the original Unity VR study. The extracted UPM package must still be checked independently in a clean Unity project. Record the actual hardware, software and network environment for every validation run.

| Item | Record before testing |
|---|---|
| Workstation | Windows version, GPU model and driver |
| Unity | Editor version, package version/commit, XR packages |
| VR | Headset model, Link software version, connection mode |
| Neon | Module/mount configuration, Companion phone and app version |
| Network | Approved local network, current Companion IP and stream ports |
| Recording | Enabled streams, head source, event adapters, output root, media tools |

Do not copy another lab's IP address or assume a previously working endpoint is still current. Find the device's current connection details in Companion. See the [official API guide](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/).

## 1. Prepare the physical setup

- Confirm the headset and Neon mount match the configuration used in the study. This note does not supply a universal mounting design.
- Check fit, visibility, cable routing and strain relief before each session. Repeat the study's gaze sanity check after removing/reseating or adjusting the equipment.
- Assign the actual XR head/camera Transform to the recorder. Verify movement and rotation in a short pose export.
- Record mount changes as session metadata. A mount adjustment can change the relationship between eye-tracker and headset coordinates.
- Check power, storage capacity and recording coverage for the full intended task duration.

Unity head/camera position and rotation are **head pose**, not raw accelerometer/gyroscope IMU streams. Do not describe the current pose CSV as raw IMU data.

## 2. Meta Quest Link / NVIDIA: an observed workaround

The original field notes report frequent Link crashes on the lab's newer NVIDIA-equipped PCs. The following change substantially improved stability in that environment:

1. Open Meta Horizon Link and Oculus Debug Tool:

   ```text
   C:\Program Files\Oculus\Support\oculus-diagnostics\OculusDebugTool.exe
   ```

2. Note the original values before changing them.
3. In the reported setup, **Video Codec** was changed from **Default** to **H.265**, and **Sliced Encoding** from **Default** to **Off**.
4. Test the complete VR task and record the headset, GPU, driver and Link versions with the result.

This is a historical lab workaround, not an official vendor fix or a required setting for every user. If the options differ or stability worsens, restore the recorded original values and troubleshoot that configuration separately. The source notes do not establish the exact affected GPU/driver matrix.

## 3. Networking: connected does not mean receiving data

In the original university-network deployment:

- The API/RTSP handshake succeeded.
- The UDP path received no eye-data packets.
- Connections were re-established roughly every 7.5 seconds.
- Switching to a WebSocket/TCP pathway provided reliable reception in the tested lab network.

The extracted preview implements a managed WebSocket RTSP/RTP gateway client. Its data transport is distinct from the Companion API's status WebSocket. An HTTP status response, successful handshake or connected indicator does not prove that eye samples are arriving.

Check separately:

1. The workstation and Companion are on a permitted network where they can communicate. Follow local network authorization procedures.
2. Companion's current IPv4/API/stream endpoint matches the plugin configuration.
3. The monitor's sample count increases and sample age remains recent.
4. Gaze/pupil fields and quality flags correspond to the stream actually available.
5. Alignment status and clock measurements are available where expected.
6. A short interruption produces a visible data gap/retry, followed by recovery without starting a new participant session.

Ask local IT about client isolation and permitted connections if necessary. Do not disable the firewall as a blanket solution. A transport workaround does not guarantee operation on other networks.

Official background: [network troubleshooting](https://pupil-labs.github.io/pl-realtime-api/dev/troubleshooting/) and [protocol/stream details](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/).

## 4. Eye measurements, fit and spatial mapping

Neon's gaze estimation is described by Pupil Labs as calibration-free. That does not establish accurate mapping from a particular VR mount into Unity world coordinates or objects. Treat these as separate questions:

- Is the device producing valid eye measurements?
- Is the physical fit/mount stable?
- Is gaze expressed in scene-image pixels, an SDK-converted representation or a validated Unity reference frame?
- Does the chosen measure support the intended analysis?

The current plugin does not implement calibrated gaze-to-Unity-object mapping. Do not interpret a region interval based on head location as a gaze target or an eye fixation.

The original notes propose relative gaze movement, pupil size and gaze duration as possible alternatives to precise absolute spatial measures. This remains a methodological proposal requiring validation. Those measures are not automatically reliable after mount shifts, and "gaze duration" needs an explicit definition and segmentation method. The plugin does not supply a fixation detector.

Keep original units/provenance. Earlier study demonstrations display the original SDK pupil representation; the extracted decoder uses documented wire-format pupil fields. Do not silently relabel or combine the two as equivalent measurements.

Official reference: [Neon data streams](https://docs.pupil-labs.com/neon/data-collection/data-streams/).

## 5. Recording and local archive

Choose a dedicated output folder. The original study used a workstation-specific directory; the package default is `NeonResearchData` under the host project. Do not assume a lab's path exists on another computer.

For each session, check:

- Anonymous participant ID and experimental condition are correct.
- A fresh session folder is created and participant snapshot saved.
- Required eye, pose and behavioral channels produce rows.
- Interaction callbacks are wired to real events, rather than only synthetic test markers.
- Optional video/audio is explicitly enabled, points to the right source and produces finalized files.
- Raw files, manifests, media and annotation exports remain available after Unity closes.

For optional capture, configure operator-installed Python, FFmpeg/FFprobe and the intended window/microphone. Verify actual sound and image coverage; process launch or file existence alone is not enough.

## 6. Timestamps, video and HTML review

The HTML review links video, eye traces and behavioral events for retrospective inspection. It can operate without reopening Unity or vendor software, using the completed session's exported files.

- Preserve native device, receive, process and aligned session times.
- Treat RTCP/Time Echo alignment as estimated and inspect alignment quality/gaps.
- Check video offsets against an observable marker. Capture launch time is not measured first-frame time.
- Verify marker alignment at the beginning, middle and end of a full task-length trial to assess drift.
- Keep video and annotations with the session folder; they are not all embedded in the HTML.
- Mark a point or interval, record the label/description, coder and evidence source, then export annotation JSON. Reimport that JSON to resume. CSV is for analysis.

Researcher observation, participant self-report and retrospective interview are distinct evidence sources. Manual labels are not automatic cognitive-state diagnoses.

## 7. Shutdown and incomplete sessions

The original study's final-screen freeze was traced to a shader-related problem and reportedly resolved, with an observed exit time of approximately 0.7 seconds. This is an experiment-specific observation, not a plugin performance guarantee or proof that all shutdown faults are shader-related.

After normal Stop, verify raw-stream closure, export errors, optional media finalization and replay generation separately. If a freeze recurs, preserve logs and partial files and distinguish rendering/XR teardown, acquisition shutdown, IO and encoder finalization. Do not force a manifest's closed flag to make an incomplete session appear successful.

## 8. Before a participant session

- [ ] Approved network access and room/equipment availability confirmed.
- [ ] Hardware fit, mount, cables and head Transform checked.
- [ ] Participant ID, condition, output folder and free space checked.
- [ ] Recent eye samples, field availability, drops and timing status inspected.
- [ ] Actual interaction callbacks verified with a short test.
- [ ] Required video/microphone sources and capture status verified.
- [ ] A complete task-length run has passed on this configuration.
- [ ] Stop/export, HTML opening and annotation JSON roundtrip checked.

Keep access links, credentials, workstation addresses, participant records and lab scheduling logistics in a separate private operator runbook. This public note intentionally excludes those details from the original field report.
