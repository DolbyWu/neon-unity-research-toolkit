using System;
using System.IO;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Separate additive export; existing raw Neon data remains unchanged.</summary>
    public static class StudyGazeTargetLog
    {
        private static StreamWriter writer;
        private static StreamWriter approximateWriter;
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
        public struct QueryResult { public StudyResearchTarget target; public Vector3 point; public string status,geometry,colliderPath; }
        private static string ColliderPath(Transform value)=>value.parent==null?value.name+"["+value.GetSiblingIndex()+"]":ColliderPath(value.parent)+"/"+value.name+"["+value.GetSiblingIndex()+"]";
        /// <summary>Read-only nearest-collider query shared by logging and calibration. Never writes a row.</summary>
        public static QueryResult Query(Ray input,int mask,float distance)
        {
            var result=new QueryResult{status="invalid_ray",geometry=""};
            if(!Finite(input.origin)||!Finite(input.direction)||input.direction.sqrMagnitude<.000001f||!Finite(distance)||distance<=0)return result;
            var ray=new Ray(input.origin,input.direction.normalized);
            bool solid=Physics.Raycast(ray,out RaycastHit hit,distance,mask,QueryTriggerInteraction.Ignore);
            float nearest=solid?hit.distance:distance;
            if(solid){result.target=ResolveTarget(hit.collider);result.point=hit.point;result.geometry="solid_surface";result.colliderPath=ColliderPath(hit.collider.transform);}
            int count=Physics.RaycastNonAlloc(ray,triggerHits,nearest,mask,QueryTriggerInteraction.Collide);
            if(count==triggerHits.Length){result.target=null;result.geometry="";result.status="geometry_query_overflow";return result;}
            for(int i=0;i<count;i++){
                var candidate=triggerHits[i];if(!candidate.collider.isTrigger||candidate.distance>=nearest)continue;
                var owner=ResolveTarget(candidate.collider);if(owner==null||!owner.includeTriggerColliders)continue;
                nearest=candidate.distance;result.target=owner;result.point=candidate.point;result.geometry="authored_trigger_volume";result.colliderPath=ColliderPath(candidate.collider.transform);
            }
            if(result.target==null)result.status=solid?"non_target_occluder":"no_hit";
            else if(!result.target.HasUniqueId()){result.target=null;result.status="missing_or_duplicate_target_id";}
            else result.status="target_hit";
            return result;
        }
        public static bool Record(StudyGazeRaySample sample,int mask,float distance,out string targetId)
        {
            targetId="";if(!StudyEvents.Recording)return false;
            bool geometryValid=Finite(sample.worldRay.origin)&&Finite(sample.worldRay.direction)&&sample.worldRay.direction.sqrMagnitude>.000001f&&Finite(distance)&&distance>0;
            bool valid=sample.valid&&geometryValid;
            bool candidate=!valid&&sample.candidateRayAvailable&&geometryValid;
            var result=valid||candidate?Query(sample.worldRay,mask,distance):new QueryResult{status=sample.valid?"invalid_ray":sample.status??"unknown",geometry=""};
            if(result.target!=null)targetId=result.target.targetId;
            if(candidate){Write(sample,null,Vector3.zero,false,sample.status??"mapping_unverified","",result.target,result.point,result.status,result.geometry,result.colliderPath);targetId="";return false;}
            Write(sample,result.target,result.point,valid,result.status,result.geometry,colliderPath:result.colliderPath);
            return valid&&result.target!=null;
        }
        public static void Missing(string status)
        {
            if (!StudyEvents.Recording) return;
            Write(new StudyGazeRaySample { source = "provider_watchdog", status = status }, null, Vector3.zero, false, status);
        }
        private static void Write(StudyGazeRaySample sample, StudyResearchTarget target, Vector3 point, bool valid, string status, string geometry = "",StudyResearchTarget candidate=null,Vector3 candidatePoint=default,string candidateStatus="",string candidateGeometry="",string colliderPath="")
        {
            bool rayAvailable=valid||candidateStatus.Length>0; // Geometric availability is not validated AOI quality.
            try
            {
                if (session != StudyExportSession.SessionId) { Close(); session = StudyExportSession.SessionId; }
                if (writer == null)
                {
                    writer = new StreamWriter(Path.Combine(StudyExportSession.Folder, "gaze_targets.csv"), false);
                    writer.WriteLine("session_time_s,source,device_timestamp_ms,rtcp_synchronized,ray_valid,status,target_id,target_group,target_label,hit_x,hit_y,hit_z,calibration_id,timestamp_semantics,device_time_semantics,session_id,unity_frame,ray_origin_x,ray_origin_y,ray_origin_z,ray_direction_x,ray_direction_y,ray_direction_z,target_name,legacy_object_ids,linked_behavior_kinds,hit_geometry,verification_id,verification_status,correction_id,raw_ray_origin_x,raw_ray_origin_y,raw_ray_origin_z,raw_ray_direction_x,raw_ray_direction_y,raw_ray_direction_z,ray_processing,candidate_status,candidate_target_id,candidate_target_name,candidate_target_group,candidate_hit_x,candidate_hit_y,candidate_hit_z,candidate_hit_geometry,candidate_quality,mapping_verified,spatial_accuracy_status,query_collider_path");
                }
                writer.WriteLine(NeonStudyRecorder.Csv(StudyExportSession.Seconds, sample.source, sample.deviceTimestampMs > 0 ? (object)sample.deviceTimestampMs : "",
                    sample.rtcpSynchronized, valid, status, target == null ? "" : target.targetId,
                    target == null ? "" : target.targetGroup, target == null ? "" : target.DisplayName,
                    target == null ? "" : point.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    target == null ? "" : point.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    target == null ? "" : point.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    sample.calibrationId, "unity_callback_time_NOT_device_time_or_hardware_sync", sample.deviceTimeSemantics,
                    StudyExportSession.SessionId, Time.frameCount,
                    rayAvailable ? sample.worldRay.origin.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    rayAvailable ? sample.worldRay.origin.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    rayAvailable ? sample.worldRay.origin.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    rayAvailable ? sample.worldRay.direction.normalized.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    rayAvailable ? sample.worldRay.direction.normalized.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    rayAvailable ? sample.worldRay.direction.normalized.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "",
                    target == null ? "" : target.gameObject.name,
                    target == null ? "" : Newtonsoft.Json.JsonConvert.SerializeObject(target.legacyObjectIds),
                    target == null ? "" : Newtonsoft.Json.JsonConvert.SerializeObject(target.linkedBehaviorKinds), geometry,
                    sample.verificationId ?? "", sample.verificationStatus ?? "not_measured",sample.correctionId??"",
                    sample.hasRawWorldRay?(object)sample.rawWorldRay.origin.x:"",sample.hasRawWorldRay?(object)sample.rawWorldRay.origin.y:"",sample.hasRawWorldRay?(object)sample.rawWorldRay.origin.z:"",
                    sample.hasRawWorldRay?(object)sample.rawWorldRay.direction.x:"",sample.hasRawWorldRay?(object)sample.rawWorldRay.direction.y:"",sample.hasRawWorldRay?(object)sample.rawWorldRay.direction.z:"",
                    string.IsNullOrEmpty(sample.correctionId)?"uncorrected_or_unavailable":"head_local_direction_rotation_origin_unchanged",
                    candidateStatus,candidate==null?"":candidate.targetId,candidate==null?"":candidate.gameObject.name,candidate==null?"":candidate.targetGroup,
                    candidate==null?"":(object)candidatePoint.x,candidate==null?"":(object)candidatePoint.y,candidate==null?"":(object)candidatePoint.z,candidateGeometry,
                    candidateStatus.Length==0?"":"UNVERIFIED_CANDIDATE_NOT_ACCEPTED_AOI",valid&&sample.mappingVerified,sample.verificationStatus??"not_measured",colliderPath??""));
                if(candidateStatus.Length>0)
                {
                    if(approximateWriter==null)
                    {
                        approximateWriter=new StreamWriter(Path.Combine(StudyExportSession.Folder,"gaze_targets_approximate.csv"),false);
                        approximateWriter.WriteLine("session_time_s,status,target_id,target_name,target_group,hit_x,hit_y,hit_z,quality,calibration_id,correction_id,verification_id,verification_status,hit_geometry,source,ray_origin_x,ray_origin_y,ray_origin_z,ray_direction_x,ray_direction_y,ray_direction_z");
                    }
                    approximateWriter.WriteLine(NeonStudyRecorder.Csv(StudyExportSession.Seconds,candidateStatus,candidate==null?"":candidate.targetId,candidate==null?"":candidate.gameObject.name,candidate==null?"":candidate.targetGroup,
                        candidate==null?"":(object)candidatePoint.x,candidate==null?"":(object)candidatePoint.y,candidate==null?"":(object)candidatePoint.z,
                        "LOW_PRECISION_UNVERIFIED_NOT_ACCEPTED_AOI",sample.calibrationId,sample.correctionId??"",sample.verificationId??"",sample.verificationStatus??"not_measured",candidateGeometry,sample.source,
                        sample.worldRay.origin.x,sample.worldRay.origin.y,sample.worldRay.origin.z,sample.worldRay.direction.x,sample.worldRay.direction.y,sample.worldRay.direction.z));
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target export failed: " + e.Message); }
        }
        public static void Flush()
        {
            try { writer?.Flush(); approximateWriter?.Flush(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target flush failed: " + e.Message); }
        }
        public static void Close()
        {
            try { writer?.Dispose(); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Gaze target close failed: " + e.Message); }
            finally
            {
                try{approximateWriter?.Dispose();}catch(IOException e){StudyExportSession.ReportFailure("Approximate target close failed: "+e.Message);}
                approximateWriter=null;writer = null; session = null;
            }
        }
    }
}
