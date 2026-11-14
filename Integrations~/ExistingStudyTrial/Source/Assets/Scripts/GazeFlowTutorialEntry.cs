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
    private static StudyGazeTargetRecorder sessionRecorder;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHooks(){SceneManager.sceneLoaded-=Loaded;sessionRecorder=null;}
    // The official singleton survives scene unload; scene-authored recorder references may not.
    public static StudyGazeTargetRecorder EnsureSessionRecorder(StudyGazeRayProvider liveProvider)
    {
        if(liveProvider==null||!liveProvider.isActiveAndEnabled)throw new InvalidOperationException("An active live research provider is required.");
        var active=FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Where(r=>r.isActiveAndEnabled).ToArray();
        var chosen=sessionRecorder;
        if(chosen==null){var matching=active.Where(r=>r.provider==liveProvider).ToArray();if(matching.Length==1)chosen=matching[0];else if(active.Length==1)chosen=active[0];else if(active.Length>0)throw new InvalidOperationException("Ambiguous AOI recorders; no objects were removed.");}
        if(active.Any(r=>r!=chosen&&r.gameObject.name!="GazeFlow Research Target Recorder"))throw new InvalidOperationException("An unrelated AOI recorder is active; inspect it before continuing.");
        if(chosen!=null&&chosen.transform.parent!=null)throw new InvalidOperationException("Session recorder must be a separate root; gameplay hierarchies are not moved.");
        if(chosen==null){var go=new GameObject("GazeFlow Research Target Recorder");chosen=go.AddComponent<StudyGazeTargetRecorder>();chosen.maxDistance=30;chosen.collisionMask=~0;chosen.staleAfterSeconds=.1f;}
        foreach(var extra in active.Where(r=>r!=chosen)){extra.enabled=false;if(StudyEvents.Recording)StudyEvents.Record("aoi_duplicate_scene_recorder_disabled",extra.gameObject.scene.name,"session_recorder_reused","Duplicate initialization recorder disabled at runtime, not deleted.");}
        sessionRecorder=chosen;if(Application.isPlaying)DontDestroyOnLoad(chosen.gameObject);
        chosen.enabled=true;chosen.BindProvider(liveProvider);
        if(StudyEvents.Recording)StudyEvents.Record("aoi_session_recorder_bound",liveProvider.gameObject.scene.name,"awaiting_fresh_callbacks","Single session recorder; no stale ray replay or implicit mapping approval.");
        return chosen;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if(Application.isBatchMode || !Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/').Equals(Trial,StringComparison.OrdinalIgnoreCase))return;
        SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;
    }
    private static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.path!=Entry && scene.name!="LocomotionTutorial" && scene.name!="Room 1")return;
        if(scene.path==Entry)PrepareDirectGrabEntry(scene);
        if(StudyEvents.Recording)StudyEvents.Record("experiment_phase_start",scene.name,"scene_loaded","Editor tutorial flow; not participant interaction.");
        var runner=new GameObject("GazeFlow tutorial entry (runtime)");SceneManager.MoveGameObjectToScene(runner,scene);runner.AddComponent<GazeFlowTutorialEntry>();
        if(scene.name!="Room 1")runner.AddComponent<GazeFlowTutorialGrabRecorder>();
    }
    public static void PrepareDirectGrabEntry(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
        {
            // Arm before the authored Start transition, including initially inactive tutorial roots.
            foreach(var gate in root.GetComponentsInChildren<HandTutorialNextGate>(true))gate.RequireOperatorStart();
            // Disable only the legacy VR ID entry, never the XR rig or grabbing station.
            if(root.name=="USER ID Variant Menu" && root.GetComponentInChildren<OVRCameraRig>(true)==null && root.GetComponentInChildren<HandTutorialNextGate>(true)==null)root.SetActive(false);
            foreach(var node in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Welcome Audio"))
            {
                foreach(var audio in node.GetComponentsInChildren<AudioSource>(true)){audio.Stop();audio.playOnAwake=false;}
                node.gameObject.SetActive(false);
            }
        }
        // Scene-loaded runs before Start: the obsolete welcome coroutine never begins.
        // Session identity remains owned by the desktop participant archive, not a second ID.
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
        try{EnsureSessionRecorder(provider);provider.RefreshSourceSubscription();}catch(Exception error){Debug.LogError("[GazeFlow flow] AOI continuity: "+error.Message);yield break;}
        var source=head.GetComponent<StudyHeadPoseSource>()??head.gameObject.AddComponent<StudyHeadPoseSource>();source.head=head;
        provider.RebindDesktopHead(head,MountConfigurationId());
        if(provider.neon!=null)
            foreach(var visualizer in provider.neon.transform.root.GetComponentsInChildren<PupilLabs.GazeDataVisualizer>(true))
            {
                typeof(PupilLabs.GazeDataVisualizer).GetField("reference",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(visualizer,head);
                visualizer.RayVisible=false;visualizer.RaycastPointerVisible=false;visualizer.DoRaycast=true;
            }
        var check=provider.GetComponent<StudySpatialValidation>()??provider.gameObject.AddComponent<StudySpatialValidation>();
        check.provider=provider;check.headReference=head;check.maximumMeanErrorDegrees=3f;
        check.TutorialEntryContext=false;check.BindingReviewRequired=false;
        if(StudyEvents.Recording)StudyEvents.Record("official_gaze_head_bound",scene.name,"spatial_accuracy_not_measured","Official Neon XR mapping; optional same-session desktop offset retained when configuration matches. Accuracy approval cleared; no tutorial gate.");
        if(scene.path!=Entry)yield break;
        var gates=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<HandTutorialNextGate>(true)).ToArray();
        if(gates.Length!=1){Debug.LogError("[GazeFlow flow] Expected one original grabbing tutorial gate.");yield break;}
        var gate=gates[0];var placement=gate.GetComponent<TeleportInFront>();
        if(placement==null){Debug.LogError("[GazeFlow flow] Grabbing station placement missing.");yield break;}
        placement.HoldForValidation(false);gate.SetResearchCalibrationHold(false);
        placement.UseFixedStation(head.position,head.forward,0f);
        var actions=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<InteractableUnityEventWrapper>(true))
            .Where(w=>w.WhenSelect!=null&&Enumerable.Range(0,w.WhenSelect.GetPersistentEventCount()).Any(i=>w.WhenSelect.GetPersistentTarget(i)==gate.gameObject&&w.WhenSelect.GetPersistentMethodName(i)=="SetActive")).ToArray();
        if(actions.Length!=1){Debug.LogError("[GazeFlow flow] Original tutorial Start transition missing or ambiguous.");yield break;}
        actions[0].WhenSelect.Invoke();placement.TryTeleport();
        // Available on demand from the desktop panel, never started automatically or tied to Next.
        var flow=gameObject.AddComponent<StudyObjectCalibrationWorkflow>();flow.provider=provider;
        var ids=new[]{"target-c005dcab8aa3582f25e5b3e0","target-89f98ce317eb165fd28ddae1","target-6d5b806d43164bf1f46f0b5f"};
        var targets=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudyResearchTarget>(true)).Where(t=>t.isActiveAndEnabled).ToArray();
        for(int i=0;i<ids.Length;i++){var matches=targets.Where(t=>t.targetId==ids[i]).ToArray();if(matches.Length==1)flow.objects[i]=matches[0];}
        if(flow.objects[0]!=null)
        {
            var reference=gameObject.AddComponent<GazeFlowTutorialReferenceDot>();
            reference.Bind(flow.objects[0],head,gate,check,sessionRecorder.collisionMask.value,sessionRecorder.maxDistance);
        }
        else Debug.LogWarning("[GazeFlow flow] Unique tutorial chess target not found; no visual reference was guessed.");
        if(StudyEvents.Recording)StudyEvents.Record("tutorial_preparation_waiting","grabbing_tutorial","awaiting_operator_start","Station visible and recording continues. Optional gaze adjustment; click Start tutorial in Desktop Gaze Adjustment to begin narration.");
    }
    private static string MountConfigurationId()
    {
        var file=Path.Combine(Application.persistentDataPath,"config.json");
        try{if(File.Exists(file)){using(var hash=SHA256.Create())return "mount-config-"+BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-","").Substring(0,12).ToLowerInvariant();}}
        catch(IOException){}catch(UnauthorizedAccessException){}
        return "mount-config-unavailable";
    }
}
#endif
