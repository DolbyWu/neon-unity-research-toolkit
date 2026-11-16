# Information-surface research targets

Use a Research Target on any authored clue, sign or puzzle surface in your own Unity project. No scene names, game scripts or automatic object-name matching are required.

1. Use a suitable existing collider on the visible surface and assign a stable unique Research Target ID and meaningful group.
2. Disable **Include Child Colliders** when frames, backings or independently interactive children must not belong to that surface. The backward-compatible default includes descendants.
3. Keep independent moving or interactive parts as separate targets. If a visual child belongs to a research object for statistics, explicitly assign its ancestor **Statistics Owner** and document **Statistics Owner Rule**.
4. Check front, back, decorative frame, an independent child and an occluder in front of the surface. Normal nearest-hit occlusion still applies; do not enlarge or move physical colliders just to improve statistics.
5. Inspect the target catalogue, raw callback diagnostics, normalized object-hit CSVs and HTML together. A configured target with no hit is not proof the participant never looked at it.

This configuration does not establish spatial accuracy or change raw eye/pupil traces. Validate it on the actual device and rig. See [object ownership and exports](object-statistics-ownership.md).
