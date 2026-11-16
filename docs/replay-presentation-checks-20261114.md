# Offline replay presentation checks — 2026-11-14

Experimental working-copy changes only. No release, push, original-project edit or new participant recording.

## Changes

- Target panel starts closed with a keyboard-accessible native disclosure and callback-count summary. Its expanded statistics table scrolls internally.
- Observation review/play and saved annotation time/seek buttons use yellow `#FFDC32`; original time selection and bounded video replay remain intact.
- New intensity responses are optional 1–10 integer number buttons. No automatic score; clear saves missing. JSON/CSV/curation preserve scale and input method. Legacy zero and decimals retain their original scale/value; revisions preserve earlier reports.
- The separate 1–5 retrospective confusion measure remains unchanged. Number buttons do not imply more accurate cognitive measurement or better ML predictions; inspect missingness, provenance and scale before analysis.

## Local software results

| Check | Result |
|---|---|
| Isolated Unity core EditMode suite | PASS, 70/70 |
| Integrated-host synthetic adapter/flow checks | PASS, 31/31 |
| Shipped target metrics/filter/CSV/bundle browser checks | PASS |
| Evidence/ratings/revision/merge/JSON/CSV browser checks | PASS |
| Local synthetic video pause, scrub, chapters and consecutive annotations | PASS |
| Two observer intervals, player-response links and bounded video replay | PASS |
| Offline sample-builder tests | PASS, 11/11 |
| Tracked documentation/link checks | PASS, 33 Markdown files / 60 local links |
| Desktop and 390 px mobile visual inspection | PASS; selected score yellow, no horizontal overflow |
| New real-device spatial accuracy | NOT TESTED this update |

Browser cases made no HTTP requests and used synthetic fixtures. A private existing-session derived HTML was also checked locally without changing original recordings or saving test annotations; its data/media are not part of this repository.

Existing HTML files are snapshots, not live views of the template. Future exports from the updated package use the new design. For an older session, preserve the original HTML and exported annotation JSON; open a newly generated derived HTML and import the same-session annotation JSON. Do not reload an old page containing unexported edits.
