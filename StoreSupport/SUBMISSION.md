# GazeFlow free UPM store edition

Draft product: https://cloud.unity.com/assetstore/publisher/products/4418843

Confirmed portal technical name: `individual.binyan-xu.gazeflow`. Single package, Free Product selected. Summary, description, tags and YouTube introduction have been saved in the draft. The previous preview.5 upload was withdrawn to complete acceptance. The replacement preview.6 passed 22/22 EditMode cases including scene persistence and session-reset isolation. Official Validator: 45 passes, 2 reviewed warnings, zero errors. preview.6 has been uploaded through the official UPM Uploader and resubmitted; the product and package both showed In Vetting on 2026-11-07. Auto-publish is disabled. See validation.template.txt for the review rationale.

Publisher spelling supplied by the owner: `Binyan Michelle Xu`. Package author and current copyright statements match. Portal/Persona legal information must still agree with the publisher's actual identification; this repository cannot certify legal identity.

## Separate edition

The original GitHub package `com.dolbywu.neon-research` remains the full edition, with its existing optional capture/archive helpers intact.

The store edition is generated from that source by `Tools/Build-StorePackage.py`. It excludes Python helpers, external media capture, the archive-server launcher, executable dependencies and the corresponding setup fields. It retains Neon transport/decoding, head/behavior logging, participant intake/local session archives, CSV export, imported-video HTML review and manual annotations. Its settings are isolated under UserSettings/GazeFlow.

Do not install both editions into the same Unity project: their assemblies, namespaces and script identities overlap. No migration of an existing experiment is performed by this build tool.

## Build and test

From the repository root:

```text
python Tools/Test-StorePackage.py -v
python Tools/Build-StorePackage.py
```

Outputs are under `Artifacts/Store`, including a candidate tarball, evidence JSON and `StoreValidationProject` with an embedded package folder whose technical name matches the portal. Generated local tarballs are unsigned. The exact preview.6 content was imported, compiled, tested and checked with the official Unity Validator; the UPM Uploader handles the official upload process. Do not reuse the old preview.4 signature.

The tests check isolation from the full package, preserved core acquisition/review code, excluded external launchers and rejection of a launcher regression. They are not C# compilation or hardware tests. CI runs these tests and uploads a separately labeled unvalidated store candidate.

## Verification and submission workflow

1. Use a fresh Windows project with Unity 6000.3.23f1. Open the generated StoreValidationProject or import the candidate independently. Never open the full edition in the same host.
2. Import the sample, confirm component attachment/save/reopen, record a short synthetic session and test imported-video/annotation JSON roundtrip. Preserve the recorded preview.4 real-device baseline and the owner-reported store workflow/video regression. Do not label the hardware untested wholesale. Only claim reconnection or standalone APK deployment if separately verified; Quest Link is the recorded intended environment.
3. Install the official Asset Store UPM Publishing Tools. In the Editor, use Window > Tools > Asset Store > Validator, select UPM and address all issues. Follow the tool's actual signing/upload requirements; do not reuse the historical preview.4 signature/hash.
4. Upload `individual.binyan-xu.gazeflow` through the UPM Uploader to product 4418843. The current package is a prerelease candidate, not a guaranteed store-accepted version; adjust version/metadata only as required after validation, and retest the exact payload.
5. Supply required icon, product card and marketing images. Current draft video can be retained, but store screenshots/descriptions must represent the no-external-capture edition. Review any AI-assistance disclosures required by the current submission rules.
6. Review product details and final legal declarations, then submit. No approval or publication is guaranteed merely because the package builds.

Official upload workflow: https://docs.unity.com/en-us/asset-store/publishing/upm-packages/validate

Submission requirements: https://assetstore.unity.com/publishing/submission-guidelines


## Scene-test opt-in for a disposable Editor host

If batch licensing is unavailable, the same scene save/reopen assertions can run in an explicitly isolated Editor host. Never do this in an experiment project. Open a single untitled scene in the generated disposable StoreValidationProject. Create Library/NeonResearch.IsolatedSceneTests.txt containing the absolute path of that host's Assets directory. The guard also requires one untitled scene; without opt-in, ordinary Editor runs skip scene-destructive acceptance. Run NeonResearch.Tests in Test Runner and export results. The marker is local under Library and is never part of the distributed package.

preview.6 changed tests, verification-host hygiene and validation documentation only. No Runtime/Editor recording logic changed from preview.5, preserving the owner-reported store regression and the earlier real-device baseline. The GitHub full edition remains a separate version stream.
