using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace NeonResearch
{
    /// <summary>Known-target validation, not calibration fitting, fixation detection or clock synchronization.</summary>
    public sealed class StudySpatialValidation : MonoBehaviour
    {
        public StudyGazeRayProvider provider;
        public Transform headReference;
        public float settleSeconds=1f, measureSeconds=2f, distanceMetres=2f;
        public float maximumMeanErrorDegrees=3f, minimumCoverage=.7f;
        public bool automaticSequence=true, nineTargets;
        public bool independentCheckPattern;
        public bool ReusesReferenceGeometry => reusedGeometry;
        public float interTargetSeconds=2f;
        public bool Running { get; private set; }
        public bool WaitingForSignal { get; private set; }
        public bool AwaitingVisibilityConfirmation { get; private set; }
        public bool AutomaticRun => Running?runAutomatic:automaticSequence;
        public bool WaitingBetweenTargets => sequence!=null && sequence.WaitingGap;
        public double GapSecondsRemaining => sequence?.GapRemaining(Time.realtimeSinceStartupAsDouble)??0;
        public double TargetSecondsRemaining => Running&&!WaitingForSignal&&!AwaitingVisibilityConfirmation&&!WaitingBetweenTargets?Math.Max(0,runSettle+runMeasure-(Time.realtimeSinceStartupAsDouble-targetStarted)):0;
        public int CompletedTargets => results.Count;
        public int TargetIndex { get; private set; }
        private double lastFresh=double.NegativeInfinity;
        public bool HasRecentValidSignal => Time.realtimeSinceStartupAsDouble-lastFresh<=.25;
        public ValidationReport LastReport { get; private set; }
        public bool RestoredForReviewOnly { get; private set; }
        public bool BindingReviewRequired { get; set; }
        public string CurrentVerificationId => LastReport==null?"":LastReport.id;
        public string CurrentVerificationStatus => Running?"validation_in_progress":LastReport==null?"not_measured":LastReport.passed?"measured_pass_operator_approval_separate":"measured_fail";
        public string LastOutput { get; private set; }="";
        public bool TutorialEntryContext { get; set; }
        public event Action TutorialSkipRequested;
        private StudyGazeRayProvider subscribed;
        private GameObject[] markers;
        private Material[] ownedMaterials;
        private readonly List<float> angles=new List<float>();
        private Vector3 sumObserved,sumExpected;
        private int directionPairs;
        private readonly List<string> rows=new List<string>();
        private readonly List<TargetResult> results=new List<TargetResult>();
        private Vector3[] targets;
        private Vector3[] referenceTargets;
        private Transform referenceHead;
        private string referenceSession,referenceCalibration,referenceGeometryId;
        private bool reusedGeometry;
        private double targetStarted, previousValid=double.NaN, covered;
        private string runId, calibration, session;
        private double? startedSessionTime;
        private float runMeasure, runSettle, runMaxError, runMinCoverage, runDistance, runGap;
        private bool runAutomatic;
        private bool runIndependent;
        private bool automaticRoundStart;
        private string runCorrectionId;
        private StudyValidationSequence sequence;

        [Serializable] public sealed class TargetResult
        {
            public int target_index, sequence_ordinal, valid_callbacks;
            public double coverage, measured_seconds;
            public double? mean_error_deg, p95_error_deg;
            public float[] world_target;
            public bool passed, operator_confirmed_visible;
            public string start_method;
            public float[] mean_observed_head_local_direction,mean_expected_head_local_direction;
            public int local_direction_pairs;
        }
        [Serializable] public sealed class ValidationReport
        {
            public string schema="gazeflow-spatial-validation-v1", id, calibration_id, session_id, created_utc;
            public string time_basis="Unity realtime callback seconds; not device time or hardware sync";
            public string method="Known fixed world targets; scale-pulse cue only; angular ray-to-target error; no fitted correction; all targets must pass configured engineering thresholds";
            public string presentation_mode, abort_reason;
            public string purpose,correction_id;
            public string target_geometry_id,geometry_policy;
            public int[] target_order;
            public bool passed, aborted;
            public double? start_session_s, end_session_s;
            public float max_mean_error_deg, min_coverage, settle_seconds, measure_seconds, distance_metres, inter_target_seconds;
            public List<TargetResult> targets;
        }
        private void EnsureSubscription()
        {
            if(subscribed==provider)return;
            if(subscribed!=null)subscribed.ValidationSampleReady-=Receive;
            subscribed=provider;lastFresh=double.NegativeInfinity;
            if(subscribed!=null)subscribed.ValidationSampleReady+=Receive;
        }
        private void OnEnable()=>EnsureSubscription();
        private void OnDisable(){if(subscribed!=null)subscribed.ValidationSampleReady-=Receive;subscribed=null;lastFresh=double.NegativeInfinity;if(Running)Complete(true,"component_disabled_or_play_stopped");RemoveMarkers();}
        public void Begin(string calibrationId)
        {
            if(Running)throw new InvalidOperationException("Validation already running.");
            if(provider==null || headReference==null || string.IsNullOrWhiteSpace(calibrationId))throw new InvalidOperationException("Assign provider, tracked head and a calibration ID first.");
            if(!Finite(measureSeconds)||measureSeconds<=0||!Finite(settleSeconds)||settleSeconds<0||!Finite(distanceMetres)||distanceMetres<=0||!Finite(maximumMeanErrorDegrees)||maximumMeanErrorDegrees<=0||!Finite(minimumCoverage)||minimumCoverage<0||minimumCoverage>1||!Finite(interTargetSeconds)||interTargetSeconds<0)throw new InvalidOperationException("Invalid validation parameters.");
            if(subscribed!=provider){if(subscribed!=null)subscribed.ValidationSampleReady-=Receive;subscribed=provider;subscribed.ValidationSampleReady+=Receive;}
            runMeasure=measureSeconds;runSettle=settleSeconds;runMaxError=maximumMeanErrorDegrees;runMinCoverage=minimumCoverage;runDistance=distanceMetres;runGap=interTargetSeconds;runAutomatic=automaticSequence;
            runIndependent=independentCheckPattern;runCorrectionId=null;automaticRoundStart=false;reusedGeometry=false;
            calibration=calibrationId;session=StudyExportSession.SessionId;startedSessionTime=StudyEvents.Recording?(double?)StudyExportSession.Seconds:null;
            runId="validation-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            LastReport=null;LastOutput="";RestoredForReviewOnly=false;results.Clear();rows.Clear();sequence=null;
            rows.Add("verification_id,target_index,unity_realtime_s,session_time_s,calibration_id,source,valid,status,error_deg,target_x,target_y,target_z,device_timestamp_ms,device_time_semantics,ray_origin_x,ray_origin_y,ray_origin_z,ray_direction_x,ray_direction_y,ray_direction_z,presentation_mode,sequence_ordinal,correction_id,raw_ray_direction_x,raw_ray_direction_y,raw_ray_direction_z,observed_head_local_x,observed_head_local_y,observed_head_local_z,expected_head_local_x,expected_head_local_y,expected_head_local_z");
            Running=true;TargetIndex=0;targets=null;WaitingForSignal=true;AwaitingVisibilityConfirmation=false;lastFresh=double.NegativeInfinity;
            Event("gaze_validation_start","waiting_for_live_worn_signal","Not task data; "+Mode);
        }
        private string Mode => runAutomatic?"automatic_all_points_initial_selection":"manual_each_target_confirmation";
        private void Event(string kind,string status,string detail){if(StudyEvents.Recording)StudyEvents.Record(kind,runId,status,detail);}
        private void CreateTargets()
        {
            var offsets=new List<Vector2>{Vector2.zero,new Vector2(-12,0),new Vector2(12,0),new Vector2(0,8),new Vector2(0,-8)};
            if(!runAutomatic && nineTargets)offsets.AddRange(new[]{new Vector2(-12,8),new Vector2(12,8),new Vector2(-12,-8),new Vector2(12,-8)});
            reusedGeometry=runIndependent&&referenceTargets!=null&&referenceTargets.Length==offsets.Count&&referenceHead==headReference&&referenceSession==session&&referenceCalibration==calibration;
            if(reusedGeometry)targets=(Vector3[])referenceTargets.Clone();
            else
            {
                targets=offsets.Select(p=>headReference.TransformPoint(new Vector3(Mathf.Tan(p.x*Mathf.Deg2Rad)*runDistance,Mathf.Tan(p.y*Mathf.Deg2Rad)*runDistance,runDistance))).ToArray();
                referenceTargets=(Vector3[])targets.Clone();referenceHead=headReference;referenceSession=session;referenceCalibration=calibration;referenceGeometryId=runId+"-geometry";
            }
            WaitingForSignal=false;markers=new GameObject[targets.Length];ownedMaterials=new Material[targets.Length];
            var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
            for(int i=0;i<targets.Length;i++)
            {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);markers[i]=marker;marker.name="GazeFlow validation target "+(i+1)+" (not an AOI)";
                marker.layer=2;marker.GetComponent<Collider>().enabled=false;marker.transform.position=targets[i];marker.transform.localScale=Vector3.one*.035f;
                if(shader!=null){var material=new Material(shader);ownedMaterials[i]=material;material.color=runAutomatic?new Color(.65f,.65f,.18f):Color.yellow;marker.GetComponent<Renderer>().sharedMaterial=material;}
                marker.SetActive(runAutomatic||i==TargetIndex);
            }
            AwaitingVisibilityConfirmation=true;ClearMeasurement();
        }
        private void ClearMeasurement(){angles.Clear();covered=0;previousValid=double.NaN;sumObserved=Vector3.zero;sumExpected=Vector3.zero;directionPairs=0;}
        private void StartTarget(double now)
        {
            AwaitingVisibilityConfirmation=false;targetStarted=now;ClearMeasurement();
            markers[TargetIndex].SetActive(true);
            Event("gaze_validation_target_start",automaticRoundStart||runAutomatic&&results.Count>0?"automatic":"operator_selected","target_index="+TargetIndex+"; "+Mode);
        }
        public void StartAutomaticSequence(int firstTarget)
            => StartSequence(firstTarget,false);
        public void StartAutomaticVerificationSequence(int firstTarget)
        {
            if(!runIndependent||!reusedGeometry||string.IsNullOrEmpty(runCorrectionId))throw new InvalidOperationException("Automatic round two requires the same fixed geometry and a fresh corrected signal.");
            StartSequence(firstTarget,true);
        }
        private void StartSequence(int firstTarget,bool automaticVerification)
        {
            if(!Running||!runAutomatic||WaitingForSignal||!AwaitingVisibilityConfirmation||!HasRecentValidSignal||sequence!=null)throw new InvalidOperationException("Show the five dots, confirm wearer readiness and a live worn signal, then click ONE starting point.");
            automaticRoundStart=automaticVerification;
            sequence=new StudyValidationSequence(targets.Length,runSettle+runMeasure,runGap);
            sequence.Start(firstTarget,Time.realtimeSinceStartupAsDouble);TargetIndex=sequence.CurrentTarget;StartTarget(sequence.TargetStartedAt);
        }
        public void ConfirmTargetVisible()
        {
            if(runAutomatic)throw new InvalidOperationException("Use the five-point map to select the starting point once.");
            if(!Running||WaitingForSignal||!AwaitingVisibilityConfirmation||!HasRecentValidSignal)throw new InvalidOperationException("Wait for a live worn signal and confirm target visibility.");
            Event("gaze_validation_target_visible","operator_confirmed","target_index="+TargetIndex);StartTarget(Time.realtimeSinceStartupAsDouble);
        }
        public void RecenterPreview()
        {
            if(runIndependent)throw new InvalidOperationException("Round two keeps round-one world positions. To reposition both rounds, discard the correction and collect again.");
            if(!Running||WaitingForSignal||!AwaitingVisibilityConfirmation||results.Count!=0||!HasRecentValidSignal||sequence!=null)throw new InvalidOperationException("Only an unmeasured preview may be repositioned with a live worn signal.");
            RemoveMarkers();CreateTargets();
        }
        private void Update()
        {
            EnsureSubscription();
            if(!Running)return;
            if(session!=StudyExportSession.SessionId){Complete(true,"session_changed");return;}
            if(WaitingForSignal||AwaitingVisibilityConfirmation)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(WaitingBetweenTargets)
            {
                if(sequence.TryStartNext(now)){TargetIndex=sequence.CurrentTarget;StartTarget(now);}
                return;
            }
            // Only scale changes; the mathematical gaze target centre stays fixed.
            markers[TargetIndex].transform.localScale=Vector3.one*(.035f*(1f+.25f*Mathf.Sin((float)(now-targetStarted)*Mathf.PI*2f*1.25f)));
            var material=ownedMaterials[TargetIndex];
            if(material!=null)material.color=Color.yellow;
            if(now-targetStarted<runSettle+runMeasure)return;
            var result=EvaluateTarget(TargetIndex,targets[TargetIndex],angles,covered,runMeasure,runMaxError,runMinCoverage);
            result.local_direction_pairs=directionPairs;
            if(directionPairs>0){var a=sumObserved.normalized;var b=sumExpected.normalized;result.mean_observed_head_local_direction=new[]{a.x,a.y,a.z};result.mean_expected_head_local_direction=new[]{b.x,b.y,b.z};}
            result.sequence_ordinal=results.Count;result.operator_confirmed_visible=!automaticRoundStart&&(!runAutomatic||results.Count==0);
            result.start_method=automaticRoundStart?"automatic_verification_after_countdown":runAutomatic?(results.Count==0?"operator_selected_first_point":"automatic_after_initial_selection"):"operator_confirmed_each_point";
            results.Add(result);markers[TargetIndex].SetActive(false);
            Event("gaze_validation_target_end","measured","target_index="+TargetIndex+"; pass="+result.passed);
            if(runAutomatic)
            {
                sequence.FinishTarget(now);
                if(sequence.Finished)Complete(false,"");
                else Event("gaze_validation_gap","not_measured","gap_seconds="+runGap);
            }
            else if(++TargetIndex>=targets.Length)Complete(false,"");
            else{markers[TargetIndex].SetActive(true);AwaitingVisibilityConfirmation=true;ClearMeasurement();}
        }
        public static double? AngularError(Ray ray,Vector3 target)
        {
            if(!StudyGazeTargetLog.Finite(ray.origin)||!StudyGazeTargetLog.Finite(ray.direction)||!StudyGazeTargetLog.Finite(target)||ray.direction.sqrMagnitude<1e-8f||(target-ray.origin).sqrMagnitude<1e-8f)return null;
            return Vector3.Angle(ray.direction,target-ray.origin);
        }
        public static TargetResult EvaluateTarget(int index,Vector3 position,IEnumerable<float> errors,double coveredSeconds,double duration,double maxError,double minCoverage)
        {
            if(duration<=0||double.IsNaN(duration)||double.IsInfinity(duration)||coveredSeconds<0||double.IsNaN(coveredSeconds)||double.IsInfinity(coveredSeconds))throw new ArgumentException("Invalid duration/coverage.");
            var ordered=errors.Where(Finite).OrderBy(x=>x).ToArray();double? mean=ordered.Length==0?(double?)null:ordered.Average(x=>(double)x);
            double? p95=ordered.Length==0?(double?)null:ordered[Math.Max(0,(int)Math.Ceiling(ordered.Length*.95)-1)];double fraction=Math.Min(1,coveredSeconds/duration);
            return new TargetResult{target_index=index,world_target=new[]{position.x,position.y,position.z},valid_callbacks=ordered.Length,mean_error_deg=mean,p95_error_deg=p95,coverage=fraction,measured_seconds=duration,passed=mean.HasValue&&mean.Value<=maxError&&fraction>=minCoverage};
        }
        private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        private void Receive(StudyGazeRaySample sample)
        {
            var expected=Running?calibration:LastReport?.calibration_id;
            if(sample.valid&&sample.calibrationId==expected)lastFresh=Time.realtimeSinceStartupAsDouble;
            else if(sample.status=="not_worn"||sample.status=="missing_head_reference"||sample.status=="no_live_client_or_simulation")lastFresh=double.NegativeInfinity;
            if(!Running)return;
            string correction=sample.correctionId??"";
            if(sample.valid){if(runCorrectionId==null)runCorrectionId=correction;else if(runCorrectionId!=correction){Complete(true,"correction_changed_during_measurement");return;}}
            if(WaitingForSignal){if(HasRecentValidSignal)CreateTargets();return;}
            if(AwaitingVisibilityConfirmation||WaitingBetweenTargets)return;
            double now=Time.realtimeSinceStartupAsDouble,elapsed=now-targetStarted-runSettle;
            if(elapsed<0||elapsed>=runMeasure)return;
            double? error=sample.valid&&sample.calibrationId==calibration?AngularError(sample.worldRay,targets[TargetIndex]):null;
            if(error.HasValue){angles.Add((float)error.Value);if(!double.IsNaN(previousValid)&&now-previousValid>0&&now-previousValid<=.1)covered+=now-previousValid;previousValid=now;}
            else previousValid=double.NaN;
            var t=targets[TargetIndex];
            var raw=sample.hasRawWorldRay?sample.rawWorldRay:sample.worldRay;
            Vector3 localObserved=Vector3.zero,localExpected=Vector3.zero;
            if(error.HasValue&&headReference!=null){localObserved=headReference.InverseTransformDirection(raw.direction).normalized;localExpected=headReference.InverseTransformDirection(t-raw.origin).normalized;sumObserved+=localObserved;sumExpected+=localExpected;directionPairs++;}
            rows.Add(NeonStudyRecorder.Csv(runId,TargetIndex,now,StudyExportSession.SessionId==session&&session!=null?(object)StudyExportSession.Seconds:"",sample.calibrationId,sample.source,error.HasValue,sample.status,error.HasValue?(object)error.Value:"",t.x,t.y,t.z,sample.deviceTimestampMs>0?(object)sample.deviceTimestampMs:"",sample.deviceTimeSemantics,error.HasValue?(object)sample.worldRay.origin.x:"",error.HasValue?(object)sample.worldRay.origin.y:"",error.HasValue?(object)sample.worldRay.origin.z:"",error.HasValue?(object)sample.worldRay.direction.x:"",error.HasValue?(object)sample.worldRay.direction.y:"",error.HasValue?(object)sample.worldRay.direction.z:"",Mode,results.Count,correction,error.HasValue?(object)raw.direction.x:"",error.HasValue?(object)raw.direction.y:"",error.HasValue?(object)raw.direction.z:"",error.HasValue?(object)localObserved.x:"",error.HasValue?(object)localObserved.y:"",error.HasValue?(object)localObserved.z:"",error.HasValue?(object)localExpected.x:"",error.HasValue?(object)localExpected.y:"",error.HasValue?(object)localExpected.z:""));
        }
        public void Abort(){if(Running)Complete(true,"operator_abort");}
        public void SkipTutorialValidation()
        {
            if(!TutorialEntryContext)throw new InvalidOperationException("This is not a tutorial entry check.");
            if(Running)Complete(true,"operator_skipped_tutorial_validation");
            Event("gaze_validation_skipped","mapping_unverified","Explicit operator skip; not a passed validation.");
            TutorialSkipRequested?.Invoke();
        }
        public void RestoreSameSessionReportForReview(string directory)
        {
            if(Running)throw new InvalidOperationException("Cannot restore a report during validation.");
            string file=Path.Combine(directory,"validation.json");
            if(StudyExportSession.Folder==null)throw new InvalidOperationException("No active study session.");
            StudyArchivePaths.RequireContainedUnlinked(StudyExportSession.Folder,file);
            if(new FileInfo(file).Length>1024*1024)throw new InvalidDataException("Validation report too large.");
            var report=JsonConvert.DeserializeObject<ValidationReport>(File.ReadAllText(file));
            if(report==null || report.schema!="gazeflow-spatial-validation-v1" || report.session_id!=StudyExportSession.SessionId || string.IsNullOrWhiteSpace(report.id) || string.IsNullOrWhiteSpace(report.calibration_id))throw new InvalidDataException("Validation does not belong to the active session.");
            LastReport=report;LastOutput=directory;RestoredForReviewOnly=true;lastFresh=double.NegativeInfinity;EnsureSubscription(); // Metadata only. Never approves a new provider or head binding.
        }
        private void Complete(bool aborted,string reason)
        {
            Running=false;WaitingForSignal=false;AwaitingVisibilityConfirmation=false;
            LastReport=new ValidationReport{id=runId,calibration_id=calibration,session_id=session,created_utc=DateTime.UtcNow.ToString("O"),start_session_s=startedSessionTime,end_session_s=StudyExportSession.SessionId==session&&session!=null?(double?)StudyExportSession.Seconds:null,passed=!aborted&&targets!=null&&results.Count==targets.Length&&results.All(r=>r.passed),aborted=aborted,abort_reason=reason,presentation_mode=Mode,target_order=sequence?.Order??results.Select(r=>r.target_index).ToArray(),inter_target_seconds=runAutomatic?runGap:0,max_mean_error_deg=runMaxError,min_coverage=runMinCoverage,settle_seconds=runSettle,measure_seconds=runMeasure,distance_metres=runDistance,targets=new List<TargetResult>(results)};
            LastReport.purpose=runIndependent?"independent_live_verification":"fit_collection_or_uncorrected_validation";LastReport.correction_id=runCorrectionId??"";
            LastReport.target_geometry_id=referenceGeometryId;LastReport.geometry_policy=reusedGeometry?"same_fixed_world_targets_NEW_measurement_samples":"new_world_anchor_for_current_head_binding";
            LastReport.method="Known fixed world targets; angular ray-to-target error; "+(string.IsNullOrEmpty(runCorrectionId)?"uncorrected ray":"bounded fitted direction correction, NEW measurement samples")+"; all targets must pass project-specific engineering thresholds; repeat-layout evidence is not full-scene accuracy.";
            RemoveMarkers();sequence=null;
            Event("gaze_validation_end",aborted?"aborted":LastReport.passed?"measured_pass":"measured_fail","reason="+reason+"; Operator approval separate.");
            try
            {
                string root=StudyExportSession.SessionId==session&&StudyExportSession.Folder!=null?Path.Combine(StudyExportSession.Folder,"Calibration"):Path.Combine(StudyExportSession.OutputRoot,"ValidationChecks");
                string directory=Path.Combine(root,runId);if(Directory.Exists(directory))throw new IOException("Validation ID already exists.");Directory.CreateDirectory(directory);
                File.WriteAllLines(Path.Combine(directory,"validation_samples.csv"),rows);StudyAtomicFile.Write(Path.Combine(directory,"validation.json"),JsonConvert.SerializeObject(LastReport,Formatting.Indented));LastOutput=directory;
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){LastOutput="SAVE FAILED: "+e.Message;Debug.LogError("[GazeFlow validation] "+LastOutput);}
        }
        private void RemoveMarkers()
        {
            if(markers==null)return;
            for(int i=0;i<markers.Length;i++)
            {
                var marker=markers[i];var material=ownedMaterials==null?null:ownedMaterials[i];
                if(Application.isPlaying){if(material!=null)Destroy(material);Destroy(marker);}
                else{if(material!=null)DestroyImmediate(material);DestroyImmediate(marker);}
            }
            markers=null;ownedMaterials=null;
        }
    }
}
