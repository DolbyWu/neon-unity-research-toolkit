# Research Pipeline: beginner tutorial

Snapshot: **2026-11-13, 13:38 EST (UTC−05:00)** · branch `feature/research-pipeline-20261113-1338-est` · package version `0.2.0-exp.1`.

This is a Windows Unity Editor research workflow for recording, reviewing and annotating Neon + XR studies. It is an **experimental research-pipeline snapshot**, not a new stable release or Asset Store submission. Automated checks do not establish scientific validity or replace a fresh device test of your own setup.

The core toolkit does not contain a ready-made VR game, teaching rooms or a task-specific scene sequence. Existing projects must supply their own XR rig, scenes, interactions and transition logic. Project-specific integration code is separate from the reusable packages.

## 1. Prepare a safe test project

1. Save and close your working experiment. Make a separate project copy and open that copy through Unity Hub. Keep the original unchanged.
2. Use **Unity 6000.3.23f1** for this snapshot. Let imports finish and resolve compiler errors before Play.
3. Choose a **new local data folder outside Git**, for example `C:\ResearchData\MyStudy`. Do not use a public cloud-sharing folder for identifiable recordings.
4. Start with an anonymous technical ID and a short local test. Obtain consent before recording a participant, voice, screen or other identifiable information.

Required for real-device tests: a supported Windows PC/XR setup, Quest connected through the host project's normal Link/OpenXR workflow, correctly mounted Neon hardware, and Neon Companion Streaming. The phone and PC must be on a network that allows device-to-device connections. A working internet connection alone does not prove local connectivity.

Optional media capture requires separately installed **Python and FFmpeg**. Local reviewed-sample building requires Python. Neither is installed automatically by this package.

## 2. Install this branch

Do not install this GitHub core beside another copy of the same toolkit or its Asset Store edition: shared assemblies and GUIDs can conflict. Upgrade only in the disposable project copy.

In **Window > Package Manager**, use **Install package from Git URL** (or the equivalent `+` menu in your Editor). Install in this order:

**A. GazeFlow core**

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.dolbywu.neon-research#feature/research-pipeline-20261113-1338-est
```

**B. Official Neon XR Core 2.0.0**, pinned to the inspected upstream commit:

```text
https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a
```

Follow the upstream package's setup for its XR dependencies, imported resources/Addressables, mount configuration and Neon XR objects. Installing GazeFlow does not configure an XR rig or replace the official mount calibration.

**C. Optional GazeFlow Neon XR adapter**

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.binyanxu.gazeflow.neonxr#feature/research-pipeline-20261113-1338-est
```

Alternatively, download this branch's source and use **Install package from disk**, selecting each package's `package.json`. Keep the checkout in a durable location: moving a locally referenced package breaks the project link. Install official Core before the adapter. For reproducible deployments, retain the exact Git commit in your project lockfile rather than assuming a branch name never changes.

The raw receiver and spatial adapter have different jobs. Keep **Receive Neon gaze** enabled when you need the original eye/pupil stream. The adapter adds official spatial rays; it does not silently replace raw acquisition. Running both creates two consumers and needs a network/load test on your hardware.

## 3. Configure recording and optional media

Open **Tools > Neon Research > Setup and Recording Settings**.

1. Set **Output folder** to the independent data folder.
2. Set **Experimental condition** to your study's condition code.
3. Enter **Companion IPv4 (no port)**, then the separately confirmed API and WebSocket ports. Typical defaults are API `8080` and WebSocket `8686`; use the actual device configuration, not an old address from a previous test.
4. Enable **Record on Unity Play (opt-in)**. Enable **Receive Neon gaze (opt-in)** for the raw eye stream.
5. Enable **Open folder + HTML after Stop** if desired.
6. Click **Save project settings**. Saving settings does not start recording.

Enable Eye State on the Companion if your study needs available pupil/eye-state values. In the monitor and CSV, verify recent sample counts, written samples, quality and gaps—not just a Connected label. Missing values are not evidence of zero pupil size. The existing pupil wire values and units are retained; this pipeline does not establish their physical accuracy on your particular setup.

For **full-monitor video and continuous microphone recording**:

- Enable **Enable optional media capture**, **Record video**, and **Record microphone**.
- Choose the installed Python and FFmpeg executables and the actual microphone device name. For a Quest microphone, confirm Windows can receive its signal before testing. Recording a microphone is not the same as capturing computer/game output audio.
- Set **Video source** to **Desktop** and explicitly enable **Allow desktop (explicit opt-in)**. Set **Desktop left/top** and **Capture width/height** to the intended monitor's actual desktop rectangle. A small width/height crops the desktop; it does not automatically mean “the whole monitor.”
- Close private windows and notifications. Every visible app in that rectangle may be recorded. Check the maximum capture duration is sufficient.
- Save settings. Each session still asks for recording confirmation. Desktop capture does **not** require opening the optional Quest Mirror preview.

