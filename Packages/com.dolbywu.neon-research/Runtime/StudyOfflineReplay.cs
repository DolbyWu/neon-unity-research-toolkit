// Neon Research Toolkit: project-local research recording and review.
namespace NeonResearch
{
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>Self-contained, offline report. Raw recordings are never modified.</summary>
public static class StudyOfflineReplay
{
    public static string Build(string folder)
    {
        var template = Resources.Load<TextAsset>("NeonResearchOfflineReplay");
        if (template == null) throw new FileNotFoundException("Offline replay template missing.");
        return BuildWithTemplate(folder, template.text);
    }

    public static string BuildWithTemplate(string folder, string template)
    {
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "export_manifest.json")));
        if (manifest.Value<bool?>("closed") != true) throw new InvalidOperationException("Stop the session before creating a replay.");
        double duration = manifest.Value<double?>("durationSeconds") ?? 0;
        if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0) throw new InvalidDataException("Invalid session duration.");
        string[] neonFolders = Directory.GetDirectories(folder, "Neon-*");
        Array.Sort(neonFolders, StringComparer.Ordinal);
        foreach (string neon in neonFolders)
            if (JObject.Parse(File.ReadAllText(Path.Combine(neon,"session.json"))).Value<bool?>("closed") != true)
                throw new InvalidOperationException("Neon recording is not closed.");
        const string marker = "__STUDY_DATA__";
        int split = template.IndexOf(marker, StringComparison.Ordinal);
        if (split < 0 || split != template.LastIndexOf(marker, StringComparison.Ordinal)) throw new InvalidDataException("Invalid replay template.");
        string output = Path.Combine(folder, "Replay"); Directory.CreateDirectory(output);
        string path = Path.Combine(output, "study-replay.html"), temporary = path + ".partial";
        using (var stream = new StreamWriter(temporary, false, new UTF8Encoding(false)))
        {
            stream.Write(template.Substring(0,split));
            using (var json = new JsonTextWriter(stream) { CloseOutput = false, StringEscapeHandling = StringEscapeHandling.EscapeHtml })
            {
                json.WriteStartObject();
                json.WritePropertyName("version"); json.WriteValue(1);
                json.WritePropertyName("session"); json.WriteValue(manifest.Value<string>("sessionId") ?? Path.GetFileName(folder));
                json.WritePropertyName("participant"); json.WriteValue(manifest.Value<string>("participantId") ?? "");
                json.WritePropertyName("duration"); json.WriteValue(duration);
                json.WritePropertyName("media"); ReadVideoTiming(folder, manifest.Value<string>("sessionId") ?? Path.GetFileName(folder), duration).WriteTo(json);
                json.WritePropertyName("manifestPublishFailures"); json.WriteValue(manifest.Value<int?>("manifestPublishFailures")??0);
                json.WritePropertyName("recordingError"); json.WriteValue(manifest.Value<string>("recordingError")??"");
                WriteTable(json,"events",Path.Combine(folder,"game_events.csv"));
                WriteTable(json,"regions",Path.Combine(folder,"region_intervals.csv"));
                json.WritePropertyName("gaze"); json.WriteStartArray();
                foreach (string neon in neonFolders)
                {
                    json.WriteStartObject(); json.WritePropertyName("source"); json.WriteValue(Path.GetFileName(neon));
                    var metadata=JObject.Parse(File.ReadAllText(Path.Combine(neon,"session.json")));
                    json.WritePropertyName("queueDropped");json.WriteValue(metadata.Value<long?>("queueDropped")??0);
                    json.WritePropertyName("status");json.WriteValue(metadata.Value<string>("status")??"unknown");
                    WriteTable(json,"table",Path.Combine(neon,"NeonGaze.csv")); json.WriteEndObject();
                }
                json.WriteEndArray(); json.WriteEndObject(); json.Flush();
            }
            stream.Write(template.Substring(split+marker.Length));
        }
        StudyAtomicFile.Publish(temporary,path);
        return path;
    }
    // Optional capture evidence only. Missing/locked/invalid media metadata must not block raw-data replay.
    private static JObject ReadVideoTiming(string folder, string session, double duration)
    {
        var result = new JObject { ["video_relative_path"] = "../Video/quest-view.mp4",
            ["video_offset_s"] = null, ["timing_method"] = "unavailable_use_manual_offset_or_load_capture_metadata" };
        JObject selected = null;
        foreach (string name in new[] { "media_capture.json", "media_capture.finalization.json" })
        {
            try
            {
                string path = Path.Combine(folder, name);
                StudyArchivePaths.RequireContainedUnlinked(folder, path);
                if (!File.Exists(path)) continue;
                var metadata = JObject.Parse(File.ReadAllText(path));
                if (metadata.Value<string>("sessionId") != session) continue;
                if (selected == null || (metadata.Value<long?>("finalizedUtcMs") ?? 0) >= (selected.Value<long?>("finalizedUtcMs") ?? 0))
                    selected = metadata;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is JsonException || error is FormatException || error is InvalidCastException || error is OverflowException)
            { result["metadata_warning"] = "Capture timing unavailable or invalid; verify manually. Raw-data replay is unaffected."; }
        }
        try
        {
            double? offset = selected?.SelectToken("channels.video.estimatedSessionStartSeconds")?.Value<double?>();
            if (offset.HasValue && !double.IsNaN(offset.Value) && !double.IsInfinity(offset.Value) && Math.Abs(offset.Value) <= Math.Max(duration, 1))
            {
                result["video_offset_s"] = offset.Value;
                result["timing_method"] = "process_launch_UTC_estimate_NOT_first_frame_or_hardware_sync";
                result["capture_status_at_export"] = selected.Value<string>("status") ?? "unknown";
            }
        }
        catch (Exception error) when (error is JsonException || error is FormatException || error is InvalidCastException || error is OverflowException)
        { result["metadata_warning"] = "Invalid capture offset; verify manually."; }
        return result;
    }
    private static void WriteTable(JsonTextWriter json, string key, string path)
    {
        json.WritePropertyName(key); json.WriteStartArray();
        int columns=-1,rowNumber=0;
        foreach (var row in StudySequenceAnalysis.Rows(path))
        {
            rowNumber++;
            if(columns<0)columns=row.Length;
            if(row.Length!=columns)throw new InvalidDataException(Path.GetFileName(path)+": inconsistent column count at row "+rowNumber);
            json.WriteStartArray(); foreach (string cell in row) json.WriteValue(cell); json.WriteEndArray();
        }
        if(columns<0)throw new InvalidDataException(Path.GetFileName(path)+": missing CSV header");
        json.WriteEndArray();
    }
}

}
