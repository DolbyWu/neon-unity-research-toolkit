// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.IO;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

/// <summary>One local, monotonic timeline per Play run. This is not phone clock synchronization.</summary>
public static class StudyExportSession
{
    private static readonly object Gate = new object();
    private static string folder, sessionId;
    private static string participantId, companionRecordingId;
    private static long startTicks, eventCount, poseCount;
    private static bool closed;
    private static bool requestedVideo, requestedAudio;
    private static int manifestPublishFailures;
    private static string lastManifestError = "";
    private static string recordingError = "";
    public static void ReportFailure(string message) { recordingError=message; StudyMonitorData.Error=message; }
    private static StreamWriter events, poses, anchors;
    public static string Folder => folder;
    public static string SessionId => sessionId;
    public static double Seconds => startTicks == 0 ? 0 : (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
    public static double SecondsAt(long ticks) => startTicks == 0 ? 0 : (double)(ticks - startTicks) / Stopwatch.Frequency;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        lock (Gate)
        {
            events?.Dispose(); poses?.Dispose(); anchors?.Dispose();
            StudySequence.Close();
            StudyMonitorData.Reset();
            events = poses = anchors = null;
            folder = sessionId = participantId = companionRecordingId = null; startTicks = eventCount = poseCount = 0; closed = false;
            manifestPublishFailures=0; lastManifestError="";
            recordingError="";
            requestedVideo = requestedAudio = false;
        }
    }

    public static string OutputRoot
    {
        get
        {
            var configured = NeonStudySettings.Load().outputRoot;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return ResolveOutputRoot(Directory.GetParent(Application.dataPath).FullName, configured);
#else
            return ResolveOutputRoot(Application.persistentDataPath, configured);
#endif
        }
    }

    public static string ResolveOutputRoot(string applicationFolder, string configured)
    {
        string path = string.IsNullOrWhiteSpace(configured) ? "NeonResearchData" : configured;
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(applicationFolder, path));
    }

    public static string EnsureStarted(string rootOverride = null)
    {
        lock (Gate)
        {
            if (folder != null) return folder;
            string root = rootOverride ?? OutputRoot;
            var config = NeonStudySettings.Load();
            requestedVideo = rootOverride == null && config.mediaRecordingEnabled && config.mediaRecordVideo;
            requestedAudio = rootOverride == null && config.mediaRecordingEnabled && config.mediaRecordMicrophone;
            participantId = string.IsNullOrWhiteSpace(config.participantId) ? "technical-test" : config.participantId;
            companionRecordingId = config.companionRecordingId ?? "";
            Directory.CreateDirectory(root);
            var drive = new DriveInfo(Path.GetPathRoot(root));
            if (drive.IsReady && drive.AvailableFreeSpace < 2L * 1024 * 1024 * 1024)
                throw new IOException("Study recording requires at least 2 GiB of free disk space.");
            sessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            startTicks = Stopwatch.GetTimestamp();
            string newFolder = config.archiveEnabled && rootOverride == null
                ? StudyArchivePaths.SessionFolder(root, config.participantId, sessionId)
                : Path.Combine(root, sessionId);
            if (config.archiveEnabled && rootOverride == null)
            {
                var intake = StudyParticipantIntake.Consume();
#if UNITY_EDITOR
                if (!Application.isBatchMode && intake == null)
                    throw new InvalidDataException("Confirm participant details before Play; recording was not started.");
#endif
                StudyParticipantProfiles.WriteSessionSnapshot(root, config.participantId, sessionId, newFolder, intake);
            }
            else Directory.CreateDirectory(newFolder);
            folder = newFolder;
            if (config.archiveEnabled && rootOverride == null)
            {
                foreach (string name in new[] { "Audio", "Video", "Companion" }) Directory.CreateDirectory(Path.Combine(folder, name));
            }
            events = Writer("behavior_events.csv", "session_id,event_id,session_time_s,pc_utc_ms,kind,payload,timestamp_semantics");
            poses = Writer("head_pose.csv", "session_id,session_time_s,pc_utc_ms,unity_frame,x,y,z,qx,qy,qz,qw,camera_name");
            anchors = Writer("clock_anchors.csv", "session_id,session_time_before_s,pc_utc_ms,session_time_after_s,stopwatch_frequency");
            StudySequence.Begin(folder);
            File.WriteAllText(Path.Combine(folder, "annotations_template.csv"), "annotation_id,session_id,episode_id,start_session_s,end_session_s,dimension,label,source,evidence_reference,interview_start_s,interview_end_s,coder_id,confidence,uncertainty_note\n");
            File.WriteAllText(Path.Combine(folder, "observations_template.csv"), "observation_id,session_id,marked_session_s,estimated_episode_start_s,estimated_episode_end_s,observer_id,observable_action,interpretation,uncertainty_note\n");
            Event("session_start", "");
            Flush();
            Debug.Log("[StudyExport] Session output: " + folder);
            return folder;
        }
    }

    private static StreamWriter Writer(string name, string header)
    {
        var w = new StreamWriter(Path.Combine(folder, name), false, new UTF8Encoding(false));
        w.WriteLine(header); return w;
    }

    public static void Event(string kind, string payload)
    {
        lock (Gate)
        {
            if (closed || events == null) return;
            if (kind == "observer_marker") StudySequence.Record(kind, "", "", "", "", payload);
            events.WriteLine(NeonStudyRecorder.Csv(sessionId, ++eventCount, Seconds,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), kind, payload, "logger_call_time_not_device_sample_time"));
        }
    }

    public static void Pose(Camera camera) => Pose(camera == null ? null : camera.transform);

    public static void Pose(Transform camera)
    {
        lock (Gate)
        {
            if (closed || poses == null || camera == null) return;
            var p = camera.position; var q = camera.rotation;
            poses.WriteLine(NeonStudyRecorder.Csv(sessionId, Seconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Time.frameCount, p.x, p.y, p.z, q.x, q.y, q.z, q.w, camera.name));
            poseCount++;
        }
    }

    public static void Flush()
    {
        lock (Gate)
        {
            if (folder == null || events == null) return;
            double before = Seconds;
            long utc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double after = Seconds;
            anchors.WriteLine(NeonStudyRecorder.Csv(sessionId, before, utc, after, Stopwatch.Frequency));
            events.Flush(); poses.Flush(); anchors.Flush();
            StudySequence.Flush();
            var metadata = new Manifest { sessionId = sessionId, closed = closed,
                participantId = participantId, companionRecordingIdManualUnverified = companionRecordingId,
                durationSeconds = Seconds, behaviorEventCount = eventCount, headPoseCount = poseCount,
                condition = StudySessionSettings.Condition, unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                videoRecording = requestedVideo ? "REQUESTED_see_media_capture_json_NOT_verified_by_manifest" : "NOT_REQUESTED",
                continuousThinkAloudRecording = requestedAudio ? "REQUESTED_see_media_capture_json_NOT_verified_by_manifest" : "NOT_REQUESTED" };
            // A new temp file plus replace avoids publishing a partially-written manifest.
            metadata.manifestPublishFailures=manifestPublishFailures;
            metadata.lastManifestPublishError=lastManifestError;
            metadata.recordingError=recordingError;
            string path = Path.Combine(folder, "export_manifest.json");
            try { StudyAtomicFile.Write(path,JsonUtility.ToJson(metadata,true)); }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
            {
                manifestPublishFailures++; lastManifestError=e.GetType().Name+": "+e.Message;
                StudyMonitorData.Error="Manifest publication delayed; raw logging continues. "+e.Message;
                // Metadata contention must not disable pose/region sampling. Final publication must succeed.
                if(closed) throw;
                Debug.LogWarning("[StudyExport] Manifest publication delayed; retry next flush. "+e.Message);
            }
        }
    }

    public static void Finish()
    {
        lock (Gate)
        {
            if (closed || folder == null) return;
            Event("session_end", "Normal Unity shutdown or Play stop; does not prove puzzle completion.");
            StudySequence.Close();
            closed = true;
            StudyMonitorData.Stop(Seconds);
            try { Flush(); }
            finally { events?.Dispose(); poses?.Dispose(); anchors?.Dispose(); events = poses = anchors = null; }
            StudySequenceAnalysis.TryBuildClosed(folder);
        }
    }

    [Serializable] private sealed class Manifest
    {
        public string schemaVersion = "1", sessionId, condition, unityVersion, applicationVersion;
        public string participantId, companionRecordingIdManualUnverified;
        public bool closed;
        public double durationSeconds;
        public long behaviorEventCount, headPoseCount;
        public int manifestPublishFailures;
        public string lastManifestPublishError;
        public string recordingError;
        public string eventCoverage = "explicit_StudyEvents_and_interaction_adapters_not_all_possible_actions";
        public string phoneClockSync = "TIME_ECHO_ESTIMATES_see_Neon_clock_sync_csv_and_per_sample_status_NOT_hardware_validated";
        public string videoRecording = "NOT_IMPLEMENTED";
        public string continuousThinkAloudRecording = "NOT_IMPLEMENTED_existing_voice_chunks_are_not_equivalent";
        public string cognitiveLabels = "MANUAL_TEMPLATE_ONLY_NOT_AUTOMATIC";
        public string gazeMapping = "see_Neon_session_json_processing_pose_only";
        public string overallReadiness = "TECHNICAL_PILOT_ONLY_NOT_VALIDATED_FOR_PARTICIPANT_STUDY";
    }
}

