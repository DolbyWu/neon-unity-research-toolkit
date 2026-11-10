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
            EditorGUILayout.LabelField("3. Add a Research Target with a stable unique ID. Use an existing solid collider, or explicitly enable existing trigger AOI volumes. Trigger-volume hits are not necessarily visible-surface hits. Include solid occluders in the recorder's collision mask; do not alter gameplay physics for detection.", EditorStyles.wordWrappedLabel);
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
                var targets = FindObjectsByType<StudyResearchTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.enabled).ToArray();
                int empty = targets.Count(t => string.IsNullOrWhiteSpace(t.targetId));
                int duplicates = targets.Where(t => t.isActiveAndEnabled).GroupBy(t => t.targetId).Count(g => g.Count() > 1);
                int geometry = targets.Count(t => !t.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && (!c.isTrigger || t.includeTriggerColliders)));
                int awaiting = targets.Count(t => t.requiresSpawnIdentity);
                var recorders = FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsSortMode.None).Where(r => r.isActiveAndEnabled).ToArray();
                bool recorderOk = recorders.Length == 1 && recorders[0].provider != null && recorders[0].provider.isActiveAndEnabled;
                result = $"Loaded scenes only — enabled research targets (including inactive objects): {targets.Length}; empty IDs: {empty}; duplicate active ID groups: {duplicates}; missing eligible geometry: {geometry}; awaiting spawn registration: {awaiting}; active recorders: {recorders.Length}; assigned active provider: {recorderOk}. Unloaded/additive scenes and prefab assets require the full coverage audit. This is NOT physical calibration, coverage or timing acceptance.";
            }
            EditorGUILayout.HelpBox(result.Length == 0 ? "After a short trial, inspect gaze_targets.csv and the Research target hits panel in the generated HTML. Invalid data must remain distinguishable from a valid ray with no target hit." : result, MessageType.Info);
        }
    }
}
