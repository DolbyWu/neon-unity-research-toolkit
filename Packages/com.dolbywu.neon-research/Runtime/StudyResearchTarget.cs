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
        public bool HasUniqueId()
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            int count = 0;
            foreach (var target in FindObjectsByType<StudyResearchTarget>(FindObjectsSortMode.None))
                if (target.isActiveAndEnabled && target.targetId == targetId) count++;
            return count == 1;
        }
    }
}
