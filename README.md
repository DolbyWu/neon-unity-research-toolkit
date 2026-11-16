# GazeFlow — Neon Research Toolkit for Unity

**Record the experience. Mark what you observe. Review it with the participant.**

An unofficial, open-source research plugin for **Pupil Labs Neon + Unity**. GazeFlow brings available eye/pupil data, head pose, explicitly connected task events, optional local video/audio and human annotations into one participant/session workflow. It aims to lower technical barriers without presenting human interpretation as automatically detected cognition.

**Candidate:** `0.2.0-beta.2` · **Scope:** Windows Unity Editor `6000.3.23f1` · **Status:** independent packaged core checks passed 88/88; packaged official-SDK/adapter checks passed 97/97. Physical-device acceptance remains pending before promotion to main. See the [current acceptance checklist](docs/beta-0.2.0-beta2-acceptance.md). This is the free UPM line, not an Asset Store submission or a replacement for official Neon XR Core.

**Start here:** [Quick Start](QUICKSTART.md) · [Illustrated controls](docs/illustrated-quickstart.md) · [Generic XR setup](docs/universal-neon-xr-setup.md) · [Validation](VALIDATION.md)

**New in beta.2:** [Object identities and CSVs](docs/object-statistics-ownership.md) · [Tutorial / Study scope](docs/tutorial-study-segments.md) · [Save annotation JSON beside HTML](docs/annotation-folder-saving.md) · [Information surfaces](docs/information-surface-aoi.md).

**Maintainer handoff:** [Current beta.2 results and next hardware steps](docs/beta-0.2.0-beta2-acceptance.md). The [older beta.1 handoff](docs/resume-beta-hardware-20261115.md) is historical context only. Main promotion remains held; preparation captures are not completed wearer acceptance tests.

## The workflow

1. **Configure:** choose a private data folder, current Companion connection and optional media channels.
2. **Identify and record:** press Play, confirm an anonymous participant and recording consent, then run your own experiment.
3. **Optionally adjust gaze:** show an authored reference and the cyan research pointer, nudge the local direction, save and hide. Extra adjustment and accuracy checks are not recording gates.
4. **Observe:** use the yellow button to start/end an interval and write an observer note.
5. **Review:** open the offline HTML, pause/scrub video, revisit the interval with the participant and add their explanation, labels and optional ratings.
6. **Retain:** export annotation JSON to save/resume, CSV for analysis, and complete interval bundles.

Your project supplies its own XR rig, scenes and task. GazeFlow does not replace an experiment or automatically wire every interaction.

## What is included

| Module | Capability |
|---|---|
| Participant sessions | Anonymous IDs, optional profile details and separate session folders |
| Neon recording | Available raw eye/pupil fields, timestamps, alignment/quality flags and gaps |
| Head and events | Explicit head Transform pose and connected grab/release/custom task events |
| Optional Neon XR adapter | Official spatial research rays, authored AOI/target IDs, nearest-surface hit/occlusion records and source binding |
| Optional gaze adjustment | Cyan direction controls, explicit save receipts and a visual reference on any authored Transform |
| Live observation | Yellow start/end interval button, observer notes, up to 10 episodes per session |
| Manual annotations | Local HTML video/timeline review, multiple point/interval labels, evidence sources and participant responses |
| Exports | Original CSV, offset report/history, JSON annotation save/resume, CSV and interval bundles |

The default path is **Configure → Record → Observe → Review**, not a mandatory multi-round calibration procedure.

## Install

Back up your project and test an independent copy first. Do not install beside a second copy of the same toolkit or its store edition: assembly/GUID conflicts are possible.

### Git URL

