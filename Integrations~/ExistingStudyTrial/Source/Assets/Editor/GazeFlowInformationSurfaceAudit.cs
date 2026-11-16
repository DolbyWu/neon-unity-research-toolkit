using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NeonResearch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Read-only authored information-surface inventory; never saves scenes or starts Play.</summary>
public static class GazeFlowInformationSurfaceAudit
{
    public const string Output="Library/GazeFlowFlowChecks/information-surfaces.json";
    [InitializeOnLoadMethod] private static void Register()=>EditorApplication.delayCall+=RunOnce;
    private static void RunOnce(){if(!File.Exists(Output))Run();}
    [MenuItem("Tools/GazeFlow/Advanced/Diagnostics/Check Information Surface AOIs")]
    public static void Run()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03/Assets",StringComparison.OrdinalIgnoreCase))return;
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty){Debug.LogWarning("Save current scenes before the read-only information-surface audit.");return;}
        var records=new List<object>();
        foreach(string path in new[]{"Assets/Scenes/Main Menu AR.unity","Assets/Scenes/Tutorials/LocomotionTutorial.unity","Assets/Scenes/Rooms/Room 1.unity","Assets/Scenes/Puzzles/PuzzleSet 1.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid();
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
                {
                    if(!Regex.IsMatch(t.name,@"chalk|whiteboard|whitboard|blackboard|(^|[ _-])lid($|[ _-])|BoxLid",RegexOptions.IgnoreCase))continue;
                    var target=t.GetComponent<StudyResearchTarget>();
                    records.Add(new{scene=path,name=t.name,hierarchy=Hierarchy(t),global_id=GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),active=t.gameObject.activeInHierarchy,
                        target_id=target==null?"":target.targetId,group=target==null?"":target.targetGroup,
                        children=t.GetComponentsInChildren<Transform>(true).Select(c=>new{name=c.name,path=Hierarchy(c),colliders=c.GetComponents<Collider>().Select(v=>new{type=v.GetType().Name,enabled=v.enabled,trigger=v.isTrigger}).ToArray(),renderers=c.GetComponents<Renderer>().Select(v=>v.GetType().Name).ToArray(),components=c.GetComponents<MonoBehaviour>().Where(v=>v!=null).Select(v=>v.GetType().Name).ToArray(),target=c.GetComponent<StudyResearchTarget>()==null?"":c.GetComponent<StudyResearchTarget>().targetId}).ToArray()});
                }
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,Newtonsoft.Json.JsonConvert.SerializeObject(new{created_utc=DateTime.UtcNow,semantics="read_only_authored_inventory_no_scene_saves_no_device_test",records},Newtonsoft.Json.Formatting.Indented));
    }
    private static string Hierarchy(Transform value){var parts=new List<string>();for(var t=value;t!=null;t=t.parent)parts.Add(t.name+"["+t.GetSiblingIndex()+"]");parts.Reverse();return string.Join("/",parts);}
}
