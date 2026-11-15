using System;
using UnityEditor;
using UnityEngine;

namespace NeonResearch
{
    public sealed class GazeFlowWelcomeWindow:EditorWindow
    {
        private string message="";
        [MenuItem("Tools/GazeFlow/Start Here",false,0)]
        public static void Open(){var window=GetWindow<GazeFlowWelcomeWindow>("GazeFlow");window.minSize=new Vector2(480,420);}
        private void OnGUI()
        {
            EditorGUILayout.LabelField("GazeFlow · Neon Research Toolkit",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Windows Unity Editor beta. Configure → Record → Observe → Review. Installation does not start acquisition or media. Your project supplies its own XR rig and task.",MessageType.Info);
            if(GUILayout.Button("1 · Recording and media settings",GUILayout.Height(38)))NeonResearchSetup.Open();
            if(GUILayout.Button("2 · Optional Neon XR / 3D gaze setup",GUILayout.Height(38)))
            {
                var type=Type.GetType("NeonResearch.NeonXR.Editor.NeonXRSetupWindow, GazeFlow.NeonXR.Editor");
                if(type==null)message="Install official Neon XR Core and the matching GazeFlow Neon XR adapter first. Raw eye recording and observation/review do not require the adapter.";
                else type.GetMethod("Open")?.Invoke(null,null);
            }
            if(GUILayout.Button("3 · Participant archive"))StudyArchiveEditor.Open();
            if(GUILayout.Button("4 · Live monitor"))StudyLiveMonitor.Open();
            if(GUILayout.Button("5 · Live Observation Marker",GUILayout.Height(38)))LiveObservationWindow.Open();
            EditorGUILayout.HelpBox("Start/stop recording through the normal Play confirmation flow. The observer button marks a time interval; it does not start capture. After Stop, review the generated HTML, annotate with the participant, export JSON and reload it to resume.",MessageType.None);
            EditorGUILayout.LabelField("Raw eye data, manual adjustment, observed AOI hits and measured accuracy are separate. No fixation or cognitive-state inference is claimed.",EditorStyles.wordWrappedMiniLabel);
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
        }
    }
}
