# Beta.2 integration acceptance — 16 November 2026

Scope: Windows Unity Editor 6000.3.23f1, matching 0.2.0-beta.2 core and adapter, official Neon XR Core 2.0.0 pinned at `f4dee47b9690a059b1c4660628596d7303ade59a`. The maintainer approved publishing this as the main-branch beta after PR checks. It is not a stable certification, signed release or store edition.

## Integration boundary

Selected portable changes were reviewed from research commit `8d75437`; host scenes, integrations, participant records, case reports and pilot-analysis scripts were excluded. Existing beta setup, reference, binding and scene-name-independent behavior were retained. Generic segmentation requires an explicit manifest setting; familiar region names never silently enable it. Raw eye/pupil/ray IDs and original clocks remain unchanged.

## Software evidence

| Check | Result |
|---|---|
| Two actual UPM archive payloads | PASS: metadata, matching dependencies and no research records/bundled executables |
| Independent project with those tarballs, not source references | PASS: 97/97 Unity EditMode tests; zero failed/skipped |
| Independent core-only tarball project, without SDK/adapter | PASS: 88/88 Unity EditMode tests; zero failed/skipped |
| Reviewed-sample rules | PASS: 14/14 synthetic checks |
| Offline replay/browser regressions | PASS: nine suites, including real synthetic filesystem-handle writes/read-back and synthetic local video |
| Source isolation | PASS: no known game-script coupling or live addresses/machine paths in candidate source audit |
| Original study projects, data and submitted store artifacts | Unchanged; no participant records are included in this publication |
| Actual Neon + Quest scene, head movement and eye/pupil reception | PASS: independent technical sessions; wearer confirmed colored objects visible; not measured spatial accuracy |
| Actual left/right grab/release and observer interval/note | PASS: physical grip events plus observer start/end/note persisted; no cognitive-state inference |
| Separate participant session and clean Stop/media finalization | PASS: separate archive identities; prior manual adjustment not inherited; full physical-pixel desktop and microphone files finalized |
| Real generated HTML and local video basic review | PASS: matching object totals, observer interval display, seek/play/pause and no script errors or network requests; not a complete annotation round-trip |
| Cross-scene recording | PASS — operator-confirmed on 16 November; additional run records were not independently inspected here |
| Stream disconnect/recovery | PASS — operator-confirmed on 16 November; additional run records were not independently inspected here |
| Real annotation JSON save/resume | PASS — operator-confirmed on 16 November; additional saved/restored files were not independently inspected here |
| Native folder-picker path and full real interval-export round-trip | Not separately confirmed/inspected; synthetic permissions and export regressions passed |
| Measured spatial accuracy / hardware clock synchronization | NOT ESTABLISHED |

## Physical-device checklist

Use a new private technical session and reconfirm current endpoint and recording consent. Do not treat the old preparation capture or an inherited study session as beta.2 acceptance.

1. Confirm tracked head/hands, official provider and authored reference; show/nudge/save/hide. Check the stored corrected ray, not only the visual pointer.
2. Look at a child surface with an explicitly configured ancestor owner, another independent target and an own-collider-only panel. Exercise real grab/release. Check raw-child and canonical-object IDs, groups and HTML/CSV callback totals without merging instances by name.
3. Mark two observer intervals and notes. Keep Tutorial recorded; enter the explicitly configured start region and confirm Study begins at that original time.
4. Replace a head/scene source and briefly stop/restart the phone stream. Check stale/invalid/occluded/no-target states, conservative coverage and fresh recovery; no duplicated recorder or silent accuracy approval.
5. Stop normally; verify continuous desktop video/microphone, eye/pupil availability, head/events, catalogue and gaze-target/object-hit CSVs. Compare Tutorial/Study/Unassigned counts with full source records.
6. Open the real HTML. Pause/scrub, seek targets/events, add several annotations and player responses. Manually authorize the exact session folder and verify versioned JSON saves; reload/import; compare selected CSV and full interval bundle.
7. Start a second technical session: participant/session separation, no inherited annotation or correction setting, clean normal finalization.

Record each category as directly inspected, operator-confirmed or not tested. The maintainer subsequently confirmed the three missing workflow checks and authorized PR-based main promotion after CI. Do not relabel those confirmations as independent file-level inspection or measured spatial precision. Store compatibility problems are tracked separately; publishing this UPM beta does not rebuild or replace the submitted store package.

## Tested archive identities

- Core SHA-256: `1cfc8aabb95e7c24e4308ecd8f5e123cb51ea275e32acc760933a9ab56b4e520`.
- Adapter SHA-256: `14f4519dd3ea86b51c44f52fe32e4811b29ecb001dee56408c8b092a6e43f655`.

These identify the exact local unsigned beta.2 tarballs used by the independent installs; they are not a signed release or store approval. Rebuilds with different bytes need their own hashes and install evidence. The separate two-scene fixture was first prepared with acquisition and recording disabled, then armed after fresh endpoint confirmation and local capture consent. Basic physical-device results were inspected; the operator subsequently confirmed cross-scene recording, stream recovery and annotation save/resume. All raw technical records, endpoint/profile details and observation text stay private. No store submission was changed.
