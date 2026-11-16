# Offline pilot analysis recipe

`Analyze-P027.py` is the frozen, case-specific reproduction recipe for the 2026-11-16 P027 pilot. It is not a general cognitive-state classifier, a new recording service or a requirement for using the Unity package. It requires Python 3.12 and the pinned NumPy/Pandas dependencies; it does not install or use scikit-learn and makes no network calls.

With an authorized local copy of the original closed session:

```powershell
python -m pip install -r Tools/PilotAnalysis/requirements.txt
python Tools/PilotAnalysis/Analyze-P027.py --session "<private-P027-session-folder>" --publish
```

The recipe validates source identity and closure, runs synthetic numerical self-checks, reads the private source tables, and creates a **new** uniquely named `Analysis/ExploratoryMultimodal-...` folder inside that session. It never overwrites an earlier output, original CSV, saved human annotation, media or Unity source. Run without `--publish` for console-only analysis, or with `--inspect` for ingestion/quality diagnostics. Preserve the accompanying `p027_exploratory_report.html` template beside the Python script.

The expected private annotation file and study schema are recorded in the recipe. Adapting this to another session requires explicitly selecting its reviewed labels, boundary and event semantics, not renaming a participant or treating unlabelled time as calm. No private input dataset, raw pupil samples, audio/video, keys, machine endpoint or demographic profile is included here.

Outputs distinguish original values, heuristic pupil screening, exact annotated episodes, unlabelled same-region references, neutral unsupervised patterns and retrospective change points. Missing data are not zero-filled. No fixation detector, physiological diagnosis, validated confusion/insight classifier or measured spatial-accuracy approval is implied. Read the generated methods and sensitivity checks before interpreting a result.
