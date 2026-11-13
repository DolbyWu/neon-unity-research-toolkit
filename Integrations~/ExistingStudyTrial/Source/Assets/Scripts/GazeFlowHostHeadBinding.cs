using System.Reflection;
using NeonResearch;
using NeonResearch.NeonXR;
using UnityEngine;
/// <summary>Late-instantiated player binding, using the SAME explicitly serialized head as the legacy logger.</summary>
public sealed class GazeFlowHostHeadBinding:MonoBehaviour
{
    public NeonXRResearchProvider provider;
    [Tooltip("Calibration preview only. Showing the official pointer does NOT verify mapping or enable research hits.")]
    public bool showCalibrationPointer = true;
    private void Update()
    {
        if(provider==null||provider.headReference!=null)return;
        foreach(var logger in FindObjectsByType<PlayerDataLogger>(FindObjectsSortMode.None))
        {
            var head=typeof(PlayerDataLogger).GetField("HeadReference",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(logger) as Transform;
            if(head==null)continue;
            var source=head.GetComponent<StudyHeadPoseSource>();
            if(source==null)source=head.gameObject.AddComponent<StudyHeadPoseSource>();source.head=head;
            provider.headReference=head;
            foreach(var visualizer in provider.neon.transform.root.GetComponentsInChildren<PupilLabs.GazeDataVisualizer>(true))
            {
                typeof(PupilLabs.GazeDataVisualizer).GetField("reference",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(visualizer,head);
                visualizer.RayVisible = showCalibrationPointer;
                visualizer.DoRaycast = showCalibrationPointer;
                visualizer.RaycastPointerVisible = showCalibrationPointer;
            }
            Debug.Log("[GazeFlowTrial] Bound spatial provider to explicit PlayerDataLogger.HeadReference: "+head.name+". Mapping remains unverified.");
            break;
        }
    }
}
