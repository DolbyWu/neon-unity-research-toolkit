using System;
using System.Linq;
using System.Reflection;
using NeonResearch;
using NUnit.Framework;
using UnityEngine;

namespace EscapeRoom.ReflectiveAgent.Tests
{
    public sealed class TutorialReferenceDotTests
    {
        private const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        private static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
        private sealed class Fixture:IDisposable
        {
            public readonly GameObject host,body,head;
            public readonly Component gate,reference;
            public readonly StudyResearchTarget target;
            public readonly Type type=Find("GazeFlowTutorialReferenceDot");
            public Fixture(GameObject actual=null)
            {
                host=new GameObject("Synthetic reference controller");body=actual??GameObject.CreatePrimitive(PrimitiveType.Cube);body.transform.position=new Vector3(1000,1000,1000);
                if(actual==null)body.transform.localScale=new Vector3(.08f,.16f,.08f);
                target=body.GetComponent<StudyResearchTarget>()??body.AddComponent<StudyResearchTarget>();target.targetId="synthetic-reference-"+Guid.NewGuid().ToString("N");
                host.SetActive(false);gate=host.AddComponent(Find("HandTutorialNextGate"));gate.GetType().GetMethod("RequireOperatorStart").Invoke(gate,null);host.SetActive(true);
                var renderers=body.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                head=new GameObject("Synthetic reference head");head.transform.position=bounds.center+new Vector3(0,bounds.size.y*.9f,-Mathf.Max(.5f,bounds.size.y*3));
                reference=host.AddComponent(type);type.GetMethod("Bind").Invoke(reference,new object[]{target,head.transform,gate,null,~0,30f});Physics.SyncTransforms();
            }
            public void Tick()=>type.GetMethod("TickReference").Invoke(reference,null);
            public bool Visible=>(bool)type.GetProperty("ReferenceVisible").GetValue(reference);
            public GameObject Dot=>(GameObject)type.GetField("dot",Hidden).GetValue(reference);
            public void Dispose(){type.GetMethod("OnDestroy",Hidden).Invoke(reference,null);UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(head);}
        }
        [Test] public void LowerYellowDiscHasNoPhysicsAndCannotReplaceTheOriginalAoiHit()
        {
            using(var f=new Fixture())
            {
                string session=StudyExportSession.SessionId;int colliders=f.body.GetComponentsInChildren<Collider>(true).Length;
                f.Tick();Assert.That(f.Visible,Is.True,(string)f.type.GetProperty("Status").GetValue(f.reference));
                Assert.That(((Vector3)f.type.GetField("localPoint",Hidden).GetValue(f.reference)).y,Is.LessThan(0));
                Assert.That(f.Dot.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(f.Dot.GetComponent<Rigidbody>(),Is.Null);Assert.That(f.Dot.GetComponent<StudyResearchTarget>(),Is.Null);Assert.That(f.Dot.layer,Is.EqualTo(2));
                Assert.That(f.Dot.transform.lossyScale.x,Is.EqualTo(.032f).Within(.00001f));Assert.That((Color32)f.Dot.GetComponent<Renderer>().sharedMaterial.color,Is.EqualTo(new Color32(255,220,50,255)));
                var ray=new Ray(f.head.transform.position,f.Dot.transform.position-f.head.transform.position);var visible=StudyGazeTargetLog.Query(ray,~0,30);Assert.That(visible.target,Is.SameAs(f.target));Assert.That(visible.geometry,Is.EqualTo("solid_surface"));
                Assert.That(f.body.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(colliders));Assert.That(StudyExportSession.SessionId,Is.EqualTo(session));
            }
        }
        [Test] public void ReferenceStaysOnObjectRatherThanFollowingHeadAndHidesOnTutorialStart()
        {
            using(var f=new Fixture())
            {
                f.Tick();Assert.That(f.Visible,Is.True);var original=f.Dot.transform.position;
                f.head.transform.position+=Vector3.right*.15f;f.head.transform.rotation=Quaternion.Euler(0,25,0);f.Tick();Assert.That(Vector3.Distance(f.Dot.transform.position,original),Is.LessThan(.0001f));
                f.body.transform.position+=Vector3.right*.2f;f.Tick();Assert.That(Vector3.Distance(f.Dot.transform.position,original+Vector3.right*.2f),Is.LessThan(.0001f));
                f.gate.GetType().GetMethod("StartTutorialNarration").Invoke(f.gate,null);f.Tick();Assert.That(f.Visible,Is.False);Assert.That(f.body.GetComponent<Collider>().enabled,Is.True);
            }
        }
        [Test] public void OcclusionPreventsPlacementAndOptionalCheckTemporarilyHidesReference()
        {
            using(var f=new Fixture())
            {
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
                try{
                    wall.transform.position=Vector3.Lerp(f.head.transform.position,f.body.transform.position,.5f);wall.transform.localScale=Vector3.one*.4f;Physics.SyncTransforms();f.Tick();Assert.That(f.Visible,Is.False);Assert.That(f.Dot,Is.Null);
                    wall.SetActive(false);f.type.GetField("nextAttempt",Hidden).SetValue(f.reference,0d);Physics.SyncTransforms();f.Tick();Assert.That(f.Visible,Is.True);
                    var check=f.host.AddComponent<StudySpatialValidation>();f.type.GetField("check",Hidden).SetValue(f.reference,check);
                    var running=typeof(StudySpatialValidation).GetField("<Running>k__BackingField",Hidden);running.SetValue(check,true);f.Tick();Assert.That(f.Visible,Is.False);running.SetValue(check,false);f.Tick();Assert.That(f.Visible,Is.True);
                }finally{UnityEngine.Object.DestroyImmediate(wall);}
            }
        }
        [Test] public void ExistingAuthoredChessPrefabSupportsAVisibleLowerSurfaceWithoutColliderEdits()
        {
            var path=UnityEditor.AssetDatabase.GUIDToAssetPath("5b2febef9538fb34e972bb0070c107a5");var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null,"Expected the existing authored chess prefab; no name-based replacement.");
            using(var f=new Fixture(UnityEngine.Object.Instantiate(prefab)))
            {
                var colliders=f.body.GetComponentsInChildren<Collider>(true);var states=colliders.Select(c=>c.enabled).ToArray();f.Tick();Assert.That(f.Visible,Is.True,(string)f.type.GetProperty("Status").GetValue(f.reference));Assert.That(colliders.Select(c=>c.enabled).ToArray(),Is.EqualTo(states));Assert.That(f.Dot.GetComponents<Collider>(),Is.Empty);
            }
        }
    }
}
