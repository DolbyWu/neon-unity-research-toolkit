using System;
using NeonResearch;
using UnityEditor;
using UnityEngine;
namespace NeonResearch.NeonXR.Editor
{
    public sealed class ObjectCalibrationWindow:EditorWindow
    {
        private string error="";private Vector2 scroll;
        [MenuItem("Tools/GazeFlow/Advanced/Legacy/Tutorial Object Calibration")]
        public static void Open(){var w=GetWindow<ObjectCalibrationWindow>("Tutorial calibration");w.minSize=new Vector2(560,470);w.Show();}
        private void OnInspectorUpdate()=>Repaint();
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            var flow=UnityEngine.Object.FindFirstObjectByType<StudyObjectCalibrationWorkflow>();
            if(!EditorApplication.isPlaying||flow==null){EditorGUILayout.HelpBox("Optional extra correction / quality check. The default tutorial records official Neon XR rays without this step. Enter the grabbing tutorial to use these optional three-object controls.",MessageType.Info);EditorGUILayout.EndScrollView();return;}
            EditorGUILayout.HelpBox("OPTIONAL: recording and the original tutorial do not require this correction or a passing check. Use official rays if no extra correction is needed.",MessageType.Info);
            if(GUILayout.Button("Use official Neon XR ray — no extra correction")){if(flow.Check!=null&&flow.Check.Running)flow.Check.Abort();flow.provider.UseOfficialRayWithoutExtraCorrection();flow.Finish(false);}
            EditorGUILayout.HelpBox(flow.Message,flow.State==StudyObjectCalibrationWorkflow.Phase.Error?MessageType.Warning:MessageType.Info);
            if(error.Length>0)EditorGUILayout.HelpBox(error,MessageType.Warning);
            EditorGUILayout.LabelField("1. Chess → 2. Card → 3. Box",EditorStyles.boldLabel);
            for(int i=0;i<3;i++)
            {
                using(new EditorGUI.DisabledScope(flow.State!=StudyObjectCalibrationWorkflow.Phase.Idle&&flow.State!=StudyObjectCalibrationWorkflow.Phase.Error))flow.objects[i]=(StudyResearchTarget)EditorGUILayout.ObjectField((i<flow.roleNames.Length?flow.roleNames[i]:"Object "+(i+1)),flow.objects[i],typeof(StudyResearchTarget),true);
                if(flow.objects[i]!=null)EditorGUILayout.LabelField(flow.objects[i].DisplayName+" · "+flow.objects[i].targetId,EditorStyles.wordWrappedLabel);
            }
            var check=flow.Check;
            EditorGUILayout.HelpBox("Keep your head reasonably steady and look with your eyes at each cue. The tutorial fits yaw/pitch only (roll fixed at zero). NEW object-hit verification is still required.",MessageType.Info);
            if(check!=null)EditorGUILayout.LabelField("Latest measured ray: "+check.LastObjectQuery,EditorStyles.wordWrappedLabel);
            if(flow.State==StudyObjectCalibrationWorkflow.Phase.Idle||flow.State==StudyObjectCalibrationWorkflow.Phase.Error||flow.State==StudyObjectCalibrationWorkflow.Phase.Review)
                if(GUILayout.Button("Start / retry three-object calibration",GUILayout.Height(38)))Run(()=>{if(check!=null&&check.Running)check.Abort();flow.Begin();});
            if(check!=null&&check.Running)
            {
                EditorGUILayout.LabelField("Current: "+check.CurrentTargetName,EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Completed: "+check.CompletedTargets+" / 3");
                EditorGUILayout.LabelField(check.WaitingForSignal?"WAITING FOR FRESH WORN EYE DATA":check.AwaitingVisibilityConfirmation?"Look at the small aim cue on the named object":check.WaitingBetweenTargets?"Next object in "+check.GapSecondsRemaining.ToString("F1")+" s":"Measuring: "+check.TargetSecondsRemaining.ToString("F1")+" s remaining");
                if(flow.State==StudyObjectCalibrationWorkflow.Phase.Collecting)
                    using(new EditorGUI.DisabledScope(!check.AwaitingVisibilityConfirmation||check.WaitingForSignal||!check.HasRecentValidSignal))
                        if(GUILayout.Button("Capture "+check.CurrentTargetName+" — wearer is looking at the cue",GUILayout.Height(48)))Run(flow.CaptureCurrent);
                if(flow.State==StudyObjectCalibrationWorkflow.Phase.Countdown||flow.State==StudyObjectCalibrationWorkflow.Phase.Verifying)EditorGUILayout.HelpBox("ROUND 2 IS AUTOMATIC — no capture clicks needed. Follow the cue on each real object. Abort if an object is hidden or moving.",MessageType.Info);
                if(GUILayout.Button("Abort object calibration"))check.Abort();
            }
            if(check?.LastReport!=null)
            {
                var r=check.LastReport;EditorGUILayout.LabelField(r.aborted?"ABORTED":r.passed?"MEASURED PASS — approval separate":"MEASURED FAIL / collection evidence",EditorStyles.boldLabel);
                foreach(var t in r.targets)EditorGUILayout.LabelField((t.target_name??"Target")+": mean "+(t.mean_error_deg.HasValue?t.mean_error_deg.Value.ToString("F2")+"°":"missing")+" · valid "+t.coverage.ToString("P0")+" · correct object "+(t.correct_target_fraction.HasValue?t.correct_target_fraction.Value.ToString("P0"):"missing"),EditorStyles.wordWrappedLabel);
            }
            using(new EditorGUI.DisabledScope(flow.State!=StudyObjectCalibrationWorkflow.Phase.Review||check?.LastReport==null||!check.LastReport.passed||!check.HasRecentValidSignal))
                if(GUILayout.Button("Approve measured mapping & continue tutorial",GUILayout.Height(42)))Run(()=>flow.Finish(true));
            if(flow.State!=StudyObjectCalibrationWorkflow.Phase.Done&&GUILayout.Button("Continue tutorial WITHOUT approved AOI (explicit skip)"))Run(()=>flow.Finish(false));
            EditorGUILayout.HelpBox("No gameplay colliders or object names are changed. This validates these three objects and the current binding, not every object/depth in the game. Ray hits are not fixations or attention.",MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        private void Run(Action action){try{action();error="";}catch(Exception e){error=e.Message;}}
    }
}
