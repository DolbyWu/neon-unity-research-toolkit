using UnityEngine;
namespace NeonResearch
{
    /// <summary>Authored target identity. Ray hits are not fixations or inferred attention.</summary>
    [AddComponentMenu("GazeFlow/Research Target")]
    public sealed class StudyResearchTarget : MonoBehaviour
    {
        public string targetId = "";
        public string targetGroup = "";
        public string displayLabel = "";
        [Tooltip("Original behavioral identifiers retained as explicit aliases, not inferred name matches.")]
        public string[] legacyObjectIds = new string[0];
        public string[] linkedBehaviorKinds = new string[0];
        [Tooltip("Prefab templates must be assigned a spawn-site/instance identity before they can generate hits.")]
        public bool requiresSpawnIdentity;
        [Tooltip("Editor identity ownership: scene/prefab asset GUID. Not a device or participant identifier.")]
        public string identityOwnerGuid = "";
        [Tooltip("Explicit AOI-volume policy: existing trigger colliders may be queried without changing physics. A volume hit is NOT a visible-surface hit. Unconfigured triggers never occlude gaze.")]
        public bool includeTriggerColliders;
        public string DisplayName => string.IsNullOrWhiteSpace(displayLabel) ? gameObject.name : displayLabel;
        public bool HasUniqueId()
        {
            if (requiresSpawnIdentity || string.IsNullOrWhiteSpace(targetId)) return false;
            int count = 0;
            foreach (var target in FindObjectsByType<StudyResearchTarget>(FindObjectsSortMode.None))
                if (target.isActiveAndEnabled && target.targetId == targetId) count++;
            return count == 1;
        }
    }
}
