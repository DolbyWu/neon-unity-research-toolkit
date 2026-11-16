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
        [Tooltip("When off, only colliders on this GameObject belong to this AOI. Child frames/backings remain separate occluders unless they have their own explicit target. Existing targets default to including descendants.")]
        public bool includeChildColliders = true;
        [Tooltip("Optional explicit ancestor for object statistics. Raw hit identity and gameplay remain unchanged; keep independent interactive parts separate.")]
        public StudyResearchTarget statisticsOwner;
        public string statisticsOwnerRule = "";
        public StudyResearchTarget ResolveStatisticsOwner(out string status,bool requireActive=true)
        {
            var value=this;int hops=0;
            while(value.statisticsOwner!=null)
            {
                var next=value.statisticsOwner;
                if(++hops>32||next==value||!value.transform.IsChildOf(next.transform)||next.gameObject.scene!=gameObject.scene)
                {status="invalid_owner_chain";return null;}
                value=next;
            }
            if(string.IsNullOrWhiteSpace(value.targetId)||value.requiresSpawnIdentity||!value.enabled||(requireActive&&(!value.isActiveAndEnabled||!value.HasUniqueId())))
            {status="owner_unavailable_or_not_unique";return null;}
            status=value==this?"raw_target_is_object":"explicit_ancestor_owner";return value;
        }
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
