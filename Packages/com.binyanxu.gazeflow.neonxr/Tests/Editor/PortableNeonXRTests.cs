using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PupilLabs;
using UnityEngine;
using NeonResearch.NeonXR.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NeonResearch.NeonXR.Tests
{
    // EditMode only. No Play, official SDK Awake, transport, media or participant archive access.
    public sealed class PortableNeonXRTests
    {
        private GameObject root,head,neon,other;
        private string directory;
        private static void ResetSession()=>typeof(StudyExportSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        [SetUp] public void Before(){ResetSession();root=new GameObject("Synthetic research root");head=new GameObject("Arbitrary tracked transform");neon=new GameObject("Arbitrary official source");directory=Path.GetFullPath("Library/GazeFlowPortableTests/"+Guid.NewGuid().ToString("N"));}
        [TearDown] public void After(){foreach(var go in new[]{other,root,head,neon})if(go!=null)UnityEngine.Object.DestroyImmediate(go);StudyExportSession.Finish();ResetSession();if(Directory.Exists(directory))Directory.Delete(directory,true);}
        [Test] public void ExplicitBindingsNeedNoHostScriptsAndNeverApproveMapping()
        {
            var p=root.AddComponent<NeonXRResearchProvider>();p.calibrationId="synthetic-mount";
            var binding=root.AddComponent<StudyNeonXRBindings>();binding.provider=p;binding.headSource=head.AddComponent<StudyHeadPoseSource>();binding.headSource.head=head.transform;binding.neonSource=neon.AddComponent<NeonGazeDataProvider>();binding.RefreshBindings();
            Assert.That(p.headReference,Is.EqualTo(head.transform));Assert.That(p.neon,Is.EqualTo(binding.neonSource));Assert.That(p.mappingVerified,Is.False);Assert.That(StudyEvents.Recording,Is.False);
        }
        [Test] public void AmbiguousOrDisabledSourcesCannotContinueWithTheOldBinding()
        {
            var p=root.AddComponent<NeonXRResearchProvider>();p.calibrationId="synthetic-mount";var binding=root.AddComponent<StudyNeonXRBindings>();binding.provider=p;binding.discoverUniqueHeadSource=true;binding.neonSource=neon.AddComponent<NeonGazeDataProvider>();head.AddComponent<StudyHeadPoseSource>();binding.RefreshBindings();Assert.That(p.headReference,Is.EqualTo(head.transform));
            other=new GameObject("Second explicit source");other.AddComponent<StudyHeadPoseSource>();binding.RefreshBindings();Assert.That(p.headReference,Is.Null);Assert.That(binding.Status,Does.Contain("Ambiguous"));
            other.SetActive(false);binding.RefreshBindings();Assert.That(p.headReference,Is.EqualTo(head.transform));binding.neonSource.enabled=false;binding.RefreshBindings();Assert.That(p.headReference,Is.Null);Assert.That(p.neon,Is.Null);
        }
        [Test] public void SameSessionHeadReplacementKeepsManualOffsetButClearsApprovalAndSave()
        {
            StudyExportSession.EnsureStarted(directory);var p=root.AddComponent<NeonXRResearchProvider>();p.calibrationId="synthetic-mount";var binding=root.AddComponent<StudyNeonXRBindings>();binding.provider=p;binding.headSource=head.AddComponent<StudyHeadPoseSource>();binding.neonSource=neon.AddComponent<NeonGazeDataProvider>();binding.RefreshBindings();p.ApplyDesktopOffset(1,2);p.SaveDesktopAdjustment();string id=p.CurrentCorrection.id;
            other=new GameObject("Replacement explicit head");binding.headSource=other.AddComponent<StudyHeadPoseSource>();p.mappingVerified=true;binding.RefreshBindings();Assert.That(p.headReference,Is.EqualTo(other.transform));Assert.That(p.CurrentCorrection.id,Is.EqualTo(id));Assert.That(p.mappingVerified,Is.False);Assert.That(p.DesktopAdjustmentSaved,Is.False);
            p.calibrationId="new-mount";binding.RefreshBindings();Assert.That(p.CurrentCorrection,Is.Null);
        }
        [Test] public void WizardGuardsDuplicatesAndLeavesAcquisitionOptIn()
        {
            var source=neon.AddComponent<NeonGazeDataProvider>();Assert.That(NeonXRSetupWindow.CanCreate(head.transform,source,"mount",0,0),Is.True);Assert.That(NeonXRSetupWindow.CanCreate(head.transform,source,"",0,0),Is.False);Assert.That(NeonXRSetupWindow.CanCreate(head.transform,source,"mount",1,0),Is.False);Assert.That(NeonXRSetupWindow.CanCreate(head.transform,source,"mount",0,1),Is.False);
            var defaults=new NeonStudySettings();Assert.That(defaults.enabled||defaults.recordOnPlay||defaults.mediaRecordingEnabled||defaults.openResearchPanelsOnPlay,Is.False);
        }
        [Test] public void WizardCreatesOnlyADedicatedRigAndExplicitHeadSource()
        {
            var source=neon.AddComponent<NeonGazeDataProvider>();other=NeonXRSetupWindow.CreateResearchRig(head.transform,source,"synthetic-mount");
            var p=other.GetComponent<NeonXRResearchProvider>();var recorder=other.GetComponent<StudyGazeTargetRecorder>();var bindings=other.GetComponent<StudyNeonXRBindings>();
            Assert.That(recorder.provider,Is.EqualTo(p));Assert.That(p.headReference,Is.EqualTo(head.transform));Assert.That(bindings.neonSource,Is.EqualTo(source));Assert.That(bindings.headSource.head,Is.EqualTo(head.transform));Assert.That(bindings.persistResearchRoot,Is.True);Assert.That(bindings.discoverUniqueHeadSource||bindings.discoverUniqueNeonSource,Is.False);Assert.That(StudyEvents.Recording,Is.False);
            Assert.Throws<InvalidOperationException>(()=>NeonXRSetupWindow.CreateResearchRig(head.transform,source,"synthetic-mount"));
        }
        [TestCase(typeof(StudyNeonXRBindings))]
        [TestCase(typeof(StudyGazeReferencePoint))]
        public void PortableComponentsRetainTheirScriptsAndFieldsAfterSceneReopen(Type type)
        {
            if(!Application.isBatchMode)Assert.Ignore("Scene replacement checks run only in a disposable batch host.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);root=new GameObject("Serialized portable component");var component=(MonoBehaviour)root.AddComponent(type);Assert.That(MonoScript.FromMonoBehaviour(component),Is.Not.Null);
            if(component is StudyGazeReferencePoint reference){reference.diameterMetres=.064f;reference.showReference=false;}
            if(component is StudyNeonXRBindings bindings){bindings.discoverUniqueHeadSource=true;bindings.discoverUniqueNeonSource=true;}
            const string parent="Assets/GazeFlowPortableSerialization";if(!AssetDatabase.IsValidFolder(parent))AssetDatabase.CreateFolder("Assets","GazeFlowPortableSerialization");string path=parent+"/test-"+Guid.NewGuid().ToString("N")+".unity";
            try{Assert.That(EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),path),Is.True);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.OpenScene(path);var loaded=(MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);Assert.That(loaded,Is.Not.Null);Assert.That(MonoScript.FromMonoBehaviour(loaded),Is.Not.Null);if(loaded is StudyGazeReferencePoint savedReference)Assert.That(savedReference.diameterMetres,Is.EqualTo(.064f));if(loaded is StudyNeonXRBindings savedBindings)Assert.That(savedBindings.discoverUniqueHeadSource&&savedBindings.discoverUniqueNeonSource,Is.True);}
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);AssetDatabase.DeleteAsset(path);}
        }
        [Test] public void AuthoredReferenceDoesNotCreatePhysicsOrAutoShowOutsidePlay()
        {
            var point=root.AddComponent<StudyGazeReferencePoint>();Assert.That(point.showReference,Is.False);Assert.That(point.diameterMetres,Is.EqualTo(.032f));point.SetVisible(true);Assert.That(point.ReferenceVisible,Is.False,"No runtime reference is created outside Play.");
            var create=typeof(StudyGazeReferencePoint).GetMethod("CreateVisual",BindingFlags.Instance|BindingFlags.NonPublic);Assert.That((bool)create.Invoke(point,null),Is.True);
            var visual=(GameObject)typeof(StudyGazeReferencePoint).GetField("visual",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(point);Assert.That(visual.GetComponent<Collider>(),Is.Null);Assert.That(visual.GetComponent<Rigidbody>(),Is.Null);Assert.That(visual.GetComponent<StudyResearchTarget>(),Is.Null);Assert.That(visual.layer,Is.EqualTo(2));
        }
    }
}
