// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Observed location and discrete events, never inferred cognitive state.</summary>
public static class StudySequence
{
    private static StreamWriter events, regions, windows;
    private static string folder, region = "Unknown";
    private static long eventId;
    private static double regionStart;
    private static readonly Dictionary<string, string> held = new Dictionary<string, string>();
    public static string Region => region;
    public static void Begin(string sessionFolder)
    {
        Close(); folder = sessionFolder; eventId = 0; region = "Unknown"; regionStart = 0; held.Clear();
        events = Open("game_events.csv", "event_id,session_time_s,region,event_type,puzzle,object_id,status,outcome,detail");
        regions = Open("region_intervals.csv", "start_session_s,end_session_s,region,source");
        windows = Open("event_windows.csv", "event_id,event_session_s,start_session_s,requested_end_session_s,region,event_type,puzzle,object_id,outcome,selection_rule");
    }
    private static StreamWriter Open(string name, string header)
    {
        var w = new StreamWriter(Path.Combine(folder, name)); w.WriteLine(header); return w;
    }
    public static void SetRegion(string value)
    {
        if (events == null || value == region) return;
        double now = StudyExportSession.Seconds;
        regions.WriteLine(NeonStudyRecorder.Csv(regionStart, now, region, "head_point_in_authored_zone_10Hz"));
        string previous = region; region = value; regionStart = now;
        Record("region_change", "", "", "", "", previous + " -> " + value);
    }
    public static void Record(string kind, string puzzle, string objectId, string status, string outcome, string detail)
    {
        if (events == null) return;
        double now = StudyExportSession.Seconds;
        long id = ++eventId;
        StudyMonitorData.Event(now,kind,puzzle,objectId,outcome);
        events.WriteLine(NeonStudyRecorder.Csv(id, now, region, kind, puzzle, objectId, status, outcome, detail));
        windows.WriteLine(NeonStudyRecorder.Csv(id, now, Math.Max(0, now - 5), now + 5,
            region, kind, puzzle, objectId, outcome, "all_events_plus_minus_5s_exploratory_overlaps_allowed"));
    }
    public static void Flush() { events?.Flush(); regions?.Flush(); windows?.Flush(); }
    public static void Grab(string hand, Component target, string explicitId = null)
    {
        if (events == null) return;
        string id = "";
        if (target != null && !string.IsNullOrWhiteSpace(explicitId)) id = explicitId;
        else if (target != null)
        {
            for (Transform t = target.transform; t != null; t = t.parent) id = t.name + "[" + t.GetSiblingIndex() + "]/" + id;
            id = target.gameObject.scene.path + ":" + id;
        }
        held.TryGetValue(hand, out string previous);
        if ((previous ?? "") == id) return;
        if (!string.IsNullOrEmpty(previous)) Record("grab_end", "", previous, "", "", hand);
        if (id.Length > 0) Record("grab_start", "", id, "", "", hand);
        held[hand] = id;
    }
    public static void Close()
    {
        if (regions != null)
            regions.WriteLine(NeonStudyRecorder.Csv(regionStart, StudyExportSession.Seconds, region, "head_point_in_authored_zone_10Hz"));
        events?.Dispose(); regions?.Dispose(); windows?.Dispose(); events = regions = windows = null;
    }
    public static bool Contains(Collider collider, Vector3 point)
    {
        if (collider == null || !collider.gameObject.activeInHierarchy) return false;
        // Geometry remains meaningful if gameplay disables a completed puzzle's box trigger.
        if (collider is BoxCollider box)
        {
            Vector3 p = box.transform.InverseTransformPoint(point) - box.center;
            Vector3 half = box.size * 0.5f;
            return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
        }
        return collider.enabled && (collider.ClosestPoint(point) - point).sqrMagnitude < 0.000001f;
    }
    public static string Locate(Vector3 point)
    {
        var matches = new SortedSet<string>(StringComparer.Ordinal);
        bool configured = false;
        foreach (var zone in UnityEngine.Object.FindObjectsByType<StudyRegion>(FindObjectsSortMode.None))
        {
            if (zone.regionCollider == null || string.IsNullOrWhiteSpace(zone.regionId)) continue;
            configured = true;
            if (Contains(zone.regionCollider, point)) matches.Add(zone.regionId);
        }
        if (!configured) return "Unknown_no_configured_zones";
        if (matches.Count == 0) return "In-between";
        if (matches.Count == 1) { foreach (string match in matches) return match; }
        return "Ambiguous:" + string.Join("|", matches);
    }
}

}
