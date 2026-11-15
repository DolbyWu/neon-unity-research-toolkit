using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NeonResearch.NeonXR.Editor
{
    /// <summary>Explicit local research-ray adjustment. Not official Companion calibration or a gameplay gate.</summary>
    public sealed class DesktopGazeAdjustmentWindow:EditorWindow
    {
        /// <summary>Optional host-only study controls. No subscriber means no additional UI or gate.</summary>
        public static event Action PreparationControlsGUI;
        private readonly float[] steps={.1f,.25f,.5f,1};
        private readonly string[] stepNames={"0.10 degrees","0.25 degrees","0.50 degrees","1.00 degree"};
        private int stepIndex;
        private bool approval;
        private string context="",message="Optional. Nothing is adjusted or measured until you click. The phone only needs to stream.";
        private Vector2 scroll;
        private GUIStyle directionText,directionStep,directionOutline,directionFill,directionShadow;
        private Texture2D directionOutlineTexture,directionFillTexture,directionShadowTexture;
        private int pressedDirection=-1;
        public static Color DirectionFaceColor=>new Color32(37,230,245,255);
        public static bool CanNudge(bool recording,bool hasHead,bool measuring,bool fittedCorrection)=>recording&&hasHead&&!measuring&&!fittedCorrection;
        [MenuItem("Tools/GazeFlow/Desktop Gaze Adjustment")]
        public static void Open(){var w=GetWindow<DesktopGazeAdjustmentWindow>("Gaze Adjustment");w.minSize=new Vector2(460,560);w.Show();w.Focus();}
        private void OnInspectorUpdate()=>Repaint();
        private void OnLostFocus(){pressedDirection=-1;Repaint();}
        private void OnDisable()
        {
            foreach(var owned in new[]{directionOutlineTexture,directionFillTexture,directionShadowTexture})if(owned!=null)DestroyImmediate(owned);
            directionOutlineTexture=directionFillTexture=directionShadowTexture=null;
            directionOutline=directionFill=directionShadow=directionText=directionStep=null;pressedDirection=-1;
        }
        private static Texture2D DirectionTexture(Color color)
        {
            const int size=32;const float radius=8;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="GazeFlow direction button (owned)",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float px=x+.5f,py=y+.5f,cx=Mathf.Clamp(px,radius,size-radius),cy=Mathf.Clamp(py,radius,size-radius);
                var pixel=color;pixel.a*=Mathf.Clamp01(radius-Mathf.Sqrt((px-cx)*(px-cx)+(py-cy)*(py-cy))+.5f);pixels[y*size+x]=pixel;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
        private void EnsureDirectionStyles()
        {
            if(directionFillTexture!=null)return;
            directionOutlineTexture=DirectionTexture(new Color32(3,47,56,255));directionFillTexture=DirectionTexture(DirectionFaceColor);directionShadowTexture=DirectionTexture(new Color32(8,127,145,255));
            GUIStyle Shape(Texture2D texture)=>new GUIStyle{normal={background=texture},border=new RectOffset(9,9,9,9)};
            directionOutline=Shape(directionOutlineTexture);directionFill=Shape(directionFillTexture);directionShadow=Shape(directionShadowTexture);
            directionText=new GUIStyle(EditorStyles.boldLabel){alignment=TextAnchor.MiddleCenter,fontSize=16,normal={textColor=new Color32(3,47,56,255)}};
            directionStep=new GUIStyle(EditorStyles.label){alignment=TextAnchor.MiddleCenter,fontSize=12,wordWrap=true,normal={textColor=DirectionFaceColor}};
        }
        private bool DrawDirectionButton(Rect face,string label,int direction,bool available)
        {
            bool allowed=GUI.enabled&&available;var current=Event.current;
            if(current.rawType==EventType.MouseDown&&current.button==0&&allowed&&face.Contains(current.mousePosition))pressedDirection=direction;
            if(current.rawType==EventType.MouseUp||!available)pressedDirection=-1;
            bool clicked;using(new EditorGUI.DisabledScope(!available))clicked=GUI.Button(face,new GUIContent(label),GUIStyle.none);
            if(current.type==EventType.Repaint)
            {
                bool enabled=GUI.enabled;var color=GUI.color;var background=GUI.backgroundColor;var content=GUI.contentColor;
                try
                {
                    // Paint only; native input above stays locked. Ignore skin/Play tint, not safety guards.
                    GUI.enabled=true;GUI.color=GUI.backgroundColor=GUI.contentColor=Color.white;
                    directionShadow.Draw(new Rect(face.x,face.y+5,face.width,face.height),false,false,false,false);
                    float offset=pressedDirection==direction&&face.Contains(current.mousePosition)?3:0;face.y+=offset;
                    directionOutline.Draw(face,false,false,false,false);directionFill.Draw(new Rect(face.x+2,face.y+2,face.width-4,face.height-4),false,false,false,false);
                    var icon=new Rect(Mathf.Round(face.center.x-10),Mathf.Round(face.y+8),20,20);var ink=new Color32(3,47,56,255);
                    for(int i=0;i<10;i++)
                    {
                        // Draw geometric arrows, avoiding platform-dependent arrow glyph fonts.
                        if(direction==0)EditorGUI.DrawRect(new Rect(icon.center.x-i,icon.y+i,2*i+1,2),ink);
                        else if(direction==3)EditorGUI.DrawRect(new Rect(icon.center.x-i,icon.y+18-i,2*i+1,2),ink);
                        else if(direction==1)EditorGUI.DrawRect(new Rect(icon.x+i,icon.center.y-i,2,2*i+1),ink);
                        else EditorGUI.DrawRect(new Rect(icon.x+18-i,icon.center.y-i,2,2*i+1),ink);
                    }
                    if(direction==0)EditorGUI.DrawRect(new Rect(icon.center.x-2,icon.y+9,4,11),ink);
                    else if(direction==3)EditorGUI.DrawRect(new Rect(icon.center.x-2,icon.y,4,11),ink);
                    else if(direction==1)EditorGUI.DrawRect(new Rect(icon.x+9,icon.center.y-2,11,4),ink);
                    else EditorGUI.DrawRect(new Rect(icon.x,icon.center.y-2,11,4),ink);
                    GUI.Label(new Rect(face.x,face.y+32,face.width,face.height-32),label,directionText);
                }
                finally{GUI.enabled=enabled;GUI.color=color;GUI.backgroundColor=background;GUI.contentColor=content;}
            }
            return clicked&&allowed;
        }
        private void DrawDirectionPad(NeonXRResearchProvider p,bool available)
        {
            EnsureDirectionStyles();var area=GUILayoutUtility.GetRect(GUIContent.none,GUIStyle.none,GUILayout.Height(224),GUILayout.ExpandWidth(true));
            float width=Mathf.Min(128,(area.width-16)/3),left=area.center.x-width*1.5f-8;const float height=64,gap=9;
            Rect Cell(int column,int row)=>new Rect(left+column*(width+8),area.y+row*(height+gap),width,height);
            if(DrawDirectionButton(Cell(1,0),"UP",0,available))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees,p.DesktopVerticalDegrees+steps[stepIndex]));
            if(DrawDirectionButton(Cell(0,1),"LEFT",1,available))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees-steps[stepIndex],p.DesktopVerticalDegrees));
            if(DrawDirectionButton(Cell(2,1),"RIGHT",2,available))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees+steps[stepIndex],p.DesktopVerticalDegrees));
            if(DrawDirectionButton(Cell(1,2),"DOWN",3,available))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees,p.DesktopVerticalDegrees-steps[stepIndex]));
            if(Event.current.type==EventType.Repaint)
            {
                var old=GUI.color;bool enabled=GUI.enabled;try{GUI.enabled=true;GUI.color=Color.white;GUI.Label(Cell(1,1),steps[stepIndex].ToString("F2")+" degrees\nper click",directionStep);}finally{GUI.color=old;GUI.enabled=enabled;}
            }
            EditorGUILayout.LabelField(available?"Move the cyan gaze point — not the yellow reference.":"Direction controls locked — need Play, recording and a bound head; stop any check or reset a fitted model.",EditorStyles.wordWrappedMiniLabel);
        }
        private void Act(Action action){try{action();message="Action completed locally. Original official rays are retained; see mapping status above. Adjustment alone does not verify accuracy.";approval=false;}catch(Exception e){message=e.Message;}}
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Desktop Gaze Adjustment",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("GazeFlow local direction adjustment on top of official Neon XR. Not official Companion offset correction. No phone controls, automatic fitting or mandatory tutorial gate.",MessageType.Info);
            if(!EditorApplication.isPlaying){EditorGUILayout.LabelField("Enter Play and complete participant / recording confirmation.");DrawDirectionPad(null,false);EditorGUILayout.EndScrollView();return;}
            // Host preparation must remain accessible even with missing gaze/provider data.
            PreparationControlsGUI?.Invoke();
            var providers=UnityEngine.Object.FindObjectsByType<NeonXRResearchProvider>(FindObjectsSortMode.None).Where(p=>p.isActiveAndEnabled).ToArray();
            if(providers.Length!=1){EditorGUILayout.HelpBox("Exactly one active Neon XR Research Provider is required.",MessageType.Warning);EditorGUILayout.EndScrollView();return;}
            var p=providers[0];var check=p.GetComponent<StudySpatialValidation>();
            bool canRecord=StudyEvents.Recording&&p.headReference!=null;
            bool running=check!=null&&check.Running;
            string key=p.GetInstanceID()+":"+(p.headReference==null?0:p.headReference.GetInstanceID())+":"+(p.CurrentCorrection?.id??"")+":"+(check?.LastReport?.id??"");
            if(key!=context){context=key;approval=false;}
            EditorGUILayout.LabelField("Signal",p.DesktopSignalStatus);
            EditorGUILayout.LabelField("Head",p.headReference==null?"Not bound":p.headReference.name);
            using(new EditorGUI.DisabledScope(p.headReference==null))
                if(GUILayout.Button(p.DesktopPointerVisible?"Hide gaze pointer":"Show gaze pointer",GUILayout.Height(38)))p.SetDesktopPointerVisible(!p.DesktopPointerVisible);
            EditorGUILayout.HelpBox("The cyan pointer uses the SAME research ray as AOI logging and the same recorder collision mask. It hides on invalid/stale data. In empty space it uses a 2 m visual fallback, not an AOI hit. Official gameplay callbacks are unchanged.",MessageType.None);
            GUILayout.Space(8);EditorGUILayout.LabelField("Manual offset - head-local degrees",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Right / left",p.DesktopHorizontalDegrees.ToString("F2")+" degrees (positive = right)");
            EditorGUILayout.LabelField("Up / down",p.DesktopVerticalDegrees.ToString("F2")+" degrees (positive = up)");
            bool fitted=p.CurrentCorrection!=null&&!p.IsDesktopManualCorrection;
            if(fitted)EditorGUILayout.HelpBox("A fitted correction is active. Reset it before using arrows; different corrections are never stacked.",MessageType.Warning);
            stepIndex=EditorGUILayout.Popup("Step per click",stepIndex,stepNames);
            DrawDirectionPad(p,CanNudge(StudyEvents.Recording,p.headReference!=null,running,fitted));
            using(new EditorGUI.DisabledScope(!canRecord))
                if(GUILayout.Button("Reset - use official ray without extra correction",GUILayout.Height(34)))Act(p.UseOfficialRayWithoutExtraCorrection);
            using(new EditorGUI.DisabledScope(!canRecord||running||fitted))
                if(GUILayout.Button("Save gaze adjustment",GUILayout.Height(42)))
                {
                    try{p.SaveDesktopAdjustment();message="Adjustment saved — you can now hide the gaze pointer. This is a save confirmation, not an accuracy approval.";}
                    catch(Exception e){message="Adjustment NOT saved: "+e.Message;}
                }
            EditorGUILayout.HelpBox(p.DesktopAdjustmentSaved?"Adjustment saved — you can now hide the gaze pointer.\nSave confirmation is NOT a measured accuracy pass.":"After adjusting, click Save gaze adjustment to confirm this setting. Changing arrows, resetting, rebinding the head or starting a new session requires a new confirmation.",p.DesktopAdjustmentSaved?MessageType.Info:MessageType.None);
            if(p.DesktopAdjustmentSaved)EditorGUILayout.SelectableLabel(p.DesktopAdjustmentSavePath,EditorStyles.wordWrappedLabel,GUILayout.Height(36));
            EditorGUILayout.HelpBox("Keep the head and a clearly specified target still while nudging. Arrows move the research gaze, not the target. Each change saves a new correction ID and clears prior accuracy approval. Combined rotation limit: 12 degrees (engineering safety bound, not an accuracy standard).",MessageType.None);
            GUILayout.Space(8);EditorGUILayout.LabelField("Optional independent accuracy check",EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(!canRecord||running))
                if(GUILayout.Button("Show five check targets - no automatic fitting",GUILayout.Height(38)))Act(()=>p.BeginDesktopAccuracyCheck());
            check=p.GetComponent<StudySpatialValidation>();
            if(check!=null)
            {
                if(check.Running)
                {
                    EditorGUILayout.LabelField(check.WaitingForSignal?"Waiting for fresh worn gaze":check.AwaitingVisibilityConfirmation?"Ask the wearer: are all five targets visible?":check.WaitingBetweenTargets?"Next target in "+check.GapSecondsRemaining.ToString("F1")+" s":"Look at "+check.CurrentTargetName+" - "+check.TargetSecondsRemaining.ToString("F1")+" s");
                    if(check.AwaitingVisibilityConfirmation&&!check.WaitingForSignal)
                        using(new EditorGUI.DisabledScope(!check.HasRecentValidSignal))
                            if(GUILayout.Button("Targets visible - start one five-point check"))Act(()=>check.StartAutomaticSequence(0));
                    if(GUILayout.Button("Stop check - keep recording"))Act(check.Abort);
                }
                var report=check.LastReport;
                if(report!=null)
                {
                    EditorGUILayout.LabelField(report.aborted?"Check aborted":report.passed?"Measured pass - approval still separate":"Measured fail - recording continues",EditorStyles.boldLabel);
                    if(check.RestoredForReviewOnly)EditorGUILayout.HelpBox("Report is from a prior head binding. Run a new check here before approving this binding.",MessageType.Warning);
                    foreach(var t in report.targets??new System.Collections.Generic.List<StudySpatialValidation.TargetResult>())
                        EditorGUILayout.LabelField("Point "+(t.target_index+1)+": mean "+(t.mean_error_deg?.ToString("F2")??"missing")+" deg; p95 "+(t.p95_error_deg?.ToString("F2")??"missing")+" deg; coverage "+t.coverage.ToString("P0"),EditorStyles.wordWrappedLabel);
                    EditorGUILayout.SelectableLabel(check.LastOutput,EditorStyles.wordWrappedLabel,GUILayout.Height(40));
                    approval=EditorGUILayout.ToggleLeft("I reviewed visibility and NEW results for this wearing / head binding",approval);
                    using(new EditorGUI.DisabledScope(!canRecord||check.Running||!approval||!report.passed||report.aborted||check.RestoredForReviewOnly||!check.HasRecentValidSignal))
                        if(GUILayout.Button("Approve measured mapping for this head binding"))Act(p.ApproveCurrentMapping);
                }
            }
            EditorGUILayout.LabelField("Mapping verified: "+p.mappingVerified);
            EditorGUILayout.HelpBox(message,MessageType.Info);
            EditorGUILayout.HelpBox("The five-point check measures new samples only and does not fit or auto-approve. Skipping/failing it never stops gaze, AOI, media or the tutorial. Reset after re-wearing or setup changes. Phone-side offsets must be documented separately.",MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }
}
