# Tutorial and study exports

Recording stays continuous. Optionally enter the exact region ID in **Analysis starts at region** in **Setup and Recording Settings**. For example, use `StudyStart` and configure a Study Region with that ID in your own scene. This setting is captured in each new session manifest, so changing it later does not rewrite an older session's policy. Leave it blank to analyze the full session; no scene or region name enables partitioning automatically.

- **Tutorial:** original session time `[0, first recorded configured-region entry)`.
- **Study:** `[first recorded configured-region entry, session end)`. The boundary region itself belongs to Study.
- The HTML opens on Study by default. Select **Tutorial** to review/export the earlier portion, or **Full session** for archive diagnostics. Full-session diagnostic totals include Tutorial and are not primary study statistics.
- A scene-load callback or a video start is not a substitute for the recorded region entry.
- If a configured session never records that region entry, the session is tutorial-only. It does not silently become study data.

## Files and clocks

Every replay generation retains all original files and creates a new `Analysis/SessionSegments-<id>/` folder. `segments.json` records the boundary and source CSV record, file counts, and completion status. `Tutorial/` and `Study/` contain partitioned callback, event, head-pose, region and aligned-eye CSVs and the normalized object-hit CSV when present. `Unassigned/` preserves invalid-time, foreign-session, unaligned-eye and cross-boundary context records rather than turning them into valid zeros or assigning eye rows by PC receive time. Added `source_csv_record` values refer to the original source table's data records, excluding its header.

Partitions use half-open intervals. Records exactly at session end remain in Unassigned as boundary context. A region interval crossing the cutoff is retained unmodified in Unassigned rather than silently clipped. Counts in the three partitions must reconcile to the full source table. CSV fields retain their values; output quoting may differ.

The catalogue, clock anchors, raw payloads, full object-statistics export, legacy logs, annotations and full continuous video/audio remain in their original locations. They are shared context, not duplicated media. Session seconds, video offsets and annotation times are never reset. No hardware synchronization or spatial-accuracy approval is inferred from the split.

## Older sessions and reviewed samples

An older session without the policy field keeps full-session behavior, even if its log happens to contain a familiar region name. Do not edit the raw manifest to retrofit an analysis policy. Regenerating a closed session produces a new HTML and new derived export folders; earlier HTML, annotations and raw files are not overwritten.

The reviewed-sample builder applies the same cutoff. It retains all annotations in its interval/audit exports, rejects windows before or crossing the study boundary, and records the region-log input hash and scope parameters. Tutorial windows are never automatically used as training negatives. This rule does not upgrade draft/disputed labels or unverified AOI rays.

## Checks

Run Unity EditMode `SessionSegmentTests`, `node Packages/com.dolbywu.neon-research/Verification/Test-SessionSegments.cjs`, and `python Tools/Test-ReviewedSamples.py`. These are synthetic checks, not device acceptance.
