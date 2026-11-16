using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Separate derived replay, preserving the original replay and all CSV files.</summary>
    public static class StudyObjectOwnershipReplay
    {
        [MenuItem("Tools/GazeFlow/Advanced/Analysis/Create Separate Object Statistics Replay")]
        public static void ChooseSession()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop recording first.");return;}
            string folder=EditorUtility.OpenFolderPanel("Choose a finished session folder","","");if(string.IsNullOrEmpty(folder))return;
            try{Debug.Log("Derived object replay: "+Build(folder));}catch(Exception error){Debug.LogError(error.Message);}
        }
        public static string Build(string folder)
        {
            var template=Resources.Load<TextAsset>("NeonResearchOfflineReplay");
            if(template==null)throw new FileNotFoundException("Replay template missing.");
            string name="study-replay-object-review-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+".html";
            return StudyOfflineReplay.BuildNamedWithTemplate(folder,template.text,name);
        }
        public static void BuildFromCommandLine()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--gazeFlowOwnershipSession");
            if(index<0||index+1>=args.Length)throw new ArgumentException("Provide --gazeFlowOwnershipSession with a closed local session folder.");
            Debug.Log("Derived object replay: "+Build(args[index+1]));
        }
    }
}
