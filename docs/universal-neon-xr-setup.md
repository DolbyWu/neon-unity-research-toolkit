# Generic Neon XR setup

The core and adapter are reusable UPM packages. No particular game, tutorial scene, object name or study script is required. Verification target: Windows Unity Editor 6000.3.23f1. Use an independent project copy and private output directory first.

## Before setup

Install the core, official Neon XR Core and matching adapter using the [README](../README.md). Follow official SDK instructions for XR objects, resources and mount configuration. Identify the **actual tracked head/camera Transform**, not a floor origin/static scene camera, and the configured **NeonGazeDataProvider**. Choose a stable **Mount configuration ID** for this physical configuration; it is provenance, not an accuracy score.

## Dedicated research root

Open **Tools > GazeFlow > Neon XR Setup**. Assign those sources and mount ID. **Create dedicated research rig** creates a research provider, target recorder and scene-bindings component on a dedicated root, plus a Head Pose Source on the chosen head (or reuses its compatible existing source). Save your scenes.

It refuses duplicate loaded-scene research recorders/providers, missing references, prefab-asset selections and contradictory existing head-source configuration. If components already exist, configure them rather than duplicating the root. Creation does not enable acquisition, enter Play or approve mapping. Your existing official provider follows its own lifecycle when the project runs.

## Cross-scene sources

- **Fixed references (default):** uses the assigned Head Pose Source and official provider. Missing/disabled managed sources invalidate the managed binding. No arbitrary Main Camera replacement is chosen.
- **Unique active discovery (opt-in):** each enabled checkbox uses exactly one active Head Pose Source or official provider, instead of the corresponding fixed field. Multiple candidates are refused. No task-specific script reflection or name guessing is used.

For replacement rigs, enable **Use unique sources across scenes** during setup, and supply exactly one configured active head source and official provider in each relevant scene. Keep only the dedicated research root persistent; do not add another recorder/root to destination scenes. Two active rigs are ambiguous, while a disabled old rig does not count.

Rebinding clears accuracy approval and save confirmation. Same-session manual offset may remain under the same mount configuration; a foreign configuration or fitted correction is not silently carried over. Gaps stay gaps. Hardware testing must establish which Transform is actually tracked and whether your transitions are correct.

## Reference on any object

1. Select a scene Transform. In setup, choose **Create visual reference at selected Transform**. This creates a hidden reference child at its origin.
2. Move that child to a visible stable point, such as just in front of an existing surface. The tool does not guess visibility or move the object. A scene Gizmo previews the default 32 mm diameter.
3. Enter a confirmed recording. Open **Desktop Gaze Adjustment**, assign the component under **Optional visual reference**, then show the reference and cyan pointer.
4. Ask the wearer to look at yellow, not follow cyan. Keep head/object steady and nudge the research direction. **Save gaze adjustment**, then hide pointer/reference when ready. Your task controls its own progression; no tutorial or automatic scene jump is required.

The runtime visual is an unlit sphere with normal depth occlusion. It has no Collider, Rigidbody, Research Target, gameplay callback or through-wall rendering. Its world-space diameter does not inherit parent scale. Missing shaders produce a warning, not a physics fallback. It follows its authored object, not head motion.

This is **not measured calibration evidence**. Use official setup and a separate known-target check when accuracy evidence is needed. The five-point check button hides the selected manual reference. Applied angles, operator save receipts and independent measured approval remain different records.

## Record, observe and review

Use **Tools > GazeFlow > Start Here**. Acquisition and media are opt-in and keep the usual participant/consent flow. Optionally enable **Open monitor + observer on Play**; panels open after recording becomes active. Gaze adjustment remains manual.

Author Research Targets and wire interaction callbacks explicitly. A package installation cannot infer your task's semantics. Live Observation Marker and HTML review work even without approved mapping or an eye stream. Pause/scrub video, annotate with the participant, export JSON and import it to resume. Offset history follows callback correction IDs; raw X/Y and pupil data are unchanged.

Before a study, check head movement, known near/far objects, occlusion, scene transitions, stream loss/reconnect, a second participant/session, framing/sound and annotation save/restore. See [Validation](../VALIDATION.md) for current candidate scope.
