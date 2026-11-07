using UnityEngine;

namespace NeonResearch
{
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
}
