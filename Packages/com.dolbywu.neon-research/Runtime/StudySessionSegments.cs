using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace NeonResearch
{
    /// <summary>Derived tutorial/study partitions; full raw recordings and session clocks are immutable.</summary>
    public static class StudySessionSegments
    {
        private static bool Number(string s,out double n)=>double.TryParse((s??"").TrimStart('\''),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!double.IsNaN(n)&&!double.IsInfinity(n);
        public static JObject Describe(string folder,JObject manifest,double duration)
        {
            string region=(manifest.Value<string>("analysisStartRegion")??"").Trim();bool required=!string.IsNullOrWhiteSpace(region);
            double? boundary=null;int record=0,sourceRecord=0;
            string path=Path.Combine(folder,"region_intervals.csv");
            if(required&&File.Exists(path))
            {
                StudyArchivePaths.RequireContainedUnlinked(folder,path);string[] header=null;
                foreach(var row in StudySequenceAnalysis.Rows(path))
                {
                    if(header==null){header=row;continue;}record++;
                    string Cell(string key){int i=Array.IndexOf(header,key);return i>=0&&i<row.Length?row[i]:"";}
                    if(!Cell("region").Equals(region,StringComparison.OrdinalIgnoreCase)||!Number(Cell("start_session_s"),out double a)||!Number(Cell("end_session_s"),out double b)||a<0||a>duration||b<=a)continue;
                    if(!boundary.HasValue||a<boundary){boundary=a;sourceRecord=record;}
                }
            }
            bool enabled=required||boundary.HasValue;double start=boundary??duration;
            return new JObject{["schema"]="gazeflow-session-segments-v1",["session_id"]=manifest.Value<string>("sessionId")??"",["enabled"]=enabled,
                ["status"]=boundary.HasValue?"split_at_first_recorded_region_entry":required?"study_boundary_not_recorded_tutorial_only":"unconfigured_full_session",
                ["boundary_region"]=region,["study_start_s"]=enabled?start:0,["session_end_s"]=duration,["primary_analysis_available"]=enabled?boundary.HasValue&&start<duration:true,
                ["boundary_source"]=boundary.HasValue?"region_intervals.csv":"unavailable",["boundary_source_record"]=sourceRecord,
                ["tutorial"]=new JObject{["start_s"]=0,["end_s"]=enabled?start:0,["include_in_primary_analysis"]=false},
                ["study"]=new JObject{["start_s"]=enabled?start:0,["end_s"]=duration,["include_in_primary_analysis"]=true},
                ["method"]="First recorded entry to the explicitly configured region; [start,end). Session seconds are never reset. The boundary region itself is retained in Study. Without an explicit region the full session remains eligible. Tutorial remains recorded/exported but excluded from primary analysis. Unaligned eye rows are unassigned, not sorted using receive time."};
        }
        public static JObject Build(string folder,JObject manifest,double duration,JObject objectData)
        {
            var scope=Describe(folder,manifest,duration);if(!scope.Value<bool>("enabled"))return scope;
            if(manifest.Value<bool?>("closed")!=true)throw new InvalidOperationException("Stop recording before segment export.");
            double boundary=scope.Value<double>("study_start_s");string relative="Analysis/SessionSegments-"+Guid.NewGuid().ToString("N"),output=Path.Combine(folder,relative);
            StudyArchivePaths.RequireContainedUnlinked(folder,output);Directory.CreateDirectory(output);
            var files=new JArray();scope["complete"]=false;scope["files"]=files;
            scope["manifest_relative"]="../"+relative+"/segments.json";scope["session_relative_folder"]=relative;
            void SaveScope()=>StudyAtomicFile.Write(Path.Combine(output,"segments.json"),scope.ToString(Formatting.Indented));SaveScope();
            void Partition(string source,string destination,string timeColumn,bool eye=false,bool interval=false)
            {
                if(!File.Exists(source))return;StudyArchivePaths.RequireContainedUnlinked(folder,source);
                string[] header=null;StreamWriter tutorial=null,study=null,unassigned=null;int index=0;long tc=0,sc=0,uc=0;
                try
                {
                    StreamWriter Writer(string part)
                    {
                        string target=Path.Combine(output,part,destination);StudyArchivePaths.RequireContainedUnlinked(folder,target);Directory.CreateDirectory(Path.GetDirectoryName(target));
                        var w=new StreamWriter(target,false,new UTF8Encoding(false));w.WriteLine(string.Join(",",header.Concat(new[]{"source_csv_record","segment_assignment_note"})));return w;
                    }
                    foreach(var row in StudySequenceAnalysis.Rows(source))
                    {
                        if(header==null){header=row;tutorial=Writer("Tutorial");study=Writer("Study");unassigned=Writer("Unassigned");continue;}index++;
                        if(row.Length!=header.Length)throw new InvalidDataException("Ragged source CSV during segmentation: "+destination);
                        string Cell(string key){int i=Array.IndexOf(header,key);return i>=0&&i<row.Length?row[i]:"";}
                        int part=-1;string note="";string sid=Cell("session_id");
                        if(sid!=""&&sid!=manifest.Value<string>("sessionId"))note="foreign_session_not_analyzed";
                        else if(eye&&Cell("time_alignment_status")!="time_echo_estimated")note="unaligned_eye_not_assigned_by_receive_time";
                        else if(!Number(Cell(timeColumn),out double t)||t<0||t>=duration)note="invalid_or_outside_half_open_session_time";
                        else if(interval&&(!Number(Cell("end_session_s"),out double end)||end<=t||end>duration+1e-6))note="invalid_interval";
                        else if(interval&&t<boundary&&Number(Cell("end_session_s"),out double crossing)&&crossing>boundary)note="cross_boundary_interval_retained_as_context";
                        else part=t<boundary?0:1;
                        // These are already-published raw fields. Quote verbatim rather than
                        // applying recorder string sanitization again to signed numeric text.
                        var line=string.Join(",",row.Concat(new[]{index.ToString(CultureInfo.InvariantCulture),note}).Select(v=>"\""+(v??"").Replace("\"","\"\"")+"\""));
                        if(part==0){tutorial.WriteLine(line);tc++;}else if(part==1){study.WriteLine(line);sc++;}else{unassigned.WriteLine(line);uc++;}
                    }
                }
                finally{tutorial?.Dispose();study?.Dispose();unassigned?.Dispose();}
                files.Add(new JObject{["destination"]=destination,["time_column"]=timeColumn,["source_records"]=index,["tutorial_records"]=tc,["study_records"]=sc,["unassigned_records"]=uc,["partition_reconciles"]=tc+sc+uc==index});
            }
            foreach(string name in new[]{"gaze_targets.csv","head_pose.csv","game_events.csv","behavior_events.csv"})Partition(Path.Combine(folder,name),name,"session_time_s");
            Partition(Path.Combine(folder,"region_intervals.csv"),"region_intervals.csv","start_session_s",interval:true);
            foreach(string neon in Directory.GetDirectories(folder,"Neon-*"))Partition(Path.Combine(neon,"NeonGaze.csv"),Path.Combine(Path.GetFileName(neon),"NeonGaze.csv"),"aligned_session_time_s",eye:true);
            string hits=objectData.Value<string>("hit_csv_relative");
            if(!string.IsNullOrEmpty(hits))Partition(Path.GetFullPath(Path.Combine(folder,"Replay",hits)),"gaze_object_hits.csv","session_time_s");
            foreach(string segment in new[]{"Tutorial","Study"})
                if(objectData["segment_summaries"]?[segment] is JArray summary)
                {
                    string target=Path.Combine(output,segment,"gaze_object_summary.csv");StudyArchivePaths.RequireContainedUnlinked(folder,target);Directory.CreateDirectory(Path.GetDirectoryName(target));
                    StudyAtomicFile.Write(target,string.Join("\n",summary.OfType<JArray>().Select(r=>NeonStudyRecorder.Csv(r.Select(v=>(object)v.ToString()).ToArray())))+"\n");
                }
            scope["context_files_preserved_at_session_root"]=new JArray("target_catalog.csv","clock_anchors.csv","Neon-*/clock_sync.csv","Neon-*/raw_payloads.csv","Video/quest-view.mp4","Audio/think-aloud.wav","media_capture.json","live_observations.json","annotation JSON and legacy tracking/puzzle logs");
            scope["media_policy"]="Full continuous media retained once at original session paths; no clipping, time reset, duplicated media or hardware-sync claim. Use session times and recorded media offset to review Tutorial or Study.";
            scope["complete"]=true;SaveScope();return scope;
        }
    }
}
