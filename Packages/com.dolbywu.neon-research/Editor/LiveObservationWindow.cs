using System;
using UnityEditor;
using UnityEngine;
namespace NeonResearch
{
    public sealed class LiveObservationWindow:EditorWindow
    {
        private string observer="R001",message="Click once at the start, once at the end. Notes open AFTER the end timestamp is saved.";
        private Vector2 scroll;
        [InitializeOnLoadMethod] private static void Register()
        {
            if(Application.isBatchMode)return;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Open;};
        }
        [MenuItem("Tools/GazeFlow/Live Observation Marker")]
        public static void Open(){var w=GetWindow<LiveObservationWindow>("Observation Marker");w.minSize=new Vector2(430,300);w.Show();}
        private void OnInspectorUpdate()=>Repaint();
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(message,MessageType.Info);
            if(!EditorApplication.isPlaying||!StudyEvents.Recording)
            {
                EditorGUILayout.HelpBox("Start Play and finish participant / recording confirmation first. This window does not start recording.",MessageType.Info);
                using(new EditorGUI.DisabledScope(true))GUILayout.Button("START OBSERVATION (recording required)",GUILayout.Height(62));
                return;
            }
            try
            {
                var active=StudyLiveObservations.Active;
                using(new EditorGUI.DisabledScope(active!=null))observer=EditorGUILayout.TextField("Anonymous observer ID",observer);
                EditorGUILayout.LabelField("Intervals: "+StudyLiveObservations.Records.Count+" / 10");
                if(active!=null)EditorGUILayout.LabelField("Recording interval: "+(StudyExportSession.Seconds-active.start_s).ToString("F1")+" s");
                using(new EditorGUI.DisabledScope(active==null&&StudyLiveObservations.Records.Count>=10))
                    if(GUILayout.Button(active==null?"START OBSERVATION":"END OBSERVATION & WRITE NOTE",GUILayout.Height(62)))
                    {
                        if(active==null){StudyLiveObservations.Begin(observer);message="Interval started. Click END when the observed episode ends.";}
                        else{var ended=StudyLiveObservations.End();ObservationNoteWindow.Open(ended);message="End timestamp saved. Note-writing time is NOT included. Game continues.";}
                    }
                scroll=EditorGUILayout.BeginScrollView(scroll);
                foreach(var r in StudyLiveObservations.Records)
                {EditorGUILayout.LabelField(r.start_s.ToString("F2")+" – "+(r.end_s.HasValue?r.end_s.Value.ToString("F2"):"ongoing")+" s · "+r.label,EditorStyles.wordWrappedLabel);
                 if(r.end_s.HasValue&&GUILayout.Button("Write / edit observer note · "+r.observation_id.Substring(0,12)))ObservationNoteWindow.Open(r);}
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox("Observer interpretation only, not a confirmed cognitive state. Player explanations and 1–5 confusion ratings are added separately in the offline HTML. Raw interval files are saved automatically in this session.",MessageType.Info);
            }catch(Exception e){message=e.Message;}
        }
    }
    public sealed class ObservationNoteWindow:EditorWindow
    {
        private string session,id,label,notes,message="";
        public static void Open(StudyLiveObservations.Record record)
        {
            var w=GetWindow<ObservationNoteWindow>("Observer note");w.session=StudyExportSession.SessionId;w.id=record.observation_id;w.label=record.label;w.notes=record.notes;
            w.minSize=new Vector2(450,330);w.ShowUtility();w.Focus();
        }
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Interval timing is already saved. Writing this note does not extend the interval or pause the game.",MessageType.Info);
            label=EditorGUILayout.TextField("Observation label",label??"");EditorGUILayout.LabelField("What did you observe? Separate visible behavior from interpretation.");
            notes=EditorGUILayout.TextArea(notes??"",GUILayout.MinHeight(160));
            EditorGUILayout.LabelField(message,EditorStyles.wordWrappedLabel);
            using(new EditorGUI.DisabledScope(!StudyEvents.Recording||session!=StudyExportSession.SessionId))
                if(GUILayout.Button("Save observer note",GUILayout.Height(36)))
                {try{StudyLiveObservations.SaveNote(id,label,notes);Close();}catch(Exception e){message=e.Message;}}
            if(!StudyEvents.Recording||session!=StudyExportSession.SessionId)EditorGUILayout.HelpBox("This session has ended. The interval remains saved; add further interpretation in its HTML replay.",MessageType.Warning);
        }
    }
}
