using System;
using System.IO;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Separate additive export; existing raw Neon data remains unchanged.</summary>
    public static class StudyGazeTargetLog
    {
        private static StreamWriter writer;
        private static string session;
        private static readonly RaycastHit[] triggerHits = new RaycastHit[256];
        private static StudyResearchTarget ResolveTarget(Collider collider)
        {
            foreach (var item in collider.GetComponentsInParent<StudyResearchTarget>())
                if (item.isActiveAndEnabled) return item;
            return null;
        }
        public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        public static bool Record(StudyGazeRaySample sample, int mask, float distance, out string targetId)
        {
            targetId = "";
            if (!StudyEvents.Recording) return false;
            string status = sample.status ?? "unknown";
            StudyResearchTarget target = null;
            Vector3 point = Vector3.zero;
            string geometry = "";
            bool valid = sample.valid && Finite(sample.worldRay.origin) && Finite(sample.worldRay.direction)
                && sample.worldRay.direction.sqrMagnitude > 0.000001f && Finite(distance) && distance > 0;
            if (!valid) status = sample.valid ? "invalid_ray" : status;
            else
            {
                var ray = new Ray(sample.worldRay.origin, sample.worldRay.direction.normalized);
                bool solid = Physics.Raycast(ray, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore);
                float nearest = solid ? hit.distance : distance;
                if (solid) { target = ResolveTarget(hit.collider); point = hit.point; geometry = "solid_surface"; }
                // Only explicitly authored trigger AOIs are eligible. All solid colliders
                // still limit the query, including non-target walls. No collider/layer/physics is modified.
                int count = Physics.RaycastNonAlloc(ray, triggerHits, nearest, mask, QueryTriggerInteraction.Collide);
                bool overflow = count == triggerHits.Length;
                for (int i = 0; i < count && !overflow; i++)
                {
                    var candidate = triggerHits[i];
                    if (!candidate.collider.isTrigger || candidate.distance >= nearest) continue;
                    var owner = ResolveTarget(candidate.collider);
                    if (owner == null || !owner.includeTriggerColliders) continue;
                    nearest = candidate.distance; target = owner; point = candidate.point; geometry = "authored_trigger_volume";
                }
                if (overflow) { target = null; geometry = ""; status = "geometry_query_overflow"; }
                else if (target == null) status = solid ? "non_target_occluder" : "no_hit";
                else if (!target.HasUniqueId()) { target = null; status = "missing_or_duplicate_target_id"; }
                else { targetId = target.targetId; status = "target_hit"; }
            }
            Write(sample, target, point, valid, status, geometry);
            return true;
        }
        public static void Missing(string status)
        {
            if (!StudyEvents.Recording) return;
            Write(new StudyGazeRaySample { source = "provider_watchdog", status = status }, null, Vector3.zero, false, status);
        }
        private static void Write(StudyGazeRaySample sample, StudyResearchTarget target, Vector3 point, bool valid, string status, string geometry = "")
        {
            try
            {
                if (session != StudyExportSession.SessionId) { Close(); session = StudyExportSession.SessionId; }
                if (writer == null)
                {
                    writer = new StreamWriter(Path.Combine(StudyExportSession.Folder, "gaze_targets.csv"), false);
                    writer.WriteLine("session_time_s,source,device_timestamp_ms,rtcp_synchronized,ray_valid,status,target_id,target_group,target_label,hit_x,hit_y,hit_z,calibration_id,timestamp_semantics,device_time_semantics,session_id,unity_frame,ray_origin_x,ray_origin_y,ray_origin_z,ray_direction_x,ray_direction_y,ray_direction_z,target_name,legacy_object_ids,linked_behavior_kinds,hit_geometry");
                }
                writer.WriteLine(NeonStudyRecorder.Csv(StudyExportSession.Seconds, sample.source, sample.deviceTimestampMs > 0 ? (object)sample.deviceTimestampMs : "",
                    sample.rtcpSynchronized, valid, status, target == null ? "" : target.targetId,
                    target == null ? "" : target.targetGroup, target == null ? "" : target.DisplayName,
                    target == null ? "" : point.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    target == null ? "" : point.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    target == null ? "" : point.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    sample.calibrationId, "unity_callback_time_NOT_device_time_or_hardware_sync", sample.deviceTimeSemantics,
                    StudyExportSession.SessionId, Time.frameCount,
                    valid ? sample.worldRay.origin.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    valid ? sample.worldRay.origin.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    valid ? sample.worldRay.origin.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    valid ? sample.worldRay.direction.normalized.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    valid ? sample.worldRay.direction.normalized.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    valid ? sample.worldRay.direction.normalized.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    target == null ? "" : target.gameObject.name,
                    target == null ? "" : Newtonsoft.Json.JsonConvert.SerializeObject(target.legacyObjectIds),
                    target == null ? "" : Newtonsoft.Json.JsonConvert.SerializeObject(target.linkedBehaviorKinds), geometry));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target export failed: " + e.Message); }
        }
        public static void Flush()
        {
            try { writer?.Flush(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target flush failed: " + e.Message); }
        }
        public static void Close()
        {
            try { writer?.Dispose(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target close failed: " + e.Message); }
            finally { writer = null; session = null; }
        }
    }
}
