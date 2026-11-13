using System;
using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEngine;

namespace NeonResearch.NeonXR.Editor
{
    public sealed class PreRoomValidationWindow : EditorWindow
    {
        private string message="Choose ONE point to begin round 1. Sufficient data triggers automatic correction and a 3-second fresh-signal countdown into round 2 at the SAME fixed dots. If data are insufficient, the panel lists which points need recollection. AOI approval stays separate.";
        private Vector2 scroll;
        private bool approval, advanced;
        private int approvalProvider,approvalHead;
        private static readonly string[] PointNames={"1 · Centre","2 · Left","3 · Right","4 · Up","5 · Down"};
        [MenuItem("Tools/GazeFlow/Pre-room Gaze Validation")]
        public static void Open(){var window=GetWindow<PreRoomValidationWindow>("Gaze validation");window.minSize=new Vector2(560,480);window.Show();window.Focus();}
        private void OnInspectorUpdate()=>Repaint();
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.HelpBox(message,MessageType.Info);
            if(!EditorApplication.isPlaying){EditorGUILayout.LabelField("Enter Play. The trial starts with validation in the first tutorial room.");EditorGUILayout.EndScrollView();return;}
            var providers=UnityEngine.Object.FindObjectsByType<NeonXRResearchProvider>(FindObjectsSortMode.None).Where(p=>p.isActiveAndEnabled).ToArray();
            if(providers.Length!=1){EditorGUILayout.LabelField("Exactly one active Neon XR Research Provider is required.");EditorGUILayout.EndScrollView();return;}
            var provider=providers[0];var check=provider.GetComponent<StudySpatialValidation>()??provider.gameObject.AddComponent<StudySpatialValidation>();
            EditorGUILayout.HelpBox(provider.AutomaticValidationStatus,provider.AutomaticValidationStatus.Contains("NOT STARTED")?MessageType.Warning:MessageType.Info);
            if(provider.AutomaticVerificationPending&&GUILayout.Button("Pause automatic round-two transition"))provider.CancelAutomaticValidationTransition();
            check.provider=provider;check.headReference=provider.headReference;
            int headId=provider.headReference==null?0:provider.headReference.GetInstanceID();
            if(approvalProvider!=provider.GetInstanceID()||approvalHead!=headId){approval=false;approvalProvider=provider.GetInstanceID();approvalHead=headId;}
            if(check.BindingReviewRequired && !provider.mappingVerified)
                EditorGUILayout.HelpBox("SCENE HEAD CHANGED — AOI HITS ARE NOT APPROVED. Raw gaze/video continue, but AOI rows remain mapping_unverified. If wearing and mount configuration are unchanged, review the saved report and explicitly approve this binding below. Otherwise repeat validation here before the task.",MessageType.Warning);
            if(check.TutorialEntryContext)EditorGUILayout.HelpBox("FIRST TUTORIAL ROOM: five-point check in front. Grabbing stays hidden during the check; turn around 180 degrees for grabbing afterwards. Computer operator selects one starting point; remaining points run automatically.",MessageType.Info);
            using(new EditorGUI.DisabledScope(check.Running))
            {
                EditorGUI.BeginChangeCheck();provider.calibrationId=EditorGUILayout.TextField("Mount calibration ID",provider.calibrationId);
                if(EditorGUI.EndChangeCheck()){provider.mappingVerified=false;approval=false;}
                check.automaticSequence=EditorGUILayout.ToggleLeft("Automatic five points — click ONE starting point on the computer",check.automaticSequence);
                provider.automaticTwoRoundFlow=EditorGUILayout.ToggleLeft("Automatically fit and start round 2 when round 1 has enough data",provider.automaticTwoRoundFlow);
                if(GUILayout.Button(check.automaticSequence?"Show five targets":"Show manual validation target",GUILayout.Height(36)))
                {
                    try
                    {
                        provider.mappingVerified=false;approval=false;
                        provider.CancelAutomaticValidationTransition();check.independentCheckPattern=provider.CurrentCorrection!=null;
                        GazePointerWindow.ApplyVisibility(UnityEngine.Object.FindObjectsByType<GazeDataVisualizer>(FindObjectsSortMode.None),false);
                        if(provider.CurrentCorrection==null)provider.ClearCorrectionAndCollectAgain();else check.Begin(provider.calibrationId);
                        message=check.automaticSequence?"All five dots appear after a live worn signal. Ask the wearer to confirm visibility, then click ONE point on the map below to start the whole sequence.":"Manual mode: confirm every visible target before starting its timer.";
                    }catch(Exception e){message=e.Message;}
                }
            }
            if(check.Running)
            {
                string status=check.WaitingForSignal?"WAITING for live worn eye data":check.AwaitingVisibilityConfirmation?(check.AutomaticRun?"FIVE DOTS READY — choose the first point below":"PREVIEW — confirm target visibility"):check.WaitingBetweenTargets?$"NEXT POINT in {check.GapSecondsRemaining:F1} s":$"LOOK at {PointNames[Math.Min(check.TargetIndex,4)]} · {check.TargetSecondsRemaining:F1} s remaining";
                EditorGUILayout.LabelField(status,EditorStyles.boldLabel);EditorGUILayout.LabelField($"Completed: {check.CompletedTargets} / {(check.AutomaticRun?5:check.nineTargets?9:5)}");
                if(!check.WaitingForSignal&&!check.HasRecentValidSignal)EditorGUILayout.HelpBox("No recent valid worn signal. Missing data will not count as valid coverage. Check Neon; abort if the wearer cannot follow the targets.",MessageType.Warning);
                if(check.AwaitingVisibilityConfirmation)
                {
                    EditorGUILayout.HelpBox(provider.AutomaticVerificationPending?"ROUND 2: no click needed. Countdown waits for fresh worn data. Abort or skip if the SAME dots are no longer visible.":check.AutomaticRun?"Ask: Can you see all five fixed dots? Click ONE starting point below. Its size pulses for the gaze period, then it disappears. After the configured gap, the next point starts automatically. No further clicks are needed. Abort if any target cannot be seen.":"Confirm the wearer can see the fixed yellow target. Do not confirm an unseen target.",MessageType.Info);
                    using(new EditorGUI.DisabledScope(!check.HasRecentValidSignal))
                    {
                        if(!check.independentCheckPattern&&check.CompletedTargets==0&&GUILayout.Button("Reposition the unmeasured preview in front of the current headset"))check.RecenterPreview();
                        if(check.AutomaticRun&&!provider.AutomaticVerificationPending)DrawStartMap(check);
                        else if(!check.AutomaticRun&&GUILayout.Button("Target visible — start measuring this target",GUILayout.Height(38)))check.ConfirmTargetVisible();
                    }
                }
                if(GUILayout.Button("Abort validation"))check.Abort();
            }
            if(check.TutorialEntryContext && GUILayout.Button("Skip validation and continue to grabbing (AOI stays unverified)"))
            {provider.mappingVerified=false;check.SkipTutorialValidation();message="Validation explicitly skipped. Mapping was NOT approved.";}
            var report=check.LastReport;
            if(report!=null)
            {
                EditorGUILayout.LabelField(report.aborted?"ABORTED":report.passed?"MEASURED PASS — approval separate":"MEASURED FAIL",EditorStyles.boldLabel);
                if(report.aborted)EditorGUILayout.LabelField("Reason: "+report.abort_reason);
                foreach(var t in report.targets)EditorGUILayout.LabelField($"Point {t.target_index+1}: mean {(t.mean_error_deg.HasValue?t.mean_error_deg.Value.ToString("F2"):"missing")}°; p95 {(t.p95_error_deg.HasValue?t.p95_error_deg.Value.ToString("F2"):"missing")}°; coverage {t.coverage:P0}");
                EditorGUILayout.SelectableLabel(check.LastOutput,EditorStyles.wordWrappedLabel,GUILayout.Height(45));
                if(provider.CurrentCorrection==null&&!check.RestoredForReviewOnly&&!check.Running&&!report.aborted)
                {
                    var blockers=StudyValidationReadiness.FitBlockers(report);
                    if(blockers.Length>0)EditorGUILayout.HelpBox("ROUND 2 NOT STARTED — insufficient first-round collection:\n"+string.Join("\n",blockers)+"\nCheck wearing/stream quality, then repeat all five points. Earlier files remain saved.",MessageType.Warning);
                    if(GUILayout.Button("Retry round 1 — all five points",GUILayout.Height(36)))
                    {try{approval=false;provider.ClearCorrectionAndCollectAgain();message="New round-one collection. Confirm the five dots, then select ONE starting point. Earlier files are retained.";}catch(Exception e){message=e.Message;}}
                    if((!provider.automaticTwoRoundFlow||!check.automaticSequence)&&GUILayout.Button("Fit correction & show NEW verification points",GUILayout.Height(36)))
                    {try{approval=false;provider.FitCorrectionAndBeginVerification();message="Correction saved; AOI still unapproved. Confirm the NEW five dots are visible, then choose one starting point. Only a passing independent run can enable corrected hits.";}catch(Exception e){message=e.Message;}}
                }
                if(provider.CurrentCorrection!=null)
                {
                    EditorGUILayout.LabelField("Correction: "+provider.CurrentCorrection.id+" · "+provider.CurrentCorrection.angle_degrees.ToString("F2")+"°; verification required");
                    if(!check.Running&&GUILayout.Button("Discard current correction & collect five points again"))
                    {try{approval=false;provider.ClearCorrectionAndCollectAgain();message="Collecting a NEW uncorrected fitting run. Existing files are retained. Refit and independently verify afterwards.";}catch(Exception e){message=e.Message;}}
                }
                if(check.RestoredForReviewOnly)EditorGUILayout.HelpBox("This measurement was made before the scene/head replacement. It is evidence for review, not a new accuracy measurement of this binding.",MessageType.Info);
                approval=EditorGUILayout.ToggleLeft("I reviewed visibility and the report; wearing/mount setup is unchanged for this head binding",approval);
                using(new EditorGUI.DisabledScope(!approval||!report.passed||report.aborted||check.Running||!check.HasRecentValidSignal||report.calibration_id!=provider.calibrationId||check.LastOutput.StartsWith("SAVE FAILED")||(check.TutorialEntryContext&&provider.CurrentCorrection==null)))
                    if(GUILayout.Button("Approve this mapping for AOI"))
                    {try{provider.ApproveCurrentMapping();approval=false;message="Operator approved current mapping. Revalidate after wearing/mount changes. Not hardware synchronization.";}catch(Exception e){message=e.Message;}}
            }
            advanced=EditorGUILayout.Foldout(advanced,"Advanced: timing, geometry and engineering thresholds",true);
            if(advanced)using(new EditorGUI.DisabledScope(check.Running))
            {
                if(!check.automaticSequence)check.nineTargets=EditorGUILayout.Toggle("Nine manual targets",check.nineTargets);
                check.distanceMetres=EditorGUILayout.FloatField("Target distance (metres)",check.distanceMetres);
                check.settleSeconds=EditorGUILayout.FloatField("Settle seconds per point",check.settleSeconds);
                check.measureSeconds=EditorGUILayout.FloatField("Measure seconds per point",check.measureSeconds);
                check.interTargetSeconds=EditorGUILayout.FloatField("Gap between automatic points",check.interTargetSeconds);
                check.maximumMeanErrorDegrees=EditorGUILayout.FloatField("Project tolerance (degrees)",check.maximumMeanErrorDegrees);
                check.minimumCoverage=EditorGUILayout.FloatField("Minimum valid coverage (0–1)",check.minimumCoverage);
                EditorGUILayout.HelpBox("Defaults: 1 s settle + 2 s measurement; 2 s gap. Thresholds are configurable engineering policy, NOT validated scientific standards. Targets keep fixed world centres; animation changes size only. Other dots remain static until their turn.",MessageType.Warning);
            }
            EditorGUILayout.LabelField("AOI operator approval: "+provider.mappingVerified);
            if(GUILayout.Button("Open gaze pointer display switch"))GazePointerWindow.Open();
            EditorGUILayout.EndScrollView();
        }
        private void DrawStartMap(StudySpatialValidation check)
        {
            var area=GUILayoutUtility.GetRect(360,160);float cx=area.center.x,top=area.y;
            var locations=new[]{new Vector2(cx-55,top+58),new Vector2(cx-175,top+58),new Vector2(cx+65,top+58),new Vector2(cx-55,top+8),new Vector2(cx-55,top+108)};
            for(int i=0;i<locations.Length;i++)
                if(GUI.Button(new Rect(locations[i].x,locations[i].y,110,36),PointNames[i]))
                {try{check.StartAutomaticSequence(i);message="Automatic sequence started. Wearer follows the pulsing dot. No more point confirmations; stop if a target is not visible.";}catch(Exception e){message=e.Message;}}
        }
    }
}
