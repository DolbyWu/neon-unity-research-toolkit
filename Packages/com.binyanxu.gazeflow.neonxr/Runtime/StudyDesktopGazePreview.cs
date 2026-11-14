using UnityEngine;

namespace NeonResearch.NeonXR
{
    /// <summary>Display-only preview of exactly the research callback ray. Never invokes gameplay hits or logs samples.</summary>
    public sealed class StudyDesktopGazePreview:MonoBehaviour
    {
        public LayerMask collisionMask=~0;
        public float maxDistance=30,staleAfterSeconds=.1f;
        private StudyGazeRayProvider subscribed;
        private StudyGazeRayProvider bound;
        private StudyGazeRaySample latest;
        private double received=double.NegativeInfinity;
        private GameObject dot;
        private Material material;
        private bool visible;
        public bool Visible {get=>visible;set{visible=value;if(!visible)Invalidate();}}
        public void Bind(StudyGazeRayProvider provider)
        {
            bound=provider;
            var desired=isActiveAndEnabled?provider:null;
            if(ReferenceEquals(desired,subscribed))return;
            if(!ReferenceEquals(subscribed,null))subscribed.SampleReady-=Receive;
            subscribed=desired;Invalidate();
            if(isActiveAndEnabled&&subscribed!=null)subscribed.SampleReady+=Receive;
        }
        public void Invalidate(){received=double.NegativeInfinity;if(dot!=null)dot.SetActive(false);}
        private void Receive(StudyGazeRaySample sample){latest=sample;received=Time.realtimeSinceStartupAsDouble;}
        public static bool CanDisplay(StudyGazeRaySample sample,double age,float maximumAge)=>sample.valid&&age>=0&&age<=maximumAge&&maximumAge>0&&!float.IsNaN(maximumAge)&&!float.IsInfinity(maximumAge)&&StudyGazeTargetLog.Finite(sample.worldRay.origin)&&StudyGazeTargetLog.Finite(sample.worldRay.direction)&&sample.worldRay.direction.sqrMagnitude>.000001f;
        private void LateUpdate()
        {
            if(!visible||subscribed==null||!subscribed.isActiveAndEnabled||!CanDisplay(latest,Time.realtimeSinceStartupAsDouble-received,staleAfterSeconds)){if(dot!=null)dot.SetActive(false);return;}
            if(dot==null)
            {
                dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="[GazeFlow] Research gaze preview (not an AOI)";dot.hideFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;dot.layer=2;
                dot.GetComponent<Collider>().enabled=false;dot.transform.SetParent(transform,true);dot.transform.localScale=Vector3.one*.015f;
                var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
                if(shader!=null){material=new Material(shader){color=Color.cyan};dot.GetComponent<Renderer>().sharedMaterial=material;}
            }
            var result=StudyGazeTargetLog.Query(latest.worldRay,collisionMask,maxDistance);
            if(result.status=="geometry_query_overflow"||result.status=="invalid_ray"){dot.SetActive(false);return;}
            // Free-space location is a visual fallback, never a synthetic AOI hit.
            var point=string.IsNullOrEmpty(result.geometry)?new Ray(latest.worldRay.origin,latest.worldRay.direction.normalized).GetPoint(Mathf.Min(2,maxDistance)):result.point;
            dot.transform.position=point;dot.SetActive(true);
        }
        private void OnEnable()=>Bind(bound);
        private void OnDisable(){if(!ReferenceEquals(subscribed,null))subscribed.SampleReady-=Receive;subscribed=null;Invalidate();}
        private void OnDestroy(){if(Application.isPlaying){if(dot!=null)Destroy(dot);if(material!=null)Destroy(material);}else{if(dot!=null)DestroyImmediate(dot);if(material!=null)DestroyImmediate(material);}}
    }
}