Do a ten-second spoken test, stop, then open the actual audio and video files. Confirm framing and audibility before a full session.

## 4. Bind real spatial gaze and research targets

Use **Tools > GazeFlow > Research Target Setup (Experimental)** and the [detailed spatial setup guide](../Packages/com.dolbywu.neon-research/Documentation~/neon-xr-targets.md).

1. Configure the official Neon XR provider and mount calibration for the headset mounting.
2. Add **Neon XR Research Provider (Experimental)**. Assign its official `neon` provider and the **actual tracked head/camera Transform** as `headReference`. The stationary XR rig root is not a substitute for the moving head.
3. Set a meaningful versioned **Mount calibration ID**. Leave mapping approval off until measured/reviewed.
4. Add **Research Target** to every object you intend to study. Preserve its original object name. Assign a stable unique `targetId`, study `targetGroup`, optional display label, behavior kinds and explicit legacy event aliases. Two same-name instances must have different IDs.
5. Include child colliders under the correct target owner. Keep intervening solid occluders in the recorder's collision mask. Do not add or change gameplay physics just to detect gaze. Existing authored trigger AOIs require explicit opt-in and are labeled as volume hits, not visible-surface hits.
6. Give runtime-spawned objects deterministic non-reused spawn identities through the host integration. An unused prefab or a created target catalogue entry does not prove an object was observed or interacted with.
7. Add exactly one enabled **Gaze Target Recorder**, assign its provider and set the appropriate range/collision mask. Run **Check target configuration**.
8. Wire your game's real grab, release, placement, rotation, button and puzzle events to the toolkit with matching stable IDs or declared aliases. Names alone do not create a reliable link. The optional three-target sample is only a demonstration, not full scene coverage.

The raw live monitor displays the original image-space stream. Spatial object mapping is supplied separately by the optional adapter above.

## 5. Start a participant session

1. Have the wearer enter the host project's usual Quest Link/XR setup and keep Neon Streaming active.
2. Click Unity **Play**. With recording enabled, participant intake happens **before** game entry and capture.
3. Choose **New participant** or **Returning participant**. Enter an anonymous ID, such as `P001`; demographics/background fields are optional voluntary self-report. Keep names and contact details separately.
4. Enter the researcher/session notes if useful. Check the displayed save location, then **Save participant and continue**.
5. Complete the media/recording confirmation. Cancel means no recording starts.

Each recording creates a new session beneath `Participants/<ID>/Sessions/<new-session>/`; a returning participant receives another session rather than replacing the earlier one. The **Observation Marker** window does not start acquisition itself.

Open **Tools > Neon Research > Open Live Monitor**. Verify samples are arriving and being written. Check the correct scene and tracking in the headset independently; increasing counters alone do not prove the wearer is in the expected task.

## 6. Run two-round gaze validation

The reusable package exposes the validation window; automatic entry in a particular tutorial/pre-task room requires that host project's integration. In a generic project, open **Tools > GazeFlow > Pre-room Gaze Validation** during Play.

1. Confirm the one active research provider has the correct moving head binding and mount ID. Enable **Automatic five points**.
2. Click **Show five targets**, unless your integrated host already starts the preview. The system waits for a fresh worn signal. Confirm the wearer can see all five fixed targets without obstruction.
3. If necessary, reposition the **first unmeasured preview** before starting. Do not start with unseen targets.
4. On the computer's five-point map, click **one starting point**. That point pulses; the wearer looks at its centre. Default timing is **1 second settling + 2 seconds measurement**, then a **2-second gap**. The remaining points advance automatically.
5. With **Automatically fit and start round 2 when round 1 has enough data** enabled (the default), the provider checks collection quality automatically. Each point needs at least 70% valid coverage under the default policy, at least 12 valid head-local direction pairs and finite directions. A large first-round angular error alone does not block fitting. Insufficient collection shows **ROUND 2 NOT STARTED** and identifies points with their coverage/pair problems. Check signal/wearing, then **Retry round 1 — all five points**; earlier files remain saved. Reports are not combined across retries.
6. Sufficient saved collection automatically fits correction, shows the **SAME fixed world positions** and starts a **3-second continuously fresh-signal countdown**. Round two begins from the centre without another click. Keep watching and abort/skip if targets are no longer visible; the automatic start is explicitly recorded, not claimed as new operator visibility confirmation. Stream loss or a long update gap resets the countdown; changed session/head/calibration, abort or skip cancels it. Correction does not rewrite raw gaze or old reports. To change geometry, discard the correction and collect again. Advanced/manual mode retains the explicit fit and target-confirmation controls.
7. Review the saved second-round report. If it passes, the setup is unchanged and the live signal is fresh, tick the review confirmation and click **Approve this mapping for AOI**. Approval is a separate researcher declaration—not automatic, and not clock synchronization.

