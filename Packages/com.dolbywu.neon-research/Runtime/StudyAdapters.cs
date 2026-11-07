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

}
