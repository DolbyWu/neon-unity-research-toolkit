using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonResearch.Tests
{
    public sealed class SyntheticValidationRayProvider : StudyGazeRayProvider
    {
        public void Emit(StudyGazeRaySample sample)=>PublishValidation(sample);
    }
    public sealed class AutomaticValidationRuntimeTests
    {
        [Test] public void RestoredEvidenceNeedsFreshSignalAndCannotCrossSession()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);
            reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/AutomaticValidationTests/"+Guid.NewGuid().ToString("N"));
            var host=new GameObject("Synthetic restored evidence only");
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);
                string dir=Path.Combine(folder,"Calibration/synthetic");Directory.CreateDirectory(dir);
                var report=new StudySpatialValidation.ValidationReport{id="synthetic",calibration_id="synthetic-calibration",session_id=StudyExportSession.SessionId,passed=true};
                File.WriteAllText(Path.Combine(dir,"validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));
                var provider=host.AddComponent<SyntheticValidationRayProvider>();
                var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;
                check.RestoreSameSessionReportForReview(dir);
                Assert.That(check.RestoredForReviewOnly,Is.True);Assert.That(check.Running,Is.False);Assert.That(check.HasRecentValidSignal,Is.False);
                provider.Emit(new StudyGazeRaySample{valid=true,calibrationId="synthetic-calibration",worldRay=new Ray(Vector3.zero,Vector3.forward)});
                Assert.That(check.HasRecentValidSignal,Is.True,"Freshness monitoring must keep working after measurement, including restored evidence.");
                provider.Emit(new StudyGazeRaySample{status="not_worn"});Assert.That(check.HasRecentValidSignal,Is.False);
                report.session_id="different-session";File.WriteAllText(Path.Combine(dir,"validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));
                Assert.Throws<InvalidDataException>(()=>check.RestoreSameSessionReportForReview(dir));
            }
            finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }

        [Test] public void RemovingValidationMarkersDoesNotRemoveGameCollidersOrEraseAoiReplayRows()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/AutomaticValidationTests/"+Guid.NewGuid().ToString("N"));
            var host=new GameObject("Synthetic validation cleanup only");var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);
                cube.name="Synthetic persistent game target";cube.transform.position=new Vector3(1000,1000,1003);
                cube.AddComponent<StudyResearchTarget>().targetId="persistent-game-target";Physics.SyncTransforms();
                var sample=new StudyGazeRaySample{valid=true,worldRay=new Ray(new Vector3(1000,1000,1000),Vector3.forward),source="synthetic_only"};
                StudyGazeTargetLog.Record(sample,~0,10,out string before);Assert.That(before,Is.EqualTo("persistent-game-target"));
                var provider=host.AddComponent<SyntheticValidationRayProvider>();var check=host.AddComponent<StudySpatialValidation>();
                check.provider=provider;check.headReference=host.transform;check.Begin("synthetic-calibration");
                provider.Emit(new StudyGazeRaySample{valid=true,calibrationId="synthetic-calibration",worldRay=new Ray(Vector3.zero,Vector3.forward)});
                check.Abort();
                Assert.That(cube.GetComponent<Collider>().enabled,Is.True);
                StudyGazeTargetLog.Record(sample,~0,10,out string after);Assert.That(after,Is.EqualTo(before));
                StudyExportSession.Finish();
                string csv=File.ReadAllText(Path.Combine(folder,"gaze_targets.csv"));
                Assert.That(csv.Split('\n').Skip(1).Count(line=>line.Contains("target_hit")),Is.EqualTo(2));
                string html=File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html"));
                Assert.That(html,Does.Contain("persistent-game-target"));Assert.That(html,Does.Contain("gazeTargets"));
            }
            finally{UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(cube);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }

        [Test] public void AutomaticVerificationRecordsAutomaticStartWithoutClaimingOperatorVisibility()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/AutomaticValidationTests/"+Guid.NewGuid().ToString("N"));var host=new GameObject("Synthetic automatic round-two provenance");
            try
            {
                StudyExportSession.EnsureStarted(root);var provider=host.AddComponent<SyntheticValidationRayProvider>();
                var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;
                var sample=new StudyGazeRaySample{valid=true,calibrationId="synthetic",source="synthetic_only",worldRay=new Ray(Vector3.zero,Vector3.forward)};
                check.Begin("synthetic");provider.Emit(sample);
                Assert.Throws<InvalidOperationException>(()=>check.StartAutomaticVerificationSequence(0));check.Abort();
                check.independentCheckPattern=true;check.Begin("synthetic");sample.correctionId="synthetic-correction";provider.Emit(sample);
                check.StartAutomaticVerificationSequence(0);
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;double past=Time.realtimeSinceStartupAsDouble-10;
                typeof(StudySpatialValidation).GetField("targetStarted",flags).SetValue(check,past);
                var sequence=(StudyValidationSequence)typeof(StudySpatialValidation).GetField("sequence",flags).GetValue(check);
                typeof(StudyValidationSequence).GetField("<TargetStartedAt>k__BackingField",flags).SetValue(sequence,past);
                typeof(StudySpatialValidation).GetMethod("Update",flags).Invoke(check,null);check.Abort();
                var result=check.LastReport.targets[0];Assert.That(result.start_method,Is.EqualTo("automatic_verification_after_countdown"));
                Assert.That(result.operator_confirmed_visible,Is.False);Assert.That(result.mean_error_deg,Is.Null);Assert.That(result.passed,Is.False);
                Assert.That(File.ReadAllText(Path.Combine(check.LastOutput,"validation.json")),Does.Contain("automatic_verification_after_countdown"));
            }
            finally{UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        [Test] public void ActualFiveMarkersAdvanceAutomaticallyAndGapsCannotBecomeSamples()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static);
            reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/AutomaticValidationTests/"+Guid.NewGuid().ToString("N"));
            var host=new GameObject("Synthetic validation only");
            try
            {
                StudyExportSession.EnsureStarted(root);
                var provider=host.AddComponent<SyntheticValidationRayProvider>();
                var check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;
                check.settleSeconds=1;check.measureSeconds=2;check.interTargetSeconds=2;
                check.Begin("synthetic-calibration");Assert.That(check.WaitingForSignal,Is.True);
                var sample=new StudyGazeRaySample{valid=true,calibrationId="synthetic-calibration",source="synthetic_not_participant_data",worldRay=new Ray(Vector3.zero,Vector3.forward)};
                provider.Emit(sample);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                var markers=(GameObject[])typeof(StudySpatialValidation).GetField("markers",flags).GetValue(check);
                Assert.That(markers.Length,Is.EqualTo(5));Assert.That(markers.All(m=>m.activeSelf),Is.True);
                Assert.That(markers.All(m=>!m.GetComponent<Collider>().enabled),Is.True);
                Assert.That(check.AwaitingVisibilityConfirmation,Is.True);
                var positions=markers.Select(m=>m.transform.position).ToArray();
                provider.Emit(sample);check.StartAutomaticSequence(2);
                var update=typeof(StudySpatialValidation).GetMethod("Update",flags);
                var targetStarted=typeof(StudySpatialValidation).GetField("targetStarted",flags);
                var sequenceField=typeof(StudySpatialValidation).GetField("sequence",flags);
                var sampleRows=typeof(StudySpatialValidation).GetField("rows",flags);
                for(int ordinal=0;ordinal<5;ordinal++)
                {
                    int current=check.TargetIndex;update.Invoke(check,null);
                    Assert.That(markers[current].transform.position,Is.EqualTo(positions[current]),"Pulsing must not move the error target centre.");
                    var sequence=(StudyValidationSequence)sequenceField.GetValue(check);
                    double past=Time.realtimeSinceStartupAsDouble-10;
                    targetStarted.SetValue(check,past);
                    typeof(StudyValidationSequence).GetField("<TargetStartedAt>k__BackingField",flags).SetValue(sequence,past);
                    update.Invoke(check,null);
                    Assert.That(check.CompletedTargets,Is.EqualTo(ordinal+1));
                    if(ordinal<4)
                    {
                        Assert.That(markers[current].activeSelf,Is.False);
                        Assert.That(check.WaitingBetweenTargets,Is.True);
                        int before=((System.Collections.ICollection)sampleRows.GetValue(check)).Count;
                        provider.Emit(sample);
                        Assert.That(((System.Collections.ICollection)sampleRows.GetValue(check)).Count,Is.EqualTo(before),"The gap is not a measurement interval.");
                        typeof(StudyValidationSequence).GetField("nextAt",flags).SetValue(sequence,Time.realtimeSinceStartupAsDouble-1);
                        update.Invoke(check,null);
                        Assert.That(check.AwaitingVisibilityConfirmation,Is.False,"Later points must not require another operator click.");
                    }
                }
                Assert.That(check.Running,Is.False);Assert.That(check.LastReport.targets.Count,Is.EqualTo(5));
                Assert.That(check.LastReport.target_order,Is.EqualTo(new[]{2,0,1,3,4}));
                Assert.That(check.LastReport.targets.Skip(1).All(t=>!t.operator_confirmed_visible),Is.True);
                Assert.That(check.LastReport.targets.All(t=>t.mean_error_deg==null&&t.coverage==0),Is.True);
                Assert.That(check.LastReport.passed,Is.False,"An automatic run with no measurement samples must never pass.");
                Assert.That(File.Exists(Path.Combine(check.LastOutput,"validation.json")),Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);StudyExportSession.Finish();reset.Invoke(null,null);
                if(Directory.Exists(root))Directory.Delete(root,true);
            }
        }
    }
}
