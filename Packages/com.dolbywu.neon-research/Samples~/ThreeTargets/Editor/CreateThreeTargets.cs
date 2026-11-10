using UnityEditor;
using UnityEngine;
namespace NeonResearch.Samples
{
    public static class CreateThreeTargets
    {
        [MenuItem("Tools/GazeFlow/Create Three-Target Demo")]
        public static void Create()
        {
            var root = new GameObject("GazeFlow three-target demo - synthetic scene objects");
            Undo.RegisterCreatedObjectUndo(root, "Create GazeFlow demo targets");
            var camera = Camera.main;
            if (camera != null) { root.transform.position = camera.transform.position; root.transform.rotation = camera.transform.rotation; }
            string[] ids = { "demo_key", "demo_clue", "demo_door" };
            for (int i = 0; i < ids.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = ids[i];
                Undo.RegisterCreatedObjectUndo(go, "Create GazeFlow demo target");
                go.transform.SetParent(root.transform, false); go.transform.localPosition = new Vector3((i-1)*0.8f,0,3);
                go.transform.localScale = Vector3.one*0.5f;
                var target = go.AddComponent<StudyResearchTarget>(); target.targetId = ids[i]; target.targetGroup = "synthetic_demo";
                target.displayLabel = ids[i];
            }
            Selection.activeGameObject = root;
            Debug.Log("[GazeFlow] Three synthetic scene objects created. No session or eye simulation started. Configure the official provider and one target recorder before a trial.");
        }
    }
}
