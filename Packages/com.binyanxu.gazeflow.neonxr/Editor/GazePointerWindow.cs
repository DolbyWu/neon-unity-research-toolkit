using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEngine;

namespace NeonResearch.NeonXR.Editor
{
    /// <summary>Display-only control; never approves mapping or starts/stops acquisition.</summary>
    public sealed class GazePointerWindow : EditorWindow
    {
        // Legacy callable alias retained without a duplicate menu entry.
        public static void Open()
        {
            DesktopGazeAdjustmentWindow.Open();
        }

        private void OnInspectorUpdate() => Repaint();

        public static void ApplyVisibility(GazeDataVisualizer[] visualizers, bool visible)
        {
            if(!visible)foreach(var preview in Object.FindObjectsByType<StudyDesktopGazePreview>(FindObjectsSortMode.None))preview.Visible=false;
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