The current default project policy is **mean angular error ≤3° for every target and valid coverage ≥70%**. The previous 2° cutoff and current 3° cutoff are engineering policies, not universal scientific acceptance standards. Loosening a threshold does not improve measured precision. Repeating fixed positions checks repeat-layout performance; it does not establish accuracy at every scene depth or object.

If verification fails, do not force approval. Check wearing, mount configuration, head binding and signal quality, then repeat as needed. In the integrated trial, completed round two may continue to the task even if failed; generic hosts must decide their own scene transition behavior. Raw data and clearly labeled low-precision geometric observations remain independent of approval.

To hide only the visible gaze dot/line, open **Tools > GazeFlow > Neon XR > Gaze Pointer** and choose **Hide gaze pointer**. This does not stop acquisition or approve mapping. Do not use the pointer's apparent alignment as a replacement for measured validation.

## 7. Mark an observed episode during play

Open **Tools > GazeFlow > Live Observation Marker** if its desktop window is not visible. This is an Editor interface for the researcher, not a headset overlay.

1. Enter an anonymous observer ID.
2. Click **START OBSERVATION** when the episode begins.
3. Click the same button, now **END OBSERVATION & WRITE NOTE**, when it ends. The end time is fixed immediately.
4. In the popup, enter an **Observation label** and what you observed. Click **Save observer note**. Typing time is excluded; the game is not paused.
5. Repeat, up to **10 intervals per session**. Use **Write / edit observer note** to complete an earlier note during recording. Save notes before stopping Play.

The interval is saved even if you close its note popup without writing. A still-open interval at session shutdown is marked **interrupted**, not a normal researcher-ended interval. Observation markers do not require valid eye data or approved AOI mapping. Describe visible behavior separately from your tentative interpretation.

## 8. Stop and check the session folder

Stop Unity Play normally. Wait for media finalization before moving or reviewing the folder. If enabled, the finished folder and HTML open automatically; otherwise use the live monitor/archive menu to find the session.

Depending on enabled streams, inspect:

| Output | What to check |
| --- | --- |
| `export_manifest.json` | Session identity, closure, duration and export errors |
| `Neon-*/NeonGaze.csv` | Raw available eye/pupil values, quality, connection epochs and timing fields |
| `game_events.csv`, `behavior_events.csv`, `head_pose.csv`, region files | Real behaviors, tracking/context and session times |
| `target_catalog.csv` | Names, unique IDs, groups and explicit behavior associations |
| `gaze_targets.csv` | Accepted target fields and explicit validity/mapping/gap states; inspect candidate columns separately |
| `gaze_targets_approximate.csv` | Separately labeled unverified low-precision geometric observations, not accepted AOI evidence |
| `Calibration/` | Per-run reports, measured samples and any saved correction evidence |
| `live_observations.json` / `.csv` | Observer-selected start/end times and notes |
| `Video/`, audio/media metadata | Actual framing, sound, completed files and timing limitations |
| `Replay/study-replay.html` | Offline review page for this session |

Enabled streams can fail or be absent; the existence of a file or header is not proof of real data. Distinguish unverified mapping, invalid gaze, stale/disconnected signal, a valid ray with no target, non-target occlusion and an accepted target hit. Missing/stale data never become valid zeroes or continued target coverage.

**Audio is not automatically transcribed.** This standalone snapshot has no built-in OpenAI transcription/upload button. A host-specific helper/button, if separately installed, is a different optional workflow requiring explicit consent, a locally stored API key and possible API charges. Never put a key in offline HTML, a scene asset or Git. Check whether an actual transcript exists before describing a session as transcribed.

## 9. Review video and obtain player explanations

Open `Replay/study-replay.html` in a local browser. Keep the surrounding session/media folders together. The HTML embeds review data, but the video remains a separate local file, normally `../Video/quest-view.mp4`. Use the page's local video-loading control if needed.

1. Check video coverage and the saved offset against a recognizable event. The video time and experiment time are different clocks. Software launch timing is an estimate, not hardware synchronization; do not invent exact alignment across gaps.
2. Under **Manual annotations**, find **Live researcher observations**. Orange timeline marks show their intervals and the original observation text remains visible.
3. **Play this interval** seeks to the start and pauses at the end when the video covers it. **Review segment / add player response** selects the interval, seeks its start and prepares a separate retrospective-interview annotation.
4. Ask “What happened here?” and “What were you thinking?” Enter the player's explanation, an anonymous annotator ID and optionally their integer **confusion rating 1–5**: 1 = not confused, 5 = very confused. Leave missing ratings blank.
5. Click **Add annotation**. It creates a new record linked to the original observation ID, not a replacement for the researcher's note. More intervals and multiple annotations are allowed.
6. For other moments, pause or drag the video, **Mark current frame / time**, choose a label or description, then **Add annotation**. Use **Add another annotation here** to start another record instead of editing an old one.

