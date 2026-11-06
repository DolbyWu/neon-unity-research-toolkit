# Neon Unity Research Toolkit — independent extraction preview

Version **0.1.0-preview.1**, created 6 November 2026. Unofficial research tooling, not a Pupil Labs or Unity product. This is a separately compiled development preview, **not a participant-study or public-release certification**.

**Start here:** [features, setup, Neon connection and 60-second acceptance checklist](QUICKSTART.md). Measured tests and remaining limits are in [VALIDATION.md](VALIDATION.md).

Personal repository: [DolbyWu/neon-unity-research-toolkit](https://github.com/DolbyWu/neon-unity-research-toolkit) (**private preview**). This is separate from Ghostlab-Studios/EscapeRoom. Repository access is required for other users; no public release/license clearance is claimed.

## Your working EscapeRoom is preserved

- The working `EscapeRoom-Neon-Publish-20261014` project was read only. Nothing in it was re-wired to this package.
- This directory has its own package, blank verification host, settings and default output folder. It contains no participant recordings/profiles, live phone address, credentials, original `.git` history, game scenes/assets or third-party native SDK binaries.
- Do not install this preview into the working EscapeRoom yet. Test the sibling verification project or another disposable project first.
- The extraction manifest records hashes of the 24 selected source files. The validation report includes a final comparison with the originals.

## What is here

```text
Packages/com.dolbywu.neon-research/   installable local UPM source package
VerificationProject/                separate, minimal Unity test project
Artifacts/                          ignored, synthetic browser/media outputs
EXTRACTION_MANIFEST.json             source provenance (no copied live configuration)
VALIDATION.md                       measured checks and remaining limits
```

The source package includes anonymous participant intake/profile snapshots, raw head pose, explicit grab/custom game events, generic region components, Neon transport/clock estimation, an English monitor, the offline video/manual-annotation HTML, and optional Python/FFmpeg capture helpers. Every user-facing menu starts with **Tools > Neon Research**. Runtime types and Editor state use the independent `NeonResearch` namespace/prefix.

## Safest first opening

1. In Unity Hub, add/open **VerificationProject**, using **Unity 6000.3.23f1**. This is not the old EscapeRoom project.
2. Let the Unity registry restore JSON/test dependencies. No Meta, OpenAI, MCP, Pupil Labs Unity SDK or EscapeRoom dependency is required to compile this preview.
3. Open **Tools > Neon Research > Setup and Recording Settings**, then **Open Live Monitor**. Installation defaults are inert: recording, Neon reception and media are all off.
4. For a synthetic/manual event check, explicitly enable **Record on Unity Play**, choose an independent folder, leave Neon/media off, save, then Play. The participant form opens before recording.
5. For actual head pose, assign your head/camera Transform using **Head Pose Source** (otherwise Main Camera is used). Configure **Study Region** colliders. Wire the **Interaction Adapter** methods to real grab/release UnityEvents, or call `StudyEvents` from your game. Grabs do not record automatically without an adapter.
6. Stop normally. The finished session folder and HTML open. Load a corresponding finalized local video if available, annotate, export annotations JSON and import it later to resume. Raw head pose remains in CSV; it is not newly plotted in this HTML.

For another disposable project, use **Package Manager > Install/Add package from disk** and select `Packages/com.dolbywu.neon-research/package.json`. Do not open the package directory itself as a Unity project. A separately generated `.tgz`, if provided, can be installed from tarball.

## Actual Neon testing is still required

This preview uses an independently implemented decoder for documented 9/17/25/65/89/105-byte Neon wire payloads, not the original SDK's converted representation. A raw payload CSV is retained for supported/unsupported packet inspection; queue drops and unsupported lengths are counted. Monocular-only packets are not turned into invented binocular gaze.

Wire pupil fields are labelled millimetres per the [official protocol](https://pupil-labs.github.io/pl-realtime-api/dev/guides/under-the-hood/), **not physically validated measurements from this preview**. This does not relabel or rescale any old EscapeRoom data. Real phone connection, Eye State availability, pupil magnitude, sample quality, reconnect/Stop behavior and full task duration must all be rechecked. No real device was connected for extraction tests.

No gaze-to-VR-object calibration, fixation detection or automatic cognitive-state inference is implemented. Time Echo and video offsets remain estimates, not hardware synchronization. Generic events must be explicitly connected to the host game's interaction framework. Optional capture still requires separately configured Python/FFmpeg and per-session confirmation; full desktop DPI/cropping and full-duration media remain unvalidated.

## Privacy and release

Do not point this preview at the working `C:\VRStudyData\PlayTests` archive during initial tests. Its defaults use **NeonResearchData under the new host project**; settings are under that host's **UserSettings/NeonResearch**. Do not publish test archives or credentials. License/ownership review and real-device verification are required before public distribution. The independent preview repository is intended for the personal **DolbyWu** account, not the original Ghostlab-Studios EscapeRoom repository; its private upload is separate from the original extraction.
