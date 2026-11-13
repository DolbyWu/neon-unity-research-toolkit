# GazeFlow — Research Pipeline

**Timestamped experimental snapshot:** 2026-11-13, 13:38 EST (18:38 UTC)

**Branch:** `feature/research-pipeline-20261113-1338-est`

**Package version:** `0.2.0-exp.1` · Windows Unity Editor `6000.3.23f1`

GazeFlow is an unofficial Unity + Pupil Labs Neon research toolkit. It connects participant/session organization, eye and head data, authored interaction events, optional local media, live researcher observations and offline participant reflection. Its purpose is to lower technical barriers to multimodal research while keeping measurements, missing data and human interpretations distinct.

This branch is the **Research Pipeline version**, not the default `main` branch, a new signed release, or an Asset Store submission. Package versions remain unchanged; identify this snapshot by its branch and commit. No participant archives, fresh study recordings, live configuration or API keys are included.

**Start here:** [Beginner tutorial](docs/research-pipeline-tutorial.md) · [Snapshot contents and checks](docs/research-pipeline-snapshot-20261113.md) · [Live observation instructions](docs/live-observer-intervals.md)

## The complete workflow

1. **Prepare:** choose a private data root, connect the real tracked head and check Neon streaming.
2. **Identify:** confirm an anonymous participant and a new session before recording.
3. **Validate:** choose one starting point for round one. With automatic two-round flow enabled, sufficient collection triggers bounded direction fitting and a 3-second fresh-signal countdown into NEW round-two samples at the SAME fixed world targets. Insufficient collection lists the blocked points instead of silently waiting for a fit button.
4. **Record:** preserve raw gaze/pupil availability, head pose, explicitly integrated behavior, target-ray records and optional desktop/microphone media.
5. **Observe:** click once to start an observer interval, once to end it, then write the researcher note in the popup.
6. **Reflect:** replay the selected episode with the participant; save their explanation and optional confusion rating separately.
7. **Export:** retain the original archive, offline HTML, annotation JSON/CSV and interval bundles; optionally build quality-filtered, human-reviewed sample tables.

These steps do not infer a participant's cognition automatically.

## What this snapshot includes

| Area | Capability |
|---|---|
| Participant archive | Anonymous participant IDs, new per-session folders, profile snapshots and existing archive workflow |
| Raw acquisition | Available Neon eye/pupil fields, payloads, native/receive times, alignment and validity flags |
| Head and behavior | Tracked head world position/quaternion rotation; explicitly connected grabs, releases and custom study events |
| Optional spatial adapter | Official Neon XR Core bridge, research target IDs/names/groups, nearest-collider hit/occlusion records and binding checks |
| Two-round validation | Five fixed targets; bounded head-local direction fitting; new repeat-layout measurement samples; explicit approval |
| Live observation | Desktop **Observation Marker**: start/end toggle, note popup, up to 10 intervals per session |
| Offline HTML | Local video pause/scrub, experiment/video clocks, eye traces, target/event filters, orange observer intervals and manual annotations |
| Participant reflection | Separate retrospective explanation, optional integer confusion rating **1–5**, linked original observation |
| Save and export | JSON save/resume, CSV, selected intervals and full interval bundles |
| Reviewed samples | Offline deterministic curation of reviewed labels and quality flags; no automatic model training |

The existing optional 0–10 participant intensity is a **different field** from the new 1–5 retrospective confusion rating. Neither is inferred from eye data. Head pose is not a raw accelerometer/gyroscope stream. Authored interaction bindings and real hardware remain necessary; the toolkit does not automatically understand every scene object.

## Installation — this branch

Back up your study and work in an independent copy. Do not upgrade an ongoing production experiment in place.

### No Git installation required

