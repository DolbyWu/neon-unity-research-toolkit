// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

/// <summary>Descriptive, reproducible episodes. No inferred attention, intent or significance.</summary>
public static class StudyEpisodes
{
    public sealed class Episode
    {
        public string id, kind, region, objectId = "", hand = "", startEvent = "", endEvent = "", endReason;
        public double start, end;
        public long aligned, valid, unaligned;
    }
    private static double N(string s) => double.Parse(s, CultureInfo.InvariantCulture);
    private static List<string[]> Load(string path)
    {
        var rows = new List<string[]>(StudySequenceAnalysis.Rows(path));
        if (rows.Count > 0) rows.RemoveAt(0); return rows;
    }
    public static List<Episode> Define(List<string[]> regions, List<string[]> events, double duration)
    {
        var result = new List<Episode>(); int visit = 0, gap = 0, grab = 0;
        foreach (var r in regions)
        {
            double start = Math.Max(0, N(r[0])), end = Math.Min(duration, N(r[1]));
            if (end <= start) continue;
            string id = "V" + (++visit).ToString("D4");
            result.Add(new Episode { id = id, kind = "region_visit", region = r[2], start = start, end = end,
                endReason = end >= duration - 0.01 ? "session_end_censored" : "region_boundary" });
            // Partition every visit into non-overlapping gaps between observed game-event times.
            // Equal-time events share the same boundary; a gap never claims continuous interaction.
            double previous = start; string previousId = "";
            foreach (var e in events)
            {
                double t = N(e[1]);
                if (t < start || t >= end || e[3] == "region_change" || e[3] == "observer_marker") continue;
                if (t > previous)
                    result.Add(new Episode { id = "S" + (++gap).ToString("D5"), kind = "between_logged_events",
                        region = r[2], start = previous, end = t, startEvent = previousId, endEvent = e[0], endReason = "next_logged_event" });
                previous = t; previousId = e[0];
            }
            if (end > previous)
                result.Add(new Episode { id = "S" + (++gap).ToString("D5"), kind = "between_logged_events",
                    region = r[2], start = previous, end = end, startEvent = previousId,
                    endReason = "region_or_session_end_no_inferred_inactivity" });
        }
        var held = new Dictionary<string, Episode>();
        foreach (var e in events)
        {
            if (e[3] != "grab_start" && e[3] != "grab_end") continue;
            string hand = e[8]; double t = N(e[1]);
            if (e[3] == "grab_start")
            {
                if (held.TryGetValue(hand, out var interrupted))
                { interrupted.end = t; interrupted.endReason = "new_grab_without_release_censored"; }
                var item = new Episode { id = "G" + (++grab).ToString("D4"), kind = "object_hold", region = "see_time_resolved_region",
                    objectId = e[5], hand = hand, start = t, end = duration, startEvent = e[0], endReason = "session_end_without_release_censored" };
                held[hand] = item; result.Add(item);
            }
            else if (held.TryGetValue(hand, out var item) && item.objectId == e[5])
            { item.end = t; item.endEvent = e[0]; item.endReason = "observed_release"; held.Remove(hand); }
        }
        return result;
    }
    public static string Build(string folder, string selectedEpisode = null)
    {
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(folder, "export_manifest.json")));
        if (!(bool)manifest["closed"]) throw new InvalidOperationException("Stop before analysis.");
        double duration = (double)manifest["durationSeconds"];
        var regions = Load(Path.Combine(folder, "region_intervals.csv"));
        var events = Load(Path.Combine(folder, "game_events.csv"));
        // Stable sorting preserves equal-time event order and reproducible episode IDs.
        events.Sort((a,b) => { int c = N(a[1]).CompareTo(N(b[1])); return c != 0 ? c : N(a[0]).CompareTo(N(b[0])); });
        var episodes = Define(regions, events, duration);
        if (selectedEpisode != null)
        {
            episodes.RemoveAll(e => e.id != selectedEpisode);
            if (episodes.Count == 0) throw new ArgumentException("Episode ID not found. Use an ID from episodes.csv, e.g. V0001, S00001 or G0001.");
        }
        string output = Path.Combine(folder, "Analysis", "Episodes-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0,6));
        Directory.CreateDirectory(output);
        // Summaries are calculated one layer at a time with a sweep over sorted sample times.
        // This supports overlapping two-hand holds without assuming independent episodes.
        var samples = new List<(double time, bool aligned, bool valid)>();
        using (var extracted = selectedEpisode == null ? null : new StreamWriter(Path.Combine(output, "episode_gaze.csv")))
        {
            bool wroteHeader = false;
            foreach (string neon in Directory.GetDirectories(folder, "Neon-*"))
            {
                if (!(bool)JObject.Parse(File.ReadAllText(Path.Combine(neon,"session.json")))["closed"]) throw new InvalidOperationException("Neon is still recording.");
                string[] header = null;
                foreach (var row in StudySequenceAnalysis.Rows(Path.Combine(neon,"NeonGaze.csv")))
                {
                    if (header == null) { header = row; if (!wroteHeader && extracted != null) { extracted.WriteLine("episode_id,region_at_sample_time,source_neon_folder," + NeonStudyRecorder.Csv(header)); wroteHeader = true; } continue; }
                    string Get(string name) { int i = Array.IndexOf(header,name); if (i < 0) throw new InvalidDataException("Recording lacks " + name); return row[i]; }
                    bool aligned = Get("time_alignment_status") == "time_echo_estimated";
                    double t = N(Get(aligned ? "aligned_session_time_s" : "receive_session_time_s"));
                    bool valid = Get("sample_quality") == "basic_valid_not_full_eye_quality_assessment";
                    samples.Add((t,aligned,valid));
                    if (extracted != null && aligned && StudySequenceAnalysis.InInterval(t,episodes[0].start,episodes[0].end))
                        extracted.WriteLine(NeonStudyRecorder.Csv(selectedEpisode,StudySequenceAnalysis.RegionAt(regions,t),Path.GetFileName(neon)) + "," + NeonStudyRecorder.Csv(row));
                }
            }
        }
        samples.Sort((a,b) => a.time.CompareTo(b.time));
        using (var table = new StreamWriter(Path.Combine(output,"episodes.csv")))
        using (var links = new StreamWriter(Path.Combine(output,"episode_events.csv")))
        {
            table.WriteLine("episode_id,type,start_session_s,end_session_s,duration_s,region,object_id,hand,start_event_id,end_event_id,end_reason,within_interval_event_count,aligned_samples,basic_valid_samples,basic_valid_fraction,unsynced_by_receive_time_only,max_aligned_gap_including_edges_s,quality_note");
            links.WriteLine("episode_id,event_id,event_session_s,event_region,event_type,puzzle,object_id,status,outcome,detail,relation");
            foreach (var e in episodes)
            {
                int lo=0,hi=samples.Count;
                while(lo<hi) { int mid=(lo+hi)/2; if(samples[mid].time<e.start) lo=mid+1; else hi=mid; }
                double previous=e.start,maxGap=0;
                for(int i=lo;i<samples.Count && samples[i].time<e.end;i++)
                {
                    var s=samples[i]; if(!s.aligned) { e.unaligned++; continue; }
                    e.aligned++; if(s.valid)e.valid++;
                    maxGap=Math.Max(maxGap,s.time-previous); previous=s.time;
                }
                maxGap=Math.Max(maxGap,e.end-previous);
                int count=0;
                foreach(var ev in events)
                {
                    bool inside=StudySequenceAnalysis.InInterval(N(ev[1]),e.start,e.end);
                    bool boundary=ev[0]==e.endEvent;
                    if(!inside && !boundary)continue;
                    if(inside)count++;
                    links.WriteLine(NeonStudyRecorder.Csv(e.id)+","+NeonStudyRecorder.Csv(ev)+","+NeonStudyRecorder.Csv(inside?"within_half_open_interval":"end_boundary_event"));
                }
                table.WriteLine(NeonStudyRecorder.Csv(e.id,e.kind,e.start,e.end,e.end-e.start,e.region,e.objectId,e.hand,e.startEvent,e.endEvent,e.endReason,count,
                    e.aligned,e.valid,e.aligned>0?(object)((double)e.valid/e.aligned):"",e.unaligned,maxGap,
                    e.aligned==0?"NO_ALIGNED_EYE_DATA":"review_gaps_sync_and_quality_not_cognitive_label"));
            }
        }
        File.WriteAllText(Path.Combine(output,"episode_method.json"),new JObject {
            ["complete"]=true,["version"]="1",["sessionId"]=(string)manifest["sessionId"],["selectedEpisode"]=selectedEpisode,
            ["boundaries"]="[start,end); closing event linked separately; no assumed 10s interaction duration",
            ["layers"]="region_visit; between_logged_events; object_hold",
            ["limits"]="Layers overlap and are not independent trials. Holding is not continuous active manipulation. Gaps between logs are not proof of inactivity. Basic validity fraction is not network completeness. No fixation or cognition inference."
        }.ToString());
        return output;
    }
}

}
