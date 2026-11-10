using System;
using UnityEngine;
namespace NeonResearch
{
    public struct StudyGazeRaySample
    {
        public Ray worldRay;
        public bool valid;
        public string source, calibrationId, status, deviceTimeSemantics;
        public long deviceTimestampMs;
        public bool rtcpSynchronized;
    }
    /// <summary>Only publish fresh samples on Unity's main thread. No implicit recording.</summary>
    public abstract class StudyGazeRayProvider : MonoBehaviour
    {
        public event Action<StudyGazeRaySample> SampleReady;
        protected void Publish(StudyGazeRaySample sample) => SampleReady?.Invoke(sample);
    }
}
