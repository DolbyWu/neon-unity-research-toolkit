using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NeonResearch;
using Oculus.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Trial-only read-only scene audit. Never saves assets, changes physics or records hardware.</summary>
public static class GazeFlowObjectOwnershipCoverage
{
    [InitializeOnLoadMethod] private static void Register()=>EditorApplication.delayCall+=RunOnce;
    private const string Report="Library/GazeFlowFlowChecks/object-ownership-coverage.json";
    private static void RunOnce(){if(!File.Exists(Report))Run();}
    [MenuItem("Tools/GazeFlow/Advanced/Diagnostics/Check Object Statistics Ownership")]
    public static void Run()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03/Assets",StringComparison.OrdinalIgnoreCase))return;
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty){Debug.LogWarning("Save current scenes before the read-only ownership audit.");return;}
        var records=new List<object>();
        foreach(string path in new[]{"Assets/Scenes/Main Menu AR.unity","Assets/Scenes/Tutorials/LocomotionTutorial.unity","Assets/Scenes/Rooms/Room 1.unity","Assets/Scenes/Puzzles/PuzzleSet 1.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var leaf in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudyResearchTarget>(true)))
                {
                    var grab=leaf.GetComponentInParent<Grabbable>(true);
                    var owner=grab==null?null:grab.GetComponentsInParent<StudyResearchTarget>(true).FirstOrDefault(t=>t.enabled&&!string.IsNullOrWhiteSpace(t.targetId));
                    bool eligible=GazeFlowObjectStatisticsBinding.CanBindVisualLeaf(leaf,owner);
                    records.Add(new{scene=path,leaf_id=leaf.targetId,leaf_name=leaf.name,owner_id=owner==null?"":owner.targetId,owner_name=owner==null?"":owner.name,active=leaf.isActiveAndEnabled,eligible,semantics="read_only_authored_component_check_NOT_gaze_evidence; runtime requires unique active behavior owner"});
                }
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));Debug.Log("[GazeFlow] Read-only object ownership audit saved: "+Report);
    }
}
