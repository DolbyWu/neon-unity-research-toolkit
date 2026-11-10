using PupilLabs;
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
        private NeonGazeDataProvider subscribed;
        private RTSPClient lastClient;
        private long lastTimestamp;
        private void OnEnable()
        {
            lastClient = null; lastTimestamp = 0;
            subscribed = neon;
            if (subscribed != null && subscribed.gazeDataReady != null) subscribed.gazeDataReady.AddListener(Receive);
        }
        private void OnDisable()
        {
            if (subscribed != null && subscribed.gazeDataReady != null) subscribed.gazeDataReady.RemoveListener(Receive);
        }
        private void Receive(GazeDataProvider provider)
        {
            var client = neon == null ? null : neon.RTSPClient;
            // Official simulated samples have no live RTSP client. Never treat them as research data.
            if (client == null) { Invalid("no_live_client_or_simulation"); return; }
            var data = client.GazeData;
            if (client != lastClient) { lastClient = client; lastTimestamp = 0; }
            if (data.timestampMs <= 0 || data.timestampMs <= lastTimestamp)
            { Invalid("missing_duplicate_or_out_of_order_device_time"); return; }
            lastTimestamp = data.timestampMs;
            var sample = new StudyGazeRaySample { source = "pupil_labs_neon_xr_callback",
                deviceTimestampMs = data.timestampMs, rtcpSynchronized = data.rtcpSynchronized,
                calibrationId = calibrationId, status = "mapping_unverified",
                deviceTimeSemantics = "latest_RTSP_snapshot_NOT_atomic_ray_timestamp_pair" };
            if (!mappingVerified || string.IsNullOrWhiteSpace(calibrationId)) { Publish(sample); return; }
            if (!data.worn) { sample.status = "not_worn"; Publish(sample); return; }
            if (headReference == null) { sample.status = "missing_head_reference"; Publish(sample); return; }
            var localRay = provider.GazeRay;
            sample.worldRay = new Ray(headReference.TransformPoint(localRay.origin), headReference.TransformDirection(localRay.direction));
            sample.valid = true; sample.status = "operator_verified_mapping_not_hardware_validated";
            Publish(sample);
        }
        private void Invalid(string status) => Publish(new StudyGazeRaySample { source = "pupil_labs_neon_xr_callback", status = status, calibrationId = calibrationId });
    }
}
