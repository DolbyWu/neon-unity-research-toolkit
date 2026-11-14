using System.Collections.Generic;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Owns only generated visuals/resources, never gameplay objects. Also removes orphan EditMode test previews.</summary>
    [ExecuteAlways,AddComponentMenu("")]
    public sealed class StudyCalibrationCueGroup:MonoBehaviour
    {
        [HideInInspector] public StudySpatialValidation owner;
        [SerializeField,HideInInspector] private List<Object> ownedResources=new List<Object>();
        public void Own(Object resource){if(resource!=null&&!ownedResources.Contains(resource))ownedResources.Add(resource);}
        private void Update(){if(owner==null){gameObject.SetActive(false);if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}}
        private void OnDestroy(){foreach(var resource in ownedResources)if(resource!=null){if(Application.isPlaying)Destroy(resource);else DestroyImmediate(resource);}ownedResources.Clear();}
    }
}
