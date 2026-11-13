using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class GazeFlowCorrectionChecks
{
    [InitializeOnLoadMethod] private static void Register()=>EditorApplication.delayCall+=Run;
    [MenuItem("Tools/GazeFlow/Check Correction and Tutorial CSV")]
    public static void Run()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.Replace('\\','/').Equals("C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03/Assets",StringComparison.OrdinalIgnoreCase))return;
        string file="Library/GazeFlowFlowChecks/correction-integration.txt";Directory.CreateDirectory(Path.GetDirectoryName(file));int count=0;
        try
        {
            foreach(string name in new[]{"EscapeRoom.ReflectiveAgent.Tests.AoiCorrectionIntegrationTests","EscapeRoom.ReflectiveAgent.Tests.AoiBindingApprovalTests","EscapeRoom.ReflectiveAgent.Tests.AoiAutomaticTransitionTests"})
            {
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);if(type==null)throw new InvalidOperationException("Test assembly not loaded: "+name);
                var obj=Activator.CreateInstance(type);
                foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly).Where(m=>m.GetParameters().Length==0)){method.Invoke(obj,null);count++;}
            }
            File.WriteAllText(file,"PASS: "+count+" synthetic trial checks. Correction requires independent verification; tutorial grabs/releases reach CSV without eye approval. No real hardware recorded.\n");
            Debug.Log("[GazeFlow checks] Correction integration passed: "+count);
        }
        catch(Exception error){File.WriteAllText(file,"FAIL after "+count+": "+error.GetBaseException());Debug.LogError("[GazeFlow checks] "+error.GetBaseException());}
    }
}
