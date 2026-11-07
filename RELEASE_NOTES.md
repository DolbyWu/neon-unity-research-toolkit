# 0.1.0-preview.4 — 7 November 2026

Public pilot-use preview of the unofficial Neon Unity Research Toolkit. Windows Unity Editor 6000.3.23f1 is the tested environment. This is a GitHub preview release, not Asset Store approval or vendor certification.

## Install

Download `com.dolbywu.neon-research-0.1.0-preview.4.tgz` and `SHA256SUMS.txt` from the [release](https://github.com/DolbyWu/neon-unity-research-toolkit/releases/tag/v0.1.0-preview.4). Verify the checksum and install with Unity Package Manager's **Install/Add package from tarball**. Do not edit or repack the signed archive. Acquisition remains off by default. Follow [Quick Start](QUICKSTART.md); configure your own XR project and explicitly wire head/interaction sources.

The release distributes the exact signed artifact used for the hardware trials, SHA-256 `293cf019a2f168cad88724346b32f49c150112318c512b521c2a835688bb5022`. Its embedded README/changelog preserve the pre-publication candidate wording. These repository/release notes add the subsequent hardware test record; the signed file has not been rebuilt from publication-day documentation.

## Changes and evidence

- Fixed scene persistence for the three public MonoBehaviours with separate matching script files and stable GUIDs.
- Added serialization regressions and explicit JSON/physics/IMGUI dependencies for minimal hosts; included required import metadata.
- Final unsigned package acceptance: 26/26 Unity checks, two offline browser suites and 14/14 synthetic Python tests. Signed payload integrity and fresh import checked; short real Neon + Quest 3/media/review trials subsequently exercised the signed archive.
- Real Neon + Quest 3 core-function tests passed. See the [test results](docs/hardware-acceptance-2026-11-07.md) and [Validation](VALIDATION.md).

## Upgrade and known issues

- Back up scenes. Earlier Missing Script components are not automatically repaired: remove only the broken component, re-add it and restore its fields and callbacks.
- Keep data paths short and local. Recording optional media requires separately installed Python/FFmpeg and explicit confirmation. Verify the selected microphone, intelligible speech, video view and complete durations after each test.
- Network interruptions can create gaps even with automatic reconnect. Review epoch, quality, alignment and missing intervals rather than accepting a connection message alone.
- This artifact includes a harmless orphan Python-cache folder metadata entry; invoking Python can also create cache files in Unity's immutable package cache and cause metadata warnings. Such warnings were seen in the interactive host; the tested capture/export nevertheless completed. They are not a missing-data indicator. Do not alter the signed archive to suppress them. Packaging/cache hygiene remains follow-up work.
- Static validation strings in the signed package are conservative pre-trial provenance, not a live certification status. The installation-from-tag route has not been separately revalidated after publishing this tag.
- Pupil values use documented wire units; clock and media offsets are estimates rather than hardware synchronization. Windows Editor is the supported preview scope; other platforms and standalone/Android builds are unverified. No automatic cognitive-state inference, gaze-to-object mapping, fixation/saccade analysis or cloud transcription is included.

No raw test recordings or private device/network details are distributed. MIT applies to the author's owned plugin code/software documentation; third-party notices and demonstration-media exclusions still apply.
