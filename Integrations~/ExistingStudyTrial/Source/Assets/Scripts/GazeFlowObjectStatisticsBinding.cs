#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NeonResearch;
using Oculus.Interaction;
using UnityEngine;

/// <summary>Trial-only explicit statistics links; never changes collider, raw hit ID or gameplay ownership.</summary>
public sealed class GazeFlowObjectStatisticsBinding:MonoBehaviour
{
    private double nextScan;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(Application.isBatchMode||!Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03",StringComparison.OrdinalIgnoreCase))return;
        if(FindObjectsByType<GazeFlowObjectStatisticsBinding>(FindObjectsSortMode.None).Length>0)return;
        var go=new GameObject("GazeFlow object statistics binding (runtime)");DontDestroyOnLoad(go);go.AddComponent<GazeFlowObjectStatisticsBinding>();
    }
    public static bool BindVisualLeaf(StudyResearchTarget leaf,StudyResearchTarget owner)
    {
        if(!CanBindVisualLeaf(leaf,owner))return false;
        leaf.statisticsOwner=owner;leaf.statisticsOwnerRule="trial_meta_grabbable_visual_leaf_v1";return true;
    }
    public static bool CanBindVisualLeaf(StudyResearchTarget leaf,StudyResearchTarget owner)
    {
        if(leaf==null||owner==null||leaf==owner||leaf.statisticsOwner!=null||!leaf.transform.IsChildOf(owner.transform)||leaf.gameObject.scene!=owner.gameObject.scene)return false;
        // Project-reviewed visual containers only, not every descendant or same-named instance.
        if(leaf.name!="Visuals"&&leaf.name!=owner.name&&leaf.name!=owner.name+" Visuals")return false;
        for(var node=leaf.transform;node!=owner.transform;node=node.parent)
        {
            if(node.GetComponent<Grabbable>()!=null||node.GetComponent<Rigidbody>()!=null)return false;
            if(node.GetComponents<MonoBehaviour>().Any(c=>c!=null&&c.GetType().GetInterfaces().Any(i=>i.Name=="IInteractable"||i.Name=="IInteractableView"||i.Name=="IPointable")))return false;
        }
        return true;
    }
    private void Update()
    {
        if(Time.realtimeSinceStartupAsDouble<nextScan)return;nextScan=Time.realtimeSinceStartupAsDouble+.5;
        int linked=0;
        foreach(var target in FindObjectsByType<StudyResearchTarget>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(!target.enabled||target.statisticsOwner!=null)continue;
            var grab=target.GetComponentInParent<Grabbable>(true);
            var owner=StudySequence.ResolveResearchTarget(grab);
            if(BindVisualLeaf(target,owner))linked++;
        }
        if(linked>0&&StudyEvents.Recording)
        {
            StudyTargetCatalog.Snapshot();
            StudyEvents.Record("target_object_ownership_bound","visual-leaves","configuration_only","linked="+linked+"; trial_meta_grabbable_visual_leaf_v1; nearest actual Grabbable behavior ID; raw target/collider/physics unchanged");
        }
    }
}
#endif
