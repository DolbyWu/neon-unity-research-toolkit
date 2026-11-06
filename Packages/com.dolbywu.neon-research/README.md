# Neon Research Toolkit (Unofficial Preview)

Local UPM package `com.dolbywu.neon-research`, version `0.1.0-preview.2`, with a separate blank Unity 6000.3.23f1 verification project. No existing EscapeRoom project dependency.

Offline replay uses the updated workflow: pause/scrub video, mark the current frame, choose a label or enter a description, and add multiple independent annotations. Video and experiment clocks are shown together. Manual annotations follow the eye-data timeline; Game log is last. Export annotation JSON before closing; import it to resume. Videos stay as separate local files, not embedded bytes.

Open **Tools > Neon Research > Setup and Recording Settings**, choose an independent output folder and explicitly enable **Record on Unity Play**. Open **Tools > Neon Research > Open Live Monitor**. Participant information is confirmed before Play; completed sessions contain raw logs and an English offline video/annotation HTML.

All acquisition is off by default. Real Neon hardware is not simulated or certified by synthetic tests. See `Documentation~/workflow.md` for integration, timing, optional external capture and validation limits. License is deliberately UNLICENSED/private until ownership and redistribution have been reviewed; this package is not an approved Asset Store submission.
