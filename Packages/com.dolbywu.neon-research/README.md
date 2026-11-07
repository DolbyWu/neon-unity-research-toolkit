# Neon Unity Research Toolkit

**0.1.0-preview.4 (unreleased)** · Unofficial Windows Unity Editor development preview.

This local candidate fixes serialization of the three public scene components. Earlier previews may leave Missing Script references after saving/reopening a scene. Back up affected scenes, re-add only the broken component, and restore its fields/callbacks. Existing broken references are not automatically migrated.

Record anonymous participant sessions, head/camera pose, explicitly connected interaction events and available Neon eye/pupil data. Review finished sessions in English offline HTML with linked local video and optional manual annotations.

## Start here

1. Install with Unity Package Manager **Install/Add package from disk** and select this `package.json`. The tested Unity version is **6000.3.23f1**.
2. Open **Tools → Neon Research → Setup and Recording Settings**.
3. Choose a fresh output folder, enable **Record on Unity Play (opt-in)**, leave Neon/media off for your first test, then **Save project settings**.
4. Open **Tools → Neon Research → Open Live Monitor**. Press Play, confirm an anonymous participant and click **Mark observation** during a short test.
5. Stop normally. The session folder and HTML open when auto-open is enabled.

No Git, headset, Neon, Python or FFmpeg is needed for that first data-only check. Installation alone does not start acquisition.

Read [the integration and workflow guide](Documentation~/workflow.md) for setup, eye-data acceptance, event wiring, optional media and annotation steps. The repository's Quick Start includes a full beginner walkthrough and troubleshooting.

## Offline annotation

Pause/drag video → mark current frame → choose a label or enter a description → add the annotation. Video and experiment clocks are shown together. **Add another annotation here** preserves previous records. The order is **eye timeline → Manual annotations → interval export → Game log**.

Export annotation **JSON** before closing and import it to resume. CSV is for analysis only. Video and JSON remain separate local files; copy the whole session folder.

## Research readiness

Real-device compatibility, physical pupil units, timing and full-duration capture remain unvalidated. Gaze is not mapped to Unity objects; no fixation/saccade or cognitive-state classifier is included. Interactions require explicit callbacks. Optional capture requires separately configured Python/FFmpeg and per-session confirmation.

Acquisition is off by default. Settings/data are project-local. See the [MIT License](LICENSE.md) for the plugin's software licensing terms. Third-party materials retain their own licenses; demonstration media and research data are excluded. See [dependency notices](<Third Party Notices.md>) for the licensing scope. It is not an official vendor product or an approved Asset Store submission.
