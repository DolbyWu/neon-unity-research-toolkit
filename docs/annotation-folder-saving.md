# Save annotations beside the replay

Research branch update, 2026-11-16. This is an optional, local-only saving route. The existing JSON download, import, merge and CSV buttons remain available.

## First save

1. Open a newly generated session HTML in desktop Microsoft Edge or Chrome. Add the player's labels, explanation and optional ratings as usual.
2. Click **Save JSON to session folder**. In the browser's folder picker select the **session folder containing export_manifest.json**, one level above Replay. Do not select the participant root, Participants, or Replay itself.
3. Authorize access when asked. The replay checks the exact session ID, participant ID, closed-session flag and duration before accepting the folder.
4. Wait until the folder status shows **Last verified save**, including the filename. The JSON is in **Replay/**, next to the HTML. Saving includes a read-back check; a permission or write failure is not reported as a successful save.

Example layout (anonymous placeholders):

```text
Participants/Pxxx/Sessions/<session>/
  export_manifest.json
  Replay/
    study-replay.html
    <session>-annotations-r<revision>-<UTC>-<unique-id>.json
```

Every save is a new dated version. No earlier JSON, HTML, original CSV or video is overwritten. Multiple saves of one revision are allowed. The directory and participant/session checks prevent accidental saving into another participant's session; they are consistency checks, not protection against someone deliberately modifying a manifest.

## Continue annotating

After choosing a folder, **After choosing a folder, also save when I add, edit or delete an annotation** is enabled by default. Accepted changes, deletion audit, merged records and changed video timing are queued as complete snapshots. Finish or cancel a form draft first; unapplied form text is not an annotation and is not included in saved files. Keep the page open until pending saves finish. You can uncheck automatic saving and use **Save JSON to session folder** explicitly instead.

The status displays the **last** saved filename, not a claim that an unfinished form is saved. Errors retain the annotations in the tab. Retry the save or download JSON as a fallback. Avoid repeated saves after an error until you have checked the message: a failed/cancelled filesystem write may leave an incomplete new file, but it never replaces an earlier version.

The selected folder is kept only in the open tab. When reopening or reloading the HTML, import the saved JSON version you want to continue, then select the same session folder again. Existing history is not silently loaded, merged or overwritten. Import and merge retain their normal validation and confirmation rules. Do not close a dirty tab until you have a usable JSON file.

## Browser limits and privacy

The browser controls permission and may restrict this API in embedded previews, unsupported browsers or insecure contexts. The built-in preview may require opening the local HTML in external Edge/Chrome. The page cannot infer the local parent folder or silently write to C: from its URL. A picker/permission step is required. No local helper, server, API key, network connection, browser-cache backup or audio upload is introduced.

If direct saving is unavailable, **Export annotations JSON (save / resume)** still downloads the identical annotation format; move it into Replay or choose that download destination yourself. CSV is for analysis, not restoring an editing session. Raw eye data, target hit records and clock-alignment limitations are unchanged.

## Verification

Run **Verification/Test-AnnotationFolderSave.cjs** with the existing local Playwright/Edge setup. It uses synthetic data only, a bounded filesystem fixture with an injected picker, and real native Chromium filesystem handles in isolated test storage. It checks permissions/cancellation, wrong session/participant/duration/open-manifest rejection, read-back, immutable versions, multiple annotations, revisions/deletions, ratings, queueing, permission failure/retry, pending drafts, reload/import and the download fallback. The native operating-system permission dialog itself remains an operator step; automated tests do not grant access to participant folders.
