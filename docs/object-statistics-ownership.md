# Object statistics versus raw child targets

Research branch update, 2026-11-16. A visual mesh target and its behavior object can have different stable IDs. For example, a diary's grab event may use the diary ID while its surface ray hits use the child Visuals ID. A zero on the diary's raw row is not proof of missing gaze; nor does grabbing prove gaze.

## Current evidence-based table and automatic CSVs

The default table is **Hits or linked interactions** in the selected interval. **Recorded hits only** shows positive hit rows only. **Full configured catalogue** restores every configured entry, with separate evidence and catalogue-snapshot status columns. An inactive catalogue snapshot is not proof that an object was inactive for the entire session. A linked interaction with no hit stays zero; it is never converted into a gaze observation.

Every newly generated HTML also saves a new private **Analysis/ObjectStatistics-<unique-id>/** folder beside the session data:

- **gaze_object_hits.csv**: valid target_hit records only, one row per callback. It retains the original callback-record index, raw target ID/name/group, canonical object ID/name/group, ownership status/rule, callback time, correction/mount identifiers, accuracy flag, hit geometry/coordinates and anonymous session/participant IDs.
- **gaze_object_summary.csv**: hit objects only, with the same grouping, callbacks, observed visits and conservative same-object covered seconds used by the HTML. Raw member IDs are retained. No inactive configured-only zero rows are inserted into this hit summary.
- **export_checks.json**: record/summary totals, unresolved ownership, invalid time/session exclusions and ownership conflicts.

The HTML links to those full-session CSVs. **Object hit records CSV (selected interval)** downloads the corresponding filtered hit records. **Export counts CSV for current view** exports the displayed table, including clearly labelled interaction-only zeros if selected. **Raw callback diagnostics CSV (all statuses)** is different: it deliberately retains no-hit, occlusion and invalid callbacks for diagnosis. Do not interpret every row in a diagnostics file as a hit.

For new recordings, the per-callback statistics_owner_id/status is authoritative and is not overridden by a later catalogue snapshot. A changed owner is applied only to the records that explicitly carry it. Historical records without those columns may use a same-session reviewed structural map whose exact IDs and ancestor relationship are checked against the recorded catalogue. Unknown, conflicting or invalid live owners have a blank canonical object ID and an explicit unresolved status; their raw hit identity is retained, not guessed.

The HTML embeds the same normalized hit records used to save the CSVs. Session/time boundaries, invalid data and source/configuration/correction/quality changes remain separate. Negative numeric coordinates stored with the earlier CSV formula-escape apostrophe are decoded only in the derived numeric coordinate columns; raw_hit_x/y/z preserve the exact original text. No raw sensor or gameplay file is modified.

## What is preserved

- Nearest-hit/occlusion queries, raw target IDs, original ray data, colliders, Rigidbody behavior, independent interactions and spatial-accuracy flags are unchanged.
- New callbacks append statistics_owner_id/name/group/rule metadata. New target catalogues also include the explicit owner status. These are additional analytical identity links, not extra observations.
- Existing CSV files and original HTML are not rewritten by the separate-review action.

## Configure explicit ownership

On a visual leaf's Research Target, set **Statistics Owner** to its actual ancestor Research Target and document the rule. An unlinked target owns its own statistics, even if another target is above it. Links to unrelated objects, disabled/unresolved/duplicate active owners and invalid/cyclic chains are not accepted for live metadata. Never identify objects solely by their name.

Do not link independent lids, buttons, physical bodies or nested grabbables simply because they share a hierarchy. A compound object can contain several legitimate research objects. Keep their raw identities and choose the analytical unit explicitly.

The optional existing-study integration includes a trial-only binder. It uses the same nearest actual Meta Grabbable and authored research ID as the behavior bridge. It links only reviewed visual-container patterns (Visuals, an exact repeated mesh name, or the owner's name plus Visuals), and rejects intervening Rigidbody, Grabbable or interaction interfaces. It rescans for later scene/object activation and dynamic instances; it does not save scene/prefab edits or apply this policy to arbitrary host projects. Runtime identity still must be unique and configured. Its read-only coverage checker includes inactive loaded scene objects.

## Review in HTML

Expand **Research target hits** and choose:

- **Objects (explicit ownership)**, the default: analytical object counts and visits.
- **Raw child targets**: the original surface/child identities and counts.

Only explicit owner metadata or a same-session reviewed mapping is used. Identical names are never a join key. Conflicting or cyclic links are excluded. Objects without an explicit mapping remain separate.

One callback is counted once. Consecutive valid callbacks on different linked surfaces of the same object may form one observed visit, but source/configuration/correction/quality changes, invalid data and gaps over 0.1 seconds still break coverage. These are ray-hit statistics, not fixations, inferred attention or an accuracy pass.

Target/group/interaction filters use the selected identity level. Object-level interaction filtering includes exact IDs and declared aliases of explicitly owned surfaces. The raw event identities remain available. Callback CSV exports retain original columns and append analysis_object_id/name/group/ownership_rule; the counts export provides names, IDs, groups, hit callbacks, observed visits and conservative covered time for the current view. Complete interval JSON retains raw callbacks, ownership provenance and current-view statistics.

## Historical data

Do not automatically merge old rows using names or hierarchy strings. If needed, create an inspectable, reviewed sidecar at **Analysis/target-object-ownership.json**:

```json
{
  "schema": "gazeflow-reviewed-target-ownership-v1",
  "session_id": "EXACT_SESSION_ID",
  "links": [
    {
      "child_id": "EXACT_VISUAL_TARGET_ID",
      "owner_id": "EXACT_BEHAVIOR_OBJECT_ID",
      "rule": "documented_structural_review"
    }
  ],
  "provenance": {
    "method": "Describe the recorded evidence and review",
    "limitation": "Static ownership grouping is not time-resolved geometry or accuracy validation"
  }
}
```

Both IDs must be present in the recording. Review independent parts, instances and any reparenting separately; ambiguous mappings must remain ungrouped. The exporter checks the sidecar session, size and unlinked path; the HTML rejects unknown/conflicting identities and cycles.

After stopping recording, use **Tools > GazeFlow > Advanced > Analysis > Create Separate Object Statistics Replay** and select the finished session folder. It creates a new timestamped HTML in the same Replay folder, preserving relative video paths, the original study-replay.html, annotation JSON and all raw CSVs. Import any saved annotation JSON into the new review explicitly.

The existing-study-only **Advanced > Diagnostics > Check Object Statistics Ownership** reads relevant authored scenes without saving them. It writes a local coverage report in Library/GazeFlowFlowChecks. This is configuration coverage, not hardware acceptance.
