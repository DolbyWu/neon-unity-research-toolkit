using UnityEngine;
namespace NeonResearch
{
    [AddComponentMenu("GazeFlow/Gaze Target Recorder")]
    public sealed class StudyGazeTargetRecorder : MonoBehaviour
    {
        public StudyGazeRayProvider provider;
        public LayerMask collisionMask = ~0;
        [Min(0.01f)] public float maxDistance = 20;
        [Min(0.01f)] public float staleAfterSeconds = 0.1f;
        private StudyGazeRayProvider subscribed;
        private string session, lastTarget = "";
        private double lastSample;
        private bool waiting = true;
        private double nextCatalog;
        private void OnEnable() { subscribed = provider; if (subscribed != null) subscribed.SampleReady += Receive; }
        private void OnDisable()
        {
            if (subscribed != null) subscribed.SampleReady -= Receive;
            if (!waiting && StudyEvents.Recording) { StudyGazeTargetLog.Missing("provider_disabled"); EndTarget("provider_disabled"); }
            subscribed = null; waiting = true; lastTarget = "";
        }
        private void Update()
        {
            if (!StudyEvents.Recording) { waiting = true; lastTarget = ""; return; }
            if (session != StudyExportSession.SessionId)
            { session = StudyExportSession.SessionId; lastSample = StudyExportSession.Seconds; waiting = false; lastTarget = ""; nextCatalog = 0; }
            if (StudyExportSession.Seconds >= nextCatalog) { nextCatalog = StudyExportSession.Seconds + 2; StudyTargetCatalog.Snapshot(); }
            if (!waiting && StudyExportSession.Seconds - lastSample > staleAfterSeconds)
            { StudyGazeTargetLog.Missing("stale_or_disconnected"); EndTarget("stale_or_disconnected"); waiting = true; }
        }
        private void EndTarget(string reason)
        {
            if (lastTarget.Length > 0) StudyEvents.Record("gaze_target_exit", lastTarget, reason, "watchdog/logger time, not measured end of fixation");
            lastTarget = "";
        }
        private void Receive(StudyGazeRaySample sample)
        {
            if (!StudyEvents.Recording) return;
            int recorderCount = 0;
            foreach (var recorder in FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None))
                if (recorder.isActiveAndEnabled) recorderCount++;
            if (recorderCount != 1) { StudyGazeTargetLog.Missing("multiple_target_recorders_not_supported"); return; }
            if (session != StudyExportSession.SessionId) { session = StudyExportSession.SessionId; lastTarget = ""; }
            lastSample = StudyExportSession.Seconds; waiting = false;
            StudyGazeTargetLog.Record(sample, collisionMask, maxDistance, out string target);
            if (lastTarget != target)
            {
                if (lastTarget.Length > 0) StudyEvents.Record("gaze_target_exit", lastTarget, "", "callback_time; ray hit, not fixation");
                if (target.Length > 0) StudyEvents.Record("gaze_target_enter", target, "", "callback_time; ray hit, not fixation");
                lastTarget = target;
            }
        }
    }
}
