using PupilLabs;
using System;
using System.Threading;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
namespace NeonResearch.NeonXR
{
    [AddComponentMenu("GazeFlow/Neon XR Research Provider (Experimental)")]
    public sealed class NeonXRResearchProvider : StudyGazeRayProvider
    {
        public NeonGazeDataProvider neon;
        [Tooltip("The same XR head/camera Transform used by the official gaze visualizer.")]
        public Transform headReference;
        [Tooltip("Operator confirmation only, not an automatic calibration quality measurement.")]
        public bool mappingVerified;
        public string calibrationId = "";
        public StudyGazeCorrection.Model CurrentCorrection { get; private set; }
        public string CorrectionPath { get; private set; }="";
        public void ClearCorrectionAndCollectAgain()
        {
            var check=GetComponent<StudySpatialValidation>();
            if(check==null||check.Running)throw new InvalidOperationException("Stop the current measurement before recollecting.");
            string previous=CurrentCorrection?.id??"";mappingVerified=false;CurrentCorrection=null;CorrectionPath="";
            if(StudyEvents.Recording)StudyEvents.Record("gaze_correction_cleared",previous,"new_collection_required","Prior files retained; no historical data rewritten.");
            check.independentCheckPattern=false;check.Begin(calibrationId);
        }
        public void FitCorrectionAndBeginVerification()
        {
            var check=GetComponent<StudySpatialValidation>();var report=check?.LastReport;
            if(check==null||check.Running||report==null||report.aborted||check.RestoredForReviewOnly||
                report.session_id!=StudyExportSession.SessionId||report.calibration_id!=calibrationId||
                !string.IsNullOrEmpty(report.correction_id)||report.targets==null||report.targets.Count!=5||
                report.targets.Any(t=>t.coverage<report.min_coverage||t.local_direction_pairs<12||t.mean_observed_head_local_direction?.Length!=3||t.mean_expected_head_local_direction?.Length!=3))
                throw new InvalidOperationException("Collect five complete uncorrected targets in this session first. Each needs adequate coverage and at least 12 valid direction pairs. Old exports without head-local pairs cannot be fitted.");
            Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
            var q=StudyGazeCorrection.Fit(report.targets.Select(t=>V(t.mean_observed_head_local_direction)).ToArray(),report.targets.Select(t=>V(t.mean_expected_head_local_direction)).ToArray());
            var model=new StudyGazeCorrection.Model{id="correction-"+Guid.NewGuid().ToString("N"),session_id=StudyExportSession.SessionId,calibration_id=calibrationId,
                training_verification_id=report.id,quaternion_xyzw=new[]{q.x,q.y,q.z,q.w},angle_degrees=Quaternion.Angle(Quaternion.identity,q)};
            string folder=Path.Combine(StudyExportSession.Folder,"Calibration",model.id);Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"correction.json");StudyAtomicFile.Write(path,JsonConvert.SerializeObject(model,Formatting.Indented));
            CurrentCorrection=model;CorrectionPath=path;mappingVerified=false;
            if(StudyEvents.Recording)StudyEvents.Record("gaze_correction_fitted",model.id,"independent_verification_required","training="+report.id+"; rotation_only; angle_deg="+model.angle_degrees);
            check.independentCheckPattern=true;check.Begin(calibrationId);
        }
        public void RestoreCorrectionForSameSession(string path)
        {
            mappingVerified=false;
            if(StudyExportSession.Folder==null)throw new InvalidOperationException("No active session.");
            StudyArchivePaths.RequireContainedUnlinked(StudyExportSession.Folder,path);
            if(new FileInfo(path).Length>65536)throw new InvalidDataException("Correction file too large.");
            var model=JsonConvert.DeserializeObject<StudyGazeCorrection.Model>(File.ReadAllText(path));
            if(model==null||model.schema!="gazeflow-local-ray-correction-v1"||model.session_id!=StudyExportSession.SessionId||model.calibration_id!=calibrationId||string.IsNullOrWhiteSpace(model.id)||string.IsNullOrWhiteSpace(model.training_verification_id))throw new InvalidDataException("Correction session/configuration mismatch.");
            var rotation=model.Rotation;CurrentCorrection=model;CorrectionPath=path;
        }
        public Ray CorrectLocalRay(Ray raw)
        {
            if(CurrentCorrection==null)return raw;
            if(CurrentCorrection.session_id!=StudyExportSession.SessionId||CurrentCorrection.calibration_id!=calibrationId)throw new InvalidOperationException("Correction no longer matches the current session/configuration.");
            return new Ray(raw.origin,CurrentCorrection.Rotation*raw.direction);
        }
        private bool ReportMatchesCorrection(StudySpatialValidation check)
        {
            if(check==null||check.LastReport==null)return false;
            var report=check.LastReport;
            if(CurrentCorrection==null)return string.IsNullOrEmpty(report.correction_id);
            return report.correction_id==CurrentCorrection.id&&report.purpose=="independent_live_verification"&&report.id!=CurrentCorrection.training_verification_id&&CurrentCorrection.session_id==StudyExportSession.SessionId&&CurrentCorrection.calibration_id==calibrationId;
        }
        /// <summary>Call only after explicit operator review. Scene rebinding never calls this automatically.</summary>
        public void ApproveCurrentMapping()
        {
            var check=GetComponent<StudySpatialValidation>();
            if(headReference==null || check==null || check.headReference!=headReference || check.Running ||
                check.LastReport==null || !check.LastReport.passed || check.LastReport.aborted ||
                check.LastReport.session_id!=StudyExportSession.SessionId ||
                check.LastReport.calibration_id!=calibrationId || !check.HasRecentValidSignal ||
                !ReportMatchesCorrection(check) ||
                !System.IO.File.Exists(System.IO.Path.Combine(check.LastOutput,"validation.json")) ||
                !System.IO.File.Exists(System.IO.Path.Combine(check.LastOutput,"validation_samples.csv")))
                throw new InvalidOperationException("A saved passing same-session report, matching calibration/head and fresh worn signal are required. Revalidate if the setup changed.");
            mappingVerified=true;check.BindingReviewRequired=false;
            if(StudyEvents.Recording)StudyEvents.Record("gaze_mapping_operator_approved",check.LastReport.id,
                check.RestoredForReviewOnly?"explicit_reconfirmation_after_head_rebind":"explicit_initial_approval",
                "scene="+headReference.gameObject.scene.name+"; head="+headReference.name+"; same wearing/setup confirmed by operator, not a new accuracy measurement");
        }
        private NeonGazeDataProvider subscribed;
        private volatile RTSPClient lastClient;
        private long lastTimestamp;
        private long receiveSequence, consumedSequence;
        private void TrackClient(RTSPClient client)
        {
            if (ReferenceEquals(client, lastClient)) return;
            if (lastClient != null) lastClient.GazeDataReceived -= PacketReceived;
            lastClient = client; lastTimestamp = 0; consumedSequence = 0;
            Interlocked.Exchange(ref receiveSequence, 0);
            if (client != null) client.GazeDataReceived += PacketReceived;
        }
        private void PacketReceived(object sender, EventArgs args)
        {
            if (ReferenceEquals(sender, lastClient)) Interlocked.Increment(ref receiveSequence);
        }
        private void Update() => TrackClient(neon == null ? null : neon.RTSPClient);
        private void OnEnable()
        {
            TrackClient(null); lastTimestamp = 0;
            subscribed = neon;
            if (subscribed != null && subscribed.gazeDataReady != null) subscribed.gazeDataReady.AddListener(Receive);
        }
        private void OnDisable()
        {
            TrackClient(null);
            if (subscribed != null && subscribed.gazeDataReady != null) subscribed.gazeDataReady.RemoveListener(Receive);
        }
        private void Receive(GazeDataProvider provider)
        {
            var client = neon == null ? null : neon.RTSPClient;
            // Official simulated samples have no live RTSP client. Never treat them as research data.
            if (client == null) { Invalid("no_live_client_or_simulation"); return; }
            TrackClient(client);
            var data = client.GazeData;
            bool websocket = client is RTSPClientWs;
            if (websocket)
            {
                // The pinned official WS client omits device timestamps. Require a
                // real new receive event instead; never substitute PC time as device time.
                long sequence = Interlocked.Read(ref receiveSequence);
                if (sequence <= consumedSequence) { Invalid("no_new_websocket_packet"); return; }
                consumedSequence = sequence;
            }
            else
            {
                if (data.timestampMs <= 0 || data.timestampMs <= lastTimestamp)
                { Invalid("missing_duplicate_or_out_of_order_device_time"); return; }
                lastTimestamp = data.timestampMs;
            }
            var sample = new StudyGazeRaySample { source = websocket ? "pupil_labs_neon_xr_callback_WS_no_device_time" : "pupil_labs_neon_xr_callback",
                deviceTimestampMs = websocket ? 0 : data.timestampMs, rtcpSynchronized = !websocket && data.rtcpSynchronized,
                calibrationId = calibrationId, status = "mapping_unverified",
                deviceTimeSemantics = websocket ? "device_time_UNAVAILABLE_official_WS;freshness_from_receive_sequence;latest_ray_snapshot_NOT_atomic_packet_pair" : "latest_RTSP_snapshot_NOT_atomic_ray_timestamp_pair" };
            if (!data.worn) { sample.status = "not_worn"; Reject(sample); return; }
            if (headReference == null) { sample.status = "missing_head_reference"; Reject(sample); return; }
            var rawLocalRay=provider.GazeRay;
            sample.rawWorldRay=new Ray(headReference.TransformPoint(rawLocalRay.origin),headReference.TransformDirection(rawLocalRay.direction));sample.hasRawWorldRay=true;
            sample.correctionId=CurrentCorrection?.id??"";
            Ray localRay;
            try{localRay=CorrectLocalRay(rawLocalRay);}catch(InvalidOperationException){mappingVerified=false;sample.status="correction_session_or_config_mismatch";Reject(sample);return;}
            sample.worldRay = new Ray(headReference.TransformPoint(localRay.origin), headReference.TransformDirection(localRay.direction));
            sample.valid = StudyGazeTargetLog.Finite(sample.worldRay.origin) && StudyGazeTargetLog.Finite(sample.worldRay.direction) && sample.worldRay.direction.sqrMagnitude > 0.000001f;
            sample.status = sample.valid ? "spatial_ray_before_operator_approval" : "invalid_ray";
            PublishValidation(sample);
            var validation = GetComponent<StudySpatialValidation>();
            sample.verificationId = validation == null ? "" : validation.CurrentVerificationId;
            sample.verificationStatus = validation == null ? "not_measured" : validation.CurrentVerificationStatus;
            if(validation!=null && validation.RestoredForReviewOnly)
                sample.verificationStatus=mappingVerified?"prior_binding_report_operator_reconfirmed_current_binding":"prior_binding_report_current_binding_unapproved";
            if(validation != null && validation.LastReport != null && validation.LastReport.calibration_id != calibrationId)
            { sample.verificationId=""; sample.verificationStatus="calibration_changed_since_validation"; }
            if (!mappingVerified || string.IsNullOrWhiteSpace(calibrationId) || validation==null || validation.Running || validation.LastReport==null || !validation.LastReport.passed || validation.LastReport.aborted || validation.LastReport.session_id!=StudyExportSession.SessionId || validation.LastReport.calibration_id!=calibrationId || !ReportMatchesCorrection(validation))
            { sample.candidateRayAvailable=sample.valid;sample.valid = false; sample.status = "mapping_unverified"; Publish(sample); return; }
            if (sample.valid) sample.status = "operator_verified_mapping_not_hardware_validated";
            Publish(sample);
        }
        private void Reject(StudyGazeRaySample sample) { PublishValidation(sample); Publish(sample); }
        private void Invalid(string status) => Reject(new StudyGazeRaySample { source = "pupil_labs_neon_xr_callback", status = status, calibrationId = calibrationId });
    }
}
