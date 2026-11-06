// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>Explicit per-Play consent; independent capture agent, no encoder wait on Unity's main thread.</summary>
[InitializeOnLoad]
public static class StudyMediaEditor
{
    private const string FolderKey = "NeonResearch.Media.ActiveFolder";
    private const string ConfirmedKey = "NeonResearch.Media.Confirmed";
    private const string Mirror = @"C:\Program Files\Oculus\Support\oculus-diagnostics\OculusMirror.exe";
    static StudyMediaEditor()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.quitting += Stop;
    }

    [MenuItem("Tools/Neon Research/Media/Open Quest Mirror (Preview Only)")]
    public static void Preview()
    {
        if (!File.Exists(Mirror)) throw new FileNotFoundException("Quest Mirror is not installed", Mirror);
        if (!Process.GetProcessesByName("OculusMirror").Any())
            Process.Start(new ProcessStartInfo { FileName = Mirror, UseShellExecute = true, WindowStyle = ProcessWindowStyle.Normal });
        Debug.Log("[StudyMedia] Quest Mirror preview only. Recording has not started.");
    }

    private static void Changed(PlayModeStateChange change)
    {
        if (Application.isBatchMode) return; // Offline tests must never use a real microphone or screen.
        if (change == PlayModeStateChange.ExitingEditMode) Preflight();
        if (change == PlayModeStateChange.EnteredPlayMode) Start();
        if (change == PlayModeStateChange.ExitingPlayMode) Stop();
        if (change == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(ConfirmedKey, false);
    }

    private static void Preflight()
    {
        SessionState.SetBool(ConfirmedKey, false);
        // The participant gate must precede media consent, regardless of callback registration order.
        if (!StudyParticipantStartFlow.AllowOrRequest()) return;
        var settings = NeonStudySettings.Load();
        if (!settings.recordOnPlay || !settings.mediaRecordingEnabled || !EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Archive preflight owns the missing-participant message; no capture consent without a profile.
        if (settings.archiveEnabled && !File.Exists(Path.Combine(StudyExportSession.OutputRoot, "Participants", settings.participantId, "participant.json"))) return;
        try
        {
            if (!File.Exists(settings.mediaFfmpegExecutable)) throw new FileNotFoundException("Configure the FFmpeg executable first.");
            if (!File.Exists(settings.archivePythonExecutable)) throw new FileNotFoundException("Configure the capture Python executable first.");
            if (settings.mediaRecordMicrophone && string.IsNullOrWhiteSpace(settings.mediaMicrophoneDevice))
                throw new InvalidOperationException("Select a specific microphone first.");
            if (settings.mediaRecordVideo && settings.mediaVideoSource == "desktop" && !settings.mediaAllowDesktopRecording)
                throw new InvalidOperationException("Desktop capture requires explicit configuration.");
            if (settings.mediaRecordVideo && settings.mediaVideoSource == "window" && !Process.GetProcessesByName("OculusMirror").Any())
            {
                EditorApplication.isPlaying = false;
                Preview();
                EditorUtility.DisplayDialog("Quest Mirror preview", "Confirm the headset is in Link and the Mirror shows the game view, then press Play again. No recording has started.", "OK");
                return;
            }
            bool confirmed = EditorUtility.DisplayDialog("Start session recording",
                "This Play will record:\n" + (settings.mediaRecordVideo ? "Video: " +
                    (settings.mediaVideoSource == "desktop" ? "TEST DESKTOP SCREEN, including every visible window. Close private apps. Keep Unity and Study Live Monitor visible." : settings.mediaWindowTitle + " window only.") +
                    " (" + settings.mediaVideoWidth + "x" + settings.mediaVideoHeight + ", 30 fps).\n" : "") +
                (settings.mediaRecordMicrophone ? "Continuous microphone: " + settings.mediaMicrophoneDevice + ".\n" : "") +
                "Participant: " + settings.participantId + "\nArchive: " + StudyExportSession.OutputRoot +
                "\nFiles go into this participant's NEW session. Verify the ID, correct scene/monitor and unmuted microphone.\n\nStop Play or use Stop media recording in the archive. Recording also stops if Unity exits. Do not minimize the recorded windows during the test.",
                "Start recording", "Cancel");
            SessionState.SetBool(ConfirmedKey, confirmed);
            if (!confirmed) EditorApplication.isPlaying = false;
        }
        catch (Exception error)
        {
            EditorApplication.isPlaying = false;
            Debug.LogWarning("[StudyMedia] Play cancelled: " + error.Message);
        }
    }

    private static void Start()
    {
        var settings = NeonStudySettings.Load();
        if (!settings.recordOnPlay || !settings.mediaRecordingEnabled || !SessionState.GetBool(ConfirmedKey, false)) return;
        string folder = StudyExportSession.Folder;
        if (string.IsNullOrEmpty(folder)) { Debug.LogError("[StudyMedia] No session folder; capture not started."); return; }
        try
        {
            string packageRoot = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(StudyMediaEditor).Assembly).resolvedPath;
            string script = Path.Combine(packageRoot, "Tools", "StudyArchive", "study_capture.py");
            if (!File.Exists(script)) throw new FileNotFoundException("Capture agent missing", script);
            File.WriteAllText(Path.Combine(folder, "media-start-request.json"), JsonUtility.ToJson(new Request {
                unityProcessId = Process.GetCurrentProcess().Id, requestUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                sessionSeconds = StudyExportSession.Seconds, confirmedByOperator = true }, true), new UTF8Encoding(false));
            SessionState.SetString(FolderKey, folder);
            Process.Start(new ProcessStartInfo { FileName = settings.archivePythonExecutable,
                Arguments = Quote(script) + " --session " + Quote(folder) + " --config " + Quote(NeonStudySettings.FilePath),
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
            Debug.Log("[StudyMedia] Capture requested for this session. Check media_capture.json; process launch alone is not proof of recorded media.");
        }
        catch (Exception error) { Debug.LogError("[StudyMedia] Capture launch failed: " + error.Message); }
    }

    [MenuItem("Tools/Neon Research/Media/Stop Session Media Recording")]
    public static void Stop()
    {
        string folder = SessionState.GetString(FolderKey, "");
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        File.WriteAllText(Path.Combine(folder, "media-stop.request"), "Unity/operator stop requested " + DateTime.UtcNow.ToString("O"));
        SessionState.SetString(FolderKey, "");
        SessionState.SetBool(ConfirmedKey, false);
    }

    private static string Quote(string value)
    {
        if (value.Contains("\"") || value.Contains("\r") || value.Contains("\n")) throw new ArgumentException("Invalid capture argument");
        return "\"" + value.TrimEnd('\\') + "\"";
    }
    [Serializable] private sealed class Request
    {
        public int unityProcessId;
        public long requestUtcMs;
        public double sessionSeconds;
        public bool confirmedByOperator;
    }
}

}