1. Open [this branch on GitHub](https://github.com/DolbyWu/neon-unity-research-toolkit/tree/feature/research-pipeline-20261113-1338-est).
2. Choose **Code → Download ZIP**, extract it and keep the extracted folder in a stable location.
3. Open your project using Unity `6000.3.23f1`.
4. In **Window → Package Manager**, choose **Install/Add package from disk**.
5. Select `Packages/com.dolbywu.neon-research/package.json`.

This installs the core without starting acquisition. The core can be checked without a headset or eye device. Open the included `VerificationProject` for an isolated minimal host; it is not a complete VR experiment.

### Git UPM installation

With Git installed, use **Install/Add package from Git URL**:

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.dolbywu.neon-research#feature/research-pipeline-20261113-1338-est
```

For spatial gaze, install official Neon XR Core at the recorded commit, then the optional adapter:

```text
https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a

https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.binyanxu.gazeflow.neonxr#feature/research-pipeline-20261113-1338-est
```

Official Core package version at that reference: `2.0.0`. Follow the [tutorial](docs/research-pipeline-tutorial.md) for dependencies, actual XR head binding, mount configuration and scene integration. A Git branch can move: record the resolved commits for an actual study. This snapshot does not publish new signed tarballs.

## First safe recording

1. Open **Tools → Neon Research → Setup and Recording Settings**.
2. Choose your private output folder. Enable **Record on Unity Play (opt-in)**; leave Neon and media disabled for an initial data-only check.
3. Save settings. Bind a real tracked head for VR, not an untracked Editor camera.
4. Press Play and confirm participant details. Existing participant IDs may be reused, but each recording must create a separate session.
5. For observation intervals, open **Tools → GazeFlow → Live Observation Marker**. Click **START OBSERVATION**, then **END OBSERVATION & WRITE NOTE**, type the note and save it.
6. Stop Play normally. Check the closed manifest, CSV files and generated HTML.

The note popup does not pause gameplay. The second click fixes the interval end immediately, so typing time is not included. An unfinished interval is marked interrupted on shutdown. Up to 10 live intervals are allowed per session; offline manual annotations are not limited to those 10.

## Replay and participant interview

Open the new session's `Replay/study-replay.html`. **Manual annotations** stays directly below eye data; Game log remains last.

Under **Live researcher observations**, select **Review segment / add player response** or **Play this interval**. Review the actual video if it covers the selected experiment time. Ask what happened and what the player was thinking, enter their explanation, optionally record confusion from **1 = not confused** to **5 = very confused**, then add the annotation.

Use **Export annotations JSON** to save/resume and CSV for analysis. Import the JSON into the same session's HTML to continue editing. Merely typing, closing the browser or saving a CSV does not save a resumable annotation project. The offline page does not silently rewrite raw files.

Audio recording and speech transcription are separate. **This standalone package does not currently include a cloud transcription button or automatically transcribe recorded audio.** A study-specific helper, if present elsewhere, is not part of this branch's portable workflow.

## Data quality and interpretation

- The former 2-degree validation cutoff was a project engineering starting point, not an official universal acceptance standard. This trial now defaults to **3 degrees mean error per target** and **70% valid coverage**. Changing the tolerance does not improve actual precision or revise historical reports.
- Round two uses new measurement samples at the same fixed world target centres. It checks repeat-layout alignment, not generalization to every depth or object in the experiment.
- Accepted AOI hits require the provider's explicit verification/binding checks. Fresh unverified geometric candidates are retained separately as **LOW_PRECISION_UNVERIFIED_NOT_ACCEPTED_AOI**, not silently promoted to accepted hits.
- Invalid, unworn, stale and disconnected samples remain missing/invalid. They do not become valid zero values or continued target coverage.
- Image-space gaze, corrected world rays, Unity callback times and media timing estimates have different semantics. Software video offsets are not hardware synchronization.
- Ray hits are descriptive geometric observations, not automatically fixations, attention, understanding or cognitive states.
- Missing media or an unconfigured interaction does not prove the participant was inactive.

## What is and is not verified

The branch contains automated Unity, synthetic browser/video and offline curation checks. The [snapshot report](docs/research-pipeline-snapshot-20261113.md) lists the exact outcomes and distinguishes them from real-device acceptance.

The existing study has been exercised with real devices, but earlier runs do **not** validate every new change. The latest two-round geometry/tolerance and live-observation integration still need a new end-to-end Neon + Quest check. Windows Editor is the target; standalone builds, Quest Android and other platforms are not claimed as newly validated.

## Documentation and source layout

- [Step-by-step research pipeline tutorial](docs/research-pipeline-tutorial.md)
- [Timestamped snapshot and reproducibility notes](docs/research-pipeline-snapshot-20261113.md)
- [Live observer intervals and retrospective review](docs/live-observer-intervals.md)
- [Five-point validation and current policy](docs/automatic-five-point-validation.md)
- [Evidence-aware annotations and reviewed samples](docs/reviewed-research-workflow.md)
- [Optional official Neon XR target integration](Packages/com.dolbywu.neon-research/Documentation~/neon-xr-targets.md)
- [Existing-study integration source reference](Integrations~/ExistingStudyTrial/README.md)
- [Legacy core Quick Start](QUICKSTART.md) — use this branch's tutorial for experimental additions
- [Changelog](Packages/com.dolbywu.neon-research/CHANGELOG.md)

`Packages/` contains the reusable toolkit and optional adapter. `Integrations~/` contains source-only host integration reference material, not a complete game or an automatically imported sample. `VerificationProject/` is an isolated test host. `Tools/` and package verification scripts use synthetic fixtures.

## Privacy, licensing and release boundaries

Use anonymous identifiers and an approved consent process. Keep identity mappings separately. Choose an output root outside Git. Entire-desktop recordings may capture unrelated windows or notifications; inspect the desktop and obtain appropriate consent before recording. Optional cloud processing requires separate authorization and secure local keys.

No participant archives, new recordings, live device addresses, secret settings or external executables are uploaded with this snapshot. Existing repository demonstration media are historical materials, not newly supplied participant data.

This is an independent, unofficial project, not endorsed by Pupil Labs or Unity. Plugin software remains under the [MIT License](LICENSE); third-party components keep their own licenses. Demonstration photos/videos, research data and independently owned study assets are not relicensed by the code license. See [media permissions](docs/media/README.md) and [third-party notices](<Packages/com.dolbywu.neon-research/Third Party Notices.md>).

The previous [public signed preview](https://github.com/DolbyWu/neon-unity-research-toolkit/releases/tag/v0.1.0-preview.4), `main` and the submitted Asset Store edition are separate from this branch.
