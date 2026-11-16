# Information surfaces as research AOIs

Research-branch follow-up, 2026-11-16. This configures the isolated existing-study copy, not the original project, main branch or submitted package. It does not retroactively reclassify participant recordings or approve spatial accuracy.

## Reviewed scope

| Existing scene object | New research group | Geometric scope |
| --- | --- | --- |
| ChalkBaord Front | clue | Existing front MeshCollider only, not the back or frame |
| Hub Puzzle_ Cooking Pot Whitboard | puzzle_panel | Existing board MeshCollider only, not its child decorative frame |
| Protien Puzzle Box Lid | puzzle_panel | Existing lid MeshCollider; independent socket targets remain separate |

Object names are intentionally preserved, including original spelling. The existing protein-lid ID, event aliases and behavior links are retained. The two previously unconfigured surfaces receive deterministic IDs derived from their exact reviewed Unity GlobalObjectIds, not their names. The configuration report records the previous group and any previous research component metadata. Ordinary sauce-jar lids and the tutorial box lid are not relabelled as puzzle panels.

The panel/lid AOIs represent their whole configured mesh surfaces. They do not identify a particular sentence, slot or printed symbol, nor prove reading, fixation, attention or understanding. Separate existing socket targets may still produce authored-trigger-volume hits; the raw geometry field keeps those distinct from solid-surface hits.

## Collider ownership policy

Research Target now has **Include Child Colliders**, default on to preserve existing behavior. When off, only colliders on that target's own GameObject resolve to it. A child decorative frame stays a non-target occluder unless it has an explicit independent target. It does not inherit the panel target or allow the query to continue through it to a target behind the panel. Independent child targets resolve to themselves.

The three reviewed surfaces use this own-collider-only policy. No collider, trigger, layer, mesh, Rigidbody, transform, material or gameplay event is added or changed. The existing nearest-solid/explicit-trigger query policy remains in place. The catalogue exports include_child_colliders to document this configuration.

## Configuration and recovery

The existing-study source reference includes a guarded configuration tool and read-only inventory. Only the exact isolated trial project is eligible; they are not imported/executed by the portable package.

- **Advanced > Diagnostics > Check Information Surface AOIs** inventories relevant authored objects, including inactive descendants, without saving scenes.
- **Advanced > Diagnostics > Configure Reviewed Information AOIs** checks the exact scene asset/object identities, checkpoints the scene plus meta and previous research metadata, compares all non-research component state, configures the three targets and writes a local report. It never starts Play.
- Backup/report paths are under **Library/GazeFlowFlowChecks/** in the trial. Preserve a backup elsewhere before clearing Library if recovery matters.
- The configuration refuses dirty scenes or changed/missing reviewed identities rather than guessing by a partial name. Failed validation does not produce a saved success report.

The public integration reference contains source and identity rules, not host scene/assets, backup copies or participant recordings.

## Export and acceptance

Future sessions use the normal pipeline: target_catalog.csv, gaze_targets.csv, automatic gaze_object_hits.csv/summary, and HTML target/group filters. The groups clue and puzzle_panel appear through the existing export schema; raw IDs and per-callback object ownership remain available. Adding AOIs does not set mapping_verified or change the raw eye/pupil traces.

Synthetic core checks cover own/child geometry, decorative occlusion, independent children, backwards-compatible descendant ownership and CSV/replay preservation. Host checks cover existing IDs/names/aliases and unchanged physics components. Actual authored-mesh checks are recorded separately in the local configuration report. None of those is a Neon/Quest device accuracy test.

Local 2026-11-16 checks: **82/82 core EditMode tests**, **47/47 host synthetic checks**, existing HTML filter/CSV/interval regression, three authored-surface queries and decorative-frame exclusion passed. The checkpointed scene retained all 162 prior target IDs and added two; the scene meta and non-research component state were unchanged. A new physical-device recording is still pending.

Next device check: observe the blackboard front, whiteboard surface and lid; then the blackboard back, decorative whiteboard frame and an independent socket. Confirm the first three retain their original names/unique IDs and the intended groups; excluded surfaces remain separate; an occluder blocks a target behind it. Stop normally and inspect the new session CSVs and HTML. Do not overwrite a historical P027 target-hit file with reclassified collider contacts.
