using UnityEngine;

namespace NeonResearch
{
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
