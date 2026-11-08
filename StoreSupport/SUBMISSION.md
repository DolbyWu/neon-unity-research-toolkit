# GazeFlow free UPM store edition

Draft product: https://cloud.unity.com/assetstore/publisher/products/4418843

Confirmed portal technical name: `individual.binyan-xu.gazeflow`. Single package, Free Product selected. Summary, description, tags and YouTube introduction have been saved in the draft. No package has been uploaded or submitted for review.

Publisher spelling supplied by the owner: `Binyan Michelle xu`. Package author and current copyright statements match. Portal/Persona legal information must still agree with the publisher's actual identification; this repository cannot certify legal identity.

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

Outputs are under `Artifacts/Store`, including a candidate tarball, evidence JSON and `StoreValidationProject` with an embedded package folder whose technical name matches the portal. Generated candidates are unsigned and have not passed Unity Editor validation here.

The tests check isolation from the full package, preserved core acquisition/review code, excluded external launchers and rejection of a launcher regression. They are not C# compilation or hardware tests. CI runs these tests and uploads a separately labeled unvalidated store candidate.

## Required Windows/Unity work

1. Use a fresh Windows project with Unity 6000.3.23f1. Open the generated StoreValidationProject or import the candidate independently. Never open the full edition in the same host.
2. Import the sample, confirm component attachment/save/reopen, record a short synthetic session and test imported-video/annotation JSON roundtrip. Then run real Neon + Quest 3 reception/reconnection/Stop checks. This edition's settings, menus and finalization paths differ from the tested full edition and need fresh acceptance.
3. Install the official Asset Store UPM Publishing Tools. In the Editor, use Window > Tools > Asset Store > Validator, select UPM and address all issues. Follow the tool's actual signing/upload requirements; do not reuse the historical preview.4 signature/hash.
4. Upload `individual.binyan-xu.gazeflow` through the UPM Uploader to product 4418843. The current package is a prerelease candidate, not a guaranteed store-accepted version; adjust version/metadata only as required after validation, and retest the exact payload.
5. Supply required icon, product card and marketing images. Current draft video can be retained, but store screenshots/descriptions must represent the no-external-capture edition. Review any AI-assistance disclosures required by the current submission rules.
6. Review product details and final legal declarations, then submit. No approval or publication is guaranteed merely because the package builds.

Official upload workflow: https://docs.unity.com/en-us/asset-store/publishing/upm-packages/validate

Submission requirements: https://assetstore.unity.com/publishing/submission-guidelines
