using System;
using UnityEngine;
namespace NeonResearch
{
    public struct StudyGazeRaySample
    {
        public Ray worldRay;
        public Ray rawWorldRay;
        public bool hasRawWorldRay;
        public bool candidateRayAvailable;
        public string correctionId;
        public bool valid;
        public string source, calibrationId, status, deviceTimeSemantics;
        public string verificationId, verificationStatus;
        public long deviceTimestampMs;
        public bool rtcpSynchronized;
    }
    /// <summary>Only publish fresh samples on Unity's main thread. No implicit recording.</summary>
    public abstract class StudyGazeRayProvider : MonoBehaviour
    {
        public event Action<StudyGazeRaySample> SampleReady;
        // For known-target validation BEFORE operator approval; never feed this into AOI logging.
        public event Action<StudyGazeRaySample> ValidationSampleReady;
        protected void PublishValidation(StudyGazeRaySample sample) => ValidationSampleReady?.Invoke(sample);
        protected void Publish(StudyGazeRaySample sample) => SampleReady?.Invoke(sample);
    }
}
