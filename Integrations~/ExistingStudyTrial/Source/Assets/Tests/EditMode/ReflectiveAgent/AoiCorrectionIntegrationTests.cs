using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NeonResearch;
using NUnit.Framework;
using UnityEngine;
namespace EscapeRoom.ReflectiveAgent.Tests
{
    public sealed class AoiCorrectionIntegrationTests
    {
        private static void Set(object obj,string name,object value)=>obj.GetType().GetField("<"+name+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
        [Test] public void FittingCannotApproveItselfAndOnlyIndependentMatchingReportEnablesMapping()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/CorrectionIntegration/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic correction pipeline");
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);var type=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);
                var provider=(StudyGazeRayProvider)host.AddComponent(type);type.GetField("headReference").SetValue(provider,host.transform);type.GetField("calibrationId").SetValue(provider,"synthetic-config");
                var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;
                var pattern=new[]{Vector3.forward,new Vector3(-.21f,0,1).normalized,new Vector3(.21f,0,1).normalized,new Vector3(0,.14f,1).normalized,new Vector3(0,-.14f,1).normalized};
                var q=Quaternion.Euler(2,-4,0);
                var report=new StudySpatialValidation.ValidationReport{id="synthetic-training",session_id=StudyExportSession.SessionId,calibration_id="synthetic-config",min_coverage=.7f,targets=new List<StudySpatialValidation.TargetResult>()};
                foreach(var expected in pattern){var raw=Quaternion.Inverse(q)*expected;report.targets.Add(new StudySpatialValidation.TargetResult{coverage=1,local_direction_pairs=50,mean_observed_head_local_direction=new[]{raw.x,raw.y,raw.z},mean_expected_head_local_direction=new[]{expected.x,expected.y,expected.z}});}
                Set(check,"LastReport",report);type.GetMethod("FitCorrectionAndBeginVerification").Invoke(provider,null);
                var model=(StudyGazeCorrection.Model)type.GetProperty("CurrentCorrection").GetValue(provider);
                Assert.That(Quaternion.Angle(model.Rotation,q),Is.LessThan(.05f));Assert.That(check.Running,Is.True);Assert.That(check.independentCheckPattern,Is.True);
                Assert.That((bool)type.GetField("mappingVerified").GetValue(provider),Is.False);
                Assert.Throws<TargetInvocationException>(()=>type.GetMethod("ApproveCurrentMapping").Invoke(provider,null));
                var rawRay=new Ray(new Vector3(.01f,.02f,.03f),Quaternion.Inverse(q)*Vector3.forward);
                var corrected=(Ray)type.GetMethod("CorrectLocalRay").Invoke(provider,new object[]{rawRay});Assert.That(corrected.origin,Is.EqualTo(rawRay.origin));Assert.That(Vector3.Angle(corrected.direction,Vector3.forward),Is.LessThan(.05f));
                check.Abort();report.id="synthetic-new-verification";report.passed=true;report.aborted=false;report.correction_id=model.id;report.purpose="fit_collection_or_uncorrected_validation";
                string dir=Path.Combine(folder,"Calibration/synthetic-new-verification");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"validation.json"),JsonUtility.ToJson(report));File.WriteAllText(Path.Combine(dir,"validation_samples.csv"),"synthetic_only\n");Set(check,"LastReport",report);Set(check,"LastOutput",dir);
                typeof(StudyGazeRayProvider).GetMethod("PublishValidation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(provider,new object[]{new StudyGazeRaySample{valid=true,calibrationId="synthetic-config",correctionId=model.id,worldRay=new Ray(Vector3.zero,Vector3.forward)}});
                Assert.Throws<TargetInvocationException>(()=>type.GetMethod("ApproveCurrentMapping").Invoke(provider,null));
                report.purpose="independent_live_verification";File.WriteAllText(Path.Combine(dir,"validation.json"),JsonUtility.ToJson(report));
                type.GetMethod("ApproveCurrentMapping").Invoke(provider,null);Assert.That((bool)type.GetField("mappingVerified").GetValue(provider),Is.True);
                type.GetField("calibrationId").SetValue(provider,"changed-config");Assert.Throws<TargetInvocationException>(()=>type.GetMethod("CorrectLocalRay").Invoke(provider,new object[]{rawRay}));
            }
            finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        [Test] public void TutorialGrabAndReleaseWriteEvenWithoutEyeMappingApproval()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/CorrectionIntegration/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic tutorial bridge");var target=new GameObject("Synthetic tutorial cup");target.SetActive(false);
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);target.AddComponent<StudyResearchTarget>().targetId="synthetic-tutorial-cup";
                Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
                var grabbable=target.AddComponent(Find("Oculus.Interaction.Grabbable"));target.SetActive(true);var bridge=host.AddComponent(Find("GazeFlowTutorialGrabRecorder"));
                var pointer=Find("Oculus.Interaction.PointerEvent");var kind=Find("Oculus.Interaction.PointerEventType");
                object E(string type)=>Activator.CreateInstance(pointer,new object[]{42,Enum.Parse(kind,type),Pose.identity,null});
                var record=bridge.GetType().GetMethod("RecordPointer");record.Invoke(bridge,new[]{(object)grabbable,E("Select")});record.Invoke(bridge,new[]{(object)grabbable,E("Select")});record.Invoke(bridge,new[]{(object)grabbable,E("Unselect")});
                StudyExportSession.Finish();var csv=File.ReadAllLines(Path.Combine(folder,"game_events.csv"));
                Assert.That(csv.Count(line=>line.Contains("grab_start")),Is.EqualTo(1));Assert.That(csv.Count(line=>line.Contains("grab_end")),Is.EqualTo(1));Assert.That(string.Join("\n",csv),Does.Contain("synthetic-tutorial-cup"));
            }
            finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(target);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
}
