using PupilLabs;
using System;
using System.Threading;
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
