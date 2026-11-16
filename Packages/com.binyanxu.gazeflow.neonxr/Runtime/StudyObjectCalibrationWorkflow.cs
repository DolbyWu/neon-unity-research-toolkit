using System;
using System.Linq;
using NeonResearch;
using UnityEngine;
namespace NeonResearch.NeonXR
{
    /// <summary>Three operator captures, one fit, NEW automatic three-object verification. No implicit approval.</summary>
    public sealed class StudyObjectCalibrationWorkflow:MonoBehaviour
    {
        public enum Phase { Idle, Collecting, Countdown, Verifying, Review, Error, Done }
        public NeonXRResearchProvider provider;
        public StudyResearchTarget[] objects=new StudyResearchTarget[3];
        public string[] roleNames={"Reference A","Reference B","Reference C"};
        public Phase State {get;private set;}
        public string Message {get;private set;}="Bind three actual reference targets before this optional check.";
        public double CountdownRemaining {get;private set;}=3;
        public event Action Finished;
        private Vector3[] aimPoints,positions;private Quaternion[] rotations;private string session,calibration;private Transform head;private double countdown=-1,lastTick=-1;
        public StudySpatialValidation Check=>provider==null?null:provider.GetComponent<StudySpatialValidation>();
        public void Begin()
        {
            if(!StudyEvents.Recording||provider==null||provider.headReference==null)throw new InvalidOperationException("An active session, actual provider and tracked head are required.");
            if(objects==null||objects.Length!=3||objects.Any(t=>t==null||!t.isActiveAndEnabled||!t.HasUniqueId())||objects.Select(t=>t.targetId).Distinct().Count()!=3)throw new InvalidOperationException("Bind three active unique targets; do not guess from object names.");
            var check=Check??provider.gameObject.AddComponent<StudySpatialValidation>();if(check.Running)throw new InvalidOperationException("Finish the current measurement first.");
            var recorders=UnityEngine.Object.FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Where(r=>r.isActiveAndEnabled&&r.provider==provider).ToArray();
            if(recorders.Length!=1)throw new InvalidOperationException("Exactly one active target recorder bound to this provider is required.");
            check.objectCollisionMask=recorders[0].collisionMask.value;check.objectRayDistance=recorders[0].maxDistance;
            provider.ClearCorrectionForObjectCollection();check.provider=provider;check.headReference=provider.headReference;
            session=StudyExportSession.SessionId;calibration=provider.calibrationId;head=provider.headReference;
            positions=objects.Select(t=>t.transform.position).ToArray();rotations=objects.Select(t=>t.transform.rotation).ToArray();
            aimPoints=objects.Select(t=>AimPoint(t,head.position,check.objectCollisionMask,check.objectRayDistance)).ToArray();
            check.BeginObjectTargets(calibration,objects,aimPoints,false);State=Phase.Collecting;Message="ROUND 1: follow the cue on each of your three configured reference objects in order. Click Capture once for EACH object. Keep the objects still.";
        }
        public static Vector3 AimPoint(StudyResearchTarget target,Vector3 head,int mask,float range)
        {
            var colliders=target.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
            foreach(var collider in colliders)
            {
                var ray=new Ray(head,collider.bounds.center-head);
                if(!collider.Raycast(ray,out var surface,range))continue;
                var hit=StudyGazeTargetLog.Query(ray,mask,range);
                if(hit.target==target&&hit.status=="target_hit")return surface.point-ray.direction.normalized*.001f;
            }
            throw new InvalidOperationException("No visible solid surface for "+target.DisplayName+". Face the tutorial objects and remove real occlusion; no colliders were changed.");
        }
        public void CaptureCurrent()
        {
            if(State!=Phase.Collecting)throw new InvalidOperationException("Not collecting object calibration.");
            Check.ConfirmTargetVisible();
        }
        public void Finish(bool approve)
        {
            if(approve&&(session!=StudyExportSession.SessionId||provider.headReference!=head||provider.calibrationId!=calibration))throw new InvalidOperationException("Calibration context changed. No approval transferred.");
            if(approve){if(State!=Phase.Review||Check.LastReport==null||!Check.LastReport.passed)throw new InvalidOperationException("A saved passing NEW object verification is required.");provider.ApproveCurrentMapping();}
            else{provider.mappingVerified=false;if(Check!=null&&Check.Running)Check.Abort();StudyEvents.Record("object_calibration_skipped","tutorial_objects","mapping_unverified","Explicit continuation; raw/low-precision records are not accepted AOI.");}
            State=Phase.Done;Message=approve?"Measured mapping approved for this binding. Continue the original tutorial.":"Optional check skipped. Usable rays and observed hits continue; spatial accuracy remains unverified.";Finished?.Invoke();
        }
        private void Update()=>Tick(Time.realtimeSinceStartupAsDouble);
        public void Tick(double now)
        {
            if(State==Phase.Idle||State==Phase.Done||State==Phase.Error)return;
            if(!StudyEvents.Recording||session!=StudyExportSession.SessionId||provider==null||provider.calibrationId!=calibration||provider.headReference!=head||Check==null){State=Phase.Error;Message="Session, mount ID or tracked head changed. No approval transferred; start a new object collection.";if(provider!=null)provider.mappingVerified=false;return;}
            if(objects.Where((t,i)=>t==null||!t.isActiveAndEnabled||!t.HasUniqueId()||Vector3.Distance(t.transform.position,positions[i])>.01f||Quaternion.Angle(t.transform.rotation,rotations[i])>1).Any())
            {if(Check.Running)Check.Abort();State=Phase.Error;provider.mappingVerified=false;Message="A target moved or its identity changed. Put the objects down and repeat the three captures.";return;}
            if(Check.LastReport!=null&&Check.LastReport.aborted){State=Phase.Error;Message="Object measurement aborted. Retry or explicitly continue unverified.";return;}
            if(State==Phase.Collecting&&!Check.Running)
            {
                try{provider.FitObjectTargetsAndBeginVerification(objects,aimPoints);State=Phase.Countdown;countdown=lastTick=-1;Message="Correction saved. ROUND 2 follows automatically at the SAME three objects; no more clicks.";}
                catch(Exception error){State=Phase.Error;Message=error.Message+"\nRetry all three objects; prior data remain saved.";StudyEvents.Record("object_calibration_fit_failed","tutorial_objects","mapping_unverified",error.Message);}
            }
            if(State==Phase.Countdown)
            {
                bool continuous=lastTick>=0&&now>=lastTick&&now-lastTick<=.25;lastTick=now;
                if(Check.WaitingForSignal||!Check.HasRecentValidSignal){countdown=-1;CountdownRemaining=3;Message="ROUND 2 waiting for fresh worn eye data. Countdown will restart; no click needed.";return;}
                if(!Check.ReusesReferenceGeometry){Check.Abort();State=Phase.Error;Message="Object geometry changed; repeat calibration.";return;}
                if(!continuous)countdown=-1;if(countdown<0)countdown=now;CountdownRemaining=Math.Max(0,3-(now-countdown));
                Message="ROUND 2 starts in "+CountdownRemaining.ToString("F1")+" s. Look at your three configured reference objects in order as each cue pulses. Keep them still.";
                if(CountdownRemaining==0){try{Check.StartAutomaticVerificationSequence(0);State=Phase.Verifying;Message="ROUND 2: follow the pulsing cues; NEW samples and actual target IDs are checked. No clicks needed.";}catch(Exception error){State=Phase.Error;Message=error.Message;}}
            }
            if(State==Phase.Verifying&&!Check.Running){State=Phase.Review;Message=Check.LastReport!=null&&Check.LastReport.passed?"NEW object verification PASSED. Review target names, hit fractions and coverage, then Approve & continue tutorial.":"NEW object verification FAILED. Retry or explicitly continue unverified. A fitted correction is not an accuracy pass.";}
        }
    }
}
