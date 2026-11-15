# Resume the portable beta hardware check

**Saved 15 November 2026. Work stopped at the operator's request.** Do not resume capture, promote main or publish a signed release without a new operator go-ahead.

## Saved candidate

- Branch: `release/gazeflow-0.2.0-beta.1`.
- Implementation commit: `91c2a6c`.
- [Draft pull request #1](https://github.com/DolbyWu/neon-unity-research-toolkit/pull/1), targeting main. **Keep it draft pending hardware acceptance.**
- The configured research-pipeline branch, original study projects, existing archives and store submission remain unchanged.

## Completed

- Project-independent core/adapter packages, matching beta identity.
- Start Here hub, explicit head/official-provider setup, optional unambiguous scene binding and authored visual references on arbitrary objects.
- Retained optional gaze adjustment, participant sessions, raw recording, observer intervals, local replay/annotation save-resume and offset reports.
- Independent core host: **72/72** tests.
- Independent official-SDK adapter host: **81/81** tests.
- Actual final local tarballs imported into a separate project: **81/81** tests.
- Five offline browser regression suites passed. Two archive payload/metadata/privacy inspections passed.
- Documentation checks: **45 Markdown files, 76 local links**. Candidate source audit passed.
- GitHub's **Documentation and UPM packaging (no Unity execution)** check passed for the implementation candidate. This CI job does not establish hardware behavior.

## Preparation only — not accepted hardware results

A separate local two-scene XR fixture was prepared using ordinary reference objects and the installed beta packages. The official phone API responded during preparation. A short preparation capture exists locally, but the wearer said they had **not started the actual test**. Preserve it privately and do not count it as a completed wearer trial or accuracy evidence.

The last preparation capture was closed normally without a recorded export error. No study data, live endpoint, physical device calibration, local media or machine-specific fixture configuration is part of this repository.

The fixture still needs its controller interaction profiles finalized and its intended graphics mode checked. Scene/rig initialization output alone is not sufficient evidence that the wearer saw the correct scene or had tracked controls. Complete these before asking the operator to start.

## Next session

1. Confirm the original project is safe/stopped and the operator wants to resume. Obtain the current Companion address/ports and fresh local-capture consent; do not assume yesterday's connection is still valid.
2. Use only the independent hardware fixture or another independent configured XR copy. Keep the final tested beta packages consistent; do not silently substitute the old game integration.
3. Finish the fixture's XR/controller/display configuration while **not in Play**. Confirm the actual tracked head, the single research recorder/provider and official source.
4. Let the operator explicitly start a new technical session. Confirm they see the reference objects and yellow point before treating it as a running hardware trial.
5. Test Show/nudge/Save/Hide, at least two observation intervals/notes, real configured object interactions, a head/scene transition and a brief stream loss/reconnect.
6. Stop normally. Inspect the actual exports and finalized media. In the generated HTML, check offset history/cursor, video seeking, multiple annotations, JSON save/reload and CSV/interval bundles. Check another session remains isolated.
7. Record pass/fail/not-tested in [the acceptance checklist](beta-0.2.0-acceptance.md). Keep all raw research records private.
8. Only after the remaining checks pass, update the candidate status and decide on promoting the draft PR to main. No main merge, release tag or signed release has been performed yet.

See [generic setup](universal-neon-xr-setup.md), [Quick Start](../QUICKSTART.md) and [Validation](../VALIDATION.md).
