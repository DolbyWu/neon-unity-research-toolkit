# Minimal study sample

Import this sample with Package Manager. Open any blank scene with a tagged Main Camera, or assign a head Transform using **Neon Research > Head Pose Source**. Add a box with a BoxCollider and **Study Region**; assign the collider and a region ID. Add **Interaction Adapter** to an object and wire GrabLeft/ReleaseLeft (or right) to the real interaction framework's UnityEvents. No automatic grab detection is claimed.

Open **Tools > Neon Research > Setup and Recording Settings**. Choose an independent output folder, explicitly enable Record on Unity Play, and save. Leave Neon/media disabled for a synthetic event-only trial. Open the monitor, then Play and confirm anonymous participant intake. Stop normally; inspect the session files and new offline replay.

To receive real Neon data, separately enable reception and enter the current Companion IP/ports. No Quest or Neon hardware setup, calibration or physical timing verification is performed by importing the sample. Never use the synthetic methods as actual participant labels.
