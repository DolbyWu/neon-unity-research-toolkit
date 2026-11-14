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
        [MenuItem("Tools/GazeFlow/Desktop Gaze Adjustment")]
        public static void Open(){var w=GetWindow<DesktopGazeAdjustmentWindow>("Gaze Adjustment");w.minSize=new Vector2(460,560);w.Show();w.Focus();}
        private void OnInspectorUpdate()=>Repaint();
        private void Act(Action action){try{action();message="Action completed locally. Original official rays are retained; see mapping status above. Adjustment alone does not verify accuracy.";approval=false;}catch(Exception e){message=e.Message;}}
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Desktop Gaze Adjustment",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("GazeFlow local direction adjustment on top of official Neon XR. Not official Companion offset correction. No phone controls, automatic fitting or mandatory tutorial gate.",MessageType.Info);
            if(!EditorApplication.isPlaying){EditorGUILayout.LabelField("Enter Play and complete participant / recording confirmation.");EditorGUILayout.EndScrollView();return;}
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
            using(new EditorGUI.DisabledScope(!canRecord||running||fitted))
            {
                using(new EditorGUILayout.HorizontalScope()){GUILayout.FlexibleSpace();if(GUILayout.Button("UP",GUILayout.Width(110),GUILayout.Height(34)))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees,p.DesktopVerticalDegrees+steps[stepIndex]));GUILayout.FlexibleSpace();}
                using(new EditorGUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("LEFT",GUILayout.Height(34)))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees-steps[stepIndex],p.DesktopVerticalDegrees));
                    if(GUILayout.Button("RIGHT",GUILayout.Height(34)))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees+steps[stepIndex],p.DesktopVerticalDegrees));
                }
                using(new EditorGUILayout.HorizontalScope()){GUILayout.FlexibleSpace();if(GUILayout.Button("DOWN",GUILayout.Width(110),GUILayout.Height(34)))Act(()=>p.ApplyDesktopOffset(p.DesktopHorizontalDegrees,p.DesktopVerticalDegrees-steps[stepIndex]));GUILayout.FlexibleSpace();}
            }
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
