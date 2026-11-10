using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace NeonResearch.Tests
{
    public sealed class GazeTargetTests
    {
        private string root;
        private GameObject target, blocker;
        private static void Reset() => typeof(StudyExportSession).GetMethod("Reset", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        // Windows requires the reader to share an already-open writer. Do not weaken the logger's single-writer policy.
        private static string ReadLive(string path)
        { using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) using(var reader=new StreamReader(stream))return reader.ReadToEnd(); }
        [SetUp] public void Before()
        {
            Reset(); root = Path.GetFullPath("Library/GazeTargetTests/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }
        [TearDown] public void After()
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (blocker != null) UnityEngine.Object.DestroyImmediate(blocker);
            StudyExportSession.Finish(); Reset(); Directory.Delete(root, true);
        }
        private void Target()
        {
            target = GameObject.CreatePrimitive(PrimitiveType.Cube); target.transform.position = new Vector3(1000, 1000, 1003);
            target.AddComponent<StudyResearchTarget>().targetId = "key"; Physics.SyncTransforms();
        }
        private StudyGazeRaySample Sample(bool valid = true) => new StudyGazeRaySample { worldRay = new Ray(new Vector3(1000,1000,1000),Vector3.forward), valid = valid,
            source = "synthetic_test_not_participant_data", status = valid ? "synthetic" : "mapping_unverified", calibrationId = "synthetic-calibration" };
        [Test] public void TargetLoggingIsOptInAndDoesNotStartSession()
        {
            Assert.That(StudyGazeTargetLog.Record(Sample(), ~0, 10, out _), Is.False);
            StudyGazeTargetLog.Missing("test"); Assert.That(StudyExportSession.Folder, Is.Null);
        }
        [Test] public void NearestNonTargetColliderBlocksTargetBehindIt()
        {
            Target(); StudyExportSession.EnsureStarted(root);
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new Vector3(1000,1000,1001.5f); Physics.SyncTransforms();
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.Empty);
            StudyGazeTargetLog.Flush(); Assert.That(ReadLive(Path.Combine(StudyExportSession.Folder,"gaze_targets.csv")), Does.Contain("non_target_occluder"));
        }
        [Test] public void DuplicateTargetIdsAreRejectedInsteadOfSilentlyMerged()
        {
            Target(); StudyExportSession.EnsureStarted(root);
            blocker = new GameObject("duplicate"); blocker.AddComponent<StudyResearchTarget>().targetId = "key";
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.Empty);
            StudyGazeTargetLog.Flush(); Assert.That(ReadLive(Path.Combine(StudyExportSession.Folder,"gaze_targets.csv")), Does.Contain("missing_or_duplicate_target_id"));
        }
        [Test] public void InvalidMappingNeverCreatesHitAndMissingDataSurvivesReplayExport()
        {
            Target(); string folder = StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(false), ~0, 10, out string id); Assert.That(id, Is.Empty);
            StudyGazeTargetLog.Missing("stale_or_disconnected"); StudyExportSession.Finish();
            string csv = ReadLive(Path.Combine(folder,"gaze_targets.csv"));
            Assert.That(csv, Does.Contain("mapping_unverified")); Assert.That(csv, Does.Contain("stale_or_disconnected")); Assert.That(csv, Does.Not.Contain(",target_hit,"));
            string html = File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html")); Assert.That(html, Does.Contain("gazeTargets")); Assert.That(html, Does.Contain("stale_or_disconnected"));
        }
        [Test] public void HitExportPreservesIdentityAndClosesFilesWithoutLeakingAcrossSessions()
        {
            Target(); string first = StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.EqualTo("key"));
            StudyExportSession.Finish();
            using (new FileStream(Path.Combine(first,"gaze_targets.csv"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            Reset(); string second = StudyExportSession.EnsureStarted(root); StudyExportSession.Finish();
            Assert.That(File.Exists(Path.Combine(second,"gaze_targets.csv")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(first,"gaze_targets.csv")), Does.Contain("unity_callback_time_NOT_device_time_or_hardware_sync"));
            Assert.That(JObject.Parse(File.ReadAllText(Path.Combine(second,"export_manifest.json"))).Value<bool>("closed"), Is.True);
        }
        [Test] public void WatchdogWritesOneGapAndEndsTargetInsteadOfReusingLastRay()
        {
            Target(); string folder = StudyExportSession.EnsureStarted(root);
            blocker = new GameObject("recorder"); var recorder = blocker.AddComponent<StudyGazeTargetRecorder>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(StudyGazeTargetRecorder).GetMethod("Receive", flags).Invoke(recorder, new object[] { Sample() });
            typeof(StudyGazeTargetRecorder).GetField("lastSample", flags).SetValue(recorder, -10d);
            var update = typeof(StudyGazeTargetRecorder).GetMethod("Update", flags); update.Invoke(recorder, null); update.Invoke(recorder, null);
            StudyGazeTargetLog.Flush(); StudySequence.Flush();
            string csv = ReadLive(Path.Combine(folder,"gaze_targets.csv"));
            Assert.That(csv.Split(new[] { "stale_or_disconnected" }, StringSplitOptions.None).Length-1, Is.EqualTo(1));
            Assert.That(ReadLive(Path.Combine(folder,"game_events.csv")), Does.Contain("gaze_target_exit"));
        }
        [Test] public void NonFiniteRayIsExplicitlyInvalid()
        {
            Target(); StudyExportSession.EnsureStarted(root); var sample = Sample(); sample.worldRay = new Ray(Vector3.zero, new Vector3(float.NaN,0,1));
            StudyGazeTargetLog.Record(sample, ~0, 10, out string id); Assert.That(id, Is.Empty); StudyGazeTargetLog.Flush();
            Assert.That(ReadLive(Path.Combine(StudyExportSession.Folder,"gaze_targets.csv")), Does.Contain("invalid_ray"));
        }
        [Test] public void TriggerAoiRequiresExplicitOptInAndExportsGeometryWithoutChangingPhysics()
        {
            Target(); var collider = target.GetComponent<Collider>(); collider.isTrigger = true;
            var research = target.GetComponent<StudyResearchTarget>();
            StudyExportSession.EnsureStarted(root); Physics.SyncTransforms();
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string disabled); Assert.That(disabled, Is.Empty);
            research.includeTriggerColliders = true;
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string enabled); Assert.That(enabled, Is.EqualTo("key"));
            Assert.That(collider.isTrigger, Is.True); Assert.That(collider.enabled, Is.True);
            StudyGazeTargetLog.Flush();
            Assert.That(ReadLive(Path.Combine(StudyExportSession.Folder,"gaze_targets.csv")), Does.Contain("authored_trigger_volume"));
        }
        [Test] public void SolidWallOccludesOptedInTriggerAoi()
        {
            Target(); target.GetComponent<Collider>().isTrigger = true;
            target.GetComponent<StudyResearchTarget>().includeTriggerColliders = true;
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new Vector3(1000,1000,1001.5f);
            Physics.SyncTransforms(); StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.Empty);
            StudyGazeTargetLog.Flush(); Assert.That(ReadLive(Path.Combine(StudyExportSession.Folder,"gaze_targets.csv")), Does.Contain("non_target_occluder"));
        }
        [Test] public void UnconfiguredTriggerDoesNotHideSolidTarget()
        {
            Target(); blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(1000,1000,1001.5f); blocker.GetComponent<Collider>().isTrigger = true;
            Physics.SyncTransforms(); StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.EqualTo("key"));
        }
        [Test] public void DisabledHelperTargetDoesNotMaskEnabledParentIdentity()
        {
            Target(); blocker = new GameObject("parent physical object");
            blocker.AddComponent<StudyResearchTarget>().targetId = "parent-id";
            target.transform.SetParent(blocker.transform, true); target.GetComponent<StudyResearchTarget>().enabled = false;
            StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string id); Assert.That(id, Is.EqualTo("parent-id"));
        }
        [Test] public void PrefabTemplateCannotProduceHitBeforeExplicitSpawnRegistration()
        {
            Target(); var research = target.GetComponent<StudyResearchTarget>(); research.requiresSpawnIdentity = true;
            StudyExportSession.EnsureStarted(root);
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string before); Assert.That(before, Is.Empty);
            StudyTargetSpawnIdentity.Register(target, "synthetic-site", "instance-1");
            StudyGazeTargetLog.Record(Sample(), ~0, 10, out string after); Assert.That(after, Does.StartWith("spawn-"));
        }
        [Test] public void CatalogPreservesRealNameGroupAndExplicitAliasesWithoutClaimingObservation()
        {
            Target(); var research = target.GetComponent<StudyResearchTarget>();
            target.name = "Cup, original name"; research.targetGroup = "Kitchen";
            research.legacyObjectIds = new[] { "original-event-id" }; research.includeTriggerColliders = true;
            StudyExportSession.EnsureStarted(root); StudyTargetCatalog.Snapshot();
            string csv = File.ReadAllText(Path.Combine(StudyExportSession.Folder,"target_catalog.csv"));
            Assert.That(csv, Does.Contain("Cup, original name")); Assert.That(csv, Does.Contain("Kitchen"));
            Assert.That(csv, Does.Contain("original-event-id")); Assert.That(csv, Does.Contain("NOT_evidence"));
            Assert.That(csv, Does.Contain("authored_trigger_volume"));
        }
    }
}
