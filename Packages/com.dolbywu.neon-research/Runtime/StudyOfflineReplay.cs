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
                json.WritePropertyName("gazeAdjustments"); ReadGazeAdjustments(folder,manifest.Value<string>("sessionId")??Path.GetFileName(folder),duration).WriteTo(json);
                json.WritePropertyName("spatialValidations"); json.WriteStartArray();
                string validationFolder=Path.Combine(folder,"Calibration");
                if(Directory.Exists(validationFolder))
                    foreach(string file in Directory.GetFiles(validationFolder,"validation.json",SearchOption.AllDirectories))
                    {
                        try
                        {
                            StudyArchivePaths.RequireContainedUnlinked(folder,file);
                            if(new FileInfo(file).Length>1024*1024)throw new InvalidDataException("Validation report too large.");
                            JObject.Parse(File.ReadAllText(file)).WriteTo(json);
                        }
                        catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is JsonException)
                        { new JObject { ["id"]=Path.GetFileName(Path.GetDirectoryName(file)),["report_error"]="Validation report unavailable or invalid; raw replay preserved." }.WriteTo(json); }
                    }
                json.WriteEndArray();
                WriteTable(json,"events",Path.Combine(folder,"game_events.csv"));
                WriteTable(json,"regions",Path.Combine(folder,"region_intervals.csv"));
                if (File.Exists(Path.Combine(folder,"target_catalog.csv")))
                    WriteTable(json,"targetCatalog",Path.Combine(folder,"target_catalog.csv"));
                if (File.Exists(Path.Combine(folder,"gaze_targets.csv")))
                    WriteTable(json,"gazeTargets",Path.Combine(folder,"gaze_targets.csv"));
                if (File.Exists(Path.Combine(folder,"gaze_targets_approximate.csv")))
                    WriteTable(json,"gazeApproximate",Path.Combine(folder,"gaze_targets_approximate.csv"));
                JArray observations=new JArray();
                string observationFile=Path.Combine(folder,"live_observations.json");
                if(File.Exists(observationFile))
                    try {
                        StudyArchivePaths.RequireContainedUnlinked(folder,observationFile);
                        if(new FileInfo(observationFile).Length>1024*1024)throw new InvalidDataException("Observation file too large.");
                        var data=JObject.Parse(File.ReadAllText(observationFile));
                        if(data.Value<string>("schema")!="gazeflow-live-observations-v1"||data.Value<string>("session_id")!=(manifest.Value<string>("sessionId")??Path.GetFileName(folder))||!(data["records"] is JArray records)||records.Count>10)throw new InvalidDataException("Invalid observation session.");
                        observations=records;
                    } catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is JsonException) { Debug.LogWarning("[StudyReplay] Observer intervals unavailable: "+error.Message); }
                json.WritePropertyName("liveObservations");observations.WriteTo(json);
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
    // Optional evidence only: bounded, session-checked reads. Never reconstruct or rewrite historical rays.
    private static JObject ReadGazeAdjustments(string folder,string session,double duration)
    {
        var models=new JArray();var receipts=new JArray();var warnings=new JArray();
        var result=new JObject{["schema"]="gazeflow-replay-offset-metadata-v1",["models"]=models,["confirmations"]=receipts,["warnings"]=warnings};
        long totalBytes=0;int files=0;bool limitReported=false;
        bool TimeOk(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0&&value<=duration;
        void Read(string path,bool receipt)
        {
            if(!File.Exists(path))return;
            try
            {
                StudyArchivePaths.RequireContainedUnlinked(folder,path);
                long size=new FileInfo(path).Length;
                if(++files>2048||size>1024*1024||(totalBytes+=size)>8*1024*1024)
                {if(!limitReported){warnings.Add("Offset metadata limit reached; some records were omitted. Original files are retained.");limitReported=true;}return;}
                var data=JObject.Parse(File.ReadAllText(path));
                if(data.Value<string>("session_id")!=session)throw new InvalidDataException("Wrong metadata session.");
                if(receipt)
                {
                    if(data.Value<string>("schema")!="gazeflow-desktop-adjustment-save-v1"||string.IsNullOrWhiteSpace(data.Value<string>("id"))||!TimeOk(data.Value<double?>("saved_session_s")??double.NaN))throw new InvalidDataException("Invalid save receipt.");
                    string mode=data.Value<string>("mode");double h=data.Value<double?>("horizontal_degrees")??double.NaN,v=data.Value<double?>("vertical_degrees")??double.NaN;
                    if((mode!="manual_yaw_pitch"&&mode!="official_ray_no_extra_correction")||double.IsNaN(h)||double.IsNaN(v)||Math.Abs(h)>12||Math.Abs(v)>12||(mode=="official_ray_no_extra_correction"&&(h!=0||v!=0))||data.Value<bool?>("accuracy_approval_granted")!=false)throw new InvalidDataException("Invalid save angles or meaning.");
                    var clean=new JObject();foreach(string key in new[]{"schema","id","session_id","calibration_id","correction_id","mode","head_name","saved_session_s","horizontal_degrees","vertical_degrees","raw_official_ray_retained","accuracy_approval_granted","limitation"})if(data[key]!=null)clean[key]=data[key].DeepClone();receipts.Add(clean);
                }
                else
                {
                    if(data.Value<string>("schema")!="gazeflow-local-ray-correction-v1"||string.IsNullOrWhiteSpace(data.Value<string>("id")))throw new InvalidDataException("Invalid correction metadata.");
                    var model=data.ToObject<StudyGazeCorrection.Model>();var rotation=model.Rotation;
                    if(model.applied_session_s.HasValue&&!TimeOk(model.applied_session_s.Value))throw new InvalidDataException("Invalid correction time.");
                    if(model.correction_kind=="manual_yaw_pitch")
                    {
                        if(data["horizontal_degrees"]==null||data["vertical_degrees"]==null||Quaternion.Angle(rotation,StudyGazeCorrection.ManualRotation(model.horizontal_degrees,model.vertical_degrees))>.01f)throw new InvalidDataException("Manual angle/rotation mismatch.");
                    }
                    else if(model.correction_kind!="fitted_rotation")throw new InvalidDataException("Unknown correction mode.");
                    models.Add(JObject.FromObject(model));
                }
            }
            catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException||error is JsonException||error is ArgumentException||error is InvalidOperationException||error is FormatException||error is InvalidCastException||error is OverflowException)
            {if(warnings.Count<20)warnings.Add(receipt?"An offset save receipt was unavailable or invalid and was omitted.":"An offset model was unavailable or invalid and was omitted.");}
        }
        try
        {
            string calibration=Path.Combine(folder,"Calibration");StudyArchivePaths.RequireContainedUnlinked(folder,calibration);
            if(!Directory.Exists(calibration))return result;
            var directories=Directory.GetDirectories(calibration);Array.Sort(directories,StringComparer.Ordinal);
            foreach(string directory in directories){if(limitReported)break;Read(Path.Combine(directory,"correction.json"),false);}
            string confirmations=Path.Combine(calibration,"gaze-confirmations");StudyArchivePaths.RequireContainedUnlinked(folder,confirmations);
            if(Directory.Exists(confirmations)){var paths=Directory.GetFiles(confirmations,"*.json");Array.Sort(paths,StringComparer.Ordinal);foreach(string path in paths){if(limitReported)break;Read(path,true);}}
        }
        catch(Exception error) when(error is IOException||error is InvalidDataException||error is UnauthorizedAccessException)
        {warnings.Add("Offset metadata directory unavailable; raw replay is preserved.");}
        return result;
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
