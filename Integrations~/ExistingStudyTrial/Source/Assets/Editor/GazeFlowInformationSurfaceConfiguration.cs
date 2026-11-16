using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using NeonResearch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checkpointed, copy-only configuration of three reviewed information surfaces.</summary>
public static class GazeFlowInformationSurfaceConfiguration
{
    private const string Trial="C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03";
    private const string ScenePath="Assets/Scenes/Puzzles/PuzzleSet 1.unity";
    public const string Report="Library/GazeFlowFlowChecks/information-surface-configuration-v1.json";
    private static readonly string[] Ids={
        "GlobalObjectId_V1-2-2dc329d16b640404899db6cb752f6d49-7127840849612022063-1274257297276855254",
        "GlobalObjectId_V1-2-2dc329d16b640404899db6cb752f6d49-2637929683018352692-1274257297276855254",
        "GlobalObjectId_V1-2-2dc329d16b640404899db6cb752f6d49-1004559869609765598-8244559667402106321"};
    private static readonly string[] Names={"ChalkBaord Front","Hub Puzzle_ Cooking Pot Whitboard","Protien Puzzle Box Lid"};
    private static readonly string[] Groups={"clue","puzzle_panel","puzzle_panel"};
    [InitializeOnLoadMethod]private static void Register()=>EditorApplication.delayCall+=ApplyOnce;
    private static void ApplyOnce(){if(!File.Exists(Report))Apply();}
    [MenuItem("Tools/GazeFlow/Advanced/Diagnostics/Configure Reviewed Information AOIs")]
    public static void Apply()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals(Trial+"/Assets",StringComparison.OrdinalIgnoreCase))return;
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty){Debug.LogWarning("Information AOIs not applied: save current scenes first.");return;}
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid();
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var objects=Ids.Select(id=>{if(!GlobalObjectId.TryParse(id,out var value))throw new InvalidOperationException("Invalid reviewed identity.");return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(value) as GameObject;}).ToArray();
            for(int i=0;i<objects.Length;i++)if(objects[i]==null||objects[i].scene!=scene||objects[i].name!=Names[i]||objects[i].GetComponents<Collider>().All(c=>c.isTrigger))throw new InvalidOperationException("Reviewed surface identity or own solid geometry changed: "+Names[i]);
            var before=NonResearchState(scene);
            string backup=Path.Combine("Library/GazeFlowFlowChecks","InformationSurfaceBackup-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);
            File.Copy(ScenePath,Path.Combine(backup,"PuzzleSet 1.unity"));File.Copy(ScenePath+".meta",Path.Combine(backup,"PuzzleSet 1.unity.meta"));
            var previous=objects.Select(g=>new{name=g.name,id=g.GetComponent<StudyResearchTarget>()?.targetId??"",group=g.GetComponent<StudyResearchTarget>()?.targetGroup??"",research_component=g.GetComponent<StudyResearchTarget>()==null?null:EditorJsonUtility.ToJson(g.GetComponent<StudyResearchTarget>())}).ToArray();
            for(int i=0;i<objects.Length;i++)
            {
                string newId="target-info-"+Stable(Ids[i]);var target=ConfigureSurface(objects[i],Groups[i],newId);
                target.identityOwnerGuid=AssetDatabase.AssetPathToGUID(ScenePath);EditorUtility.SetDirty(target);PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            if(before!=NonResearchState(scene))throw new InvalidOperationException("Non-research component state changed; scene was NOT saved.");
            var surfaces=objects.Select((g,i)=>{var t=g.GetComponent<StudyResearchTarget>();return new{name=g.name,id=t.targetId,group=t.targetGroup,original_group=previous[i].group,include_child_colliders=t.includeChildColliders,original_behavior_aliases=t.legacyObjectIds,linked_behavior_kinds=t.linkedBehaviorKinds,query=Probe(g.GetComponent<Collider>(),t)};}).ToArray();
            if(surfaces.Any(s=>s.query!="PASS"))throw new InvalidOperationException("Own-surface query check failed; scene was NOT saved. "+string.Join("; ",surfaces.Select(s=>s.name+"="+s.query)));
            var board=objects[1];var frame=board.GetComponentsInChildren<Collider>(true).First(c=>c.transform!=board.transform&&c.name=="Hub Puzzle_ Cooking Pot Whitboard Frame");
            string frameCheck=Probe(frame,null);if(frameCheck!="PASS")throw new InvalidOperationException("Decorative frame exclusion failed; scene was NOT saved.");
            if(before!=NonResearchState(scene))throw new InvalidOperationException("Diagnostic altered non-research state; scene was NOT saved.");
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Unable to save information AOI scene.");
            var report=new{created_utc=DateTime.UtcNow,scene=ScenePath,backup=Path.GetFullPath(backup),previous,surfaces,frame_exclusion=frameCheck,non_research_component_state_unchanged=true,hardware_acceptance="NOT TESTED",scope="Only three explicitly reviewed scene objects. Existing IDs/names and independent sockets retained; new groups are semantic AOI categories; no new collider, trigger or Rigidbody. No participant files changed."};
            Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));Debug.Log("[GazeFlow] Information AOIs configured and checkpointed: "+Report);
        }
        catch(Exception error){Debug.LogError("[GazeFlow] Information AOI configuration stopped: "+error);}
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    public static StudyResearchTarget ConfigureSurface(GameObject go,string group,string stableId)
    {
        if(go==null||!go.GetComponents<Collider>().Any(c=>!c.isTrigger))throw new InvalidOperationException("An existing own solid collider is required; physics will not be added or changed.");
        var target=go.GetComponent<StudyResearchTarget>();
        if(target==null)target=Undo.AddComponent<StudyResearchTarget>(go);
        else{if(!target.enabled||target.requiresSpawnIdentity||target.statisticsOwner!=null)throw new InvalidOperationException("Existing target requires explicit identity/ownership review.");Undo.RecordObject(target,"Configure information AOI metadata");}
        if(string.IsNullOrWhiteSpace(target.targetId))target.targetId=stableId;
        target.targetGroup=group;target.includeChildColliders=false;return target;
    }
    private static string Stable(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant().Substring(0,24);}
    private static string NonResearchState(Scene scene)
    {
        var records=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c!=null&&!(c is StudyResearchTarget)).Select(c=>GlobalObjectId.GetGlobalObjectIdSlow(c)+"|"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal);
        return Stable(string.Join("\n",records));
    }
    private static string Probe(Collider collider,StudyResearchTarget expected)
    {
        Physics.SyncTransforms();float reach=Mathf.Max(1,collider.bounds.size.magnitude*2);var center=collider.bounds.center;
        string diagnostic="no_own_collider_intersection; active="+collider.gameObject.activeInHierarchy+"; enabled="+collider.enabled;
        string ownPath=collider.name+"["+collider.transform.GetSiblingIndex()+"]";for(var p=collider.transform.parent;p!=null;p=p.parent)ownPath=p.name+"["+p.GetSiblingIndex()+"]/"+ownPath;
        foreach(var x in new[]{0f,-.8f,.8f})foreach(var y in new[]{0f,-.8f,.8f})foreach(var z in new[]{0f,-.8f,.8f})
        foreach(var axis in new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
        {
            // The panel centre can legitimately be covered by an independent socket.
            // Sample other authored surface positions; never disable those colliders.
            var sampleCenter=center+Vector3.Scale(collider.bounds.extents,new Vector3(x,y,z));
            if(!collider.Raycast(new Ray(sampleCenter+axis*reach,-axis),out var hit,reach*2))continue;
            var ray=new Ray(hit.point+hit.normal*.02f,-hit.normal);var result=StudyGazeTargetLog.Query(ray,~0,.04f);
            diagnostic=result.status+"; actual="+(result.target==null?"none":result.target.targetId)+"; expected_unique="+(expected==null||expected.HasUniqueId())+"; collider="+result.colliderPath;
            if(result.colliderPath==ownPath&&result.target==expected&&(expected==null?result.status=="non_target_occluder":result.status=="target_hit"))return "PASS";
        }
        return "FAIL_or_no_unobstructed_own_surface: "+diagnostic;
    }
}
