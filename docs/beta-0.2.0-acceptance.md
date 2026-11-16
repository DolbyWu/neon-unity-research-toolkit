# GazeFlow 0.2.0-beta.1 candidate acceptance

**15 November 2026 · Windows Unity Editor 6000.3.23f1.** This candidate is separate from the configured research-pipeline project and the store submission. Main promotion is held until the pending visual/hardware checks below have been completed. No new study recording or hardware accuracy is claimed by software tests.

| Check | Result | Scope |
|---|---|---|
| Independent core project | PASS · 72/72 | No game or headset SDK required; synthetic data only |
| Independent official-SDK/adapter source project | PASS · 81/81 | No experiment assets or host scripts; pinned official Neon XR Core |
| Fresh project installing both actual beta tarballs | PASS · 81/81 | Package cache/import/compilation and tests from packaged payloads, not source-folder references |
| New component scene save/reopen | PASS | Scene Bindings and Visual Gaze Reference retain scripts/fields |
| Setup and binding checks | PASS | Duplicate/missing-source guards, ambiguous/disabled managed sources, same-session manual offset, cleared approval/save on rebind |
| Offline browser regression suites | PASS | Video pause/scrub/multiple annotations, JSON restore, offset history, AOI filters/export, evidence provenance |
| Payload inspection | PASS | Two UPM source archives; import metadata and matching dependency versions; no study records or bundled executables |
| New generic controls visual walk-through | NOT TESTED | Start Here, XR Setup, authored reference in an actual running scene |
| New generic-rig Neon + Quest test | NOT TESTED | Physical head binding, visible yellow reference, correction direction and known-target behavior |
| New generic cross-scene / disconnect trial | NOT TESTED | Real stream loss/recovery and replacement source selection in a configured experiment |
| Quantitative spatial/timing calibration | NOT ESTABLISHED | Applied offsets and save receipts are not measured accuracy or hardware synchronization |

Official SDK reference: `f4dee47b9690a059b1c4660628596d7303ade59a`, package identity 2.0.0. The minimal SDK host additionally pinned Addressables 2.9.1, Input System 1.20.0, UGUI 2.0.0, Unity Test Framework 1.6.0 and Newtonsoft JSON 3.2.2. Other Unity/SDK combinations are not accepted by these results.

Synthetic tests never entered a live SDK session or recorded microphone/desktop/participants. An initial deeply nested Windows test host exceeded platform path limits; the accepted checks used fresh short-path hosts. Use a reasonably short private data root and do not infer arbitrary long-path support.

## Final operator walk-through before main promotion

Use a **separate configured XR project copy** with matching beta packages, not the working experiment. Obtain recording consent and current Companion connection details before real capture.

1. Open **Start Here** and **Neon XR Setup**. Bind the actual head, official provider and mount ID. Verify no scene/task-specific code or required object names are needed.
2. Author a reference on any stable visible object. Enter a technical session. Confirm reference position/size/occlusion, show the cyan ray, adjust both directions, save and hide. Verify known-target behavior without interpreting a save as measured accuracy.
3. Mark two observer intervals and notes during the task. With configured research targets, check head/AOI/behavior records independently of raw eye reception.
4. Test one real scene-head/source replacement and a brief stream loss/recovery. Check gaps, duplicate-source refusal, retained same-session manual correction and separately cleared mapping approval.
5. Stop normally. Confirm participant/session separation, optional media framing/speech and finalized files. Open the actual HTML, seek video/timeline/offset history, add multiple notes and a player response, export/import annotation JSON and inspect CSV/interval bundles.
6. Repeat with another technical participant/session. No prior session's correction or annotations should silently carry into the next.

Keep raw data private. Update this checklist with actual observations and failures; do not substitute inherited study results or a manually approved flag for the new checks. Candidate archives are unsigned local build artifacts, not new signed releases. Existing signed releases/tags, original projects and store submission are unchanged.
