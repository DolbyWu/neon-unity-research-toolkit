using System;
using System.Linq;
using PupilLabs;
using UnityEngine;

namespace NeonResearch.NeonXR
{
    /// <summary>Explicit, project-independent source binding. No names, tags or host-script reflection.</summary>
    [AddComponentMenu("GazeFlow/Neon XR Scene Bindings")]
    public sealed class StudyNeonXRBindings:MonoBehaviour
    {
        public NeonXRResearchProvider provider;
        [Tooltip("Prefer these explicit references. Unique-source discovery is optional and refuses ambiguity.")]
        public StudyHeadPoseSource headSource;
        public NeonGazeDataProvider neonSource;
        [Tooltip("Opt in to exactly-one active source discovery across scenes, instead of the fixed headSource reference.")]
        public bool discoverUniqueHeadSource;
        [Tooltip("Opt in to exactly-one active source discovery across scenes, instead of the fixed neonSource reference.")]
        public bool discoverUniqueNeonSource;
        [Tooltip("Enable only on the dedicated GazeFlow research root, never the player or task root.")]
        public bool persistResearchRoot;
        public string Status {get;private set;}="Not resolved.";
        private Transform boundHead;
        private NeonGazeDataProvider boundNeon;
        private string boundMount,lastStatus="";
        private double nextRefresh;
        private void Awake()
        {
            if(Application.isPlaying&&persistResearchRoot)
            {
                if(transform.parent!=null){Status="Persistence refused: use a dedicated scene-root object.";return;}
                DontDestroyOnLoad(gameObject);
            }
        }
        private void Update(){if(Time.realtimeSinceStartupAsDouble<nextRefresh)return;nextRefresh=Time.realtimeSinceStartupAsDouble+.1;RefreshBindings();}
        public void RefreshBindings()
        {
            if(provider==null){SetStatus("Assign the research provider.");return;}
            var heads=discoverUniqueHeadSource?FindObjectsByType<StudyHeadPoseSource>(FindObjectsSortMode.None).Where(s=>s.isActiveAndEnabled).ToArray():headSource!=null?new[]{headSource}:Array.Empty<StudyHeadPoseSource>();
            var sources=discoverUniqueNeonSource?FindObjectsByType<NeonGazeDataProvider>(FindObjectsSortMode.None).Where(s=>s.isActiveAndEnabled).ToArray():neonSource!=null?new[]{neonSource}:Array.Empty<NeonGazeDataProvider>();
            Transform head=heads.Length==1&&heads[0].isActiveAndEnabled?(heads[0].head!=null?heads[0].head:heads[0].transform):null;
            var neon=sources.Length==1&&sources[0].isActiveAndEnabled?sources[0]:null;
            if(head==null||neon==null||string.IsNullOrWhiteSpace(provider.calibrationId))
            {
                // Only invalidate bindings managed by this component; never silently invent a replacement.
                if(boundHead!=null&&provider.headReference==boundHead){provider.GetComponent<StudySpatialValidation>()?.MarkPriorBindingForReview();provider.headReference=null;provider.mappingVerified=false;}
                if(boundNeon!=null&&provider.neon==boundNeon)provider.neon=null;
                boundHead=null;boundNeon=null;boundMount=null;
                SetStatus(heads.Length>1||sources.Length>1?"Ambiguous sources: assign explicit references or keep exactly one active source of each kind.":"Waiting for one explicit tracked-head source, official Neon provider and mount configuration ID. No camera-name fallback.");return;
            }
            if(boundHead!=head||provider.headReference!=head||boundMount!=provider.calibrationId)
                provider.RebindDesktopHead(head,provider.calibrationId);
            if(provider.neon!=neon)provider.neon=neon;
            boundHead=head;boundNeon=neon;boundMount=provider.calibrationId;
            SetStatus("Bound explicit research head and official Neon source. Mapping accuracy remains a separate check.");
        }
        private void SetStatus(string message)
        {
            Status=message;if(lastStatus==message)return;lastStatus=message;
            if(StudyEvents.Recording)StudyEvents.Record("gaze_scene_binding_status","research_rig","binding_status_not_accuracy",message);
        }
    }
}
