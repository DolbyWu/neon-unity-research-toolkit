using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor-only startup override in the isolated trial; never opens or saves the working scene.</summary>
public static class GazeFlowTutorialPlayEntry
{
    public const string EntryScene="Assets/Scenes/Main Menu AR.unity";
    private const string Trial="C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03";
    private static bool IsTrial => Path.GetFullPath(Path.GetDirectoryName(Application.dataPath)).Replace('\\','/').Equals(Trial,StringComparison.OrdinalIgnoreCase);

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        if(!IsTrial || Application.isBatchMode)return;
        EditorApplication.delayCall+=Configure;
        EditorApplication.delayCall+=CheckLayout;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingEditMode)Configure();};
    }
    [MenuItem("Tools/GazeFlow/Use Tutorial Entry for Play")]
    public static void Configure()
    {
        if(!IsTrial || EditorApplication.isPlaying)return;
        var entry=AssetDatabase.LoadAssetAtPath<SceneAsset>(EntryScene);
        if(entry==null){Debug.LogError("[GazeFlow flow] Grabbing tutorial entry scene missing.");return;}
        EditorSceneManager.playModeStartScene=entry;
        Directory.CreateDirectory("Library/GazeFlowFlowChecks");
        File.WriteAllText("Library/GazeFlowFlowChecks/play-entry.txt",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        Debug.Log("[GazeFlow flow] Play starts with five points -> turn around for grabbing -> LocomotionTutorial -> Room 1. Working scene is not changed.");
    }
    [MenuItem("Tools/GazeFlow/Check Tutorial Entry Layout")]
    public static void CheckLayout()
    {
        if(!IsTrial||Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string path="Library/GazeFlowFlowChecks/opposite-layout-check.txt";Directory.CreateDirectory(Path.GetDirectoryName(path));
        try
        {
            var script=AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath("1464721f2283eb14e94a33e812b47be4"));
            if(script==null||script.GetClass()!=typeof(Oculus.Interaction.InteractableUnityEventWrapper))throw new InvalidOperationException("Authored Start wrapper type mismatch.");
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("EscapeRoom.ReflectiveAgent.Tests.TutorialValidationLayoutTests")).FirstOrDefault(t=>t!=null);
            if(type==null)throw new InvalidOperationException("Layout test assembly is not loaded; checks not run.");
            var instance=Activator.CreateInstance(type);
            var methods=type.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(m=>m.GetParameters().Length==0).ToArray();
            foreach(var method in methods)method.Invoke(instance,null);
            File.WriteAllText(path,"PASS: "+methods.Length+" synthetic layout/visibility checks; authored Start wrapper type checked. Real headset visibility is NOT tested.\n");
            Debug.Log("[GazeFlow flow] Opposite layout and renderer hold checks passed: "+methods.Length+". Headset check still required.");
        }
        catch(Exception error){File.WriteAllText(path,"FAIL: "+(error.InnerException??error).Message);Debug.LogError("[GazeFlow flow] Layout check: "+(error.InnerException??error).Message);}
    }
}
