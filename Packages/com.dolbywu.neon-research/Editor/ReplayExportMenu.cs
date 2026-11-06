using System;
using UnityEditor;
using UnityEngine;

namespace NeonResearch
{
    public static class ReplayExportMenu
    {
        [MenuItem("Tools/Neon Research/Regenerate Finished Session HTML")]
        public static void Open()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorUtility.DisplayDialog("Offline replay","Stop Play first.","OK");return;}
            string folder=EditorUtility.OpenFolderPanel("Select one completed session",StudyExportSession.OutputRoot,"");
            if(string.IsNullOrEmpty(folder))return;
            try{Application.OpenURL(new Uri(StudyOfflineReplay.Build(folder)).AbsoluteUri);}
            catch(Exception e){EditorUtility.DisplayDialog("Offline replay",e.Message,"OK");}
        }
    }
}
