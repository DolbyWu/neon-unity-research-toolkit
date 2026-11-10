using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Configured-object metadata, separate from observed gaze and behavioral evidence.</summary>
    public static class StudyTargetCatalog
    {
        private static string session;
        private static readonly Dictionary<string,string> entries = new Dictionary<string,string>();
        public static void Snapshot()
        {
            if (!StudyEvents.Recording) return;
            try
            {
                if(session != StudyExportSession.SessionId) { session=StudyExportSession.SessionId; entries.Clear(); }
                var text = new StringBuilder("session_id,target_id,target_name,target_label,target_group,legacy_object_ids,linked_behavior_kinds,scene,hierarchy,active,unique_active_id,metadata_semantics,geometry_policy\n");
                foreach (var target in UnityEngine.Object.FindObjectsByType<StudyResearchTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    // Disabled research helpers are excluded, while inactive authored
                    // experiment objects remain available for later activation/review.
                    if (!target.enabled) continue;
                    string hierarchy = target.name;
                    for (var parent = target.transform.parent; parent != null; parent = parent.parent) hierarchy = parent.name + "/" + hierarchy;
                    bool unique=target.HasUniqueId();
                    string key=unique?target.targetId:target.targetId+"|"+target.gameObject.scene.path+"|"+hierarchy;
                    entries[key]=NeonStudyRecorder.Csv(StudyExportSession.SessionId, target.targetId,
                        target.gameObject.name, target.DisplayName, target.targetGroup,
                        JsonConvert.SerializeObject(target.legacyObjectIds), JsonConvert.SerializeObject(target.linkedBehaviorKinds),
                        target.gameObject.scene.path, hierarchy, target.isActiveAndEnabled, unique,
                        "configured_metadata_NOT_evidence_of_gaze_or_behavior; scene/hierarchy_are_descriptors_NOT_identity",
                        target.includeTriggerColliders ? "solid_or_authored_trigger_volume_NOT_visible_surface_only" : "solid_surface_only");
                }
                foreach(var entry in entries.Values) text.AppendLine(entry);
                // This is a refreshable catalogue, not an append-only sensor stream.
                StudyAtomicFile.Write(Path.Combine(StudyExportSession.Folder, "target_catalog.csv"), text.ToString());
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { StudyExportSession.ReportFailure("Target catalogue export failed: " + e.Message); }
        }
    }
}
