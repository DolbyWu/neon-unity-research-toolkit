using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NeonResearch.Tests
{
    public sealed class ExtractionTests
    {
        private string root;
        private static void ResetSession()=>typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        // Explicitly opt in only a disposable verification host. Never create this marker in an experiment.
        private static bool IsIsolatedSceneTestHost()
        {
            const string marker="Library/NeonResearch.IsolatedSceneTests.txt";
            return File.Exists(marker) &&
                string.Equals(File.ReadAllText(marker).Trim(),Path.GetFullPath(Application.dataPath),StringComparison.Ordinal) &&
                SceneManager.sceneCount==1 && string.IsNullOrEmpty(SceneManager.GetSceneAt(0).path);
        }
        [SetUp]public void Before()
        {
            root=Path.Combine(Path.GetFullPath("Library/StandaloneVerification"),"test-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);ResetSession();
        }
        [TearDown]public void After()
        {
            StudyExportSession.Finish();ResetSession();StudyParticipantStartFlow.Clear();
            Assert.That(root.StartsWith(Path.GetFullPath("Library/StandaloneVerification")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));
            Directory.Delete(root,true);
        }
        [Test]public void OwnershipSidecarIsSessionBoundedAndInvalidEvidenceDoesNotBlockReplay()
        {
            Directory.CreateDirectory(Path.Combine(root,"Analysis"));string file=Path.Combine(root,"Analysis","target-object-ownership.json");
            var read=typeof(StudyOfflineReplay).GetMethod("ReadTargetOwnership",BindingFlags.Static|BindingFlags.NonPublic);
            File.WriteAllText(file,new JObject{["schema"]="gazeflow-reviewed-target-ownership-v1",["session_id"]="foreign",["links"]=new JArray()}.ToString());
            var rejected=(JObject)read.Invoke(null,new object[]{root,"expected"});Assert.That(rejected.Value<string>("warning"),Is.Not.Empty);Assert.That(rejected["links"].Count(),Is.Zero);
            File.WriteAllText(file,new JObject{["schema"]="gazeflow-reviewed-target-ownership-v1",["session_id"]="expected",["links"]=new JArray(new JObject{["child_id"]="visual",["owner_id"]="diary",["rule"]="reviewed"})}.ToString());
            var accepted=(JObject)read.Invoke(null,new object[]{root,"expected"});Assert.That(accepted["links"].Count(),Is.EqualTo(1));
            File.WriteAllText(file,"invalid JSON");Assert.That(((JObject)read.Invoke(null,new object[]{root,"expected"})).Value<string>("warning"),Is.Not.Empty);
        }
        [Test]public void DefaultsAreInertAndDoNotReuseExistingStudyConfiguration()
        {
            var settings=new NeonStudySettings();Assert.That(settings.recordOnPlay,Is.False);Assert.That(settings.enabled,Is.False);
            Assert.That(settings.mediaRecordingEnabled,Is.False);Assert.That(settings.companionIp,Is.Empty);
            Assert.That(settings.outputRoot,Is.EqualTo("NeonResearchData"));
            Assert.That(NeonStudySettings.Folder.Replace('\\','/'),Does.EndWith("UserSettings/NeonResearch"));
            Assert.That(StudyEvents.Record("must_not_autostart"),Is.False);Assert.That(StudyExportSession.Folder,Is.Null);
        }
        [Test]public void ReplayOffsetsEmbedSessionCheckedImmutableModelsAndSaveReceipts()
        {
            const string session="20990101T000000000Z-abcdef01";string calibration=Path.Combine(root,"Calibration"),modelFolder=Path.Combine(calibration,"manual-test"),receiptFolder=Path.Combine(calibration,"gaze-confirmations");Directory.CreateDirectory(modelFolder);Directory.CreateDirectory(receiptFolder);
            var q=StudyGazeCorrection.ManualRotation(3.7f,2.7f);
            var model=new StudyGazeCorrection.Model{id="manual-test",session_id=session,calibration_id="mount",correction_kind="manual_yaw_pitch",horizontal_degrees=3.7f,vertical_degrees=2.7f,applied_session_s=1,quaternion_xyzw=new[]{q.x,q.y,q.z,q.w}};
            string original=Newtonsoft.Json.JsonConvert.SerializeObject(model),modelPath=Path.Combine(modelFolder,"correction.json");File.WriteAllText(modelPath,original);
            File.WriteAllText(Path.Combine(receiptFolder,"save.json"),new JObject{["schema"]="gazeflow-desktop-adjustment-save-v1",["id"]="save-test",["session_id"]=session,["calibration_id"]="mount",["mode"]="manual_yaw_pitch",["correction_id"]="manual-test",["saved_session_s"]=2,["horizontal_degrees"]=3.7,["vertical_degrees"]=2.7,["accuracy_approval_granted"]=false}.ToString());
            var report=(JObject)typeof(StudyOfflineReplay).GetMethod("ReadGazeAdjustments",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,session,10d});
            Assert.That(report["models"].Count(),Is.EqualTo(1));Assert.That(report["confirmations"].Count(),Is.EqualTo(1));Assert.That(report["models"][0].Value<float>("horizontal_degrees"),Is.EqualTo(3.7f));Assert.That(report["warnings"].Count(),Is.Zero);Assert.That(File.ReadAllText(modelPath),Is.EqualTo(original));
        }
        [Test]public void ReplayOffsetsOmitWrongSessionUnsafeAnglesAndCorruptFilesWithoutBlockingReplay()
        {
            const string session="20990101T000000000Z-abcdef01";string calibration=Path.Combine(root,"Calibration");
            foreach(string name in new[]{"bad","foreign","unsafe"})Directory.CreateDirectory(Path.Combine(calibration,name));
            File.WriteAllText(Path.Combine(calibration,"bad","correction.json"),"not json");
            var model=new StudyGazeCorrection.Model{id="foreign",session_id="another-session",calibration_id="mount",correction_kind="manual_yaw_pitch",horizontal_degrees=1,quaternion_xyzw=new[]{0f,0f,0f,1f}};
            File.WriteAllText(Path.Combine(calibration,"foreign","correction.json"),Newtonsoft.Json.JsonConvert.SerializeObject(model));model.session_id=session;model.id="unsafe";model.horizontal_degrees=99;
            File.WriteAllText(Path.Combine(calibration,"unsafe","correction.json"),Newtonsoft.Json.JsonConvert.SerializeObject(model));
            var report=(JObject)typeof(StudyOfflineReplay).GetMethod("ReadGazeAdjustments",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,session,10d});
            Assert.That(report["models"].Count(),Is.Zero);Assert.That(report["confirmations"].Count(),Is.Zero);Assert.That(report["warnings"].Count(),Is.EqualTo(3));
            var missing=(JObject)typeof(StudyOfflineReplay).GetMethod("ReadGazeAdjustments",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Path.Combine(root,"missing"),session,10d});Assert.That(missing["models"].Count(),Is.Zero);Assert.That(missing["warnings"].Count(),Is.Zero);
        }
        [Test]public void IndependentSessionExportsHeadEventsAndTheVideoAnnotationReplay()
        {
            string folder=StudyExportSession.EnsureStarted(root);var go=new GameObject("Synthetic head");
            try
            {
                go.transform.position=new Vector3(1,2,3);go.transform.rotation=Quaternion.Euler(10,20,30);
                StudyExportSession.Pose(go.transform);StudyEvents.Record("synthetic_marker","cube","test","Not participant data");
                StudyEvents.Grab("Left",go.transform,"synthetic_stable_object_id");StudyEvents.Release("Left");StudyExportSession.Finish();
                Assert.That(File.ReadAllLines(Path.Combine(folder,"head_pose.csv")).Length,Is.EqualTo(2));
                string events=File.ReadAllText(Path.Combine(folder,"game_events.csv"));Assert.That(events,Does.Contain("grab_start"));Assert.That(events,Does.Contain("grab_end"));Assert.That(events,Does.Contain("synthetic_stable_object_id"));
                Assert.That(JObject.Parse(File.ReadAllText(Path.Combine(folder,"export_manifest.json"))).Value<bool>("closed"),Is.True);
                string html=File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html"));
                Assert.That(html,Does.Contain("annotationsImport"));Assert.That(html,Does.Contain("Choose local video"));
                Assert.That(html,Does.Contain("Add another annotation here"));Assert.That(html,Does.Contain("Pause &amp; mark this frame"));
                Assert.That(html,Does.Contain("reviewClocks"));Assert.That(html,Does.Not.Contain("__STUDY_DATA__"));
                Assert.That(html,Does.Contain("Gaze offset report"));Assert.That(html,Does.Contain("gazeflow-replay-offset-metadata-v1"));
                string derived=StudyOfflineReplay.BuildNamedWithTemplate(folder,Resources.Load<TextAsset>("NeonResearchOfflineReplay").text,"study-replay-object-review-test.html");
                Assert.That(File.Exists(derived),Is.True);Assert.That(File.ReadAllText(Path.Combine(folder,"Replay/study-replay.html")),Is.EqualTo(html),"Derived review never replaces the original replay.");
                Assert.Throws<ArgumentException>(()=>StudyOfflineReplay.BuildNamedWithTemplate(folder,"__STUDY_DATA__","../outside.html"));
                int timeline=html.IndexOf("id=\"timelinePanel\"",StringComparison.Ordinal),annotations=html.IndexOf("id=\"annotationPanel\"",StringComparison.Ordinal);
                int interval=html.IndexOf("id=\"intervalExportPanel\"",StringComparison.Ordinal),gameLog=html.IndexOf("id=\"gameLogPanel\"",StringComparison.Ordinal);
                Assert.That(timeline,Is.GreaterThan(0));Assert.That(annotations,Is.GreaterThan(timeline));Assert.That(interval,Is.GreaterThan(annotations));Assert.That(gameLog,Is.GreaterThan(interval));
                Assert.That(html,Does.Contain("pupil_left_mm"));Assert.That(html,Does.Not.Contain("pupil_left_sdk_units"));
                Assert.That(Directory.GetDirectories(folder,"Neon-*").Length,Is.Zero,"No real or fabricated eye stream is started by this synthetic test.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [Test]public void ProfilesSnapshotsAndPathsAreIndependentAndImmutable()
        {
            const string id="P-SYNTHETIC";StudyParticipantProfiles.Save(root,id,false,"","Synthetic only",new JObject());
            string hash=StudyParticipantProfiles.Fingerprint(File.ReadAllText(Path.Combine(root,"Participants",id,"participant.json")));
            const string sid="20990101T000000000Z-abcdef01";string folder=StudyArchivePaths.SessionFolder(root,id,sid);
            var intake=new JObject{["participantId"]=id,["archiveRoot"]=root,["profileHash"]=hash};
            StudyParticipantProfiles.WriteSessionSnapshot(root,id,sid,folder,intake);
            Assert.Throws<IOException>(()=>StudyParticipantProfiles.WriteSessionSnapshot(root,id,sid,folder,intake));
            Assert.Throws<InvalidDataException>(()=>StudyArchivePaths.ParticipantFolder(root,"../escape"));
            Assert.That(File.Exists(Path.Combine(folder,"participant_summary.txt")),Is.True);
        }
        [Test]public void GenericZonesUseExplicitCollidersAndIds()
        {
            var go=new GameObject("Synthetic region");
            try{var box=go.AddComponent<BoxCollider>();box.size=Vector3.one*2;var zone=go.AddComponent<StudyRegion>();zone.regionCollider=box;zone.regionId="Test area";
                Assert.That(StudySequence.Locate(Vector3.zero),Is.EqualTo("Test area"));Assert.That(StudySequence.Locate(Vector3.one*10),Is.EqualTo("In-between"));}
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        private static void Float(byte[] bytes,int offset,float value){var data=BitConverter.GetBytes(value);if(BitConverter.IsLittleEndian)Array.Reverse(data);Buffer.BlockCopy(data,0,bytes,offset,4);}
        [TestCase(typeof(StudyHeadPoseSource))]
        [TestCase(typeof(StudyRegion))]
        [TestCase(typeof(StudyInteractionAdapter))]
        [TestCase(typeof(StudyResearchTarget))]
        [TestCase(typeof(StudyGazeTargetRecorder))]
        public void PublicComponentsHavePersistentScriptsAndRetainFieldsAfterSceneReopen(Type type)
        {
            if(!Application.isBatchMode && !IsIsolatedSceneTestHost())
                Assert.Ignore("Use batch mode or an explicitly marked disposable host with one untitled scene; never run against experiment scenes.");
            const string parent="Assets/NeonResearchSerializationTests";
            if(!AssetDatabase.IsValidFolder(parent))AssetDatabase.CreateFolder("Assets","NeonResearchSerializationTests");
            string testFolder=parent+"/test-"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder(parent,Path.GetFileName(testFolder));
            string scenePath=testFolder+"/Roundtrip.unity";
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            try
            {
                var go=new GameObject("Serialized component");
                var component=(MonoBehaviour)go.AddComponent(type);
                if(component is StudyRegion region){region.regionId="ROUNDTRIP-REGION";region.regionCollider=go.AddComponent<BoxCollider>();}
                if(component is StudyHeadPoseSource head){head.head=go.transform;}
                if(component is StudyInteractionAdapter interaction){interaction.objectId="ROUNDTRIP-OBJECT";}
                if(component is StudyResearchTarget target){target.targetId="ROUNDTRIP-TARGET";target.targetGroup="test";}
                if(component is StudyGazeTargetRecorder recorder){recorder.maxDistance=13;recorder.collisionMask=3;}
                var script=MonoScript.FromMonoBehaviour(component);
                Assert.That(script,Is.Not.Null);Assert.That(script.GetClass(),Is.EqualTo(type));
                string scriptPath=AssetDatabase.GetAssetPath(script);
                Assert.That(scriptPath,Does.EndWith("/"+type.Name+".cs"),"Each attachable component must have its own matching source file.");
                Assert.That(EditorSceneManager.SaveScene(scene,scenePath),Is.True);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
                var restored=scene.GetRootGameObjects()[0].GetComponent(type);
                Assert.That(restored,Is.Not.Null,"The component must not become Missing Script after reopening.");
                if(restored is StudyRegion restoredRegion){Assert.That(restoredRegion.regionId,Is.EqualTo("ROUNDTRIP-REGION"));Assert.That(restoredRegion.regionCollider,Is.Not.Null);}
                if(restored is StudyHeadPoseSource restoredHead){Assert.That(restoredHead.head,Is.EqualTo(restoredHead.transform));}
                if(restored is StudyInteractionAdapter restoredInteraction){Assert.That(restoredInteraction.objectId,Is.EqualTo("ROUNDTRIP-OBJECT"));}
                if(restored is StudyResearchTarget restoredTarget){Assert.That(restoredTarget.targetId,Is.EqualTo("ROUNDTRIP-TARGET"));Assert.That(restoredTarget.targetGroup,Is.EqualTo("test"));}
                if(restored is StudyGazeTargetRecorder restoredRecorder){Assert.That(restoredRecorder.maxDistance,Is.EqualTo(13));Assert.That(restoredRecorder.collisionMask.value,Is.EqualTo(3));}
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                AssetDatabase.DeleteAsset(testFolder);
            }
        }
        [TestCase(9)][TestCase(17)][TestCase(25)][TestCase(65)][TestCase(89)][TestCase(105)]
        public void DocumentedPacketSizesDecodeWithoutNativeSdk(int size)
        {
            var payload=new byte[size];Float(payload,0,100.25f);Float(payload,4,200.5f);payload[8]=255;
            if(size>=65){Float(payload,9,4.25f);Float(payload,37,3.75f);}
            var data=NeonWireDecoder.Decode(payload);Assert.That(data.supported,Is.True);Assert.That(data.worn,Is.True);
            Assert.That(data.eyeState,Is.EqualTo(size>=65));Assert.That(data.binocular,Is.EqualTo(size!=17));
            if(data.eyeState){Assert.That(data.leftPupil,Is.EqualTo(4.25f));Assert.That(data.rightPupil,Is.EqualTo(3.75f));}
            else{Assert.That(float.IsNaN(data.leftPupil),Is.True);}
            if(size==17)Assert.That(float.IsNaN(data.x),Is.True,"Do not invent a binocular gaze from monocular-only packets.");
        }
        [TestCase(0)][TestCase(8)][TestCase(64)][TestCase(100)]public void UnknownPayloadsAreFlaggedNotInvented(int size)
        {var data=NeonWireDecoder.Decode(new byte[size]);Assert.That(data.supported,Is.False);Assert.That(float.IsNaN(data.leftPupil),Is.True);}
        [Test]public void WirePupilsAreNotReplacedByZeroForMissingOrInvalidSamples()
        {Assert.That(float.IsNaN(StudyMonitorData.Pupil(0,true)),Is.True);Assert.That(float.IsNaN(StudyMonitorData.Pupil(4,false)),Is.True);Assert.That(StudyMonitorData.Pupil(4.25f,true),Is.EqualTo(4.25f));}
        [Test]public void ParticipantApprovalIsNamespacedAndConsumedOnce()
        {
            StudyParticipantStartFlow.Approve(root,"P-SYNTHETIC","{\"participantId\":\"P-SYNTHETIC\"}");
            Assert.That(StudyParticipantStartFlow.ApprovedFor(root,"P-SYNTHETIC"),Is.True);
            Assert.That(StudyParticipantIntake.Consume(),Is.Not.Null);Assert.That(StudyParticipantIntake.Consume(),Is.Null);
        }
        [Test]public void ClosedReplayResolvesWithoutArchiveServer()
        {
            string folder=StudyExportSession.EnsureStarted(root);StudyExportSession.Finish();
            Assert.That(StudyArchiveEditor.FinishedReplayPath(root,folder),Does.EndWith("study-replay.html"));
            Assert.Throws<InvalidDataException>(()=>StudyArchiveEditor.FinishedReplayPath(folder,root));
        }
        [Test]public void TransportRejectsMalformedRtpAndNeedsSenderReportForAbsoluteTime()
        {
            int offset,length;Assert.That(NeonTcpGazeClient.TryGetGazePayload(new byte[5],out offset,out length),Is.False);
            var clock=new NeonRtpClock();long timestamp;Assert.That(clock.TryMap(new byte[12],out timestamp),Is.False);
            Assert.That(NeonClockSync.Map(1000250,1000000,8),Is.EqualTo(8.25));
        }
        [Test]public void SessionResetReleasesFilesAndPreventsPriorSessionDataLeakingIntoTheNextSession()
        {
            string first=StudyExportSession.EnsureStarted(root);
            StudyEvents.Record("old_session_marker");
            StudyMonitorData.Sample(1,10,20,4,4,true,true,true,1);
            StudyMonitorData.Error="old error";
            StudyMonitorData.Stop(2);
            ResetSession();
            Assert.That(StudyExportSession.Folder,Is.Null);
            Assert.That(StudyEvents.Record("must_not_autostart"),Is.False);
            Assert.That(StudyMonitorData.Samples,Is.Zero);
            Assert.That(StudyMonitorData.Points,Is.Empty);
            Assert.That(StudyMonitorData.Events,Is.Empty);
            Assert.That(StudyMonitorData.Error,Is.Empty);
            Assert.That(StudyMonitorData.Stopped,Is.False);
            // Exclusive access proves stale session writers were disposed, including with no domain reload.
            using(var stream=new FileStream(Path.Combine(first,"head_pose.csv"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)){}
            string second=StudyExportSession.EnsureStarted(root);
            StudyEvents.Record("new_session_marker");StudyExportSession.Finish();
            Assert.That(second,Is.Not.EqualTo(first));
            string events=File.ReadAllText(Path.Combine(second,"game_events.csv"));
            Assert.That(events,Does.Contain("new_session_marker"));
            Assert.That(events,Does.Not.Contain("old_session_marker"));
        }
    }
}
