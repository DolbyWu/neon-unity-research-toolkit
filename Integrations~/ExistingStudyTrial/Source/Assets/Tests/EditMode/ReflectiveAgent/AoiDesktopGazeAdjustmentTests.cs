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
    /// <summary>Synthetic local data only. Never connects to Neon, records media or touches participant archives.</summary>
    public sealed class AoiDesktopGazeAdjustmentTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private static Type Adapter=>Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);
        private static Type Preview=>Type.GetType("NeonResearch.NeonXR.StudyDesktopGazePreview, GazeFlow.NeonXR",true);
        private static void Reset()=>typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        private static void Set(object obj,string property,object value)=>obj.GetType().GetField("<"+property+">k__BackingField",Hidden).SetValue(obj,value);
        private sealed class Fixture:IDisposable
        {
            public readonly GameObject host;
            public readonly StudyGazeRayProvider provider;
            public readonly string root,folder;
            public Fixture(){Reset();root=Path.GetFullPath("Library/DesktopGazeTests/"+Guid.NewGuid().ToString("N"));folder=StudyExportSession.EnsureStarted(root);host=new GameObject("Synthetic desktop adjustment");provider=(StudyGazeRayProvider)host.AddComponent(Adapter);Adapter.GetField("headReference").SetValue(provider,host.transform);Adapter.GetField("calibrationId").SetValue(provider,"synthetic-mount");}
            public object Call(string name,params object[] args)=>Adapter.GetMethod(name).Invoke(provider,args);
            public StudyGazeCorrection.Model Model=>(StudyGazeCorrection.Model)Adapter.GetProperty("CurrentCorrection").GetValue(provider);
            public bool Approved=>(bool)Adapter.GetField("mappingVerified").GetValue(provider);
            public void Emit(StudyGazeRaySample sample)=>Adapter.GetMethod("PublishResearch",Hidden).Invoke(provider,new object[]{sample});
            public void Dispose(){UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();Reset();if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        [Test] public void ManualChangesSaveImmutableVersionsPreserveOriginAndNeverApprove()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",1f,2f);var first=f.Model;string path=(string)Adapter.GetProperty("CorrectionPath").GetValue(f.provider);var bytes=File.ReadAllText(path);
                Assert.That(first.correction_kind,Is.EqualTo("manual_yaw_pitch"));Assert.That(first.training_verification_id,Is.Null);Assert.That(first.applied_session_s.HasValue,Is.True);Assert.That(f.Approved,Is.False);
                var raw=new Ray(new Vector3(.01f,.02f,.03f),Vector3.forward);var corrected=(Ray)f.Call("CorrectLocalRay",raw);
                Assert.That(corrected.origin,Is.EqualTo(raw.origin));Assert.That(corrected.direction.x,Is.GreaterThan(0));Assert.That(corrected.direction.y,Is.GreaterThan(0));Assert.That(raw.direction,Is.EqualTo(Vector3.forward));
                var quality=(StudyGazeRaySample)f.Call("ApplyMappingQuality",new StudyGazeRaySample{valid=true,worldRay=corrected,rawWorldRay=raw,hasRawWorldRay=true,correctionId=first.id});Assert.That(quality.valid,Is.True);Assert.That(quality.mappingVerified,Is.False);
                f.Call("ApplyDesktopOffset",.5f,1f);Assert.That(f.Model.id,Is.Not.EqualTo(first.id));Assert.That(File.ReadAllText(path),Is.EqualTo(bytes));Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void ChangesRequireRecordingAndCannotEditDuringCheckOrExceedBounds()
        {
            using(var f=new Fixture())
            {
                Assert.Throws<TargetInvocationException>(()=>f.Call("ApplyDesktopOffset",float.NaN,0f));Assert.Throws<TargetInvocationException>(()=>f.Call("ApplyDesktopOffset",10f,10f));
                var check=(StudySpatialValidation)f.Call("BeginDesktopAccuracyCheck");Assert.That(check.Running,Is.True);Assert.That(check.TutorialEntryContext,Is.False);Assert.That((bool)Adapter.GetField("automaticTwoRoundFlow").GetValue(f.provider),Is.False);
                Assert.Throws<TargetInvocationException>(()=>f.Call("ApplyDesktopOffset",1f,0f));Assert.That(f.Model,Is.Null);
                f.Call("UseOfficialRayWithoutExtraCorrection");Assert.That(check.Running,Is.False);Assert.That(check.LastReport.aborted,Is.True);
                StudyExportSession.Finish();Reset();Assert.Throws<TargetInvocationException>(()=>f.Call("ApplyDesktopOffset",1f,0f));Assert.That(StudyExportSession.Folder,Is.Null);
            }
        }
        [Test] public void ManualOffsetSurvivesSameSessionSceneRebindButNotConfigurationOrSessionChange()
        {
            using(var f=new Fixture())
            {
                var head=new GameObject("Synthetic next scene head");
                try{
                    f.Call("ApplyDesktopOffset",1f,0f);var id=f.Model.id;Adapter.GetField("mappingVerified").SetValue(f.provider,true);
                    f.Call("RebindDesktopHead",head.transform,"synthetic-mount");Assert.That(f.Model.id,Is.EqualTo(id));Assert.That(f.Approved,Is.False);
                    f.Call("RebindDesktopHead",head.transform,"different-mount");Assert.That(f.Model,Is.Null);
                    f.Call("ApplyDesktopOffset",1f,0f);StudyExportSession.Finish();Reset();StudyExportSession.EnsureStarted(f.root);
                    f.Call("RebindDesktopHead",head.transform,"different-mount");Assert.That(f.Model,Is.Null);Assert.That(f.Approved,Is.False);
                }finally{UnityEngine.Object.DestroyImmediate(head);}
            }
        }
        [Test] public void OnlyFreshIndependentDesktopCheckCanApproveManualRevision()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",1f,0f);Assert.Throws<TargetInvocationException>(()=>f.Call("ApproveCurrentMapping"));
                var check=(StudySpatialValidation)f.Call("BeginDesktopAccuracyCheck");Assert.That(check.independentCheckPattern,Is.True);
                var sample=new StudyGazeRaySample{valid=true,calibrationId="synthetic-mount",correctionId=f.Model.id,worldRay=new Ray(Vector3.zero,Vector3.forward),rawWorldRay=new Ray(Vector3.zero,Vector3.forward),hasRawWorldRay=true,source="synthetic_only"};
                typeof(StudyGazeRayProvider).GetMethod("PublishValidation",Hidden).Invoke(f.provider,new object[]{sample});
                var points=(Vector3[])typeof(StudySpatialValidation).GetField("targets",Hidden).GetValue(check);
                var results=(List<StudySpatialValidation.TargetResult>)typeof(StudySpatialValidation).GetField("results",Hidden).GetValue(check);
                for(int i=0;i<points.Length;i++)results.Add(new StudySpatialValidation.TargetResult{target_index=i,passed=true,coverage=1,mean_error_deg=.5,p95_error_deg=.7,world_target=new[]{points[i].x,points[i].y,points[i].z}});
                typeof(StudySpatialValidation).GetMethod("Complete",Hidden).Invoke(check,new object[]{false,"synthetic_test_completion"});
                Assert.That(check.LastReport.purpose,Is.EqualTo("independent_live_verification"));Assert.That(check.LastReport.correction_id,Is.EqualTo(f.Model.id));Assert.That(f.Approved,Is.False);
                f.Call("ApproveCurrentMapping");Assert.That(f.Approved,Is.True);
                string reportId=check.LastReport.id;f.Call("ApplyDesktopOffset",2f,0f);Assert.That(f.Approved,Is.False);Assert.Throws<TargetInvocationException>(()=>f.Call("ApproveCurrentMapping"));
                var quality=(StudyGazeRaySample)f.Call("ApplyMappingQuality",sample);Assert.That(quality.mappingVerified,Is.False);
                var newCheck=(StudySpatialValidation)f.Call("BeginDesktopAccuracyCheck");Assert.That(newCheck.RunId,Is.Not.EqualTo(reportId));Assert.That(newCheck.LastReport,Is.Null);Assert.That(f.Model.horizontal_degrees,Is.EqualTo(2));newCheck.Abort();
            }
        }
        [Test] public void RawAndAdjustedRayExportsRemainDistinctAndOccludersStillBlock()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",2f,0f);var raw=new Ray(new Vector3(1000,1000,1000),Vector3.forward);var corrected=(Ray)f.Call("CorrectLocalRay",raw);
                var target=GameObject.CreatePrimitive(PrimitiveType.Cube);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                try{
                    target.name="Synthetic adjusted object";target.transform.position=corrected.GetPoint(3);target.transform.localScale=Vector3.one*.1f;target.AddComponent<StudyResearchTarget>().targetId="synthetic-adjusted-target";
                    wall.transform.position=corrected.GetPoint(1);wall.transform.localScale=Vector3.one*.1f;wall.SetActive(false);Physics.SyncTransforms();
                    var sample=new StudyGazeRaySample{valid=true,worldRay=corrected,rawWorldRay=raw,hasRawWorldRay=true,correctionId=f.Model.id,calibrationId="synthetic-mount",source="synthetic_only"};
                    Assert.That(StudyGazeTargetLog.Record(sample,~0,10,out var id),Is.True);Assert.That(id,Is.EqualTo("synthetic-adjusted-target"));
                    wall.SetActive(true);Physics.SyncTransforms();Assert.That(StudyGazeTargetLog.Record(sample,~0,10,out _),Is.False);
                    sample.valid=false;sample.status="not_worn";StudyGazeTargetLog.Record(sample,~0,10,out _);StudyExportSession.Finish();
                    var table=StudySequenceAnalysis.Rows(Path.Combine(f.folder,"gaze_targets.csv")).ToArray();var hit=table.Single(r=>r[Array.IndexOf(table[0],"status")]=="target_hit");
                    Assert.That(hit[Array.IndexOf(table[0],"correction_id")],Is.EqualTo(f.Model.id));Assert.That(hit[Array.IndexOf(table[0],"raw_ray_direction_x")],Is.EqualTo("0"));Assert.That(hit[Array.IndexOf(table[0],"mapping_verified")].ToLowerInvariant(),Is.EqualTo("false"));
                    Assert.That(File.ReadAllText(Path.Combine(f.folder,"gaze_targets.csv")),Does.Contain("non_target_occluder"));Assert.That(File.ReadAllText(Path.Combine(f.folder,"Replay/study-replay.html")),Does.Contain(f.Model.id));
                }finally{UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(wall);}
            }
        }
        [Test] public void ResearchPreviewUsesPublishedRayButNeverLogsOrDisplaysInvalidStaleGeometry()
        {
            using(var f=new Fixture())
            {
                var preview=f.host.AddComponent(Preview);Preview.GetMethod("Bind").Invoke(preview,new object[]{f.provider});
                var sample=new StudyGazeRaySample{valid=true,worldRay=new Ray(new Vector3(1000,1000,1000),Vector3.forward),source="synthetic_preview_only"};
                f.Emit(sample);var latest=(StudyGazeRaySample)Preview.GetField("latest",Hidden).GetValue(preview);Assert.That(latest.source,Is.EqualTo(sample.source));
                var display=Preview.GetMethod("CanDisplay");Assert.That((bool)display.Invoke(null,new object[]{sample,0d,.1f}),Is.True);Assert.That((bool)display.Invoke(null,new object[]{sample,.11d,.1f}),Is.False);
                Assert.That((bool)display.Invoke(null,new object[]{sample,0d,float.PositiveInfinity}),Is.False);sample.valid=false;Assert.That((bool)display.Invoke(null,new object[]{sample,0d,.1f}),Is.False);
                Assert.That(File.Exists(Path.Combine(f.folder,"gaze_targets.csv")),Is.False,"A preview cannot start target logging.");
            }
        }
        [Test] public void PreviewRebindAndReenableRejectOldSourceCallbacks()
        {
            using(var f=new Fixture())
            {
                var other=new GameObject("Synthetic replacement source");
                try{
                    var second=other.AddComponent(Adapter);var preview=(Behaviour)f.host.AddComponent(Preview);var bind=Preview.GetMethod("Bind");
                    void Fire(Component p,string source)=>Adapter.GetMethod("PublishResearch",Hidden).Invoke(p,new object[]{new StudyGazeRaySample{valid=true,source=source,worldRay=new Ray(Vector3.zero,Vector3.forward)}});
                    string Latest()=>((StudyGazeRaySample)Preview.GetField("latest",Hidden).GetValue(preview)).source;
                    bind.Invoke(preview,new object[]{f.provider});Fire(f.provider,"A");Assert.That(Latest(),Is.EqualTo("A"));bind.Invoke(preview,new object[]{second});Fire(f.provider,"obsolete");Assert.That(Latest(),Is.EqualTo("A"));Fire(second,"B");Assert.That(Latest(),Is.EqualTo("B"));
                    preview.enabled=false;Preview.GetMethod("OnDisable",Hidden).Invoke(preview,null);bind.Invoke(preview,new object[]{f.provider});preview.enabled=true;Preview.GetMethod("OnEnable",Hidden).Invoke(preview,null);Fire(second,"obsolete B");Assert.That(Latest(),Is.EqualTo("B"));Fire(f.provider,"C");Assert.That(Latest(),Is.EqualTo("C"));
                }finally{UnityEngine.Object.DestroyImmediate(other);}
            }
        }
        [Test] public void PreviewRendersResearchRayAtNearestOccluderWithoutAddingPhysicsOrLogging()
        {
            using(var f=new Fixture())
            {
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                try{
                    var ray=new Ray(new Vector3(1000,1000,1000),Quaternion.Euler(0,5,0)*Vector3.forward);wall.transform.position=ray.GetPoint(2);wall.transform.localScale=Vector3.one*.2f;Physics.SyncTransforms();
                    var preview=f.host.AddComponent(Preview);Preview.GetMethod("Bind").Invoke(preview,new object[]{f.provider});Preview.GetProperty("Visible").SetValue(preview,true);
                    f.Emit(new StudyGazeRaySample{valid=true,worldRay=ray,source="synthetic_display_only"});Preview.GetMethod("LateUpdate",Hidden).Invoke(preview,null);
                    var dot=(GameObject)Preview.GetField("dot",Hidden).GetValue(preview);Assert.That(dot.activeSelf,Is.True);Assert.That(dot.layer,Is.EqualTo(2));Assert.That(dot.GetComponent<Collider>().enabled,Is.False);
                    Assert.That(Vector3.Distance(dot.transform.position,StudyGazeTargetLog.Query(ray,~0,30).point),Is.LessThan(.0001f));Assert.That(wall.GetComponent<Collider>().enabled,Is.True);
                    Preview.GetField("received",Hidden).SetValue(preview,-999999d);Preview.GetMethod("LateUpdate",Hidden).Invoke(preview,null);Assert.That(dot.activeSelf,Is.False);
                    Assert.That(File.Exists(Path.Combine(f.folder,"gaze_targets.csv")),Is.False);
                    // Non-ExecuteAlways MonoBehaviour lifecycle is not automatically run in EditMode.
                    Preview.GetMethod("OnDestroy",Hidden).Invoke(preview,null);UnityEngine.Object.DestroyImmediate(preview);Assert.That(dot==null,Is.True);Assert.That(wall.activeSelf,Is.True);
                }finally{UnityEngine.Object.DestroyImmediate(wall);}
            }
        }
        [Test] public void SaveConfirmationPersistsSelectedOffsetWithoutApprovingOrHidingAndIsIdempotent()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",.3f,-.2f);string model=File.ReadAllText((string)Adapter.GetProperty("CorrectionPath").GetValue(f.provider));
                string path=(string)f.Call("SaveDesktopAdjustment");string receipt=File.ReadAllText(path);
                Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.True);Assert.That(f.Approved,Is.False);Assert.That((bool)Adapter.GetProperty("DesktopPointerVisible").GetValue(f.provider),Is.False);
                Assert.That(receipt,Does.Contain(f.Model.id));Assert.That(receipt,Does.Contain("\"accuracy_approval_granted\": false"));Assert.That(receipt,Does.Contain("manual_yaw_pitch"));Assert.That((string)f.Call("SaveDesktopAdjustment"),Is.EqualTo(path));Assert.That(File.ReadAllText(path),Is.EqualTo(receipt));
                f.Call("SetDesktopPointerVisible",false);Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.True);
                f.Call("ApplyDesktopOffset",.4f,-.2f);Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(receipt));
                Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(path),"../"+f.Model.id+"/correction.json")),Is.True);Assert.That(model,Does.Contain("manual_yaw_pitch"));
            }
        }
        [Test] public void SaveConfirmationClearsOnResetRebindAndNewSessionWithoutDeletingReceipts()
        {
            using(var f=new Fixture())
            {
                string baseline=(string)f.Call("SaveDesktopAdjustment");Assert.That(File.ReadAllText(baseline),Does.Contain("official_ray_no_extra_correction"));Assert.That(f.Model,Is.Null);Assert.That(f.Approved,Is.False);
                f.Call("UseOfficialRayWithoutExtraCorrection");Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);string again=(string)f.Call("SaveDesktopAdjustment");Assert.That(again,Is.Not.EqualTo(baseline));
                f.Call("RebindDesktopHead",f.host.transform,"synthetic-mount");Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);f.Call("SaveDesktopAdjustment");
                StudyExportSession.Finish();Reset();StudyExportSession.EnsureStarted(f.root);Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);Assert.That(File.Exists(baseline),Is.True);
            }
        }
        [Test] public void SaveRefusesMissingModelOrInactiveRecordingRatherThanReportingSuccess()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",1f,0f);string path=(string)Adapter.GetProperty("CorrectionPath").GetValue(f.provider);string original=File.ReadAllText(path);
                File.WriteAllText(path,"{}");Assert.Throws<TargetInvocationException>(()=>f.Call("SaveDesktopAdjustment"));Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);File.WriteAllText(path,original);
                StudyExportSession.Finish();Assert.Throws<TargetInvocationException>(()=>f.Call("SaveDesktopAdjustment"));Assert.That((bool)Adapter.GetProperty("DesktopAdjustmentSaved").GetValue(f.provider),Is.False);
            }
        }
        [Test] public void RestoreManualRevisionRejectsMismatchedMetadataAndNeedsNewVerification()
        {
            using(var f=new Fixture())
            {
                f.Call("ApplyDesktopOffset",1f,2f);string path=(string)Adapter.GetProperty("CorrectionPath").GetValue(f.provider);string id=f.Model.id;
                f.Call("UseOfficialRayWithoutExtraCorrection");Assert.That(f.Model,Is.Null);Assert.That(File.Exists(path),Is.True);
                f.Call("RestoreCorrectionForSameSession",path);Assert.That(f.Model.id,Is.EqualTo(id));Assert.That(f.Approved,Is.False);Assert.Throws<TargetInvocationException>(()=>f.Call("ApproveCurrentMapping"));
                var corrupt=JsonUtility.FromJson<StudyGazeCorrection.Model>(File.ReadAllText(path));corrupt.horizontal_degrees=3;
                string invalid=Path.Combine(f.folder,"Calibration/synthetic-invalid.json");File.WriteAllText(invalid,JsonUtility.ToJson(corrupt));Assert.Throws<TargetInvocationException>(()=>f.Call("RestoreCorrectionForSameSession",invalid));
            }
        }
    }
}
