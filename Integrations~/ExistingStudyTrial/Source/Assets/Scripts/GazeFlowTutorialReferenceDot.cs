#if UNITY_EDITOR
using System;
using System.Linq;
using NeonResearch;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Fixed visual-only preparation cue on an existing target surface; never a new AOI or calibration.</summary>
public sealed class GazeFlowTutorialReferenceDot:MonoBehaviour
{
    private StudyResearchTarget target;
    private Transform head;
    private HandTutorialNextGate gate;
    private StudySpatialValidation check;
    private int mask=~0;
    private float range=30;
    private const float Diameter=.032f;
    private Vector3 localPoint,localNormal;
    private bool placed;
    private double nextAttempt;
    private GameObject dot;
    private Mesh mesh;
    private Material material;
    public string Status {get;private set;}="Waiting for the tutorial reference object.";
    public bool ReferenceVisible=>dot!=null&&dot.activeSelf;
    public void Bind(StudyResearchTarget actualTarget,Transform actualHead,HandTutorialNextGate tutorialGate,StudySpatialValidation qualityCheck,int collisionMask,float maximumDistance)
    {
        if(actualTarget==null||actualHead==null||tutorialGate==null)throw new ArgumentException("An actual target, tracked head and tutorial gate are required.");
        target=actualTarget;head=actualHead;gate=tutorialGate;check=qualityCheck;mask=collisionMask;range=maximumDistance;
        placed=false;nextAttempt=0;Hide();
    }
    private void LateUpdate()=>TickReference();
    public void TickReference()
    {
        if(target==null||head==null||gate==null||!target.isActiveAndEnabled){Hide();Status="Reference object unavailable; no substitute target was created.";return;}
        if(!gate.AwaitingOperatorStart){Hide();Status="Reference hidden — tutorial has started.";return;}
        if(check!=null&&check.Running){Hide();Status="Reference hidden during the optional accuracy check.";return;}
        if(!placed)
        {
            if(Time.realtimeSinceStartupAsDouble<nextAttempt)return;nextAttempt=Time.realtimeSinceStartupAsDouble+.25;
            if(!TryAnchor()){Hide();return;}
            if(!CreateVisual()){Hide();return;}
            placed=true;
        }
        var normal=target.transform.TransformDirection(localNormal).normalized;
        dot.transform.SetPositionAndRotation(target.transform.TransformPoint(localPoint)+normal*.0015f,Quaternion.FromToRotation(Vector3.forward,normal));
        dot.SetActive(true);Status="Look at the fixed yellow dot on the lower part of "+target.DisplayName+". Move cyan gaze to it, Save, Hide pointer, then Start tutorial. Visual reference only — not accuracy approval.";
    }
    private bool TryAnchor()
    {
        if(!target.HasUniqueId()){Status="Reference ID is missing or duplicated; no dot shown.";return false;}
        var renderers=target.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(renderers.Length==0){Status="Reference mesh is not visible yet.";return false;}
        Bounds bounds=default;bool first=true;
        foreach(var renderer in renderers)
        {
            var b=renderer.localBounds;
            for(int i=0;i<8;i++)
            {
                var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var point=target.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
            }
        }
        // Choose a visible LOWER surface once; subsequent head motion never moves the reference.
        foreach(float height in new[]{.25f,.18f,.32f,.4f,.1f})
        {
            var aim=target.transform.TransformPoint(new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,height),bounds.center.z));
            var ray=new Ray(head.position,aim-head.position);
            if(!Physics.Raycast(ray,out var hit,range,mask,QueryTriggerInteraction.Ignore)||hit.collider.GetComponentInParent<StudyResearchTarget>()!=target)continue;
            var local=target.transform.InverseTransformPoint(hit.point);
            if(local.y>bounds.center.y+.0001f)continue;
            localPoint=local;localNormal=target.transform.InverseTransformDirection(hit.normal).normalized;return true;
        }
        Status="No visible lower solid surface on "+target.DisplayName+". Face the object and check real occlusion; no collider or object was changed.";return false;
    }
    private bool CreateVisual()
    {
        if(dot!=null)return true;
        var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
        if(shader==null){Status="Reference shader unavailable; no pink fallback or geometry change.";return false;}
        const int segments=48;var vertices=new Vector3[segments+1];var triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            float a=i*Mathf.PI*2/segments;vertices[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.5f;
            int j=i*6,next=(i+1)%segments+1;triangles[j]=0;triangles[j+1]=i+1;triangles[j+2]=next;triangles[j+3]=0;triangles[j+4]=next;triangles[j+5]=i+1;
        }
        mesh=new Mesh{name="GazeFlow visual-only yellow reference disc",hideFlags=HideFlags.DontSave};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();
        material=new Material(shader){name="GazeFlow yellow reference",hideFlags=HideFlags.DontSave,color=new Color32(255,220,50,255)};
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",new Color32(255,220,50,255));
        dot=new GameObject("[GazeFlow] Chess reference dot (visual only)"){layer=2,hideFlags=HideFlags.DontSave};dot.transform.SetParent(transform,false);dot.transform.localScale=Vector3.one*Diameter;
        dot.AddComponent<MeshFilter>().sharedMesh=mesh;var display=dot.AddComponent<MeshRenderer>();display.sharedMaterial=material;display.shadowCastingMode=ShadowCastingMode.Off;display.receiveShadows=false;
        return true; // No collider, Rigidbody, target, interaction component or depth-test override is added.
    }
    private void Hide(){if(dot!=null)dot.SetActive(false);}
    private void OnDisable()=>Hide();
    private void OnDestroy()
    {
        foreach(var owned in new UnityEngine.Object[]{dot,mesh,material})if(owned!=null){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
        dot=null;mesh=null;material=null;
    }
}
#endif
