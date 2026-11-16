using UnityEngine;
using UnityEngine.Rendering;

namespace NeonResearch.NeonXR
{
    /// <summary>An authored visual reference, not an AOI, collider, fitting step or quality approval.</summary>
    [AddComponentMenu("GazeFlow/Visual Gaze Reference Point")]
    public sealed class StudyGazeReferencePoint:MonoBehaviour
    {
        [Min(.001f)] public float diameterMetres=.032f;
        public bool showReference;
        public Color color=new Color32(255,220,50,255);
        private GameObject visual;
        private Mesh mesh;
        private Material material;
        public bool ReferenceVisible=>visual!=null&&visual.activeSelf;
        public string Status {get;private set;}="Place this Transform on a visible stable reference surface, then show it explicitly.";
        public void SetVisible(bool value){showReference=value;UpdateVisual();}
        private void LateUpdate()=>UpdateVisual();
        public void UpdateVisual()
        {
            if(!showReference||!isActiveAndEnabled||!Application.isPlaying){if(visual!=null)visual.SetActive(false);return;}
            if(float.IsNaN(diameterMetres)||float.IsInfinity(diameterMetres)||diameterMetres<.001f||diameterMetres>.5f){Status="Choose a finite reference diameter from 0.001 to 0.5 metres.";if(visual!=null)visual.SetActive(false);return;}
            if(visual==null&&!CreateVisual())return;
            visual.transform.SetPositionAndRotation(transform.position,Quaternion.identity);
            visual.transform.localScale=Vector3.one*diameterMetres;visual.SetActive(true);
            Status="Fixed authored visual reference. Look here while adjusting cyan research gaze; this is not measured accuracy.";
        }
        private bool CreateVisual()
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
            if(shader==null){Status="No supported unlit reference shader found; no physics fallback created.";return false;}
            const int rings=12,segments=24;var vertices=new Vector3[(rings+1)*(segments+1)];var triangles=new int[rings*segments*6];
            for(int r=0;r<=rings;r++)for(int s=0;s<=segments;s++){float phi=Mathf.PI*r/rings,theta=2*Mathf.PI*s/segments;vertices[r*(segments+1)+s]=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta))*.5f;}
            int index=0;for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){int a=r*(segments+1)+s,b=a+segments+1;triangles[index++]=a;triangles[index++]=a+1;triangles[index++]=b;triangles[index++]=a+1;triangles[index++]=b+1;triangles[index++]=b;}
            mesh=new Mesh{name="GazeFlow reference sphere (visual only)",hideFlags=HideFlags.DontSave};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
            material=new Material(shader){name="GazeFlow visual reference",hideFlags=HideFlags.DontSave,color=color};if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            visual=new GameObject("[GazeFlow] Visual reference only"){layer=2,hideFlags=HideFlags.DontSave};
            visual.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return true; // No primitive factory, Collider, Rigidbody, ResearchTarget or depth override.
        }
        private void OnDisable(){if(visual!=null)visual.SetActive(false);}
        private void OnDestroy()
        {
            foreach(var owned in new Object[]{visual,mesh,material})if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
            visual=null;mesh=null;material=null;
        }
        private void OnDrawGizmos(){Gizmos.color=color;Gizmos.DrawWireSphere(transform.position,Mathf.Clamp(diameterMetres,.001f,.5f)*.5f);}
    }
}
