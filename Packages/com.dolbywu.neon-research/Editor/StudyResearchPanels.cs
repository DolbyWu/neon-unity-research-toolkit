using UnityEditor;
using UnityEngine;

namespace NeonResearch
{
    [InitializeOnLoad]
    public static class StudyResearchPanels
    {
        private static bool pending;
        static StudyResearchPanels()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode&&!Application.isBatchMode)
                {try{var settings=NeonStudySettings.Load();pending=settings.recordOnPlay&&settings.openResearchPanelsOnPlay;}catch{pending=false;}}
                if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)pending=false;
            };
            EditorApplication.update+=()=>
            {
                if(!pending||!EditorApplication.isPlaying||!StudyEvents.Recording)return;
                pending=false;StudyLiveMonitor.Open();LiveObservationWindow.Open();
            };
        }
    }
}
