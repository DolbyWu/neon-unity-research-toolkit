using System;
using System.Linq;
using PupilLabs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonResearch.NeonXR.Editor
{
    public sealed class NeonXRSetupWindow:EditorWindow
    {
        [SerializeField] private Transform trackedHead;
        [SerializeField] private NeonGazeDataProvider officialProvider;
        [SerializeField] private string mountId="";
        [SerializeField] private bool crossSceneSources;
        private string message="";
        [MenuItem("Tools/GazeFlow/Neon XR Setup")]
        public static void Open(){var window=GetWindow<NeonXRSetupWindow>("GazeFlow XR Setup");window.minSize=new Vector2(480,440);}
        public static bool CanCreate(Transform head,NeonGazeDataProvider source,string mount,int recorderCount,int researchProviderCount)=>head!=null&&head.gameObject.scene.IsValid()&&source!=null&&source.gameObject.scene.IsValid()&&!string.IsNullOrWhiteSpace(mount)&&recorderCount==0&&researchProviderCount==0;
        public static GameObject CreateResearchRig(Transform head,NeonGazeDataProvider source,string mount)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before configuring scene objects.");
            int recorders=UnityEngine.Object.FindObjectsByType<StudyGazeTargetRecorder>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,providers=UnityEngine.Object.FindObjectsByType<NeonXRResearchProvider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;
            if(!CanCreate(head,source,mount,recorders,providers))throw new InvalidOperationException("Assign a real head, official provider and mount ID; reuse existing research rig components instead of creating duplicates.");
            var existing=head.GetComponent<StudyHeadPoseSource>();
            if(existing!=null&&existing.head!=null&&existing.head!=head)throw new InvalidOperationException("Selected head already has a Head Pose Source pointing elsewhere; resolve it explicitly first.");
            int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Create GazeFlow research rig");
            var headSource=existing??Undo.AddComponent<StudyHeadPoseSource>(head.gameObject);Undo.RecordObject(headSource,"Bind tracked head");headSource.head=head;
            var root=new GameObject("GazeFlow Research");Undo.RegisterCreatedObjectUndo(root,"Create research root");
            var provider=Undo.AddComponent<NeonXRResearchProvider>(root);provider.neon=source;provider.headReference=head;provider.calibrationId=mount;provider.mappingVerified=false;
            var recorder=Undo.AddComponent<StudyGazeTargetRecorder>(root);recorder.provider=provider;
            var bindings=Undo.AddComponent<StudyNeonXRBindings>(root);bindings.provider=provider;bindings.headSource=headSource;bindings.neonSource=source;bindings.persistResearchRoot=true;
            // Cross-scene discovery is opt-in: an explicit source must exist in each relevant scene.
            bindings.discoverUniqueHeadSource=false;bindings.discoverUniqueNeonSource=false;
            EditorUtility.SetDirty(root);EditorUtility.SetDirty(headSource);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.MarkSceneDirty(head.gameObject.scene);Undo.CollapseUndoOperations(group);return root;
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Neon XR research setup",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Install and configure official Neon XR Core first. This tool binds existing scene objects; it never installs an XR rig, changes the phone mount profile or starts recording.",MessageType.Info);
            trackedHead=(Transform)EditorGUILayout.ObjectField("Actual tracked head",trackedHead,typeof(Transform),true);
            officialProvider=(NeonGazeDataProvider)EditorGUILayout.ObjectField("Official Neon provider",officialProvider,typeof(NeonGazeDataProvider),true);
            mountId=EditorGUILayout.TextField("Mount configuration ID",mountId);
            crossSceneSources=EditorGUILayout.Toggle("Use unique sources across scenes",crossSceneSources);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("Create dedicated research rig",GUILayout.Height(36)))try{var rig=CreateResearchRig(trackedHead,officialProvider,mountId);var bindings=rig.GetComponent<StudyNeonXRBindings>();bindings.discoverUniqueHeadSource=bindings.discoverUniqueNeonSource=crossSceneSources;Selection.activeGameObject=rig;message="Created research root and explicit Head Pose Source. Save your scenes. Recording is still off until you opt in and confirm it.";}catch(Exception e){message=e.Message;}
                if(GUILayout.Button("Create visual reference at selected Transform",GUILayout.Height(36)))
                {
                    var anchor=Selection.activeTransform;
                    if(anchor==null||!anchor.gameObject.scene.IsValid())message="Select an actual scene Transform. Place the new reference on a visible surface, not inside the object.";
                    else{var point=new GameObject("GazeFlow Visual Reference");Undo.RegisterCreatedObjectUndo(point,"Create visual reference");Undo.SetTransformParent(point.transform,anchor,"Parent reference");point.transform.localPosition=Vector3.zero;var component=Undo.AddComponent<StudyGazeReferencePoint>(point);Selection.activeGameObject=point;EditorSceneManager.MarkSceneDirty(point.scene);message="Move this reference Transform to a visible stable point on your chosen object. Its 32 mm visual has no collider or AOI and starts hidden.";}
                }
            }
            EditorGUILayout.HelpBox("One active research recorder/provider per recording. For scene changes: keep the dedicated research root; explicitly enable unique-source discovery only when each relevant scene supplies exactly one Head Pose Source and one configured official Neon provider. Ambiguous sources are refused. Never duplicate the persistent root in destination scenes.",MessageType.None);
            if(GUILayout.Button("Open recording settings"))NeonResearchSetup.Open();
            if(GUILayout.Button("Open gaze adjustment"))DesktopGazeAdjustmentWindow.Open();
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
        }
    }
}
