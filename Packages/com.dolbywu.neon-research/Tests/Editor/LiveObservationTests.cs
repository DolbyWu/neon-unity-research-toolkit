using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonResearch.Tests
{
 public sealed class LiveObservationTests
 {
  [Test] public void ClickIntervalsNotesLimitAndInterruptedEndAreSavedPerSession()
  {
   var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);
   reset.Invoke(null,null);string root=Path.GetFullPath("Library/ObservationTests/"+Guid.NewGuid().ToString("N"));
   try{
    Assert.Throws<InvalidOperationException>(()=>StudyLiveObservations.Begin("R1"));
    string folder=StudyExportSession.EnsureStarted(root);
    var first=StudyLiveObservations.Begin("R1");Assert.That(StudyLiveObservations.Active,Is.SameAs(first));
    Assert.Throws<InvalidOperationException>(()=>StudyLiveObservations.Begin("R1"));
    StudyLiveObservations.End();double end=first.end_s.Value;StudyLiveObservations.SaveNote(first.observation_id,"Possible confusion","Looked between controls.");
    Assert.That(first.end_s,Is.EqualTo(end));Assert.That(File.ReadAllText(Path.Combine(folder,"live_observations.json")),Does.Contain("Looked between controls."));
    for(int i=1;i<9;i++){StudyLiveObservations.Begin("R1");StudyLiveObservations.End();}
    var last=StudyLiveObservations.Begin("R1");StudyExportSession.Finish();Assert.That(last.status,Is.EqualTo("interrupted"));
    Assert.That(StudyLiveObservations.Records.Count,Is.EqualTo(10));
    string raw=File.ReadAllText(Path.Combine(folder,"live_observations.csv"));
    reset.Invoke(null,null);StudyExportSession.EnsureStarted(root);Assert.That(StudyLiveObservations.Records.Count,Is.Zero);
    for(int i=0;i<10;i++){StudyLiveObservations.Begin("R2");StudyLiveObservations.End();}
    Assert.Throws<InvalidOperationException>(()=>StudyLiveObservations.Begin("R2"));
    Assert.That(File.ReadAllText(Path.Combine(folder,"live_observations.csv")),Is.EqualTo(raw));
   } finally {StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
  }
  [Test] public void SecondRoundUsesSameWorldTargetsDespiteHeadMovement()
  {
   var go=new GameObject("Synthetic only");
   try{
    var provider=go.AddComponent<SyntheticValidationRayProvider>();var check=go.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=go.transform;
    Assert.That(check.maximumMeanErrorDegrees,Is.EqualTo(3));check.Begin("synthetic");
    var sample=new StudyGazeRaySample{valid=true,calibrationId="synthetic",worldRay=new Ray(Vector3.zero,Vector3.forward)};provider.Emit(sample);
    var field=typeof(StudySpatialValidation).GetField("targets",BindingFlags.NonPublic|BindingFlags.Instance);var original=(Vector3[])((Vector3[])field.GetValue(check)).Clone();check.Abort();
    go.transform.position=new Vector3(1,2,3);go.transform.rotation=Quaternion.Euler(10,40,0);check.independentCheckPattern=true;check.Begin("synthetic");provider.Emit(sample);
    Assert.That((Vector3[])field.GetValue(check),Is.EqualTo(original));check.Abort();
    Assert.That(StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new[]{3f},2,2,3,.7).passed,Is.True);
    Assert.That(StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new[]{3.01f},2,2,3,.7).passed,Is.False);
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
 }
}
