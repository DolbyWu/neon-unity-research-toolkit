using System;
using UnityEngine;

namespace NeonResearch
{
    public static class StudySessionSettings { public static string Condition { get; set; }="baseline"; }

    /// <summary>Explicit game adapter. Events outside an active session do not start recording.</summary>
    public static class StudyEvents
    {
        public static bool Recording=>StudyExportSession.Folder!=null&&!StudyMonitorData.Stopped;
        public static bool Record(string kind,string objectId="",string outcome="",string detail="",string task="")
        {
            if(!Recording)return false;StudySequence.Record(kind,task,objectId,"",outcome,detail);return true;
        }
        public static bool Grab(string hand,Component target,string objectId=null)
        {if(!Recording)return false;StudySequence.Grab(hand,target,objectId);return true;}
        public static bool Release(string hand)=>Grab(hand,null);
    }

    [AddComponentMenu("Neon Research/Study Region")]
    public sealed class StudyRegion:MonoBehaviour
    {
        public string regionId="Region";
        public Collider regionCollider;
        private void Reset(){regionCollider=GetComponent<Collider>();}
    }

    [AddComponentMenu("Neon Research/Head Pose Source")]
    public sealed class StudyHeadPoseSource:MonoBehaviour
    {
        public Transform head;
        private static StudyHeadPoseSource active;
        public static Transform Head=>active!=null?(active.head!=null?active.head:active.transform):(Camera.main!=null?Camera.main.transform:null);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetStatic(){active=null;}
        private void OnEnable(){active=this;}private void OnDisable(){if(active==this)active=null;}
        private void Reset(){head=transform;}
    }

    /// <summary>Wire these public methods to XRI/Meta/other grab/release UnityEvents explicitly.</summary>
    [AddComponentMenu("Neon Research/Interaction Adapter")]
    public sealed class StudyInteractionAdapter:MonoBehaviour
    {
        public string objectId="";
        public void GrabLeft()=>StudyEvents.Grab("Left",transform,Id);
        public void ReleaseLeft()=>StudyEvents.Release("Left");
        public void GrabRight()=>StudyEvents.Grab("Right",transform,Id);
        public void ReleaseRight()=>StudyEvents.Release("Right");
        public void MarkAttempt()=>StudyEvents.Record("attempt",Id);
        public void MarkSuccess()=>StudyEvents.Record("success",Id,"success");
        public void MarkFailure()=>StudyEvents.Record("failure",Id,"failure");
        private string Id=>string.IsNullOrWhiteSpace(objectId)?gameObject.scene.path+":"+name:objectId;
    }
}
