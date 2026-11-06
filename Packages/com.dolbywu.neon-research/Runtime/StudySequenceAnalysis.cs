// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>Deterministic post-run interval joins. Raw files are never modified.</summary>
public static class StudySequenceAnalysis
{
    private static readonly HashSet<string> generated = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGenerated() => generated.Clear();
    private sealed class Window
    {
        public string[] row;
        public double start, end;
        public int samples, basicValid, receiveOnly;
    }
    public static void TryBuildClosed(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        folder=Path.GetFullPath(folder);
        if(generated.Contains(folder))return;
        try
        {
            var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "export_manifest.json")));
            if (!(bool)manifest["closed"]) return;
            foreach (string neon in Directory.GetDirectories(folder, "Neon-*"))
            {
                string metadata = Path.Combine(neon, "session.json");
                if (!File.Exists(metadata) || !(bool)JObject.Parse(File.ReadAllText(metadata))["closed"]) return;
            }
            if (File.Exists(Path.Combine(folder, "game_events.csv")))
            {
                StudyOfflineReplay.Build(folder);
                StudyEpisodes.Build(folder);
            }
            else Build(folder);
            generated.Add(folder);
            // Synchronous notification after raw finalization and replay generation, before XR teardown.
            // Editor launches an independent process; no listener or native worker is hosted in Unity.
            StudyArchiveNotifications.Publish(folder);
        }
        catch (Exception e) { StudyMonitorData.Error="Post-run export incomplete: "+e.Message; UnityEngine.Debug.LogWarning("[StudySequence] Analysis incomplete; regenerate after Stop: " + e.Message); }
    }
    public static IEnumerable<string[]> Rows(string path)
    {
        using (var reader = new StreamReader(path))
        {
            var fields = new List<string>(); var value = new StringBuilder(); bool quoted = false;
            int c;
            while ((c = reader.Read()) >= 0)
            {
                char ch = (char)c;
                if (ch == '"')
                {
                    if (quoted && reader.Peek() == '"') { reader.Read(); value.Append('"'); }
                    else quoted = !quoted;
                }
                else if (ch == ',' && !quoted) { fields.Add(value.ToString()); value.Clear(); }
                else if ((ch == '\n' || ch == '\r') && !quoted)
                {
                    if (ch == '\r' && reader.Peek() == '\n') reader.Read();
                    fields.Add(value.ToString()); value.Clear(); yield return fields.ToArray(); fields.Clear();
                }
                else value.Append(ch);
            }
            if (quoted) throw new InvalidDataException("Truncated quoted CSV record: " + Path.GetFileName(path));
            if (fields.Count > 0 || value.Length > 0) { fields.Add(value.ToString()); yield return fields.ToArray(); }
        }
    }
    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);
    private static string Cell(string[] row, string[] header, string name)
    {
        int i = Array.IndexOf(header, name); return i < 0 || i >= row.Length ? "" : row[i];
    }
    public static bool InInterval(double time, double start, double end) => time >= start && time < end;
    public static string RegionAt(List<string[]> regions, double time)
    {
        int lo = 0, hi = regions.Count;
        while (lo < hi) { int mid = (lo + hi) / 2; if (Number(regions[mid][0]) <= time) lo = mid + 1; else hi = mid; }
        return lo > 0 && InInterval(time, Number(regions[lo - 1][0]), Number(regions[lo - 1][1])) ? regions[lo - 1][2] : "Unknown_outside_region_log";
    }
    public static string Build(string folder, string selectedEventId = null)
    {
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "export_manifest.json")));
        if (!(bool)manifest["closed"]) throw new InvalidOperationException("Stop the session before analysis.");
        double duration = (double)manifest["durationSeconds"];
        var windows = new List<Window>(); bool first = true;
        foreach (var row in Rows(Path.Combine(folder, "event_windows.csv")))
        {
            if (first) { first = false; continue; }
            if (selectedEventId == null || row[0] == selectedEventId)
                windows.Add(new Window { row = row, start = Number(row[2]), end = Math.Min(duration, Number(row[3])) });
        }
        if (selectedEventId != null && windows.Count == 0) throw new ArgumentException("Event ID not found.");
        windows.Sort((a, b) => a.start.CompareTo(b.start));
        var regions = new List<string[]>(); first = true;
        foreach (var row in Rows(Path.Combine(folder, "region_intervals.csv")))
        { if (first) { first = false; continue; } regions.Add(row); }
        string output = Path.Combine(folder, "Analysis", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
        Directory.CreateDirectory(output);
        long total = 0, alignedCount = 0;
        using (var joined = new StreamWriter(Path.Combine(output, selectedEventId == null ? "gaze_with_regions.csv" : "selected_event_gaze.csv")))
        {
            bool headerWritten = false;
            foreach (string neon in Directory.GetDirectories(folder, "Neon-*"))
            {
                var meta = JObject.Parse(File.ReadAllText(Path.Combine(neon, "session.json")));
                if (!(bool)meta["closed"]) throw new InvalidOperationException("Neon writer is not closed.");
                string[] header = null;
                foreach (var row in Rows(Path.Combine(neon, "NeonGaze.csv")))
                {
                    if (header == null)
                    {
                        header = row;
                        if (Array.IndexOf(header, "aligned_session_time_s") < 0) throw new InvalidDataException("Old recording has no clock-aligned columns.");
                        if (!headerWritten) { joined.WriteLine(NeonStudyRecorder.Csv("source_neon_folder", "region_at_sample_time") + "," + NeonStudyRecorder.Csv(header)); headerWritten = true; }
                        continue;
                    }
                    total++;
                    bool aligned = Cell(row, header, "time_alignment_status") == "time_echo_estimated" &&
                        double.TryParse(Cell(row, header, "aligned_session_time_s"), NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    double time = Number(Cell(row, header, aligned ? "aligned_session_time_s" : "receive_session_time_s"));
                    if (aligned) alignedCount++;
                    bool valid = Cell(row, header, "sample_quality") == "basic_valid_not_full_eye_quality_assessment";
                    bool selected = selectedEventId == null;
                    // Windows all use the same five-second padding. Binary search avoids scanning every event per sample.
                    int lo = 0, hi = windows.Count;
                    while (lo < hi) { int mid = (lo + hi) / 2; if (windows[mid].end <= time) lo = mid + 1; else hi = mid; }
                    for (int i = lo; i < windows.Count && windows[i].start <= time; i++)
                    {
                        var w = windows[i]; if (!InInterval(time, w.start, w.end)) continue;
                        if (aligned) { w.samples++; if (valid) w.basicValid++; selected = true; }
                        else w.receiveOnly++;
                    }
                    // Selected-event exports never silently substitute receive time for sample time.
                    if (selected)
                        joined.WriteLine(NeonStudyRecorder.Csv(Path.GetFileName(neon), aligned ? RegionAt(regions, time) : "Unknown_unsynchronized") + "," + NeonStudyRecorder.Csv(row));
                }
            }
        }
        using (var summary = new StreamWriter(Path.Combine(output, "event_eye_summary.csv")))
        {
            summary.WriteLine("event_id,event_session_s,start_session_s,end_session_s,region_at_event,event_type,puzzle,object_id,outcome,aligned_samples,basic_valid_samples,basic_valid_fraction,unsynced_samples_by_receive_time_only,window_truncated_by_session,interpretation");
            foreach (var w in windows)
                summary.WriteLine(NeonStudyRecorder.Csv(w.row[0], w.row[1], w.start, w.end, w.row[4], w.row[5], w.row[6], w.row[7], w.row[8],
                    w.samples, w.basicValid, w.samples > 0 ? (object)((double)w.basicValid / w.samples) : "", w.receiveOnly,
                    Number(w.row[1]) < 5 || Number(w.row[3]) > duration,
                    w.samples == 0 ? "NO_ALIGNED_EYE_DATA" : "exploratory_window_not_cognitive_label_or_statistical_significance"));
        }
        File.WriteAllText(Path.Combine(output, "analysis_status.json"), new JObject {
            ["complete"] = true, ["totalRawSamples"] = total, ["clockAlignedSamples"] = alignedCount,
            ["selectedEventId"] = selectedEventId, ["timeMethod"] = "periodic_time_echo_midpoint_estimates_not_hardware_validated",
            ["limitations"] = "Overlapping windows are not independent. Basic validity is not fixation quality. Missing samples/clock intervals require review. Region is head location, not gaze target. No cognitive inference."
        }.ToString());
        return output;
    }
}

}
