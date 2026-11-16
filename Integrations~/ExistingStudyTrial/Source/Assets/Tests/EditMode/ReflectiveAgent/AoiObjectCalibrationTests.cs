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
 public sealed class AoiObjectCalibrationTests
 {
  private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  private static void Set(object o,string name,object value){var t=o.GetType();(t.GetField(name,Flags)??t.GetField("<"+name+">k__BackingField",Flags)).SetValue(o,value);}
  private sealed class Fixture:IDisposable
  {
   public readonly GameObject host;public readonly GameObject[] gos;public readonly StudyResearchTarget[] targets;public readonly StudySpatialValidation check;public readonly StudyGazeRayProvider provider;public readonly Component flow;public readonly Type type;public readonly string root,folder;private readonly MethodInfo reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);
   public Fixture(){reset.Invoke(null,null);root=Path.GetFullPath("Library/ObjectCalibrationTests/"+Guid.NewGuid().ToString("N"));folder=StudyExportSession.EnsureStarted(root);host=new GameObject("Synthetic three-object workflow");
    var pt=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);provider=(StudyGazeRayProvider)host.AddComponent(pt);pt.GetField("headReference").SetValue(provider,host.transform);pt.GetField("calibrationId").SetValue(provider,"synthetic");check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;
    var recorder=host.AddComponent<StudyGazeTargetRecorder>();recorder.provider=provider;
    gos=new[]{new Vector3(-.6f,-.3f,2),new Vector3(.6f,-.3f,2),new Vector3(0,.6f,2)}.Select(p=>{var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.position=p;g.transform.localScale=Vector3.one*.3f;return g;}).ToArray();targets=gos.Select((g,i)=>{var t=g.AddComponent<StudyResearchTarget>();t.targetId="synthetic-role-"+i;t.displayLabel=new[]{"Chess","Card","Box"}[i];return t;}).ToArray();Physics.SyncTransforms();
    type=Type.GetType("NeonResearch.NeonXR.StudyObjectCalibrationWorkflow, GazeFlow.NeonXR",true);flow=host.AddComponent(type);type.GetField("provider").SetValue(flow,provider);type.GetField("objects").SetValue(flow,targets);
   }
   public string State=>type.GetProperty("State").GetValue(flow).ToString();public void Begin()=>type.GetMethod("Begin").Invoke(flow,null);public void Tick(double now)=>type.GetMethod("Tick").Invoke(flow,new object[]{now});
   public StudyGazeCorrection.Model Model=>(StudyGazeCorrection.Model)provider.GetType().GetProperty("CurrentCorrection").GetValue(provider);
   public void Fresh(){var points=(Vector3[])type.GetField("aimPoints",Flags).GetValue(flow);typeof(StudyGazeRayProvider).GetMethod("PublishValidation",Flags).Invoke(provider,new object[]{new StudyGazeRaySample{valid=true,calibrationId="synthetic",correctionId=Model?.id??"",worldRay=new Ray(Vector3.zero,points[0]),source="synthetic_only"}});Set(check,"lastFresh",Time.realtimeSinceStartupAsDouble);}
   public void InjectCollection()
   {
    Fresh();check.Abort();var points=(Vector3[])type.GetField("aimPoints",Flags).GetValue(flow);var q=Quaternion.Euler(2,-3,0);
    var r=new StudySpatialValidation.ValidationReport{id="synthetic-object-training",session_id=StudyExportSession.SessionId,calibration_id="synthetic",min_coverage=.7f,purpose="fit_collection_or_uncorrected_validation",targets=new List<StudySpatialValidation.TargetResult>()};
    for(int i=0;i<3;i++){var e=points[i].normalized;var a=Quaternion.Inverse(q)*e;r.targets.Add(new StudySpatialValidation.TargetResult{target_index=i,target_id=targets[i].targetId,target_name=targets[i].DisplayName,coverage=.95,local_direction_pairs=50,mean_observed_head_local_direction=new[]{a.x,a.y,a.z},mean_expected_head_local_direction=new[]{e.x,e.y,e.z}});}
    Save(r);Set(flow,"State",Enum.Parse(type.GetNestedType("Phase"),"Collecting"));
   }
   public void Save(StudySpatialValidation.ValidationReport report){var dir=Path.Combine(folder,"Calibration",report.id);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"validation.json"),JsonUtility.ToJson(report));File.WriteAllText(Path.Combine(dir,"validation_samples.csv"),"synthetic_only\n");Set(check,"LastReport",report);Set(check,"LastOutput",dir);}
   public void Dispose(){if(check.Running)check.Abort();UnityEngine.Object.DestroyImmediate(host);foreach(var g in gos)UnityEngine.Object.DestroyImmediate(g);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
  }
  [Test] public void ThreeCapturesFitOnceAndAutomaticReviewDoesNotApproveItself()
  {
   using(var f=new Fixture()){f.Begin();f.Fresh();f.type.GetMethod("CaptureCurrent").Invoke(f.flow,null);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.False);f.InjectCollection();f.Tick(0);Assert.That(f.State,Is.EqualTo("Countdown"));Assert.That(f.Model,Is.Not.Null);string id=f.Model.id;
    for(int i=0;i<=31;i++){f.Fresh();f.Tick(1+i*.1);}Assert.That(f.State,Is.EqualTo("Verifying"));Assert.That(f.check.AwaitingVisibilityConfirmation,Is.False);Assert.That(f.Model.id,Is.EqualTo(id));Assert.That((bool)f.provider.GetType().GetField("mappingVerified").GetValue(f.provider),Is.False);
    f.check.Abort();var report=f.check.LastReport;report.id="synthetic-object-independent";report.aborted=false;report.passed=false;report.correction_id=id;report.purpose="independent_live_verification";f.Save(report);Set(f.flow,"State",Enum.Parse(f.type.GetNestedType("Phase"),"Review"));Assert.Throws<TargetInvocationException>(()=>f.type.GetMethod("Finish").Invoke(f.flow,new object[]{true}));report.passed=true;f.Save(report);f.Fresh();f.type.GetMethod("Finish").Invoke(f.flow,new object[]{true});Assert.That(f.State,Is.EqualTo("Done"));Assert.That((bool)f.provider.GetType().GetField("mappingVerified").GetValue(f.provider),Is.True);
   }
  }
  [Test] public void MissingCoverageBlocksFittingAndExplicitSkipDoesNotApprove()
  {
   using(var f=new Fixture()){f.Begin();f.InjectCollection();f.check.LastReport.targets[1].coverage=.05;f.Tick(0);Assert.That(f.State,Is.EqualTo("Error"));Assert.That(f.Model,Is.Null);Assert.That((string)f.type.GetProperty("Message").GetValue(f.flow),Does.Contain("Card"));f.type.GetMethod("Finish").Invoke(f.flow,new object[]{false});Assert.That(f.State,Is.EqualTo("Done"));Assert.That((bool)f.provider.GetType().GetField("mappingVerified").GetValue(f.provider),Is.False);}
  }
  [Test] public void DuplicateBindingsAndObjectMovementCannotPass()
  {
   using(var f=new Fixture()){f.type.GetField("objects").SetValue(f.flow,new[]{f.targets[0],f.targets[0],f.targets[2]});Assert.Throws<TargetInvocationException>(()=>f.Begin());}
   using(var f=new Fixture()){f.Begin();f.gos[0].transform.position+=Vector3.right*.1f;f.Tick(0);Assert.That(f.State,Is.EqualTo("Error"));Assert.That(f.check.LastReport.aborted,Is.True);}
  }
  [Test] public void TutorialNarrationAndNextCanBeHeldWithoutFakingGrabObjectives()
  {
   var root=new GameObject("Synthetic tutorial hold");root.SetActive(false);
   try{var type=Type.GetType("HandTutorialNextGate, Assembly-CSharp",true);var gate=root.AddComponent(type);var hold=type.GetMethod("SetResearchCalibrationHold");hold.Invoke(gate,new object[]{true});Assert.That(type.GetProperty("ResearchCalibrationHeld").GetValue(gate),Is.EqualTo(true));Assert.That(type.GetProperty("CompletedObjectiveCount").GetValue(gate),Is.EqualTo(0));Assert.That(type.GetProperty("NextUnlocked").GetValue(gate),Is.EqualTo(false));hold.Invoke(gate,new object[]{false});Assert.That(type.GetProperty("CompletedObjectiveCount").GetValue(gate),Is.EqualTo(0));Assert.That(type.GetProperty("NextUnlocked").GetValue(gate),Is.EqualTo(false));}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public void RealTutorialBindingsResolveUniqueEnabledColliderOwnersWithoutSavingTheScene()
  {
   const string path="Assets/Scenes/Main Menu AR.unity";var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
   try{
    var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudyResearchTarget>(true)).ToArray();
    foreach(var id in new[]{"target-c005dcab8aa3582f25e5b3e0","target-89f98ce317eb165fd28ddae1","target-6d5b806d43164bf1f46f0b5f"})
    {var candidates=all.Where(t=>t.targetId==id&&t.enabled&&!t.requiresSpawnIdentity).ToArray();Assert.That(candidates.Length,Is.EqualTo(1),id+" must have one enabled scene owner.");var target=candidates[0];Assert.That(target.GetComponentsInChildren<Collider>(true).Any(c=>!c.isTrigger&&c.enabled&&c.GetComponentsInParent<StudyResearchTarget>(true).FirstOrDefault(t=>t.enabled)==target),Is.True,id+" must own an existing solid collider.");}
   }finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
