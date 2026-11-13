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
    /// <summary>Synthetic saved reports only. No Neon connection, headset, microphone or desktop capture.</summary>
    public sealed class AoiAutomaticTransitionTests
    {
        private const BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
        private static void Set(object obj,string name,object value)
        {
            var type=obj.GetType();
            var field=type.GetField(name,PrivateInstance)??type.GetField("<"+name+">k__BackingField",PrivateInstance);
            if(field==null)throw new MissingFieldException(type.FullName,name);
            field.SetValue(obj,value);
        }
        private sealed class Fixture:IDisposable
        {
            public readonly string root,folder;
            public readonly GameObject host,otherHead;
            public readonly Type providerType;
            public readonly StudyGazeRayProvider provider;
            public readonly StudySpatialValidation check;
            public readonly StudySpatialValidation.ValidationReport report;
            private readonly MethodInfo reset=typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic);
            public Fixture()
            {
                reset.Invoke(null,null);
                root=Path.GetFullPath("Library/AutomaticTransitionTests/"+Guid.NewGuid().ToString("N"));
                folder=StudyExportSession.EnsureStarted(root);
                host=new GameObject("Synthetic automatic two-round test");otherHead=new GameObject("Synthetic replacement head");
                providerType=Type.GetType("NeonResearch.NeonXR.NeonXRResearchProvider, GazeFlow.NeonXR",true);
                provider=(StudyGazeRayProvider)host.AddComponent(providerType);
                providerType.GetField("headReference").SetValue(provider,host.transform);
                providerType.GetField("calibrationId").SetValue(provider,"synthetic-config");
                providerType.GetField("automaticTwoRoundFlow").SetValue(provider,true);
                providerType.GetField("verificationCountdownSeconds").SetValue(provider,3f);
                check=host.AddComponent<StudySpatialValidation>();check.provider=provider;check.headReference=host.transform;check.automaticSequence=true;
                var expected=new[]{Vector3.forward,new Vector3(-.21f,0,1).normalized,new Vector3(.21f,0,1).normalized,new Vector3(0,.14f,1).normalized,new Vector3(0,-.14f,1).normalized};
                var q=Quaternion.Euler(2,-4,0);
                report=new StudySpatialValidation.ValidationReport{id="synthetic-complete-round-one",purpose="fit_collection_or_uncorrected_validation",session_id=StudyExportSession.SessionId,calibration_id="synthetic-config",min_coverage=.7f,max_mean_error_deg=3,passed=false,target_geometry_id="synthetic-fixed-geometry",targets=new List<StudySpatialValidation.TargetResult>()};
                for(int i=0;i<expected.Length;i++)
                {
                    var raw=Quaternion.Inverse(q)*expected[i];
                    report.targets.Add(new StudySpatialValidation.TargetResult{target_index=i,coverage=.95,local_direction_pairs=50,mean_error_deg=8,mean_observed_head_local_direction=new[]{raw.x,raw.y,raw.z},mean_expected_head_local_direction=new[]{expected[i].x,expected[i].y,expected[i].z}});
                }
                Set(check,"LastReport",report);Set(check,"session",StudyExportSession.SessionId);Set(check,"calibration","synthetic-config");
                Set(check,"referenceTargets",expected.Select(v=>v*2).ToArray());Set(check,"referenceHead",host.transform);
                Set(check,"referenceSession",StudyExportSession.SessionId);Set(check,"referenceCalibration","synthetic-config");Set(check,"referenceGeometryId",report.target_geometry_id);
                SaveReport();
            }
            public void SaveReport()
            {
                string dir=Path.Combine(folder,"Calibration",report.id);Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir,"validation.json"),JsonUtility.ToJson(report));
                File.WriteAllText(Path.Combine(dir,"validation_samples.csv"),"synthetic_only\n");Set(check,"LastOutput",dir);
            }
            public void Tick(double now)=>providerType.GetMethod("TickAutomaticValidation").Invoke(provider,new object[]{now});
            public void Cancel()=>providerType.GetMethod("CancelAutomaticValidationTransition").Invoke(provider,null);
            public bool Pending=>(bool)providerType.GetProperty("AutomaticVerificationPending").GetValue(provider);
            public string Status=>(string)providerType.GetProperty("AutomaticValidationStatus").GetValue(provider);
            public StudyGazeCorrection.Model Model=>(StudyGazeCorrection.Model)providerType.GetProperty("CurrentCorrection").GetValue(provider);
            public bool Approved=>(bool)providerType.GetField("mappingVerified").GetValue(provider);
            public void Fresh()
            {
                typeof(StudyGazeRayProvider).GetMethod("PublishValidation",PrivateInstance).Invoke(provider,new object[]{new StudyGazeRaySample{valid=true,calibrationId="synthetic-config",correctionId=Model?.id??"",worldRay=new Ray(Vector3.zero,Vector3.forward)}});
                Set(check,"lastFresh",Time.realtimeSinceStartupAsDouble);
            }
            public void TickFreshSpan(double start,double end)
            {
                for(int step=0;start+step*.1<=end+1e-7;step++){Fresh();Tick(start+step*.1);}
            }
            public void StartAnotherSession(){StudyExportSession.Finish();reset.Invoke(null,null);StudyExportSession.EnsureStarted(root);}
            public void Dispose()
            {
                // This test deliberately changes session identity mid-preview. Prevent the
                // synthetic stale preview from falling back to the configured real output root.
                if(check!=null&&(string)typeof(StudySpatialValidation).GetField("session",PrivateInstance).GetValue(check)!=StudyExportSession.SessionId)Set(check,"Running",false);
                UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(otherHead);
                StudyExportSession.Finish();reset.Invoke(null,null);
                if(Directory.Exists(root))Directory.Delete(root,true);
            }
        }

        [Test] public void LowCoverageNamesBlockedPointsWithoutCreatingCorrection()
        {
            using(var f=new Fixture())
            {
                f.report.targets[1].coverage=.319;f.report.targets[2].coverage=.055;f.report.targets[2].local_direction_pairs=3;f.SaveReport();
                f.Tick(10);Assert.That(f.Model,Is.Null);Assert.That(f.Pending,Is.False);Assert.That(f.check.Running,Is.False);
                Assert.That(f.Status.ToLowerInvariant(),Does.Contain("point 2"));Assert.That(f.Status.ToLowerInvariant(),Does.Contain("point 3"));
                Assert.That(Directory.GetFiles(Path.Combine(f.folder,"Calibration"),"correction.json",SearchOption.AllDirectories),Is.Empty);
            }
        }
        [Test] public void CompleteHighErrorCollectionFitsOnceAndStartsNewRoundAfterFreshCountdown()
        {
            using(var f=new Fixture())
            {
                f.Tick(10);Assert.That(f.Model,Is.Not.Null,"High pre-correction error does not forbid fitting when the sample evidence is sufficient.");
                string modelId=f.Model.id;Assert.That(f.Pending,Is.True);Assert.That(f.check.Running,Is.True);Assert.That(f.check.independentCheckPattern,Is.True);Assert.That(f.Approved,Is.False);
                f.Tick(11);Assert.That(f.Model.id,Is.EqualTo(modelId));Assert.That(f.check.WaitingForSignal,Is.True);
                f.Fresh();Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);f.TickFreshSpan(12,14.9);
                Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True,"Three continuous fresh seconds must complete before automatic start.");
                f.Fresh();f.Tick(15.1);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.False);Assert.That(f.check.TargetIndex,Is.EqualTo(0));Assert.That(f.Approved,Is.False);
                Assert.That(Directory.GetFiles(Path.Combine(f.folder,"Calibration"),"correction.json",SearchOption.AllDirectories).Length,Is.EqualTo(1));
            }
        }
        [Test] public void LostSignalResetsCountdownRatherThanStartingFromElapsedWallTime()
        {
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();f.TickFreshSpan(1,2);
                Set(f.check,"lastFresh",double.NegativeInfinity);f.Tick(2.1);f.Tick(20);
                Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);Assert.That(f.Pending,Is.True);
                f.TickFreshSpan(21,23.9);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);
                f.Fresh();f.Tick(24.1);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.False);
            }
        }
        [Test] public void ExplicitCancelDoesNotRearmTheSameReportOrStartItsPreview()
        {
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();string id=f.Model.id;f.Cancel();f.TickFreshSpan(1,5);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);Assert.That(f.Model.id,Is.EqualTo(id));Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void AbortedVerificationCancelsAutomaticStartWithoutCreatingAnotherCorrection()
        {
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();f.check.Abort();f.TickFreshSpan(1,5);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.Running,Is.False);Assert.That(f.check.LastReport.aborted,Is.True);Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void ExplicitTutorialSkipCancelsPendingAutomaticStart()
        {
            using(var f=new Fixture())
            {
                f.check.TutorialEntryContext=true;f.Tick(0);f.Fresh();f.check.SkipTutorialValidation();f.Tick(1);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.Running,Is.False);Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void AnotherSessionCannotInheritPendingVerification()
        {
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();string trainingSession=f.Model.session_id;f.StartAnotherSession();f.Tick(1);
                Assert.That(StudyExportSession.SessionId,Is.Not.EqualTo(trainingSession));Assert.That(f.Pending,Is.False);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void ChangedCalibrationOrHeadBindingCancelsPendingTransition()
        {
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();f.providerType.GetField("calibrationId").SetValue(f.provider,"different-config");f.Tick(1);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);Assert.That(f.Approved,Is.False);
            }
            using(var f=new Fixture())
            {
                f.Tick(0);f.Fresh();f.providerType.GetField("headReference").SetValue(f.provider,f.otherHead.transform);f.Tick(1);
                Assert.That(f.Pending,Is.False);Assert.That(f.check.AwaitingVisibilityConfirmation,Is.True);Assert.That(f.Approved,Is.False);
            }
        }
        [Test] public void RestoredReportsAndDisabledAutomaticFlowNeverStartFitting()
        {
            using(var f=new Fixture())
            {
                Set(f.check,"RestoredForReviewOnly",true);f.Tick(0);Assert.That(f.Model,Is.Null);Assert.That(f.Pending,Is.False);
            }
            using(var f=new Fixture())
            {
                f.providerType.GetField("automaticTwoRoundFlow").SetValue(f.provider,false);f.Tick(0);Assert.That(f.Model,Is.Null);Assert.That(f.Pending,Is.False);
            }
        }
    }
}
