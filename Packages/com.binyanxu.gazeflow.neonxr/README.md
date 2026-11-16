# GazeFlow Neon XR Adapter

**0.2.0-beta.2 candidate · unofficial Windows Unity Editor add-on.** Install matching GazeFlow core and official Neon XR Core 2.0.0 (pinned upstream revision is in the repository README). This bridge does not bundle or replace the official SDK or its physical mount calibration.

Open **Tools > GazeFlow > Neon XR Setup** to assign your actual tracked head, official provider and mount configuration ID. It creates a dedicated research root without altering your task/XR rig or starting acquisition. Optional cross-scene unique-source discovery refuses ambiguity; explicit fixed references are the default.

Create a Visual Gaze Reference Point on any authored Transform and position it on a visible surface. In **Desktop Gaze Adjustment**, assign/show it and the cyan pointer, adjust the local research direction, save and hide. References add no physics or automatic accuracy approval.

The core supplies participant sessions, live observer intervals and offline video/timeline annotations. Target-ray CSV records require configured research targets/geometry and explicitly linked behavior; they are not fixations or inferred attention.

See [generic setup](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/docs/universal-neon-xr-setup.md), [Quick Start](https://github.com/DolbyWu/neon-unity-research-toolkit/blob/codex/gazeflow-beta2-integration-20261116/QUICKSTART.md) and the repository Validation report before using this candidate in a study. Hardware acceptance of a configured project is separate from synthetic checks.
