using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonResearch.Tests
{
    public sealed class GazeCorrectionTests
    {
        [Test] public void UnverifiedCandidatesAreExportedButNeverPromotedToAcceptedHits()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/CandidateCsvTests/"+Guid.NewGuid().ToString("N"));var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);cube.transform.position=new Vector3(1000,1000,1003);cube.AddComponent<StudyResearchTarget>().targetId="synthetic-candidate";Physics.SyncTransforms();
                var sample=new StudyGazeRaySample{valid=false,candidateRayAvailable=true,status="mapping_unverified",worldRay=new Ray(new Vector3(1000,1000,1000),Vector3.forward)};
                StudyGazeTargetLog.Record(sample,~0,10,out string id);Assert.That(id,Is.Empty);
                sample.candidateRayAvailable=false;sample.status="not_worn";StudyGazeTargetLog.Record(sample,~0,10,out _);
                StudyExportSession.Finish();var table=StudySequenceAnalysis.Rows(Path.Combine(folder,"gaze_targets.csv")).ToArray();
                Assert.That(table[1][Array.IndexOf(table[0],"target_id")],Is.Empty);
                Assert.That(table[1][Array.IndexOf(table[0],"ray_valid")].ToLowerInvariant(),Is.EqualTo("false"));
                Assert.That(table[1][Array.IndexOf(table[0],"candidate_target_id")],Is.EqualTo("synthetic-candidate"));
                Assert.That(table[1][Array.IndexOf(table[0],"candidate_quality")],Is.EqualTo("UNVERIFIED_CANDIDATE_NOT_ACCEPTED_AOI"));
                var approximate=StudySequenceAnalysis.Rows(Path.Combine(folder,"gaze_targets_approximate.csv")).ToArray();
                Assert.That(approximate.Length,Is.EqualTo(2));Assert.That(approximate[1][Array.IndexOf(approximate[0],"target_id")],Is.EqualTo("synthetic-candidate"));
                Assert.That(File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html")),Does.Contain("gazeApproximate"));
                Assert.That(table[2][Array.IndexOf(table[0],"candidate_target_id")],Is.Empty,"Missing/not-worn samples must not reuse a candidate ray.");
            }
            finally{UnityEngine.Object.DestroyImmediate(cube);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        private static Vector3[] Pattern()=>new[]{Vector3.forward,new Vector3(-.21f,0,1).normalized,new Vector3(.21f,0,1).normalized,new Vector3(0,.14f,1).normalized,new Vector3(0,-.14f,1).normalized};
        [Test] public void RecoversKnownHeadLocalRotationWithoutMovingOrigin()
        {
            var expected=Pattern();var rotation=Quaternion.Euler(3,-5,1);
            var observed=expected.Select(v=>Quaternion.Inverse(rotation)*v).ToArray();
            var fitted=StudyGazeCorrection.Fit(observed,expected);
            Assert.That(Quaternion.Angle(fitted,rotation),Is.LessThan(.05f));
            for(int i=0;i<expected.Length;i++)Assert.That(Vector3.Angle(fitted*observed[i],expected[i]),Is.LessThan(.05f));
        }
        [Test] public void IdentityAndHeadMotionKeepTheSameLocalCorrection()
        {
            var pattern=Pattern();Assert.That(Quaternion.Angle(StudyGazeCorrection.Fit(pattern,pattern),Quaternion.identity),Is.LessThan(.05f));
            var correction=Quaternion.Euler(2,-4,0);var head=Quaternion.Euler(11,125,3);var observed=Quaternion.Inverse(correction)*pattern[1];
            Assert.That(Vector3.Angle(head*(correction*observed),head*pattern[1]),Is.LessThan(.05f));
        }
        [Test] public void IncompleteDegenerateNonfiniteAndLargeCorrectionsAreRejected()
        {
            var p=Pattern();Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.Fit(p.Take(4).ToArray(),p.Take(4).ToArray()));
            Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.Fit(Enumerable.Repeat(Vector3.forward,5).ToArray(),p));
            var bad=Pattern();bad[1]=new Vector3(float.NaN,0,1);Assert.Throws<ArgumentException>(()=>StudyGazeCorrection.Fit(bad,p));
            Assert.Throws<InvalidOperationException>(()=>StudyGazeCorrection.Fit(p.Select(v=>Quaternion.Euler(0,25,0)*v).ToArray(),p));
        }
        [Test] public void CorrectedHitExportsRawRayCorrectionIdentityAndOcclusion()
        {
            var reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);reset.Invoke(null,null);
            string root=Path.GetFullPath("Library/CorrectionCsvTests/"+Guid.NewGuid().ToString("N"));
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);GameObject wall=null;
            try
            {
                string folder=StudyExportSession.EnsureStarted(root);cube.transform.position=new Vector3(1000,1000,1003);cube.transform.localScale=Vector3.one*.1f;cube.AddComponent<StudyResearchTarget>().targetId="corrected-target";
                Physics.SyncTransforms();var origin=new Vector3(1000,1000,1000);var raw=new Ray(origin,Quaternion.Euler(0,8,0)*Vector3.forward);
                var sample=new StudyGazeRaySample{valid=true,worldRay=raw,rawWorldRay=raw,hasRawWorldRay=true,source="synthetic_only",status="synthetic"};
                StudyGazeTargetLog.Record(sample,~0,10,out string before);Assert.That(before,Is.Empty);
                sample.worldRay=new Ray(origin,Vector3.forward);sample.correctionId="synthetic-correction";
                StudyGazeTargetLog.Record(sample,~0,10,out string after);Assert.That(after,Is.EqualTo("corrected-target"));
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1000,1000,1001.5f);Physics.SyncTransforms();
                StudyGazeTargetLog.Record(sample,~0,10,out string blocked);Assert.That(blocked,Is.Empty);
                StudyExportSession.Finish();string csv=File.ReadAllText(Path.Combine(folder,"gaze_targets.csv"));
                Assert.That(csv,Does.Contain("raw_ray_direction_x"));Assert.That(csv,Does.Contain("synthetic-correction"));Assert.That(csv,Does.Contain("corrected-target"));Assert.That(csv,Does.Contain("non_target_occluder"));
                Assert.That(File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html")),Does.Contain("synthetic-correction"));
            }
            finally{UnityEngine.Object.DestroyImmediate(cube);if(wall!=null)UnityEngine.Object.DestroyImmediate(wall);StudyExportSession.Finish();reset.Invoke(null,null);if(Directory.Exists(root))Directory.Delete(root,true);}
        }
    }
}
