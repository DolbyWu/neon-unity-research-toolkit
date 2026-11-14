using System;
using UnityEditor;
using UnityEngine;
namespace NeonResearch
{
    public sealed class LiveObservationWindow:EditorWindow
    {
        private string observer="R001",message="Click once at the start, once at the end. Notes open AFTER the end timestamp is saved.";
        private Vector2 scroll;
        private string displayedSession,participant="Selected in participant archive";
        private GUIStyle titleStyle,timerStyle,captionStyle,buttonText,outlineStyle,fillStyle,shadowStyle;
        private Texture2D outlineTexture,fillTexture,shadowTexture;
        private bool actionPressed;
        public static Color ButtonFaceColor => new Color32(255,220,50,255);
        public static string ActionLabel(bool recordingAvailable,bool marking,int count) => !recordingAvailable?"RECORDING REQUIRED":marking?"END OBSERVATION":count>=StudyLiveObservations.Limit?"10 INTERVALS SAVED":count>0?"START NEXT OBSERVATION":"START OBSERVATION";
        public static string ElapsedLabel(double seconds)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)seconds=0;
            var time=TimeSpan.FromSeconds(Math.Floor(seconds));
            return time.TotalHours>=1?((int)time.TotalHours).ToString("00")+":"+time.Minutes.ToString("00")+":"+time.Seconds.ToString("00"):((int)time.TotalMinutes).ToString("00")+":"+time.Seconds.ToString("00");
        }
        [MenuItem("Tools/GazeFlow/Live Observation Marker")]
        public static void Open(){var w=GetWindow<LiveObservationWindow>("Observation Marker");w.minSize=new Vector2(430,470);w.Show();}
        private void OnInspectorUpdate()=>Repaint();
        private void OnLostFocus(){actionPressed=false;Repaint();}
        private void OnDisable()
        {
            // Only textures owned by this window; closing never ends an interval or a session.
            if(outlineTexture!=null)DestroyImmediate(outlineTexture);
            if(fillTexture!=null)DestroyImmediate(fillTexture);
            if(shadowTexture!=null)DestroyImmediate(shadowTexture);
            outlineTexture=fillTexture=shadowTexture=null;outlineStyle=fillStyle=shadowStyle=null;
        }
        private static Texture2D RoundedTexture(Color color)
        {
            const int size=32;const float radius=10;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float px=x+.5f,py=y+.5f,cx=Mathf.Clamp(px,radius,size-radius),cy=Mathf.Clamp(py,radius,size-radius);
                var pixel=color;pixel.a*=Mathf.Clamp01(radius-Mathf.Sqrt((px-cx)*(px-cx)+(py-cy)*(py-cy))+.5f);pixels[y*size+x]=pixel;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        private static GUIStyle ShapeStyle(Texture2D texture)=>new GUIStyle{normal={background=texture},border=new RectOffset(11,11,11,11)};
        private void EnsureStyles()
        {
            if(titleStyle==null)
            {
                titleStyle=new GUIStyle(EditorStyles.boldLabel){fontSize=16};
                timerStyle=new GUIStyle(EditorStyles.boldLabel){fontSize=32,alignment=TextAnchor.MiddleCenter};
                captionStyle=new GUIStyle(EditorStyles.centeredGreyMiniLabel){wordWrap=true};
                buttonText=new GUIStyle(EditorStyles.boldLabel){fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=true,normal={textColor=new Color32(32,26,6,255)}};
            }
            if(fillTexture!=null)return;
            outlineTexture=RoundedTexture(new Color32(59,49,11,255));fillTexture=RoundedTexture(ButtonFaceColor);shadowTexture=RoundedTexture(new Color32(134,107,18,255));
            outlineStyle=ShapeStyle(outlineTexture);fillStyle=ShapeStyle(fillTexture);shadowStyle=ShapeStyle(shadowTexture);
        }
        private bool DrawActionButton(string label,bool interactable,bool marking)
        {
            var area=GUILayoutUtility.GetRect(GUIContent.none,GUIStyle.none,GUILayout.Height(94),GUILayout.ExpandWidth(true));
            var face=new Rect(area.x,area.y+2,area.width,84);
            bool allowed=GUI.enabled&&interactable;
            var current=Event.current;
            if(current.rawType==EventType.MouseDown&&current.button==0&&allowed&&face.Contains(current.mousePosition))actionPressed=true;
            if(current.rawType==EventType.MouseUp||!allowed)actionPressed=false;
            bool clicked;
            // Native button input keeps the normal recording/limit guard. Skin paints no pixels.
            using(new EditorGUI.DisabledScope(!interactable))clicked=GUI.Button(face,GUIContent.none,GUIStyle.none);
            if(current.type==EventType.Repaint)
            {
                bool oldEnabled=GUI.enabled;var oldColor=GUI.color;var oldBackground=GUI.backgroundColor;var oldContent=GUI.contentColor;
                try
                {
                    // Paint only: no interactive control inside this scope. Unity's disabled/Play
                    // tint and skin states must not gray the approved yellow face or dark text.
                    GUI.enabled=true;GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;
                    shadowStyle.Draw(new Rect(face.x,face.y+6,face.width,face.height),false,false,false,false);
                    float offset=actionPressed&&face.Contains(current.mousePosition)?4:0;
                    face.y+=offset;outlineStyle.Draw(face,false,false,false,false);
                    fillStyle.Draw(new Rect(face.x+2,face.y+2,face.width-4,face.height-4),false,false,false,false);
                    GUI.Label(new Rect(face.x+30,face.y,face.width-44,face.height),label,buttonText);
                    var icon=new Rect(face.x+17,face.center.y-6,12,12);
                    if(marking)EditorGUI.DrawRect(icon,new Color32(32,26,6,255));
                    else{outlineStyle.Draw(icon,false,false,false,false);fillStyle.Draw(new Rect(icon.x+2,icon.y+2,icon.width-4,icon.height-4),false,false,false,false);}
                }
                finally{GUI.enabled=oldEnabled;GUI.color=oldColor;GUI.backgroundColor=oldBackground;GUI.contentColor=oldContent;}
            }
            return clicked;
        }
        private void OnGUI()
        {
            EnsureStyles();
            try
            {
                bool available=EditorApplication.isPlaying&&StudyEvents.Recording;
                var active=available?StudyLiveObservations.Active:null;
                int count=available?StudyLiveObservations.Records.Count:0;
                if(available&&displayedSession!=StudyExportSession.SessionId){displayedSession=StudyExportSession.SessionId;participant=NeonStudySettings.Load().participantId;}
                GUILayout.Space(12);
                using(new EditorGUILayout.HorizontalScope())
                {GUILayout.Space(12);EditorGUILayout.LabelField("Live Observation Marker",titleStyle);GUILayout.Space(12);}
                GUILayout.Space(12);
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12);
                    using(new EditorGUILayout.VerticalScope())
                    {
                        using(new EditorGUILayout.HorizontalScope())
                        {EditorGUILayout.LabelField("Participant: "+(available?participant:"Selected in participant archive"));GUILayout.Label(count+" / 10 intervals",GUILayout.Width(92));}
                        using(new EditorGUI.DisabledScope(!available||active!=null))observer=EditorGUILayout.TextField("Anonymous observer ID",observer);
                        GUILayout.Space(8);
                        using(new EditorGUILayout.HorizontalScope())
                        {
                            var statusColor=active!=null?new Color32(255,112,128,255):available?new Color32(83,174,118,255):new Color32(174,180,190,255);
                            var dot=GUILayoutUtility.GetRect(10,16,GUILayout.Width(10));EditorGUI.DrawRect(new Rect(dot.x,dot.y+4,8,8),statusColor);
                            GUILayout.Label(!available?"Waiting for session recording":active!=null?"Marking interval - click to end":count>=StudyLiveObservations.Limit?"Maximum 10 intervals saved":"Ready to mark",EditorStyles.label);
                        }
                        GUILayout.Space(8);GUILayout.Label("CURRENT INTERVAL",captionStyle);
                        GUILayout.Label(ElapsedLabel(active==null?0:StudyExportSession.Seconds-active.start_s),timerStyle,GUILayout.Height(48));
                        GUILayout.Space(8);
                        if(DrawActionButton(ActionLabel(available,active!=null,count),available&&(active!=null||count<StudyLiveObservations.Limit),active!=null))
                        {
                            if(active==null){StudyLiveObservations.Begin(observer);message="Interval started. Click END when the observed episode ends.";}
                            else{var ended=StudyLiveObservations.End();ObservationNoteWindow.Open(ended);message="End timestamp saved. Note-writing time is NOT included. Game continues.";}
                            Repaint();
                        }
                        GUILayout.Label(!available?"Start Play and confirm the participant / recording first.":active!=null?"Timing continues until you click END.":"Click once at the beginning, once at the end.",captionStyle);
                        GUILayout.Space(10);
                        EditorGUILayout.HelpBox(!available?"This window does not start recording. The yellow button remains visible but is locked until recording is active.":message,MessageType.Info);
                        scroll=EditorGUILayout.BeginScrollView(scroll);
                        if(available)
                            foreach(var r in StudyLiveObservations.Records)
                            {
                                EditorGUILayout.LabelField(r.start_s.ToString("F2")+" - "+(r.end_s.HasValue?r.end_s.Value.ToString("F2"):"ongoing")+" s | "+r.label,EditorStyles.wordWrappedLabel);
                                if(r.end_s.HasValue&&GUILayout.Button("Write / edit observer note - "+r.observation_id.Substring(0,12)))ObservationNoteWindow.Open(r);
                            }
                        if(count==0)EditorGUILayout.LabelField("No marked intervals yet.",EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.HelpBox("Observer interpretation only, not a confirmed cognitive state. Player explanations and 1-5 confusion ratings are added in the offline HTML. No change to recording or export.",MessageType.None);
                        GUILayout.Space(8);
                    }
                    GUILayout.Space(12);
                }
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
