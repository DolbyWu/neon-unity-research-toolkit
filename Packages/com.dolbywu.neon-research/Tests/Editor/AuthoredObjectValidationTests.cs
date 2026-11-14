using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonResearch.Tests
{
 public sealed class AuthoredObjectValidationTests
 {
  private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
  [Test] public void NarrowTutorialPatternCanFitYawPitchWithoutEstimatingRollOrRelaxingSafety()
  {
   var expected=new[]{new Vector3(.097f,-.052f,.994f).normalized,new Vector3(-.047f,-.019f,.999f).normalized,new Vector3(-.007f,.003f,1).normalized};
   var known=Quaternion.Euler(2,-3,0);var observed=expected.Select(v=>Quaternion.Inverse(known)*v).ToArray();
   Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.FitThreeTargets(observed,expected));
   var fit=StudyGazeCorrection.FitThreeTargetYawPitch(observed,expected);Assert.That(Quaternion.Angle(fit,known),Is.LessThan(.05));
   Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.FitThreeTargetYawPitch(Enumerable.Repeat(Vector3.forward,3).ToArray(),expected));
   Assert.Throws<InvalidOperationException>(()=>StudyGazeCorrection.FitThreeTargetYawPitch(expected.Select(v=>Quaternion.Inverse(Quaternion.Euler(0,20,0))*v).ToArray(),expected));
   var bad=(Vector3[])observed.Clone();bad[0]=new Vector3(float.NaN,0,1);Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.FitThreeTargetYawPitch(bad,expected));
  }
  [UnityEngine.TestTools.UnityTest] public System.Collections.IEnumerator OrphanedGeneratedGroupRemovesOnlyItsOwnedVisualResources()
  {
   var ownerGo=new GameObject("Synthetic cue owner");var unrelated=GameObject.CreatePrimitive(PrimitiveType.Cube);var root=new GameObject("Synthetic owned cue group");var material=new Material(Shader.Find("Unlit/Color"));
   try{
    var group=root.AddComponent<StudyCalibrationCueGroup>();group.owner=ownerGo.AddComponent<StudySpatialValidation>();group.Own(material);var cue=GameObject.CreatePrimitive(PrimitiveType.Sphere);cue.transform.SetParent(root.transform);UnityEngine.Object.DestroyImmediate(ownerGo);UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;UnityEditor.EditorApplication.QueuePlayerLoopUpdate();yield return null;
    Assert.That(root==null,Is.True);Assert.That(material==null,Is.True);Assert.That(unrelated!=null&&unrelated.GetComponent<Collider>().enabled,Is.True);
   }finally{if(ownerGo!=null)UnityEngine.Object.DestroyImmediate(ownerGo);if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(material!=null)UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(unrelated);}
  }
  [Test] public void ThreeTargetFittingIsExplicitAndRetainsBoundedNondegeneratePolicy()
  {
   var expected=new[]{new Vector3(-.3f,-.2f,1).normalized,new Vector3(.3f,-.2f,1).normalized,new Vector3(0,.3f,1).normalized};var q=Quaternion.Euler(2,-4,0);var observed=expected.Select(v=>Quaternion.Inverse(q)*v).ToArray();
   Assert.That(Quaternion.Angle(StudyGazeCorrection.FitThreeTargets(observed,expected),q),Is.LessThan(.05));
   Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.Fit(observed,expected));
   Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.FitThreeTargets(new[]{Vector3.forward,Vector3.forward,Vector3.forward},expected));
   Assert.Throws<InvalidOperationException>(()=>StudyGazeCorrection.FitThreeTargets(expected.Select(v=>Quaternion.Inverse(Quaternion.Euler(0,20,0))*v).ToArray(),expected));
  }
  [Test] public void YellowDotBaselineUses32mmAndPulsesWithoutMovingTheReferenceOrAddingCollisions()
  {
   var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);reset.Invoke(null,null);string root=Path.GetFullPath("Library/AuthoredObjectTests/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic yellow dots");
   try{StudyExportSession.EnsureStarted(root);var provider=host.AddComponent<SyntheticValidationRayProvider>();var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;check.markerDiameter=.032f;check.bananaMarkers=false;check.singleBaselineBeforeObjects=true;check.Begin("synthetic");provider.Emit(new StudyGazeRaySample{valid=true,calibrationId="synthetic",worldRay=new Ray(Vector3.zero,Vector3.forward)});
    var markers=(GameObject[])typeof(StudySpatialValidation).GetField("markers",Flags).GetValue(check);Assert.That(markers.Length,Is.EqualTo(5));Assert.That(markers.All(m=>m.transform.childCount==0&&m.transform.localScale.x==.032f&&!m.GetComponent<Collider>().enabled),Is.True);Assert.That(markers[0].GetComponent<Renderer>().sharedMaterial.color,Is.EqualTo(Color.yellow));var group=markers[0].transform.parent;Assert.That(group.GetComponent<StudyCalibrationCueGroup>().owner,Is.SameAs(check));Assert.That(markers.All(m=>m.transform.parent==group),Is.True);Assert.That(group.childCount,Is.EqualTo(5));Assert.That((group.gameObject.hideFlags&HideFlags.DontSaveInEditor)!=0,Is.True);var position=markers[0].transform.position;check.StartAutomaticSequence(0);typeof(StudySpatialValidation).GetMethod("Update",Flags).Invoke(check,null);Assert.That(markers[0].transform.position,Is.EqualTo(position));Assert.That(markers[0].transform.localScale.x,Is.InRange(.032f*.75f,.032f*1.25f));host.transform.position=Vector3.one*10;host.transform.rotation=Quaternion.Euler(0,90,0);Assert.That(markers[0].transform.position,Is.EqualTo(position));check.Abort();Assert.That(group==null,Is.True);
   }finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
  }
  [Test] public void BananaMeshAndCueScaleDoNotMoveTheKnownTargetOrAddOcclusion()
  {
   var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);reset.Invoke(null,null);string root=Path.GetFullPath("Library/AuthoredObjectTests/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic banana cue");
   try{
    StudyExportSession.EnsureStarted(root);var provider=host.AddComponent<SyntheticValidationRayProvider>();var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;check.bananaMarkers=true;check.markerDiameter=.07f;check.Begin("synthetic");provider.Emit(new StudyGazeRaySample{valid=true,calibrationId="synthetic",worldRay=new Ray(Vector3.zero,Vector3.forward)});
    var markers=(GameObject[])typeof(StudySpatialValidation).GetField("markers",Flags).GetValue(check);Assert.That(markers.Length,Is.EqualTo(5));Assert.That(markers[0].transform.position,Is.EqualTo(new Vector3(0,0,2)));Assert.That(markers[0].transform.localScale.x,Is.EqualTo(.07f));
    var mesh=markers[0].GetComponent<MeshFilter>().sharedMesh;Assert.That(mesh.vertexCount,Is.EqualTo(190));Assert.That(mesh.subMeshCount,Is.EqualTo(3));Assert.That(markers[0].GetComponent<Renderer>().sharedMaterials.Length,Is.EqualTo(3));Assert.That(mesh.vertices.All(StudyGazeTargetLog.Finite),Is.True);Assert.That(mesh.GetTriangles(1),Is.Not.Empty);Assert.That(mesh.GetTriangles(2),Is.Not.Empty);Assert.That(markers.SelectMany(m=>m.GetComponentsInChildren<Collider>()).All(c=>!c.enabled),Is.True);check.Abort();Assert.That(markers.All(m=>m==null),Is.True);
   }finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
  }
  [Test] public void CorrectAngularDirectionCannotPassIfAnotherObjectOccludesTheExpectedTarget()
  {
   var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);reset.Invoke(null,null);string root=Path.GetFullPath("Library/AuthoredObjectTests/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic object checker");var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
   var gos=new[]{new Vector3(0,0,2),new Vector3(-.5f,0,2),new Vector3(.4f,.5f,2)}.Select(p=>{var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.position=p;g.transform.localScale=Vector3.one*.2f;return g;}).ToArray();
   try{
    string folder=StudyExportSession.EnsureStarted(root);var objects=gos.Select((g,i)=>{var t=g.AddComponent<StudyResearchTarget>();t.targetId="synthetic-object-"+i;t.targetGroup="synthetic";return t;}).ToArray();
    var provider=host.AddComponent<SyntheticValidationRayProvider>();var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;check.settleSeconds=0;
    var points=gos.Select(g=>g.transform.position-new Vector3(0,0,.1f)).ToArray();blocker.transform.position=new Vector3(0,0,1);blocker.transform.localScale=Vector3.one*.3f;Physics.SyncTransforms();
    var sample=new StudyGazeRaySample{valid=true,calibrationId="synthetic",correctionId="synthetic-correction",worldRay=new Ray(Vector3.zero,Vector3.forward)};
    check.BeginObjectTargets("synthetic",objects,points,false);provider.Emit(sample);var markers=(GameObject[])typeof(StudySpatialValidation).GetField("markers",Flags).GetValue(check);Assert.That(check.objectCueDiameter,Is.EqualTo(4*.008f));Assert.That(markers[0].transform.localScale.x,Is.EqualTo(.032f));Assert.That(markers[0].transform.position,Is.EqualTo(points[0]));check.Abort();check.BeginObjectTargets("synthetic",objects,points,true);provider.Emit(sample);check.StartAutomaticVerificationSequence(0);provider.Emit(sample);
    typeof(StudySpatialValidation).GetField("covered",Flags).SetValue(check,2d);var angles=(System.Collections.Generic.List<float>)typeof(StudySpatialValidation).GetField("angles",Flags).GetValue(check);angles.Add(0);
    double past=Time.realtimeSinceStartupAsDouble-10;typeof(StudySpatialValidation).GetField("targetStarted",Flags).SetValue(check,past);var sequence=(StudyValidationSequence)typeof(StudySpatialValidation).GetField("sequence",Flags).GetValue(check);typeof(StudyValidationSequence).GetField("<TargetStartedAt>k__BackingField",Flags).SetValue(sequence,past);
    typeof(StudySpatialValidation).GetMethod("Update",Flags).Invoke(check,null);check.Abort();var result=check.LastReport.targets[0];Assert.That(result.mean_error_deg,Is.EqualTo(0));Assert.That(result.coverage,Is.EqualTo(1));Assert.That(result.correct_target_fraction,Is.EqualTo(0));Assert.That(result.passed,Is.False);Assert.That(result.target_id,Is.EqualTo(objects[0].targetId));
    string csv=File.ReadAllText(Path.Combine(check.LastOutput,"validation_samples.csv"));Assert.That(csv,Does.Contain("expected_target_id"));Assert.That(csv,Does.Contain("non_target_occluder"));Assert.That(csv,Does.Contain("query_collider_path"));Assert.That(check.LastObjectQuery,Does.Contain(blocker.name));
   }finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(blocker);foreach(var g in gos)UnityEngine.Object.DestroyImmediate(g);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
  }
  [Test] public void ReadOnlyQueryRejectsFrontOcclusionWithoutWritingOrChangingPhysics()
  {
   var targetGo=GameObject.CreatePrimitive(PrimitiveType.Cube);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
   try{targetGo.transform.position=new Vector3(0,0,2);var target=targetGo.AddComponent<StudyResearchTarget>();target.targetId="synthetic-query-target";wall.transform.position=new Vector3(0,0,1);Physics.SyncTransforms();var hit=StudyGazeTargetLog.Query(new Ray(Vector3.zero,Vector3.forward),~0,10);Assert.That(hit.status,Is.EqualTo("non_target_occluder"));Assert.That(hit.target,Is.Null);wall.SetActive(false);Physics.SyncTransforms();Assert.That(StudyGazeTargetLog.Query(new Ray(Vector3.zero,Vector3.forward),~0,10).target,Is.SameAs(target));Assert.That(targetGo.GetComponent<Collider>().enabled,Is.True);}finally{UnityEngine.Object.DestroyImmediate(targetGo);UnityEngine.Object.DestroyImmediate(wall);}
  }
 }
}
