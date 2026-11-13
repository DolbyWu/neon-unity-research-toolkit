#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NeonResearch;
using NeonResearch.NeonXR;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>Reuses the authored Start transition; does not skip tutorial objectives or forge grab events.</summary>
public sealed class GazeFlowTutorialEntry : MonoBehaviour
{
    private const string Entry="Assets/Scenes/Main Menu AR.unity";
    private const string Trial="C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03";
    private static string reportDirectory;
    private static string correctionPath;
    private bool skipped;
    private StudySpatialValidation entryCheck;
    private GameObject validationTutorialRoot;
    private TeleportInFront validationPlacement;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHooks(){SceneManager.sceneLoaded-=Loaded;reportDirectory=null;correctionPath=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if(Application.isBatchMode || !Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/').Equals(Trial,StringComparison.OrdinalIgnoreCase))return;
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
    }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.path!=Entry && scene.name!="LocomotionTutorial" && scene.name!="Room 1")return;
        if(StudyEvents.Recording)StudyEvents.Record("experiment_phase_start",scene.name,"scene_loaded","Editor tutorial flow; not participant interaction.");
        var runner=new GameObject("GazeFlow tutorial entry (runtime)");SceneManager.MoveGameObjectToScene(runner,scene);runner.AddComponent<GazeFlowTutorialEntry>();
        if(scene.name!="Room 1")runner.AddComponent<GazeFlowTutorialGrabRecorder>();
    }
    private IEnumerator Start()
    {
        yield return null;yield return new WaitForSecondsRealtime(.25f);
        var scene=gameObject.scene;
        // The authored editor bootstrap may already be loading this scene additively.
        yield return new WaitForSecondsRealtime(1f);
        var init=SceneManager.GetSceneByName("Initialization");
        if(!init.IsValid())yield return SceneManager.LoadSceneAsync("Initialization",LoadSceneMode.Additive);
        else while(!init.isLoaded){yield return null;init=SceneManager.GetSceneByName("Initialization");}
        Transform head=null;
        float deadline=Time.realtimeSinceStartup+20f;
        while(head==null && Time.realtimeSinceStartup<deadline)
        {
            head=FindObjectsByType<OVRCameraRig>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene && r.isActiveAndEnabled).Select(r=>r.centerEyeAnchor).FirstOrDefault(h=>h!=null);
            if(head==null)yield return null;
        }
        var providers=FindObjectsByType<NeonXRResearchProvider>(FindObjectsSortMode.None);
        if(head==null||providers.Length!=1){Debug.LogError("[GazeFlow flow] Expected one live XR head and one Neon XR provider. Tutorial validation entry stopped; inspect configuration.");yield break;}
        var provider=providers[0];
        var source=head.GetComponent<StudyHeadPoseSource>()??head.gameObject.AddComponent<StudyHeadPoseSource>();source.head=head;
        provider.mappingVerified=false;provider.headReference=head;
        if(provider.neon!=null)
            foreach(var visualizer in provider.neon.transform.root.GetComponentsInChildren<PupilLabs.GazeDataVisualizer>(true))
            {
                typeof(PupilLabs.GazeDataVisualizer).GetField("reference",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(visualizer,head);
                visualizer.RayVisible=false;visualizer.RaycastPointerVisible=false;visualizer.DoRaycast=true;
            }
        var check=provider.GetComponent<StudySpatialValidation>()??provider.gameObject.AddComponent<StudySpatialValidation>();
        check.provider=provider;check.headReference=head;
        check.maximumMeanErrorDegrees=3f;
        if(scene.path!=Entry && !string.IsNullOrEmpty(reportDirectory))
        {
            try{check.RestoreSameSessionReportForReview(reportDirectory);provider.calibrationId=MountConfigurationId();if(!string.IsNullOrEmpty(correctionPath))provider.RestoreCorrectionForSameSession(correctionPath);}
            catch(Exception e){Debug.LogWarning("[GazeFlow flow] Previous validation evidence was not restored: "+e.Message);}
        }
        if(scene.path!=Entry)
        {
            check.BindingReviewRequired=true;
            if(StudyEvents.Recording)StudyEvents.Record("gaze_head_binding_review_required",scene.name,"mapping_unverified","New scene head bound. No implicit approval transfer; review validation panel before collecting valid AOI hits.");
            UnityEditor.EditorApplication.ExecuteMenuItem("Tools/GazeFlow/Pre-room Gaze Validation");
            Debug.LogWarning("[GazeFlow flow] New scene head bound: AOI mapping needs explicit operator review in Gaze validation. Video and raw gaze continue; unapproved rays cannot become valid hits.");
            yield break;
        }
        var gates=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<HandTutorialNextGate>(true)).ToArray();
        if(gates.Length!=1){Debug.LogError("[GazeFlow flow] Expected one grabbing tutorial gate; automatic entry cancelled.");yield break;}
        var gate=gates[0];
        var placement=gate.GetComponent<TeleportInFront>();
        if(placement==null){Debug.LogError("[GazeFlow flow] Grabbing station placement is missing; entry stopped.");yield break;}
        gate.gameObject.SetActive(false);
        placement.UseFixedStation(head.position,head.forward,180f);
        placement.HoldForValidation(true);validationPlacement=placement;
        var actions=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<InteractableUnityEventWrapper>(true))
            .Where(w=>w.WhenSelect!=null && Enumerable.Range(0,w.WhenSelect.GetPersistentEventCount()).Any(i=>w.WhenSelect.GetPersistentTarget(i)==gate.gameObject && w.WhenSelect.GetPersistentMethodName(i)=="SetActive")).ToArray();
        if(actions.Length!=1){Debug.LogError("[GazeFlow flow] Authored grabbing entry action missing or ambiguous; not skipping to Room 1.");yield break;}
        // Reuse VR/menu setup but postpone the grabbing root (and its narration).
        var action=actions[0].WhenSelect;
        int activation=Enumerable.Range(0,action.GetPersistentEventCount()).Single(i=>action.GetPersistentTarget(i)==gate.gameObject&&action.GetPersistentMethodName(i)=="SetActive");
        var previous=action.GetPersistentListenerState(activation);
        action.SetPersistentListenerState(activation,UnityEventCallState.Off);
        try{action.Invoke();}finally{action.SetPersistentListenerState(activation,previous);}
        gate.gameObject.SetActive(false);
        validationTutorialRoot=gate.gameObject;
        check.automaticSequence=true;check.interTargetSeconds=2f;check.TutorialEntryContext=true;
        entryCheck=check;check.TutorialSkipRequested+=OnSkip;
        provider.calibrationId=MountConfigurationId();
        check.Begin(provider.calibrationId);
        UnityEditor.EditorApplication.ExecuteMenuItem("Tools/GazeFlow/Pre-room Gaze Validation");
        Vector3 anchorPosition=head.position,anchorForward=head.forward;
        bool anchored=false;
        while(!skipped)
        {
            if(!anchored && check.Running && !check.WaitingForSignal)
            {anchorPosition=head.position;anchorForward=head.forward;anchored=true;placement.UseFixedStation(anchorPosition,anchorForward,180f);}
            if(!check.Running && check.LastReport!=null && !check.LastReport.aborted && check.LastReport.purpose=="independent_live_verification")break;
            yield return null;
        }
        if(!anchored){anchorPosition=head.position;anchorForward=head.forward;}
        check.TutorialEntryContext=false;check.TutorialSkipRequested-=OnSkip;entryCheck=null;
        validationTutorialRoot=null;
        placement.HoldForValidation(false);validationPlacement=null;
        if(!skipped && Directory.Exists(check.LastOutput)){reportDirectory=check.LastOutput;correctionPath=provider.CorrectionPath;}
        // Fixed opposite station: turn around, never cover the forward validation plane.
        placement.UseFixedStation(anchorPosition,anchorForward,180f);
        gate.gameObject.SetActive(true);
        placement.TryTeleport(); // Explicitly restore content after an interrupted initial enable delay.
        if(!gate.gameObject.activeInHierarchy){Debug.LogError("[GazeFlow flow] Authored entry did not activate the grabbing tutorial.");yield break;}
        if(StudyEvents.Recording)StudyEvents.Record("grabbing_tutorial_started","grabbing_tutorial",skipped?"validation_explicitly_skipped":"validation_completed","Station 180 degrees opposite validation entry; original objectives retained; measurement is not operator approval.");
        Debug.Log("[GazeFlow flow] Turn around for grabbing. Original Next leads to LocomotionTutorial, then Room 1. Mapping approval remains separate.");
    }
    private void LateUpdate()
    {
        if(entryCheck!=null && entryCheck.TutorialEntryContext && validationPlacement!=null)validationPlacement.HoldForValidation(true);
        // Also hold across abort/retry, until explicit continuation. Do not allow another menu callback to reveal it over the points.
        if(entryCheck!=null && entryCheck.TutorialEntryContext && validationTutorialRoot!=null && validationTutorialRoot.activeSelf)
            validationTutorialRoot.SetActive(false);
    }
    private void OnSkip()=>skipped=true;
    private void OnDestroy(){if(entryCheck!=null)entryCheck.TutorialSkipRequested-=OnSkip;}
    private static string MountConfigurationId()
    {
        var file=Path.Combine(Application.persistentDataPath,"config.json");
        try{if(File.Exists(file)){using(var hash=SHA256.Create())return "mount-config-"+BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-","").Substring(0,12).ToLowerInvariant();}}
        catch(IOException){}catch(UnauthorizedAccessException){}
        return "mount-config-unavailable";
    }
}
#endif
