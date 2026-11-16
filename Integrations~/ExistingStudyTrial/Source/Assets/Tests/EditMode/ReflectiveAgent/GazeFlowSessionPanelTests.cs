using System;
using NUnit.Framework;
using UnityEngine;
namespace EscapeRoom.ReflectiveAgent.Tests
{
    public sealed class GazeFlowSessionPanelTests
    {
        [Test] public void WindowHasNoStaticConstructorThatCanCallForbiddenUnityProperties()
        {
            var type=Type.GetType("GazeFlowTrialSessionPanel, Assembly-CSharp-Editor",true);
            Assert.That(type.TypeInitializer, Is.Null);
        }
        [Test] public void VisibilityOnlyTogglesDisplayAndPreservesEnabledState()
        {
            var go=new GameObject("synthetic visibility check");
            try
            {
                var type=Type.GetType("PupilLabs.GazeDataVisualizer, PupilLabs.NeonXR.Core.Runtime",true);
                var component=go.AddComponent(type) as Behaviour;
                var values=Array.CreateInstance(type,1);values.SetValue(component,0);
                var panel=Type.GetType("GazeFlowTrialSessionPanel, Assembly-CSharp-Editor",true);
                var apply=panel.GetMethod("ApplyPointerVisibility");
                apply.Invoke(null,new object[]{values,true});
                Assert.That((bool)type.GetProperty("RayVisible").GetValue(component),Is.True);
                Assert.That((bool)type.GetProperty("RaycastPointerVisible").GetValue(component),Is.True);
                apply.Invoke(null,new object[]{values,false});
                Assert.That((bool)type.GetProperty("RayVisible").GetValue(component),Is.False);
                Assert.That((bool)type.GetProperty("RaycastPointerVisible").GetValue(component),Is.False);
                Assert.That((bool)type.GetProperty("DoRaycast").GetValue(component),Is.True);
                Assert.That(component.enabled&&go.activeSelf,Is.True);
                Assert.That(panel.GetMethod("ConfirmCurrentCalibration"),Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
