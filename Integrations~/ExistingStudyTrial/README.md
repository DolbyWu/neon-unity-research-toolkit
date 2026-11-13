# Existing study trial: research pipeline integration reference

Snapshot date: **2026-11-13**, America/New_York. This is a **source-only research pipeline reference**, not a complete study, universal scene migration, packaged sample, release or fresh hardware acceptance result.

The portable toolkit lives under the repository's `Packages/` directory. Start with the [pipeline tutorial](../../docs/research-pipeline-tutorial.md) for a new integration. This folder preserves the additional source work used to connect an already functioning, separately owned study to those packages.

## Contents

- `Source/Assets/`: **11** newly authored bridge, tutorial-entry, pointer-panel and synthetic-test C# files with their original Unity `.meta` identities. This is a reference tree, not an automatic installer.
- `Patches/Assets/`: **20** zero-context patches for existing logging, consent guards, tutorial placement/progress, initial orientation, dynamic pin identity and legacy Editor/test namespace integration.

The pin-manager patch omits a cosmetic comment-only encoding conversion from the legacy source; its research-code changes are preserved. Therefore its recorded source fingerprint describes the full trial source, while the published patch deliberately leaves that unrelated baseline comment untouched. All 20 published patches pass a read-only application check against the recorded local baseline.
- `source-inventory.json`: relative source paths, original SHA-256 fingerprints and baseline SHA-256 for changed files. Snapshot text has normalized LF endings; fingerprints describe the original source bytes, not normalized copies.
- `legacy-source-replacement.json`: legacy source paths replaced by package-provided implementations in the trial. It is a migration inventory, **not a deletion command**.
- `dependency-reference.json`: fixed versions and non-personal technical capture choices. It is not a ready-to-use project manifest or device configuration.

None of these files are automatically imported by Unity from this tilde-suffixed directory.

## Host-specific dependencies and guards

The reference code depends on the existing study's authored scenes, initialization/bootstrap, player logger, tutorial gates, prompt zones, event wrappers, spawn manager and Meta interaction classes. These are not supplied here.

Several reference classes deliberately restrict execution to the original isolated trial path and product identity. **Do not simply remove the safety checks.** In your own saved project copy, deliberately replace the guard with your copy's exact absolute path and identity, check scene names and component bindings, and keep the original project closed and unchanged. The guards are not participant consent.

The legacy timeline bridge intentionally forwards the host API to **one** package timeline. Legacy global runtime classes were replaced by package implementations; importing both can create duplicate types or duplicate recorders. Review the replacement inventory and your dependency graph manually in a separate backup copy. No source is automatically deleted by this snapshot.

## Reviewing changes safely

1. Back up the existing study, save/close it, and work only in an independently named copy.
2. Install the fixed package dependencies from the pipeline tutorial; preserve the host's remaining dependencies.
3. Check the baseline fingerprints in `source-inventory.json`. Patches were derived against a specific local pre-migration source revision and are not guaranteed to apply to another revision.
4. Read each patch and bridge before applying it. The patches intentionally have **zero context** to minimize copied host code. A developer checking a matching baseline can use `git apply --check --unidiff-zero <patch>` in the study copy, but this snapshot does not run that command or alter any project automatically.
5. Migrate duplicate legacy research implementations deliberately. Preserve Unity metadata and authored gameplay. Rebind the actual tracked head, one provider, one target recorder, original events and stable target IDs.
6. Compile and run synthetic checks in the copy before starting a real recording. The Editor checks have idle/batch/path guards; never invoke session-resetting synthetic tests during a participant session.
7. Configure the **current** Companion endpoint and physical mount configuration yourself. No network address or live consent is supplied.
8. Complete a new Neon + Quest test of targets, occlusion, head binding, actual grabbing/releases, tutorial Next, scene transitions, stale/invalid data and reconnect. Separately check that video/audio finalize and HTML intervals/annotations export and restore correctly.

## Behavior represented by this reference

The authored tutorial entry is reused: five-point fitting/validation, then the original grabbing tutorial at the opposite side, then the movement tutorial and formal task. The grabbing station is hidden while validation runs and restored afterward. Actual grab callbacks are recorded independently of eye quality; they also update the original tutorial objectives without fabricating success.

Two rounds now use the **same fixed world target centres**, including the centre, with new samples in round two. The trial uses a **3-degree project-specific mean-error tolerance** and 70% valid coverage; those are not official scientific acceptance cutoffs. Finishing a failed measurement still allows gameplay and separately labeled low-precision geometric records, not accepted AOI hits. Scene/head-binding changes require explicit mapping review; old approval is not silently transferred.

The package itself supplies the desktop two-click **Observation Marker**, observer-note popup and linked HTML retrospective responses. This source overlay does not reimplement that feature or the raw receiver.

## Deliberate exclusions and limits

No host scenes, prefabs, textures, audio assets, participant profiles, recordings, calibration reports, real gaze/head/event CSV, annotations, transcripts, private addresses, credentials, API keys, build artifacts or Unity caches are included.

The local migration/audit utility is deliberately **omitted** because it mixes machine-specific settings/device endpoints with scene mutation operations. It is recorded as omitted in the inventory. No active loopback or fake device configuration initializer is substituted.

Consequently this folder does **not** reproduce the authored target catalogue, actual scene/prefab bindings, full scene changes, spawn positioning or media configuration by itself. Obtain the existing study's assets through its authorized distribution and configure those changes explicitly in a copy. Do not infer full-object AOI coverage or device acceptance merely from included code/tests. Research data remain private and outside Git.
