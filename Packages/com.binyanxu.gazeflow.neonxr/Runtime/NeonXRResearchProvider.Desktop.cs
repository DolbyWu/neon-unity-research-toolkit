using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace NeonResearch.NeonXR
{
    public sealed partial class NeonXRResearchProvider
    {
        private string desktopVerificationRun,desktopVerificationCorrection;
        private Transform desktopVerificationHead;
        private StudyGazeRaySample desktopLastSample;
        private double desktopLastSampleAt=double.NegativeInfinity;
        private string desktopSavedSession,desktopSavedCalibration,desktopSavedCorrection,desktopSavePath;
        private Transform desktopSavedHead;
        public bool DesktopAdjustmentSaved => StudyEvents.Recording&&headReference!=null&&desktopSavedHead==headReference
            &&desktopSavedSession==StudyExportSession.SessionId&&desktopSavedCalibration==calibrationId&&desktopSavedCorrection==(CurrentCorrection?.id??"")
            &&!string.IsNullOrEmpty(desktopSavePath)&&File.Exists(desktopSavePath);
        public string DesktopAdjustmentSavePath => DesktopAdjustmentSaved?desktopSavePath:"";
        private void InvalidateDesktopSaveConfirmation(){desktopSavedSession=desktopSavedCalibration=desktopSavedCorrection=desktopSavePath=null;desktopSavedHead=null;}
        public bool HasFreshDesktopSignal => StudyDesktopGazePreview.CanDisplay(desktopLastSample,Time.realtimeSinceStartupAsDouble-desktopLastSampleAt,.1f);
        public string DesktopSignalStatus => HasFreshDesktopSignal?"Fresh usable ray":desktopLastSample.status??"Waiting for gaze";
        private void PublishResearch(StudyGazeRaySample sample){desktopLastSample=sample;desktopLastSampleAt=Time.realtimeSinceStartupAsDouble;Publish(sample);}
        public bool IsDesktopManualCorrection => CurrentCorrection!=null&&CurrentCorrection.correction_kind=="manual_yaw_pitch";
        public float DesktopHorizontalDegrees => IsDesktopManualCorrection?CurrentCorrection.horizontal_degrees:0;
        public float DesktopVerticalDegrees => IsDesktopManualCorrection?CurrentCorrection.vertical_degrees:0;
        public bool DesktopPointerVisible => GetComponent<StudyDesktopGazePreview>()?.Visible??false;
        private void InvalidateDesktopVerification(){desktopVerificationRun=desktopVerificationCorrection=null;desktopVerificationHead=null;mappingVerified=false;}
        private void InvalidateDesktopPreview()=>GetComponent<StudyDesktopGazePreview>()?.Invalidate();
        private void RequireDesktopContext()
        {
            if(!isActiveAndEnabled||!StudyEvents.Recording||StudyExportSession.Folder==null||headReference==null||string.IsNullOrWhiteSpace(calibrationId))throw new InvalidOperationException("An active recording, provider, tracked head and mount configuration ID are required. This tool never starts recording.");
        }
        public void ApplyDesktopOffset(float horizontalDegrees,float verticalDegrees)
        {
            RequireDesktopContext();
            var check=GetComponent<StudySpatialValidation>();
            if(check!=null&&check.Running)throw new InvalidOperationException("Stop the accuracy check before changing the offset.");
            if(CurrentCorrection!=null&&!IsDesktopManualCorrection)throw new InvalidOperationException("Reset the existing fitted correction before using desktop nudges. Corrections are not stacked.");
            var q=StudyGazeCorrection.ManualRotation(horizontalDegrees,verticalDegrees);
            var model=new StudyGazeCorrection.Model{id="manual-offset-"+Guid.NewGuid().ToString("N"),session_id=StudyExportSession.SessionId,calibration_id=calibrationId,
                correction_kind="manual_yaw_pitch",horizontal_degrees=horizontalDegrees,vertical_degrees=verticalDegrees,applied_session_s=StudyExportSession.Seconds,
                quaternion_xyzw=new[]{q.x,q.y,q.z,q.w},angle_degrees=Quaternion.Angle(Quaternion.identity,q),
                method="Operator desktop yaw/pitch nudge of official head-local ray; positive horizontal=right, vertical=up; roll fixed at zero; NOT official Companion offset correction",
                limitation="Local direction-only bias adjustment; no translation, depth, distortion, timing or measured accuracy claim. Independent NEW validation and explicit approval required."};
            string directory=Path.Combine(StudyExportSession.Folder,"Calibration",model.id);Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"correction.json");StudyAtomicFile.Write(path,JsonConvert.SerializeObject(model,Formatting.Indented));
            string previous=CurrentCorrection?.id??"";
            CancelAutomaticValidationTransition();automaticTwoRoundFlow=false;InvalidateDesktopVerification();InvalidateDesktopPreview();InvalidateDesktopSaveConfirmation();
            CurrentCorrection=model;CorrectionPath=path;
            StudyGazeTargetLog.Missing("manual_offset_changed_waiting_for_fresh_sample");
            StudyEvents.Record("gaze_manual_offset_applied",model.id,"unverified_manual_adjustment","previous_correction="+previous+"; horizontal_deg="+horizontalDegrees.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"; vertical_deg="+verticalDegrees.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"; model=Calibration/"+model.id+"/correction.json; raw official ray preserved");
        }
        /// <summary>Explicit immutable operator save receipt; never approves spatial accuracy or hides the cursor.</summary>
        public string SaveDesktopAdjustment()
        {
            RequireDesktopContext();
            if(GetComponent<StudySpatialValidation>()?.Running==true)throw new InvalidOperationException("Finish or stop the accuracy check before saving the adjustment.");
            if(CurrentCorrection!=null)
            {
                if(!IsDesktopManualCorrection)throw new InvalidOperationException("This save button is for desktop manual offsets or official rays without extra correction.");
                if(CurrentCorrection.session_id!=StudyExportSession.SessionId||CurrentCorrection.calibration_id!=calibrationId||string.IsNullOrEmpty(CorrectionPath)||!File.Exists(CorrectionPath))
                    throw new InvalidOperationException("The current adjustment model has no matching saved file for this session/configuration. Reset or reapply it before confirming.");
                // Compare serialized numeric representations, not JValue float versus parsed double.
                if(!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(CorrectionPath)),Newtonsoft.Json.Linq.JToken.Parse(JsonConvert.SerializeObject(CurrentCorrection))))
                    throw new InvalidOperationException("The saved adjustment file differs from the active model. Reset or reapply it before confirming.");
            }
            if(DesktopAdjustmentSaved)return desktopSavePath;
            string id="gaze-save-"+Guid.NewGuid().ToString("N");
            string directory=Path.Combine(StudyExportSession.Folder,"Calibration","gaze-confirmations");Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,id+".json");
            var receipt=new{schema="gazeflow-desktop-adjustment-save-v1",id,session_id=StudyExportSession.SessionId,calibration_id=calibrationId,
                correction_id=CurrentCorrection?.id??"",mode=IsDesktopManualCorrection?"manual_yaw_pitch":"official_ray_no_extra_correction",head_name=headReference.name,head_instance_id=headReference.GetInstanceID(),
                saved_session_s=StudyExportSession.Seconds,horizontal_degrees=DesktopHorizontalDegrees,vertical_degrees=DesktopVerticalDegrees,
                correction_file=CurrentCorrection==null?"":"Calibration/"+CurrentCorrection.id+"/correction.json",raw_official_ray_retained=true,accuracy_approval_granted=false,
                limitation="Operator confirms the selected adjustment was saved; NOT a measured spatial accuracy pass. Recording and cursor visibility are unchanged."};
            StudyAtomicFile.Write(path,JsonConvert.SerializeObject(receipt,Formatting.Indented));
            desktopSavedSession=StudyExportSession.SessionId;desktopSavedCalibration=calibrationId;desktopSavedCorrection=CurrentCorrection?.id??"";desktopSavedHead=headReference;desktopSavePath=path;
            StudyEvents.Record("gaze_adjustment_saved",CurrentCorrection?.id??"official-ray","operator_save_not_accuracy_approval","receipt=Calibration/gaze-confirmations/"+id+".json; original official ray retained; gaze pointer visibility unchanged");
            return path;
        }
        /// <summary>Same-session head-local nudges may survive known scene-head replacement; approval never does.</summary>
        public void RebindDesktopHead(Transform actualHead,string mountConfigurationId)
        {
            if(actualHead==null||string.IsNullOrWhiteSpace(mountConfigurationId))throw new ArgumentException("An actual tracked head and configuration ID are required.");
            var check=GetComponent<StudySpatialValidation>();if(check!=null)check.MarkPriorBindingForReview();
            bool keep=IsDesktopManualCorrection&&StudyEvents.Recording&&CurrentCorrection.session_id==StudyExportSession.SessionId&&CurrentCorrection.calibration_id==mountConfigurationId;
            string previous=CurrentCorrection?.id??"";
            CancelAutomaticValidationTransition();automaticTwoRoundFlow=false;InvalidateDesktopVerification();InvalidateDesktopPreview();InvalidateDesktopSaveConfirmation();
            if(!keep){CurrentCorrection=null;CorrectionPath="";}
            headReference=actualHead;calibrationId=mountConfigurationId;
            if(check!=null){check.headReference=actualHead;check.provider=this;check.TutorialEntryContext=false;}
            if(StudyEvents.Recording)StudyEvents.Record("gaze_head_local_offset_binding",CurrentCorrection?.id??"",keep?"same_session_manual_offset_retained_unverified":"official_ray_no_extra_offset","scene="+actualHead.gameObject.scene.name+"; previous_correction="+previous+"; actual head rebound; mapping approval cleared");
        }
        public StudySpatialValidation BeginDesktopAccuracyCheck()
        {
            RequireDesktopContext();
            var check=GetComponent<StudySpatialValidation>()??gameObject.AddComponent<StudySpatialValidation>();
            if(check.Running)throw new InvalidOperationException("A quality check is already running.");
            CancelAutomaticValidationTransition();automaticTwoRoundFlow=false;InvalidateDesktopVerification();SetDesktopPointerVisible(false);
            check.provider=this;check.headReference=headReference;check.TutorialEntryContext=false;check.BindingReviewRequired=false;
            check.bananaMarkers=false;check.singleBaselineBeforeObjects=false;check.nineTargets=false;check.automaticSequence=true;check.markerDiameter=.032f;
            check.independentCheckPattern=CurrentCorrection!=null;check.ForgetReferenceGeometry();check.Begin(calibrationId);
            desktopVerificationRun=check.RunId;desktopVerificationCorrection=CurrentCorrection?.id??"";desktopVerificationHead=headReference;
            return check;
        }
        private bool DesktopVerificationMatches(StudySpatialValidation check)=>check!=null&&!check.RestoredForReviewOnly&&check.LastReport!=null&&check.LastReport.id==desktopVerificationRun&&desktopVerificationHead==headReference&&desktopVerificationCorrection==(CurrentCorrection?.id??"");
        public void SetDesktopPointerVisible(bool visible)
        {
            if(neon!=null)
                foreach(var official in neon.transform.root.GetComponentsInChildren<PupilLabs.GazeDataVisualizer>(true))
                {official.RayVisible=false;official.RaycastPointerVisible=false;}
            var preview=GetComponent<StudyDesktopGazePreview>();
            if(preview==null&&visible)preview=gameObject.AddComponent<StudyDesktopGazePreview>();
            if(preview==null)return;
            preview.Bind(this);
            var recorders=FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Where(r=>r.isActiveAndEnabled&&r.provider==this).ToArray();
            if(recorders.Length==1){preview.collisionMask=recorders[0].collisionMask;preview.maxDistance=recorders[0].maxDistance;preview.staleAfterSeconds=recorders[0].staleAfterSeconds;}
            preview.Visible=visible;
        }
    }
}
