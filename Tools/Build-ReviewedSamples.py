"""Repository entry point; the distributable offline builder lives inside the core package."""
from pathlib import Path
import runpy

script = Path(__file__).resolve().parent.parent / "Packages/com.dolbywu.neon-research/Tools~/Build-ReviewedSamples.py"
if __name__ == "__main__":
    runpy.run_path(str(script), run_name="__main__")
else:
    implementation = runpy.run_path(str(script))
    for name in ("curate", "number", "coverage", "digest", "table", "SCHEMA"):
        globals()[name] = implementation[name]
