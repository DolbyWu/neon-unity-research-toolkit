using System;
using System.IO;
using System.Linq;
using NeonResearch;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class GazeFlowTutorialObjectAudit
{
 [InitializeOnLoadMethod] private static void Register()=>EditorApplication.delayCall+=Run;
 [MenuItem("Tools/GazeFlow/Inspect Tutorial Calibration Bindings")]
 public static void Run()
 {
  if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03/Assets",StringComparison.OrdinalIgnoreCase))return;
  const string file="Library/GazeFlowFlowChecks/tutorial-object-bindings.json";if(File.Exists(file))return;
  var existing=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Menu AR.unity");bool opened=!existing.IsValid();
  var scene=opened?EditorSceneManager.OpenScene("Assets/Scenes/Main Menu AR.unity",OpenSceneMode.Additive):existing;
  try{
   string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
   var targets=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StudyResearchTarget>(true));
   var rows=targets.Select(t=>new{path=PathOf(t.transform),id=t.targetId,label=t.DisplayName,group=t.targetGroup,active=t.gameObject.activeSelf,colliders=t.GetComponentsInChildren<Collider>(true).Select(c=>new{name=c.name,trigger=c.isTrigger,layer=c.gameObject.layer,type=c.GetType().Name}).ToArray()}).ToArray();
   Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
 }
}
