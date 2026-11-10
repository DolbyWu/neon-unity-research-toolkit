using System.Linq;
using UnityEditor;
using UnityEngine;
namespace NeonResearch
{
    public sealed class StudyResearchSetup : EditorWindow
    {
        private string result = "";
        [MenuItem("Tools/GazeFlow/Research Target Setup (Experimental)")]
        private static void Open() => GetWindow<StudyResearchSetup>("GazeFlow Targets");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Experimental target-ray workflow. Keep the current receiver for raw eye/pupil export. Install the optional Neon XR adapter separately for spatial rays. Use a disposable project first.", MessageType.Info);
            EditorGUILayout.LabelField("1. Install Neon XR Core and the optional GazeFlow adapter.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("2. Configure official calibration and a Neon XR Research Provider with the actual XR head Transform. Confirm mapping only after a known-target trial.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("3. Add a Research Target to each selected object, assign a unique ID, and provide a non-trigger collider. Occluders must be included in the recorder's collision mask.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Add Research Target to selected objects"))
                foreach (var go in Selection.gameObjects)
                    if (go.GetComponent<StudyResearchTarget>() == null) Undo.AddComponent<StudyResearchTarget>(go);
            EditorGUILayout.LabelField("4. Use exactly one Gaze Target Recorder and assign its provider. Recording remains opt-in through existing recording settings.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Create target recorder"))
            {
                var go = new GameObject("GazeFlow Target Recorder"); Undo.RegisterCreatedObjectUndo(go,"Create GazeFlow recorder");
                Undo.AddComponent<StudyGazeTargetRecorder>(go); Selection.activeGameObject = go;
            }
            if (GUILayout.Button("Check target configuration"))
            {
                var targets = FindObjectsByType<StudyResearchTarget>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled).ToArray();
                int bad = targets.Count(t => !t.HasUniqueId() || !t.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger));
                var recorders = FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Where(r => r.isActiveAndEnabled).ToArray();
                bool recorderOk = recorders.Length == 1 && recorders[0].provider != null && recorders[0].provider.isActiveAndEnabled;
                result = $"Targets: {targets.Length}; invalid IDs/colliders: {bad}; active recorders: {recorders.Length}; assigned active provider: {recorderOk}. This checks configuration only; it does not validate calibration, coverage or timing.";
            }
            EditorGUILayout.HelpBox(result.Length == 0 ? "After a short trial, inspect gaze_targets.csv and the Research target hits panel in the generated HTML. Invalid data must remain distinguishable from a valid ray with no target hit." : result, MessageType.Info);
        }
    }
}
