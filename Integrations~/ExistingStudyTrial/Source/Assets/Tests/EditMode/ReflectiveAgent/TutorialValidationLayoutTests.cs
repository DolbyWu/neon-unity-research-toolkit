using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace EscapeRoom.ReflectiveAgent.Tests
{
    public sealed class TutorialValidationLayoutTests
    {
        private static Pose Layout(Vector3 origin, Vector3 forward)
        {
            var type=Type.GetType("TeleportInFront, Assembly-CSharp",true);
            return (Pose)type.GetMethod("ComputeFixedStationPose",BindingFlags.Public|BindingFlags.Static)
                .Invoke(null,new object[]{origin,forward,180f,.5f,-.8f,0f});
        }
        [Test]
        public void GrabbingStationIsOppositeEntryNotOverFivePointTest()
        {
            var origin=new Vector3(3,1.7f,9);
            var pose=Layout(origin,Vector3.forward);
            Assert.That(Vector3.Distance(pose.position,origin+new Vector3(0,-.8f,-.5f)),Is.LessThan(.0001f));
            Assert.That(Vector3.Angle(pose.rotation*Vector3.forward,Vector3.back),Is.LessThan(.001f));
        }
        [Test]
        public void LayoutUsesHorizontalEntryHeadingAndDoesNotDependOnHeadPitch()
        {
            var flat=Layout(Vector3.zero,Vector3.right);
            var pitched=Layout(Vector3.zero,new Vector3(1,.9f,0));
            Assert.That(Vector3.Distance(flat.position,pitched.position),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(flat.position,new Vector3(-.5f,-.8f,0)),Is.LessThan(.0001f));
        }
        [Test]
        public void VerticalHeadingHasDeterministicForwardFallback()
        {
            var pose=Layout(Vector3.zero,Vector3.up);
            Assert.That(Vector3.Distance(pose.position,new Vector3(0,-.8f,-.5f)),Is.LessThan(.0001f));
        }
        [Test]
        public void PreviewHoldHidesDetachedModelsAndRestoresVisibilityWithoutChangingColliders()
        {
            var root=new GameObject("Synthetic hidden tutorial root");root.SetActive(false);
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(root.transform,false);
            try
            {
                var type=Type.GetType("TeleportInFront, Assembly-CSharp",true);var placement=root.AddComponent(type);
                type.GetField("contentRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(placement,root);
                var hold=type.GetMethod("HoldForValidation");
                hold.Invoke(placement,new object[]{true});Assert.That(cube.GetComponent<Renderer>().enabled,Is.False);
                cube.transform.SetParent(null,true);cube.GetComponent<Renderer>().enabled=true;
                hold.Invoke(placement,new object[]{true});Assert.That(cube.GetComponent<Renderer>().enabled,Is.False);
                Assert.That(cube.GetComponent<Collider>().enabled,Is.True);
                hold.Invoke(placement,new object[]{false});Assert.That(cube.GetComponent<Renderer>().enabled,Is.True);
                type.GetMethod("UseFixedStation").Invoke(placement,new object[]{Vector3.zero,Vector3.forward,180f});
                Assert.That(root.transform.position.z,Is.LessThan(0));
            }
            finally{UnityEngine.Object.DestroyImmediate(cube);UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
