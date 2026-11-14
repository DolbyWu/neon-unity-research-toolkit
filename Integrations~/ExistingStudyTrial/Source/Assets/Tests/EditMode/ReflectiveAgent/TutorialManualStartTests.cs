using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NeonResearch;
using NUnit.Framework;
using UnityEngine;

namespace EscapeRoom.ReflectiveAgent.Tests
{
    // Synthetic state/callbacks only; no real audio clips, capture, device or archive writes.
    public sealed class TutorialManualStartTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
        private static void ResetSession()=>typeof(StudyExportSession).GetMethod("Reset",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        private sealed class Fixture:IDisposable
        {
            public readonly GameObject host,target,next;
            public readonly Component gate,grabbable,bridge;
            public readonly Type type=Find("HandTutorialNextGate");
            public readonly string root,folder;
            public Fixture()
            {
                ResetSession();root=Path.GetFullPath("Library/TutorialManualStartTests/"+Guid.NewGuid().ToString("N"));folder=StudyExportSession.EnsureStarted(root);
                host=new GameObject("Synthetic tutorial start gate");host.SetActive(false);host.AddComponent<AudioSource>();gate=host.AddComponent(type);
                next=new GameObject("Synthetic original Next");next.transform.SetParent(host.transform);next.SetActive(false);Set("_nextButton",next);
                target=new GameObject("Synthetic tutorial object");target.SetActive(false);grabbable=target.AddComponent(Find("Oculus.Interaction.Grabbable"));target.AddComponent<StudyResearchTarget>().targetId="synthetic-start-cup";target.SetActive(true);
                var objectiveType=type.GetNestedType("Objective");var objective=Activator.CreateInstance(objectiveType);objectiveType.GetField("label").SetValue(objective,"Synthetic objective");((IList)objectiveType.GetField("members").GetValue(objective)).Add(grabbable);
                var list=(IList)Activator.CreateInstance(type.GetField("_objectives",Hidden).FieldType);list.Add(objective);Set("_objectives",list);
                Call("RequireOperatorStart");host.SetActive(true);type.GetMethod("BuildMap",Hidden).Invoke(gate,null);
                bridge=host.AddComponent(Find("GazeFlowTutorialGrabRecorder"));
            }
            public void Set(string field,object value)=>type.GetField(field,Hidden).SetValue(gate,value);
            public object Call(string method,params object[] args)=>type.GetMethod(method).Invoke(gate,args);
            public bool Flag(string property)=>(bool)type.GetProperty(property).GetValue(gate);
            public IEnumerator Pump()=>(IEnumerator)type.GetMethod("AudioPump",Hidden).Invoke(gate,null);
            public void Pointer(string kind)
            {
                var evt=Activator.CreateInstance(Find("Oculus.Interaction.PointerEvent"),new object[]{42,Enum.Parse(Find("Oculus.Interaction.PointerEventType"),kind),Pose.identity,null});
                bridge.GetType().GetMethod("RecordPointer").Invoke(bridge,new[]{(object)grabbable,evt});
            }
            public string Csv(){StudyExportSession.Flush();using(var stream=new FileStream(Path.Combine(folder,"game_events.csv"),FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(stream))return reader.ReadToEnd();}
            public void Dispose(){UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(target);StudyExportSession.Finish();ResetSession();if(Directory.Exists(root))Directory.Delete(root,true);}
        }
        [Test] public void ScenePreparationArmsInactiveGateWithoutHidingOriginalStation()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try{
                var station=new GameObject("Synthetic original station");station.SetActive(false);var gate=station.AddComponent(Find("HandTutorialNextGate"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(station,scene);
                Find("GazeFlowTutorialEntry").GetMethod("PrepareDirectGrabEntry").Invoke(null,new object[]{scene});
                Assert.That((bool)gate.GetType().GetProperty("AwaitingOperatorStart").GetValue(gate),Is.True);Assert.That(station.activeSelf,Is.False,"Preparation does not activate or hide gameplay roots.");
                Assert.That((bool)gate.GetType().GetMethod("StartTutorialNarration").Invoke(gate,null),Is.False,"An inactive station cannot accept Start.");
            }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void NarrationWaitsForExplicitStartThenUsesOriginalDelayExactlyOnce()
        {
            using(var f=new Fixture())
            {
                string session=StudyExportSession.SessionId;float timeScale=Time.timeScale;var pump=f.Pump();
                for(int i=0;i<5;i++){Assert.That(pump.MoveNext(),Is.True);Assert.That(pump.Current,Is.Null);Assert.That(f.Flag("NarrationStarted"),Is.False);}
                f.Call("SetResearchCalibrationHold",true);f.Call("SetResearchCalibrationHold",false);Assert.That(f.Flag("AwaitingOperatorStart"),Is.True,"Validation release is not tutorial Start.");
                Assert.That((bool)f.Call("StartTutorialNarration"),Is.True);Assert.That((bool)f.Call("StartTutorialNarration"),Is.False);f.Call("RequireOperatorStart");Assert.That(f.Flag("AwaitingOperatorStart"),Is.False,"Repeated setup cannot relock an accepted Start.");
                Assert.That(pump.MoveNext(),Is.True);Assert.That(pump.Current,Is.TypeOf<WaitForSeconds>(),"Original intro delay starts after the click, not while preparing.");Assert.That(f.Flag("NarrationStarted"),Is.False);
                Assert.That(pump.MoveNext(),Is.True);Assert.That(f.Flag("NarrationStarted"),Is.True);Assert.That(StudyEvents.Recording,Is.True);Assert.That(StudyExportSession.SessionId,Is.EqualTo(session));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                string csv=f.Csv();Assert.That(csv.Split('\n').Count(l=>l.Contains("\"tutorial_start_requested\"")),Is.EqualTo(1));Assert.That(csv.Split('\n').Count(l=>l.Contains("\"grabbing_tutorial_started\"")),Is.EqualTo(1));
            }
        }
        [Test] public void PreparationGrabsRemainInCsvButObjectivesAndNextWaitForStart()
        {
            using(var f=new Fixture())
            {
                f.Pointer("Select");f.Pointer("Unselect");Assert.That((int)f.type.GetProperty("CompletedObjectiveCount").GetValue(f.gate),Is.Zero);Assert.That(f.next.activeSelf,Is.False);
                string preparation=f.Csv();Assert.That(preparation,Does.Contain("grab_start"));Assert.That(preparation,Does.Contain("grab_end"));Assert.That(preparation,Does.Not.Contain("tutorial_objective_completed"));
                f.Call("StartTutorialNarration");f.Pointer("Select");f.Pointer("Unselect");Assert.That((int)f.type.GetProperty("CompletedObjectiveCount").GetValue(f.gate),Is.EqualTo(1));
                var pump=f.Pump();pump.MoveNext();pump.MoveNext();Assert.That(pump.MoveNext(),Is.False);Assert.That(f.next.activeSelf,Is.True,"Actual objective completion and original narration completion still unlock Next.");Assert.That(f.Csv(),Does.Contain("tutorial_next_unlocked"));
            }
        }
        [Test] public void SaveConfirmationAndPreparationControlsAreSeparateFromProviderAvailability()
        {
            var window=Type.GetType("NeonResearch.NeonXR.Editor.DesktopGazeAdjustmentWindow, GazeFlow.NeonXR.Editor",true);
            Assert.That(window.GetEvent("PreparationControlsGUI"),Is.Not.Null);
            Assert.That(Find("GazeFlowTrialSessionPanel").GetMethod("DrawTutorialPreparation"),Is.Not.Null);
            // No provider or eye-approval object exists in the fixture; Start still works.
            using(var f=new Fixture()){Assert.That((bool)f.Call("StartTutorialNarration"),Is.True);Assert.That(f.Flag("AwaitingOperatorStart"),Is.False);}
        }
    }
}
