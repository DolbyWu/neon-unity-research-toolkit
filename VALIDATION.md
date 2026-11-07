# Validation and known limitations

This report describes measured software checks, not certification of a physical study setup.

## Environment and scope

Package `com.dolbywu.neon-research`, version **0.1.0-preview.2**. Tested on Windows using **Unity 6000.3.23f1** in the included **VerificationProject** with local-package installation. Other projects/platforms and install-from-tarball require their own acceptance checks.

## Automated results — 6 November 2026

- **18 / 18 Unity EditMode cases passed**, 0 failures. The completed-session test generates HTML from the actual resource and verifies video controls, multiple-annotation action, panel order, embedded data and pupil-mm field names. Other coverage includes acquisition-off defaults, participant snapshots/path boundaries, head/events export, explicit interactions, generic regions, supported/unknown wire payloads and basic clock/transport validation.
- **Two offline Edge/Playwright suites passed.** Coverage includes pause/seek, frame-linked video/experiment clocks, five independent annotations with earlier records preserved, explicit editing, description-only notes, legacy JSON import, JSON save/resume, interval/raw exports, CSV escaping, no HTTP requests and desktop/mobile layout.
- **14 / 14 synthetic Python archive/capture tests passed.** Coverage includes localhost access boundaries and generated video/audio finalization. A sandboxed localhost socket was blocked initially; the permission-approved run passed.
- The complete `Tools/Run-Checks.ps1` run exited **0**. Synthetic fixtures do not access a real microphone, phone, headset or participant archive.
- Beginner documentation checks passed for **10 Markdown files and 18 local links**, including installation/recording milestones and current standalone menu wording. Setup/monitor text was clarified without changing acquisition behavior. A later full Unity/browser/Python rerun also exited 0 after these wording updates.
- A local `0.1.0-preview.2` tarball was generated and its manifest, replay resource and Unity integration test matched the source at packaging time. Generated Artifacts are not committed; later documentation-only updates do not rewrite existing archives. Consult the source/changelog when choosing a version.

## Licensing-only preview update

Version **0.1.0-preview.3** updates licensing and package metadata without changing acquisition code. The Unity, browser and hardware-related evidence above remains the historical preview.2 baseline, not a new preview.3 device validation.

## Run the checks yourself

The script requires your own Unity, Node, Playwright, Python and FFmpeg paths. It does not install them. In PowerShell, from the repository root:

```powershell
./Tools/Run-Checks.ps1 -UnityExecutable "<Unity.exe path>" -NodeExecutable "<node.exe path>" -PlaywrightModule "<Playwright module path>" -PythonExecutable "<python.exe path>" -FfmpegExecutable "<ffmpeg.exe path>"
```

It opens only the separate VerificationProject in batch mode. Use it when that project is not already open. Results are saved to `VerificationProject/standalone-tests.xml` and `standalone-tests.log`; synthetic browser/media outputs are under ignored `Artifacts`. Optional media checks are skipped if FFmpeg is not provided.

## Device and scientific limitations

The managed receiver decodes documented wire layouts and keeps raw payloads plus quality/drop/unsupported counts. It does not certify the physical device. Pupil columns use documented wire millimetres, not a claim of physically validated measurements from this implementation. Missing/unsupported values are unavailable, not valid zeros.

Before collecting participants, verify:

- Real Neon reception, payload compatibility, Eye State/pupil availability and magnitude.
- Reconnect/Stop behavior, queue drops, actual sampling rate, time alignment and task-length coverage.
- The correct head Transform, Unity coordinate system/scale and explicitly wired interaction callbacks.
- Full-duration microphone intelligibility, video coverage, finalization and monitor DPI/cropping.
- Video offsets against observed markers. Time Echo and launch offsets are estimates, not hardware synchronization.

Not implemented: gaze-to-object mapping, fixation/saccade analysis, automatic cognitive-state classification, cloud upload or speech transcription. Raw head pose is exported as CSV, not a head chart in HTML.

## Distribution and privacy

Windows Editor is the tested target; standalone players, Quest Android, macOS/Linux and older Unity versions remain unverified. This is not an approved Asset Store submission.

Use a new local test output folder, anonymous IDs and informed consent. Keep participant archives/credentials out of GitHub. Review the [license](Packages/com.dolbywu.neon-research/LICENSE.md) and [dependency notices](<Packages/com.dolbywu.neon-research/Third Party Notices.md>) before redistribution. The repository is public. Plugin code and software documentation authored and owned by Michelle Binyan Xu are MIT-licensed; third-party materials retain their own licenses, and demonstration media/research data are excluded. No vendor endorsement is claimed.
