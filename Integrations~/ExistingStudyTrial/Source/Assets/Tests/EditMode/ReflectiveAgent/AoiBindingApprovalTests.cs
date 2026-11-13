using System;
using System.IO;
using System.Reflection;
using NeonResearch;
using NUnit.Framework;
using UnityEngine;

namespace EscapeRoom.ReflectiveAgent.Tests
{
    public sealed class AoiBindingApprovalTests
    {
        [Test] public void RestoredReportNeverAutoApprovesAndExplicitApprovalRequiresFreshMatchingBinding()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/AoiBindingApprovalTests/"+Guid.NewGuid().ToString("N"));
            var host=new GameObject("Synthetic adapter approval test");
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);string dir=Path.Combine(folder,"Calibration/synthetic");Directory.CreateDirectory(dir);
                var report=new StudySpatialValidation.ValidationReport{id="synthetic-binding-review",session_id=StudyExportSession.SessionId,calibration_id="synthetic-config",passed=true};
                File.WriteAllText(Path.Combine(dir,"validation.json"),JsonUtility.ToJson(report));File.WriteAllText(Path.Combine(dir,"validation_samples.csv"),"synthetic_only\n");
                var type=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);
                var provider=(StudyGazeRayProvider)host.AddComponent(type);
                type.GetField("headReference").SetValue(provider,host.transform);type.GetField("calibrationId").SetValue(provider,"synthetic-config");
                var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;check.BindingReviewRequired=true;
                check.RestoreSameSessionReportForReview(dir);
                var approve=type.GetMethod("ApproveCurrentMapping");
                Assert.That((bool)type.GetField("mappingVerified").GetValue(provider),Is.False);
                Assert.Throws<TargetInvocationException>(()=>approve.Invoke(provider,null));
                typeof(StudyGazeRayProvider).GetMethod("PublishValidation",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(provider,new object[]{new StudyGazeRaySample{valid=true,calibrationId="synthetic-config",worldRay=new Ray(Vector3.zero,Vector3.forward)}});
                approve.Invoke(provider,null);
                Assert.That((bool)type.GetField("mappingVerified").GetValue(provider),Is.True);Assert.That(check.BindingReviewRequired,Is.False);
                type.GetField("mappingVerified").SetValue(provider,false);type.GetField("calibrationId").SetValue(provider,"changed-config");
                Assert.Throws<TargetInvocationException>(()=>approve.Invoke(provider,null));
                Assert.That((bool)type.GetField("mappingVerified").GetValue(provider),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
}
