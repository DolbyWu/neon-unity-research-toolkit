using System;
using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEngine;

/// <summary>One display-only switch in the isolated trial. No calibration or recording mutations.</summary>
public sealed class GazeFlowTrialSessionPanel : EditorWindow
{
    private const string Trial = "C:/GazeFlowTrials/EscapeRoom_GazeFlow_0.2.0-exp.1_Test_20261110-03";
    private double repaintAt;
    private static bool IsTrial => System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(Application.dataPath))
        .Replace('\\', '/').Equals(Trial, StringComparison.OrdinalIgnoreCase);

    // No Unity native calls in a ScriptableObject constructor.
    [InitializeOnLoadMethod]
    private static void RegisterLifecycle()
    {
        if (!IsTrial || Application.isBatchMode) return;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Open;
        };
    }
    [MenuItem("Tools/GazeFlow/Gaze Pointer")]
    public static void Open()
    {
        if (!IsTrial || Application.isBatchMode) return;
        var panel = GetWindow<GazeFlowTrialSessionPanel>("Gaze Pointer");
        panel.minSize = new Vector2(360, 140);
        panel.Show(); panel.Focus();
    }
    private void OnEnable() => EditorApplication.update += Tick;
    private void OnDisable() => EditorApplication.update -= Tick;
    private void Tick()
    {
        if (EditorApplication.timeSinceStartup < repaintAt) return;
        repaintAt = EditorApplication.timeSinceStartup + .2; Repaint();
    }
    public static void ApplyPointerVisibility(GazeDataVisualizer[] visualizers, bool visible)
    {
        foreach (var visualizer in visualizers)
        {
            if (visualizer == null) continue;
            visualizer.RayVisible = visible;
            visualizer.RaycastPointerVisible = visible;
            if (visible) visualizer.DoRaycast = true;
        }
    }
    private void OnGUI()
    {
        var visualizers = FindObjectsByType<GazeDataVisualizer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(v => v.gameObject.scene.IsValid() && v.enabled).ToArray();
        bool visible = visualizers.Any(v => v.RayVisible || v.RaycastPointerVisible);
        EditorGUILayout.HelpBox("Display only. Eye tracking, AOI recording and calibration are unchanged.", MessageType.Info);
        if (!EditorApplication.isPlaying) EditorGUILayout.LabelField("Available while Unity is in Play.");
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || visualizers.Length == 0))
            if (GUILayout.Button(visible ? "Hide gaze pointer" : "Show gaze pointer", GUILayout.Height(46)))
                ApplyPointerVisibility(visualizers, !visible);
        if(EditorApplication.isPlaying)
            foreach(var gate in FindObjectsByType<HandTutorialNextGate>(FindObjectsSortMode.None))
            {
                EditorGUILayout.LabelField("Grabbing tutorial: "+gate.CompletedObjectiveCount+" / "+gate.RequiredObjectiveCount);
                EditorGUILayout.LabelField("Remaining: "+gate.RemainingObjectives,EditorStyles.wordWrappedLabel);
                EditorGUILayout.HelpBox(gate.NextUnlocked?"Next is ready — continue to the movement tutorial.":gate.WaitingForNarration?"All objectives recorded. Next appears after the queued narration finishes.":"Grab each required type: Gravity, Box, Pins (any one), NoGravity. The remaining list shows what has not been recorded.",MessageType.Info);
            }
    }
}
