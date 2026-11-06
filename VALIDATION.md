# Independent preview validation — 6 November 2026

## 0.1.0-preview.2 replay integration

- Updated only the independent package's offline replay template, tests, version and documentation. Existing runtime/Editor acquisition code and defaults were not changed. The working EscapeRoom was read only during this integration.
- **18 / 18 Unity EditMode cases passed**, 0 failures, in the separate VerificationProject at **2026-11-06 23:43:26 UTC**. The actual completed-session export test additionally checks the loaded resource has the new video controls/multi-annotation action, resolved data marker, correct panel order and independent pupil-mm column names.
- **Both offline Edge/Playwright suites passed** against the integrated package template. Coverage includes authoritative native/custom pause, paused scrubbing, video/experiment clocks and frame provenance, five independent annotation records with the first preserved, explicit exit from editing, description-only notes, legacy JSON import, save/resume, interval/raw exports, no HTTP requests, and desktop/mobile layout.
- **14 / 14 synthetic Python archive/capture tests passed**, including localhost access boundaries and generated video/audio finalization. The first sandboxed run blocked a localhost socket; the permission-approved rerun passed. No real microphone or headset was recorded.
- The complete `Tools/Run-Checks.ps1` run exited **0**. Its wait now follows only the test Editor process, avoiding a wait on persistent licensing-helper descendants. No working Editor or licensing service was stopped.
- Created local `Artifacts/com.dolbywu.neon-research-0.1.0-preview.2.tgz`, SHA-256 `7feb39f5d8f8a8732f0d1cd45ed607129ca9bd9c0bec8ca8d005c18490f0f071`. The archived manifest, replay resource and Unity integration test match the current source exactly. Archive inventory checks exclude generated data/configuration. Generated Artifacts are not committed with the source update, and no new GitHub Release is created by pushing source. Install-from-tarball and physical-device checks remain separate acceptance steps.
- The plugin's wire-protocol pupil-mm fields and physical-validation warnings were retained; EscapeRoom's SDK-unit labels were not copied over them. No real participant archive was read by these tests, no audio was uploaded, and no new acquisition/cloud feature was introduced.
- Original extraction-time hashes below are historical evidence, not a claim that later user-authorized EscapeRoom changes still equal that old baseline. The original manifest has not been rewritten.

## Original extraction scope

Local package: `com.dolbywu.neon-research` **0.1.0-preview.1**. Verification host: the sibling **VerificationProject**, Unity **6000.3.23f1**, Windows Editor. The package was not installed into the working EscapeRoom and that Editor was not closed or switched to this host. Batch tests used a separate Editor process and explicit separate logs.

## Historical 0.1.0-preview.1 results

- **18 / 18 independent Unity EditMode tests passed**, 0 failures, including defaults off, independent settings/output, head-pose and explicit grab/custom event CSV export, closed-session replay generation, participant snapshots/path boundaries, generic regions without EscapeRoom types, six documented wire payload lengths, unknown-packet/missing-pupil handling, namespaced participant approval, finished replay resolution and basic RTP/time mapping rejection.
- **Two offline Edge/Playwright suites passed** against the extracted HTML, covering existing interval/raw-data exports, display precision, no-network load, and local video/offsets/manual annotation CRUD/JSON save-import/CSV escaping and desktop/mobile layouts. Synthetic fixture outputs are under **Artifacts**, not the original project or participant archive.
- **14 / 14 synthetic Python archive/capture tests passed**, with generated tones/video only. No real microphone, desktop capture, phone or headset was used.
- **24 / 24 selected original source files match the extraction baseline**, SHA-256. Rechecked using `Tools/Verify-SourcePreserved.ps1` after compilation/tests/packaging. This is a check of selected source files, not a claim about unrelated files the operator or running Editor may modify independently.
- Local `.tgz` creation and archive inventory checks passed. Packaging uses an explicit source whitelist: no original game assets/history/data/configuration, external executables, package caches or Python bytecode. Actual install-from-tarball has not been separately tested; the compiled blank host used the local UPM source path.

The first compile exposed assembly-reference setup issues, which were fixed only in this new package; the final run above compiled and exited with code 0. The re-runnable check script is `Tools/Run-Checks.ps1`. Raw Unity evidence is `VerificationProject/standalone-tests.xml` and `.log` (ignored/generated).

## Important difference from the original

The receiver now uses a managed documented-wire decoder, not the original SDK's converted gaze/eye-state data representation. Pupil columns are explicitly labelled as documented wire millimetres, with a warning that this preview has not been physically validated. The original data and original SDK-unit column labels were **not** changed. Real-device packet compatibility and pupil magnitude must be verified before using this preview for participant collection. Supported packets retain original payload bytes; unknown lengths are marked and counted, not silently converted into valid zero-valued samples.

Generic region/head/interaction adapters replace EscapeRoom-specific classes. Interaction events require explicit host-game wiring; importing the package does not magically detect all grabs. The optional video helpers still require operator configuration of separate Python/FFmpeg and confirmation. They are not part of a certified Asset Store submission.

## Not yet verified

- Real Neon connection/reconnect, Eye State/pupil availability, sampling performance or clock accuracy with this independent decoder.
- End-to-end participant intake plus real headset Play/Stop/full-task capture in the new host.
- Full-duration audio intelligibility, desktop DPI/cropping and complete video coverage.
- Physical gaze mapping, fixation/saccade algorithms or cognitive-state validity (not implemented).
- Standalone player/Quest Android builds, macOS/Linux or older Unity versions.
- Public licensing/ownership clearance, public hosting or Asset Store approval.

Keep the working EscapeRoom as the operating version until these checks are complete. Do not use the current participant archive as this preview's output root. The original extraction performed no GitHub publishing; the subsequent user-authorized upload uses a separate private repository under the personal DolbyWu account. No public sharing or changes to the original EscapeRoom repository are authorized by this private upload.