public sealed class StudyExportLifecycle : MonoBehaviour
{
    private float nextFlush;
    private float nextRegion;
    private bool failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var settings = NeonStudySettings.Load();
        if (!settings.recordOnPlay) return;
        StudySessionSettings.Condition = settings.condition;
        var go = new GameObject("Study Export Session (runtime)");
        DontDestroyOnLoad(go); go.AddComponent<StudyExportLifecycle>();
        try { StudyExportSession.EnsureStarted(); }
        catch (Exception e) { StudyMonitorData.Error="Cannot start recording: "+e.Message; Debug.LogError("[StudyExport] Cannot start recording: " + e.Message); }
    }
    private void LateUpdate()
    {
        if (failed) return;
        try
        {
            StudyExportSession.Pose(StudyHeadPoseSource.Head);
            if (Time.unscaledTime >= nextRegion)
            {
                nextRegion = Time.unscaledTime + 0.1f;
                var head = StudyHeadPoseSource.Head;
                StudySequence.SetRegion(head == null ? "Unknown_no_head_source" : StudySequence.Locate(head.position));
            }
            if (Time.unscaledTime >= nextFlush) { nextFlush = Time.unscaledTime + 2; StudyExportSession.Flush(); }
        }
        catch (Exception e) { failed = true; StudyExportSession.ReportFailure("Raw export interrupted: "+e.Message); Debug.LogError("[StudyExport] Export failed: " + e.Message); }
    }
    private void OnApplicationQuit() => Close();
    private void OnDestroy() => Close();
    private void Close()
    {
        try { StudyExportSession.Finish(); }
        catch (Exception e) { StudyMonitorData.Error="Final export failed: "+e.Message; Debug.LogError("[StudyExport] Final export failed: " + e.Message); }
    }
}

}