In **Window > Package Manager > Install/Add package from Git URL**, install the core:

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.dolbywu.neon-research#codex/gazeflow-beta2-integration-20261116
```

For 3D research gaze and desktop adjustment, install official Neon XR Core at the inspected commit, then the matching adapter:

```text
https://github.com/pupil-labs/neon-xr.git?path=/com.pupil-labs.neon-xr.core#f4dee47b9690a059b1c4660628596d7303ade59a
```

```text
https://github.com/DolbyWu/neon-unity-research-toolkit.git?path=/Packages/com.binyanxu.gazeflow.neonxr#codex/gazeflow-beta2-integration-20261116
```

Use the same core/adapter version. A branch is movable; retain the exact resolved commit and lockfile for reproducible studies. Official Core requires its own XR/dependency/mount configuration; entering an IP in the raw recorder is not a substitute.

### Download ZIP / no Git

Choose this candidate branch, then **Code → Download ZIP**. Extract it into a stable location and use **Install/Add package from disk**, choosing `Packages/com.dolbywu.neon-research/package.json`. Install official Core before the optional adapter's `package.json`. Keep the folder in place. Candidate tarballs built locally are unsigned and are not the historical signed preview release.

Open **Tools > GazeFlow > Start Here** for the current entry points. Installation does not start recording, enable media, connect paid services or choose a participant.

With the adapter installed, **Tools > GazeFlow > Neon XR Setup** configures your explicitly selected head, official provider and optional authored visual reference. See the [generic setup guide](docs/universal-neon-xr-setup.md).

## Try it without hardware

`VerificationProject` is a minimal core-only test host. `VerificationXRProject` adds the pinned official SDK and adapter for compilation/synthetic checks. Neither contains a playable experiment or fabricated participant gaze. On Windows, use a short project path for verification so generated test paths stay within platform limits.

Start with **Record on Unity Play** enabled but Neon/media disabled. Use an anonymous technical participant for a short data-only session. Stop and inspect the manifest, head/events when supplied, generated HTML and Manual annotations. Use **Export annotations JSON** before closing and import it to resume. No eye samples should appear with Neon disabled.

## Important distinctions

- Official Neon XR supplies the spatial mapping. Our manual adjustment is a reversible research-ray rotation, **not official Companion calibration** or measured residual accuracy.
- Image-space X/Y, corrected 3D rays, observed AOI hits and independently verified mapping are separate. Ray hits are not fixations, attention, understanding or cognitive-state labels.
- Head pose is Unity position/quaternion rotation, not raw IMU accelerometer/gyroscope recording.
- Missing or stale samples remain gaps. Video and callback/eye timestamps are not claimed to be hardware synchronized.
- The optional 1–10 participant intensity rating is distinct from the retrospective 1–5 confusion rating; neither is inferred from physiology.
- Python/FFmpeg media tools are separately installed and explicitly opted in. Microphone recording is not automatically system/game audio. Close private windows before desktop capture.
- Behavioral events require explicit integration with your interaction framework; see the [Quick Start](QUICKSTART.md).

## Demo and documentation

[Watch the recorded workflow demo](https://youtu.be/ZujH4r9jMH4). It illustrates a configured study, not automatic configuration of every Unity project or an accuracy certification.

- [Screenshot tutorial](docs/illustrated-quickstart.md)
- [Generic Neon XR rig, source binding and visual reference](docs/universal-neon-xr-setup.md)
- [Desktop adjustment and offset report](docs/desktop-gaze-adjustment.md)
- [Live researcher observations](docs/live-observer-intervals.md)
- [Current Tools menu](docs/tools-menu-guide.md)
- [Validation scope and remaining acceptance](VALIDATION.md)
- [Release notes](RELEASE_NOTES.md)

Earlier research-pipeline integration sources under `Integrations~` are historical project-specific examples, not required dependencies or part of either UPM payload. Existing experimental and store versions remain available on their original branches/tags; this candidate does not alter their installed projects or recordings.

## License

MIT. See [LICENSE](LICENSE) and [third-party notices](<Packages/com.dolbywu.neon-research/Third Party Notices.md>). Unofficial and not endorsed by Pupil Labs, Unity or Meta.
