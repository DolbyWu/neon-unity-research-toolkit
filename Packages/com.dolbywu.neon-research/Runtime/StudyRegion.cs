using UnityEngine;

namespace NeonResearch
{
    [AddComponentMenu("Neon Research/Study Region")]
    public sealed class StudyRegion:MonoBehaviour
    {
        public string regionId="Region";
        public Collider regionCollider;
        private void Reset(){regionCollider=GetComponent<Collider>();}
    }
}
