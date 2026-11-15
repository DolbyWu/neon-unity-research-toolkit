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
        [Test] public void CurrentToolsStayDirectWhileLegacyAndDiagnosticsAreGrouped()
        {
            Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
            string[] Menus(string name)=>Find(name).GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).SelectMany(m=>m.GetCustomAttributes(typeof(UnityEditor.MenuItem),false).Cast<UnityEditor.MenuItem>()).Select(a=>a.menuItem).ToArray();
            Assert.That(Menus("NeonResearch.NeonXR.Editor.DesktopGazeAdjustmentWindow"),Does.Contain("Tools/GazeFlow/Desktop Gaze Adjustment"));
            Assert.That(Menus("NeonResearch.LiveObservationWindow"),Does.Contain("Tools/GazeFlow/Live Observation Marker"));
            Assert.That(Menus("NeonResearch.StudyLiveMonitor"),Does.Contain("Tools/Neon Research/Open Live Monitor"));
            Assert.That(Menus("NeonResearch.StudyArchiveEditor"),Does.Contain("Tools/Neon Research/Open Participant Archive"));
            Assert.That(Menus("NeonResearch.NeonResearchSetup"),Does.Contain("Tools/Neon Research/Setup and Recording Settings"));
            Assert.That(Menus("NeonResearch.NeonXR.Editor.GazePointerWindow"),Is.Empty);Assert.That(Menus("GazeFlowTrialSessionPanel"),Is.Empty);
            foreach(var name in new[]{"NeonResearch.NeonXR.Editor.PreRoomValidationWindow","NeonResearch.NeonXR.Editor.ObjectCalibrationWindow"})Assert.That(Menus(name).All(p=>p.StartsWith("Tools/GazeFlow/Advanced/Legacy/")),Is.True,name);
            foreach(var name in new[]{"GazeFlowCorrectionChecks","GazeFlowHierarchyOrganizer","GazeFlowTutorialObjectAudit","StudySequenceVerification","StudyParticipantVerification"})Assert.That(Menus(name).All(p=>p.StartsWith("Tools/GazeFlow/Advanced/Diagnostics/")),Is.True,name);
            Assert.That(Menus("StudyTranscriptionEditor"),Does.Contain("Tools/Study/Transcription/Transcribe Finished Session (OpenAI)"));
        }
        [Test] public void DirectGrabEntryHidesOnlyLegacyVrIdentityAndWelcomeAudio()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try{
                var menu=new GameObject("USER ID Variant Menu");var keyboard=new GameObject("ID keypad");keyboard.transform.SetParent(menu.transform);
                var tutorial=new GameObject("Original grabbing station");var welcome=new GameObject("Welcome Audio");welcome.transform.SetParent(tutorial.transform);var audio=welcome.AddComponent<AudioSource>();audio.playOnAwake=true;
                var narration=new GameObject("Grab narration");narration.transform.SetParent(tutorial.transform);narration.AddComponent<AudioSource>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(menu,scene);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tutorial,scene);
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GazeFlowTutorialEntry")).First(t=>t!=null);
                type.GetMethod("PrepareDirectGrabEntry").Invoke(null,new object[]{scene});
                Assert.That(menu.activeSelf,Is.False);Assert.That(keyboard.activeInHierarchy,Is.False);Assert.That(welcome.activeSelf,Is.False);Assert.That(audio.playOnAwake,Is.False);
                Assert.That(tutorial.activeSelf,Is.True);Assert.That(narration.activeSelf,Is.True,"Original grabbing instructions remain available.");
            }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void OptionalPanelCleanupNeverClosesArchiveMonitorOrObservationNotes()
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GazeFlowTrialSessionPanel")).First(t=>t!=null);var method=type.GetMethod("IsOptionalPanel");
            foreach(var name in new[]{"GazeFlowTrialSessionPanel","NeonResearch.NeonXR.Editor.PreRoomValidationWindow","NeonResearch.NeonXR.Editor.ObjectCalibrationWindow","NeonResearch.NeonXR.Editor.GazePointerWindow","NeonResearch.NeonXR.Editor.DesktopGazeAdjustmentWindow"})Assert.That((bool)method.Invoke(null,new object[]{name}),Is.True,name);
            foreach(var name in new[]{"NeonResearch.StudyLiveMonitor","NeonResearch.LiveObservationWindow","NeonResearch.StudyParticipantStartWindow","NeonResearch.ObservationNoteWindow"})Assert.That((bool)method.Invoke(null,new object[]{name}),Is.False,name);
        }
        [Test] public void SceneRecorderIsReusedAndReboundWhenInitializationReloadCreatesAStaleDuplicate()
        {
            var entry=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GazeFlowTutorialEntry")).First(t=>t!=null);var reset=entry.GetMethod("ResetHooks",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            var root=new GameObject("GazeFlow Research Target Recorder");var nextRoot=new GameObject("GazeFlow Research Target Recorder");var a=new GameObject("Synthetic prior official singleton");var b=new GameObject("Synthetic current singleton");
            try{
                var type=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);var first=(StudyGazeRayProvider)a.AddComponent(type);var second=(StudyGazeRayProvider)b.AddComponent(type);
                var recorder=root.AddComponent<StudyGazeTargetRecorder>();recorder.BindProvider(first);nextRoot.SetActive(false);var duplicate=nextRoot.AddComponent<StudyGazeTargetRecorder>();duplicate.provider=first;
                var ensure=entry.GetMethod("EnsureSessionRecorder");Assert.That(ensure.Invoke(null,new object[]{first}),Is.SameAs(recorder));
                UnityEngine.Object.DestroyImmediate(a);nextRoot.SetActive(true);
                Assert.That(ensure.Invoke(null,new object[]{second}),Is.SameAs(recorder));Assert.That(duplicate.enabled,Is.False);Assert.That(recorder.provider,Is.SameAs(second));Assert.That(recorder.enabled,Is.True);
                Assert.That(ensure.Invoke(null,new object[]{second}),Is.SameAs(recorder));Assert.That(UnityEngine.Object.FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Count(r=>r.isActiveAndEnabled),Is.EqualTo(1));
            }finally{if(a!=null)UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(nextRoot);reset.Invoke(null,null);}
        }
        [Test] public void OfficialSourceSubscriptionCanRebindAndReenableWithoutDuplicateListeners()
        {
            var sourceType=Type.GetType("PupilLabs.NeonGazeDataProvider, PupilLabs.NeonXR.Core.Runtime",true);var eventType=Type.GetType("PupilLabs.Serializable.GazeDataProviderEvent, PupilLabs.NeonXR.Core.Runtime",true);
            var sourceA=new GameObject("Synthetic official event A");var sourceB=new GameObject("Synthetic official event B");var host=new GameObject("Synthetic adapter");
            try{
                var a=sourceA.AddComponent(sourceType);var b=sourceB.AddComponent(sourceType);var eventField=sourceType.BaseType.GetField("gazeDataReady");eventField.SetValue(a,Activator.CreateInstance(eventType));eventField.SetValue(b,Activator.CreateInstance(eventType));
                var type=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);var provider=(StudyGazeRayProvider)host.AddComponent(type);int count=0;provider.SampleReady+=s=>{count++;Assert.That(s.valid,Is.False,"A source without a live client never becomes research data.");};
                var refresh=type.GetMethod("RefreshSourceSubscription");void Fire(Component value){var evt=eventField.GetValue(value);evt.GetType().GetMethod("Invoke").Invoke(evt,new object[]{value});}
                type.GetField("neon").SetValue(provider,a);refresh.Invoke(provider,null);Fire(a);Assert.That(count,Is.EqualTo(1));
                type.GetField("neon").SetValue(provider,b);refresh.Invoke(provider,null);refresh.Invoke(provider,null);Fire(a);Fire(b);Assert.That(count,Is.EqualTo(2));
                provider.enabled=false;type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(provider,null);Fire(b);Assert.That(count,Is.EqualTo(2));refresh.Invoke(provider,null);provider.enabled=true;type.GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(provider,null);Fire(b);Assert.That(count,Is.EqualTo(3));
            }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(sourceA);UnityEngine.Object.DestroyImmediate(sourceB);}
        }
        [Test] public void OptionalSpatialQualityNeverInvalidatesUsableOfficialRays()
        {
            var host=new GameObject("Synthetic official-first quality");
            try{
                var type=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);var provider=host.AddComponent(type);
                type.GetField("headReference").SetValue(provider,host.transform);type.GetField("calibrationId").SetValue(provider,"synthetic");
                var method=type.GetMethod("ApplyMappingQuality");var ray=new Ray(new Vector3(.01f,.02f,.03f),Vector3.forward);
                var sample=new StudyGazeRaySample{valid=true,worldRay=ray,rawWorldRay=ray,hasRawWorldRay=true};
                var result=(StudyGazeRaySample)method.Invoke(provider,new object[]{sample});Assert.That(result.valid,Is.True);Assert.That(result.mappingVerified,Is.False);Assert.That(result.worldRay.origin,Is.EqualTo(ray.origin));Assert.That(result.worldRay.direction,Is.EqualTo(ray.direction));Assert.That(result.verificationStatus,Is.EqualTo("not_measured_current_binding"));
                var check=host.AddComponent<StudySpatialValidation>();check.headReference=host.transform;Set(check,"LastReport",new StudySpatialValidation.ValidationReport{session_id=StudyExportSession.SessionId,calibration_id="synthetic",passed=false});
                result=(StudyGazeRaySample)method.Invoke(provider,new object[]{sample});Assert.That(result.valid,Is.True);Assert.That(result.mappingVerified,Is.False);Assert.That(result.verificationStatus,Is.EqualTo("measured_fail"));
                sample.valid=false;sample.status="not_worn";result=(StudyGazeRaySample)method.Invoke(provider,new object[]{sample});Assert.That(result.valid,Is.False);Assert.That(result.mappingVerified,Is.False);Assert.That(result.status,Is.EqualTo("not_worn"));
                type.GetMethod("UseOfficialRayWithoutExtraCorrection").Invoke(provider,null);Assert.That(type.GetProperty("CurrentCorrection").GetValue(provider),Is.Null);Assert.That((bool)type.GetField("automaticTwoRoundFlow").GetValue(provider),Is.False);
            }finally{UnityEngine.Object.DestroyImmediate(host);}
        }
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
                foreach(var expected in pattern){var raw=Quaternion.Inverse(q)*expected;report.targets.Add(new StudySpatialValidation.TargetResult{target_index=report.targets.Count,coverage=1,local_direction_pairs=50,mean_observed_head_local_direction=new[]{raw.x,raw.y,raw.z},mean_expected_head_local_direction=new[]{expected.x,expected.y,expected.z}});}
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
