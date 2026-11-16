using NUnit.Framework;
using Newtonsoft.Json;
using UnityEngine;
namespace NeonResearch.Tests
{
    public sealed class SpatialValidationTests
    {
        [Test] public void AutomaticSequenceShowsNoProgressBeforeFirstSelectionAndMeasuresAllFiveOnce()
        {
            var sequence=new StudyValidationSequence(5,3,2);
            Assert.That(sequence.Started,Is.False);Assert.That(sequence.CurrentTarget,Is.EqualTo(-1));Assert.That(sequence.TargetDue(999),Is.False);
            sequence.Start(2,10);Assert.That(sequence.Order,Is.EqualTo(new[]{2,0,1,3,4}));
            double started=10;
            for(int i=0;i<5;i++)
            {
                Assert.That(sequence.CurrentTarget,Is.EqualTo(sequence.Order[i]));
                Assert.That(sequence.TargetDue(started+2.99),Is.False);
                Assert.That(sequence.TargetDue(started+3),Is.True);
                sequence.FinishTarget(started+3);
                Assert.That(sequence.CompletedCount,Is.EqualTo(i+1));
                if(i<4)
                {
                    Assert.That(sequence.WaitingGap,Is.True);Assert.That(sequence.TargetDue(started+100),Is.False);
                    Assert.That(sequence.TryStartNext(started+4.999),Is.False);
                    Assert.That(sequence.TryStartNext(started+5),Is.True);started+=5;
                }
            }
            Assert.That(sequence.Finished,Is.True);Assert.That(sequence.TryStartNext(999),Is.False);
        }
        [Test] public void AutomaticSequenceRejectsInvalidInputsAndCannotSkipTargetsOrRestart()
        {
            Assert.Throws<System.ArgumentException>(()=>new StudyValidationSequence(5,double.NaN,2));
            Assert.Throws<System.ArgumentException>(()=>new StudyValidationSequence(5,3,-2));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>StudyValidationSequence.BuildOrder(5,5));
            var sequence=new StudyValidationSequence(5,3,2);sequence.Start(0,0);
            Assert.Throws<System.InvalidOperationException>(()=>sequence.Start(1,1));
            Assert.Throws<System.InvalidOperationException>(()=>sequence.FinishTarget(2.9));
            sequence.FinishTarget(3);Assert.That(sequence.TryStartNext(20),Is.True);
            Assert.That(sequence.TargetStartedAt,Is.EqualTo(20));Assert.That(sequence.TargetDue(20),Is.False,"Late frames must not instantly skip a newly presented target.");
            var copy=sequence.Order;copy[0]=99;Assert.That(sequence.Order[0],Is.EqualTo(0));
        }
        [Test] public void KnownRayErrorsAreAngularAndInvalidRaysRemainMissing()
        {
            Assert.That(StudySpatialValidation.AngularError(new Ray(Vector3.zero,Vector3.forward),new Vector3(0,0,2)),Is.EqualTo(0).Within(.0001));
            Assert.That(StudySpatialValidation.AngularError(new Ray(Vector3.zero,Vector3.forward),new Vector3(2,0,2)),Is.EqualTo(45).Within(.001));
            Assert.That(StudySpatialValidation.AngularError(new Ray(Vector3.zero,Vector3.zero),Vector3.forward),Is.Null);
            Assert.That(StudySpatialValidation.AngularError(new Ray(Vector3.zero,new Vector3(float.NaN,0,1)),Vector3.forward),Is.Null);
        }
        [Test] public void NoDataAndLowCoverageCannotPassAsZeroError()
        {
            var empty=StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new float[0],0,2,2,.7);
            Assert.That(empty.mean_error_deg,Is.Null);Assert.That(empty.passed,Is.False);
            var gap=StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new[]{0f,0f},.1,2,2,.7);
            Assert.That(gap.passed,Is.False);
        }
        [Test] public void ThresholdPolicyIsExplicitAndReportSerializesWithoutUnityVectorRecursion()
        {
            var good=StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new[]{1f,1.5f},1.8,2,2,.7);
            Assert.That(good.passed,Is.True); Assert.That(good.coverage,Is.EqualTo(.9).Within(1e-8));
            var shifted=StudySpatialValidation.EvaluateTarget(0,Vector3.forward,new[]{5f,6f},1.8,2,2,.7);
            Assert.That(shifted.passed,Is.False);
            Assert.That(JsonConvert.SerializeObject(shifted),Does.Contain("world_target"));
        }
    }
}
