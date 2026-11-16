using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
public static class GazeFlowHierarchyOrganizer
{
    private const string ArchiveName="[GazeFlow] Archived test previews (inactive)";
    private static readonly Regex PreviewName=new Regex("^(GazeFlow|Banana) validation target [1-9] \\(not an AOI\\)$");
    [InitializeOnLoadMethod] private static void Register()=>EditorApplication.delayCall+=Organize;
    [MenuItem("Tools/GazeFlow/Advanced/Diagnostics/Organize Generated Hierarchy")]
    public static void Organize()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03/Assets",StringComparison.OrdinalIgnoreCase))return;
        bool GeneratedOnly(GameObject go)=>PreviewName.IsMatch(go.name)&&!EditorUtility.IsPersistent(go)&&!PrefabUtility.IsPartOfPrefabInstance(go)&&go.GetComponent<MeshFilter>()!=null&&go.GetComponent<MeshRenderer>()!=null&&go.GetComponentsInChildren<Component>(true).All(c=>c!=null&&(c is Transform||c is MeshFilter||c is MeshRenderer||c is SphereCollider))&&go.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled);
        var unsaved=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s=>s.IsValid()&&s.isLoaded&&string.IsNullOrEmpty(s.path)).ToArray();
        var scene=unsaved.FirstOrDefault(s=>s.GetRootGameObjects().Any(GeneratedOnly));
        if(!scene.IsValid())scene=unsaved.FirstOrDefault(s=>s.GetRootGameObjects().Any(g=>g.name==ArchiveName));
        if(!scene.IsValid())return;
        var candidates=scene.GetRootGameObjects().Where(GeneratedOnly).ToArray();
        var archive=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==ArchiveName&&!g.activeSelf&&(g.hideFlags&HideFlags.DontSaveInEditor)!=0);
        if(candidates.Length>0)
        {
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organize generated GazeFlow previews");
            if(archive==null){archive=new GameObject(ArchiveName);archive.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(archive,scene);Undo.RegisterCreatedObjectUndo(archive,"Create generated-preview archive");Undo.RecordObject(archive,"Deactivate generated previews");archive.SetActive(false);}
            foreach(var cue in candidates)Undo.SetTransformParent(cue.transform,archive.transform,"Group generated test preview");
            Undo.CollapseUndoOperations(undo);EditorApplication.RepaintHierarchyWindow();
        }
        Directory.CreateDirectory("Library/GazeFlowFlowChecks");File.WriteAllText("Library/GazeFlowFlowChecks/hierarchy-organization.txt","Newly grouped: "+candidates.Length+"; archived generated previews: "+(archive==null?0:archive.transform.childCount)+". Disabled, not deleted; Edit > Undo restores this grouping. Only unnamed trial working scene; no scene asset saved.\n");
    }
}
