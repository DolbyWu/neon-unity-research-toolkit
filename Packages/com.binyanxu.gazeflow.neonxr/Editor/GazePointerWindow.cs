using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEngine;

namespace NeonResearch.NeonXR.Editor
{
    /// <summary>Display-only control; never approves mapping or starts/stops acquisition.</summary>
    public sealed class GazePointerWindow : EditorWindow
    {
        [MenuItem("Tools/GazeFlow/Neon XR/Gaze Pointer")]
        public static void Open()
        {
            var window = GetWindow<GazePointerWindow>("Gaze Pointer");
            window.minSize = new Vector2(360, 140);
            window.Show();
        }

        private void OnInspectorUpdate() => Repaint();

        public static void ApplyVisibility(GazeDataVisualizer[] visualizers, bool visible)
        {
            foreach (var visualizer in visualizers)
            {
                if (visualizer == null) continue;
                visualizer.RayVisible = visible;
                visualizer.RaycastPointerVisible = visible;
                // Keep raycast callbacks alive when hiding. Showing a pointer needs raycasting.
                if (visible) visualizer.DoRaycast = true;
            }
        }

        private void OnGUI()
        {
            var visualizers = Object.FindObjectsByType<GazeDataVisualizer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(v => v.gameObject.scene.IsValid() && v.isActiveAndEnabled).ToArray();
            bool visible = visualizers.Any(v => v.RayVisible || v.RaycastPointerVisible);
            EditorGUILayout.HelpBox(
                "Display only. Eye tracking, AOI recording and calibration are unchanged.", MessageType.Info);
            if (!EditorApplication.isPlaying)
                EditorGUILayout.LabelField("Available while Unity is in Play.");
            else if (visualizers.Length == 0)
                EditorGUILayout.LabelField("No active official gaze visualizer in the loaded scenes.");
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || visualizers.Length == 0))
                if (GUILayout.Button(visible ? "Hide gaze pointer" : "Show gaze pointer", GUILayout.Height(46)))
                    ApplyVisibility(visualizers, !visible);
        }
    }
}
