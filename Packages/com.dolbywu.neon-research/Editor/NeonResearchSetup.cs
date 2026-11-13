using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonResearch
{
    public sealed class NeonResearchSetup:EditorWindow
    {
        private NeonStudySettings settings;private Vector2 scroll;private string message="";
        [MenuItem("Tools/Neon Research/Setup and Recording Settings")]
        public static void Open()=>GetWindow<NeonResearchSetup>("Neon Research Setup");
        private void OnEnable(){settings=NeonStudySettings.Load();minSize=new Vector2(600,560);}
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("Neon Research Toolkit. Installation does not start recording. Choose a new data folder, enable Record on Unity Play and save. Leave Neon and media off for your first test. Validate your setup before collecting participant data.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                settings.recordOnPlay=EditorGUILayout.Toggle("Record on Unity Play (opt-in)",settings.recordOnPlay);
                settings.enabled=EditorGUILayout.Toggle("Receive Neon gaze (opt-in)",settings.enabled);
                settings.outputRoot=EditorGUILayout.TextField("Output folder",settings.outputRoot);
                if(GUILayout.Button("Choose data folder")){string selected=EditorUtility.OpenFolderPanel("Select an independent study data folder",StudyExportSession.OutputRoot,"");if(!string.IsNullOrEmpty(selected))settings.outputRoot=selected;}
                settings.condition=EditorGUILayout.TextField("Experimental condition",settings.condition);
                settings.companionIp=EditorGUILayout.TextField("Companion IPv4 (no port)",settings.companionIp);
                settings.companionApiPort=EditorGUILayout.IntField("Companion API port",settings.companionApiPort);
                settings.websocketPort=EditorGUILayout.IntField("Companion WebSocket port",settings.websocketPort);
                settings.archiveAutoOpen=EditorGUILayout.Toggle("Open folder + HTML after Stop",settings.archiveAutoOpen);
                EditorGUILayout.HelpBox("Enable Eye State on the Companion for pupil values. Check counts/quality, not just Connected. The raw receiver uses scene-image coordinates; 3D object mapping requires the optional Neon XR adapter, actual XR head binding and mapping verification. Decoder units follow the documented wire format; validate your own device/setup.",MessageType.Warning);
                settings.mediaRecordingEnabled=EditorGUILayout.Toggle("Enable optional media capture",settings.mediaRecordingEnabled);
                if(settings.mediaRecordingEnabled)
                {
                    EditorGUILayout.HelpBox("Requires separately installed Python and FFmpeg. Every session requests operator confirmation. Do not record other people or private desktop windows without permission. Optional external-process capture is NOT Asset Store approved.",MessageType.Warning);
                    settings.archivePythonExecutable=EditorGUILayout.TextField("Python executable",settings.archivePythonExecutable);
                    settings.mediaFfmpegExecutable=EditorGUILayout.TextField("FFmpeg executable",settings.mediaFfmpegExecutable);
                    settings.mediaRecordVideo=EditorGUILayout.Toggle("Record video",settings.mediaRecordVideo);
                    settings.mediaRecordMicrophone=EditorGUILayout.Toggle("Record microphone",settings.mediaRecordMicrophone);
                    settings.mediaMicrophoneDevice=EditorGUILayout.TextField("Microphone device name",settings.mediaMicrophoneDevice);
                    settings.mediaVideoSource=EditorGUILayout.Popup("Video source",settings.mediaVideoSource=="desktop"?1:0,new[]{"Window","Desktop"})==1?"desktop":"window";
                    settings.mediaWindowTitle=EditorGUILayout.TextField("Window title",settings.mediaWindowTitle);
                    settings.mediaAllowDesktopRecording=EditorGUILayout.Toggle("Allow desktop (explicit opt-in)",settings.mediaAllowDesktopRecording);
                    settings.mediaVideoWidth=EditorGUILayout.IntField("Capture width",settings.mediaVideoWidth);
                    settings.mediaVideoHeight=EditorGUILayout.IntField("Capture height",settings.mediaVideoHeight);
                    settings.mediaScreenX=EditorGUILayout.IntField("Desktop left",settings.mediaScreenX);
                    settings.mediaScreenY=EditorGUILayout.IntField("Desktop top",settings.mediaScreenY);
                    settings.mediaMaxMinutes=EditorGUILayout.FloatField("Maximum capture minutes",settings.mediaMaxMinutes);
                }
                if(GUILayout.Button("Save project settings"))
                {
                    try
                    {
                        if(settings.companionApiPort<1||settings.companionApiPort>65535||settings.websocketPort<1||settings.websocketPort>65535)throw new InvalidDataException("Ports must be between 1 and 65535.");
                        if(string.IsNullOrWhiteSpace(settings.outputRoot))throw new InvalidDataException("Choose a nonempty independent output folder.");
                        StudyAtomicFile.Write(NeonStudySettings.FilePath,JsonUtility.ToJson(settings,true));message="Saved. Click Unity Play for participant intake; no recording has started.";
                    }
                    catch(Exception e){message=e.Message;}
                }
            }
            EditorGUILayout.LabelField("Settings file",NeonStudySettings.FilePath,EditorStyles.wordWrappedLabel);
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
            if(GUILayout.Button("Open research monitor"))StudyLiveMonitor.Open();
            EditorGUILayout.EndScrollView();
        }
    }
}