The legacy optional **0–10 participant intensity** field is a different measure from the **1–5 retrospective confusion** rating. No automatic conversion occurs. Retrospective self-report, observer interpretation, quotations and annotator confidence should remain separate sources.

### Save, resume and export

- **Export annotations JSON** is the authoritative save/resume action. Save the downloaded file beside your private session. The offline browser page cannot silently overwrite its original HTML or raw recording files.
- Reopen the same session HTML and **Import annotations JSON** to restore records, revisions, ratings, links and saved video offset. Export again after changes. Unsaved browser-memory annotations can be lost when closing/reloading.
- **Export annotations CSV** produces an analysis table, including the separate confusion rating and observation link. Keep JSON too; CSV is not the restore format.
- Select an experiment-time interval to export raw eye/event data, filtered target records or the complete interval JSON bundle. The bundle includes overlapping observer intervals and currently saved manual annotations. Filters do not change original CSVs.
- Target/group/interaction filters use stable identities or explicit aliases, not fuzzy name matching. A target callback or pupil change is not a fixation, attention, comprehension or a diagnosed cognitive state.

For independent human coding, export separate annotator files. The page's merge/comparison workflow preserves disagreements; reconcile deliberately rather than overwriting another person's interpretation.

## 10. Build quality-filtered analysis samples

First review the annotation intervals and set eligible records' **Review status** to **Reviewed**. Resolve or explicitly exclude overlaps/conflicts. A reviewed code is a human decision, not objective ground truth. Export the updated annotation JSON.

With Play stopped, open **Tools > GazeFlow > Build Reviewed Samples (Offline)**:

1. Choose the Python executable, closed session folder and reviewed annotation JSON.
2. Choose a **NEW output folder**. The builder preserves original recordings.
3. Confirm window length and minimum valid eye coverage. Default windows are 2-second, non-overlapping windows within each annotation; short tails are audited, not silently stretched.
4. Enable **Also require measured AOI quality** only if the analysis requires approved, measured spatial evidence. Low-precision/unverified target candidates do not satisfy that criterion.
5. Click **Build reviewed samples locally**, wait for completion and inspect `samples.csv`, `curated_intervals.csv`, `audit.csv` and `manifest.json`.

Draft/disputed/excluded or unresolved labels, active overlaps, validation periods, inadequate quality and unlabeled periods receive explicit handling. Unlabeled time is **not** automatically a “not confused” class. A point annotation is not silently expanded into a training interval. The audit and input hashes are part of reproducibility, not disposable extras.

This builder does not train a classifier or turn optional ratings into an automatically validated prediction target. Before machine learning, define the label policy, examine class balance and missingness, and split training/test data by participant/session/task—not adjacent random windows. Keep raw, AOI and media timing limitations explicit.

When supplied, `confusion_rating_1_5`, its participant-retrospective source and `related_observation_id` are retained in the curated interval and sample exports. They remain human-report provenance, not automatic cognitive truth. The legacy 0–10 intensity field is retained independently.

## 11. Your first acceptance checklist

Before enrolling participants, do a short disposable technical run and verify each item rather than assuming earlier tests cover a changed setup:

- Participant IDs and successive sessions remain separate; no prior folder is overwritten.
- Eye data are recent and written, available pupil values are inspected, and head/behavior events reflect actual actions.
- Both validation rounds use the same fixed visible targets; saved error/coverage match the report; approval is not silently transferred to a changed head/mount binding.
- Known targets, head motion, front-object occlusion and different real interactions produce the intended IDs/statuses.
- Stream interruption/reconnection produces explicit gaps, not repeated old hits or extra coverage.
- Whole-monitor framing and microphone audio are correct; stopping closes playable media.
- At least two live observation intervals and notes survive into the generated HTML.
- Player responses/1–5 ratings survive JSON export, page reload/import and CSV export; video seeks and stops at the intended interval.
- Selected-interval exports and optional curated outputs preserve identities, quality flags and declared timing limitations.

Report **pass / fail / not tested** separately for raw acquisition, adapter, accepted AOI, low-precision observations, reconnect recovery, media/archive, HTML, annotations and curated exports. The snapshot's software tests are evidence about those specific automated cases, not a blanket hardware-acceptance claim.

Keep real participant data, videos, audio, transcripts, annotations, mount/device configuration and credentials out of public Git. Share only consented/de-identified material through your approved research-storage process.
