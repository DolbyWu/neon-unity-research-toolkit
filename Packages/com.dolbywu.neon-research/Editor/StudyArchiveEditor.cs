// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

/// <summary>Local archive integration only; never sends records to a cloud service.</summary>
[InitializeOnLoad]
public static class StudyArchiveEditor
{
    private const string PendingFolder = "NeonResearch.PendingFinishedFolder";
    static StudyArchiveEditor()
    {
        StudyArchiveNotifications.SessionReady += Ready;
        EditorApplication.playModeStateChanged += Preflight;
        EditorApplication.update += OpenFinishedWhenIdle;
    }

    [MenuItem("Tools/Neon Research/Open Participant Archive")]
    public static void Open() => Launch(null);

    private static void Preflight(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingEditMode || Application.isBatchMode) return;
        var settings = NeonStudySettings.Load();
        if (!settings.recordOnPlay || !settings.archiveEnabled) return;
        if (!StudyParticipantStartFlow.AllowOrRequest()) return;
        try
        {
            StudyArchivePaths.SessionFolder(StudyExportSession.OutputRoot, settings.participantId, "20000101T000000000Z-00000000");
        }
        catch (Exception error)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[StudyArchive] Play cancelled: " + error.Message);
            EditorUtility.DisplayDialog("Participant details", error.Message + "\nConfirm the participant in the pre-Play form and retry. No archive server is required.", "OK");
        }
    }

    private static void Ready(string folder)
    {
        if (Application.isBatchMode) return;
        var settings = NeonStudySettings.Load();
        if (!settings.archiveEnabled || !settings.archiveAutoOpen) return;
        try
        {
            FinishedReplayPath(StudyExportSession.OutputRoot, folder);
            // Export completes during shutdown. SessionState survives the following domain reload;
            // open external windows only after Unity has returned to idle Edit mode.
            SessionState.SetString(PendingFolder, Path.GetFullPath(folder));
        }
        catch (Exception error) { Debug.LogWarning("[StudyArchive] Files saved; could not queue results: " + error.Message); }
    }

    /// <summary>Resolve only finalized local results; null replay still allows opening the folder.</summary>
    public static string FinishedReplayPath(string root, string folder)
    {
        StudyArchivePaths.RequireContainedUnlinked(root, folder);
        string manifest = Path.Combine(folder, "export_manifest.json");
        StudyArchivePaths.RequireContainedUnlinked(root, manifest);
        var metadata = JObject.Parse(File.ReadAllText(manifest));
        if (metadata["closed"]?.Type != JTokenType.Boolean || !(bool)metadata["closed"])
            throw new InvalidDataException("Wait for this session to finish before opening results.");
        string replay = Path.Combine(folder, "Replay", "study-replay.html");
        StudyArchivePaths.RequireContainedUnlinked(root, replay);
        return File.Exists(replay) ? Path.GetFullPath(replay) : null;
    }

    private static void OpenFinishedWhenIdle()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string folder = SessionState.GetString(PendingFolder, "");
        if (string.IsNullOrEmpty(folder)) return;
        SessionState.EraseString(PendingFolder); // One attempt only; never reopen on every Editor update.
        try
        {
            var settings = NeonStudySettings.Load();
            if (!settings.archiveEnabled || !settings.archiveAutoOpen) return;
            string replay = FinishedReplayPath(StudyExportSession.OutputRoot, folder);
            try { Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true }); }
            catch (Exception error) { Debug.LogWarning("[StudyArchive] Could not open saved folder: " + error.Message); }
            if (replay != null) Application.OpenURL(new Uri(replay).AbsoluteUri);
            else Debug.LogWarning("[StudyArchive] Session folder saved, but Replay/study-replay.html is missing. Regenerate the offline replay after Stop.");
        }
        catch (Exception error) { Debug.LogWarning("[StudyArchive] Files saved; could not open results: " + error.Message); }
    }

    private static string Quote(string text)
    {
        if (text.Contains("\"") || text.Contains("\r") || text.Contains("\n")) throw new ArgumentException("Invalid launcher path");
        return "\"" + text.TrimEnd('\\') + "\"";
    }

    private static void Launch(string session)
    {
        var settings = NeonStudySettings.Load();
        if (!settings.archiveEnabled) throw new InvalidOperationException("Study archive is not configured.");
        if (string.IsNullOrWhiteSpace(settings.archivePythonExecutable) || !File.Exists(settings.archivePythonExecutable))
            throw new FileNotFoundException("Configure archivePythonExecutable with an installed Python 3.9+ executable.");
        string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(StudyArchiveEditor).Assembly).resolvedPath;
        string script = Path.Combine(packageRoot, "Tools", "StudyArchive", "study_archive.py");
        if (!File.Exists(script)) throw new FileNotFoundException("Study archive launcher missing", script);
        Process.Start(new ProcessStartInfo { FileName = settings.archivePythonExecutable,
            Arguments = Quote(script) + " --root " + Quote(StudyExportSession.OutputRoot) +
                " --config " + Quote(NeonStudySettings.FilePath) + " --open" +
                (string.IsNullOrEmpty(session) ? "" : " --session " + Quote(session)),
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
    }
}

}
