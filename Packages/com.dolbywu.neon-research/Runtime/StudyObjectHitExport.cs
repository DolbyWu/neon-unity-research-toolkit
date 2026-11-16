using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeonResearch
{
    /// <summary>One deterministic callback-to-object interpretation for HTML and derived CSV. Raw files remain immutable.</summary>
    public static class StudyObjectHitExport
    {
        private sealed class Meta { public string name="",group=""; public readonly HashSet<string> paths=new HashSet<string>(StringComparer.Ordinal); }
        private sealed class Link { public string owner,rule; }
        private sealed class Sample { public string[] r;public int index;public double t;public string raw,id,name,group,rule,status,source,mount,correction;public bool hit,verified; }
        private sealed class Stat { public string id,name,group,status;public int count,visits;public double first,seconds;public readonly SortedSet<string> rawIds=new SortedSet<string>(StringComparer.Ordinal); }
        private static string Cell(string[] r,string[] h,string key){int i=Array.IndexOf(h,key);return i>=0&&i<r.Length?r[i]:"";}
        private static bool Yes(string v)=>string.Equals(v,"true",StringComparison.OrdinalIgnoreCase);
        private static bool Number(string v,out double n)=>double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!double.IsNaN(n)&&!double.IsInfinity(n);
        private static string Text(double n)=>n.ToString("R",CultureInfo.InvariantCulture);
        private static string Coordinate(string value)
        {
            if(Number(value,out double n))return Text(n);
            if(value.Length>2&&value[0]=='\''&&(value[1]=='-'||value[1]=='+')&&Number(value.Substring(1),out n))return Text(n);
            return "";
        }
        public static JObject Build(string folder,string session,double duration,JObject reviewed,string outputTag)
        {
            if(!System.Text.RegularExpressions.Regex.IsMatch(outputTag??"",@"\A[A-Za-z0-9_-]{1,80}\z")||double.IsNaN(duration)||double.IsInfinity(duration)||duration<0)throw new ArgumentException("Invalid object export tag or duration.");
            var manifest=JObject.Parse(File.ReadAllText(Path.Combine(folder,"export_manifest.json")));
            double recordedDuration=manifest.Value<double?>("durationSeconds")??double.NaN;
            if(manifest.Value<bool?>("closed")!=true||manifest.Value<string>("sessionId")!=session||double.IsNaN(recordedDuration)||double.IsInfinity(recordedDuration)||Math.Abs(recordedDuration-duration)>1e-9)throw new InvalidDataException("Object exports require the matching closed session.");
            var metadata=new Dictionary<string,Meta>(StringComparer.Ordinal);var links=new Dictionary<string,Link>(StringComparer.Ordinal);var bad=new HashSet<string>(StringComparer.Ordinal);
            string catalogPath=Path.Combine(folder,"target_catalog.csv"),rawPath=Path.Combine(folder,"gaze_targets.csv");
            var catalogue=File.Exists(catalogPath)?StudySequenceAnalysis.Rows(catalogPath).ToArray():Array.Empty<string[]>();
            var ch=catalogue.Length>0?catalogue[0]:Array.Empty<string>();
            void Register(string id,string name,string group)
            {
                if(string.IsNullOrEmpty(id))return;if(!metadata.TryGetValue(id,out var m))metadata[id]=m=new Meta();
                if(!string.IsNullOrEmpty(name))m.name=name;if(!string.IsNullOrEmpty(group))m.group=group;
            }
            foreach(var r in catalogue.Skip(1))
            {
                if(r.Length!=ch.Length)throw new InvalidDataException("Target catalogue column mismatch.");
                string id=Cell(r,ch,"target_id");Register(id,Cell(r,ch,"target_name"),Cell(r,ch,"target_group"));
                if(string.IsNullOrEmpty(id))continue;
                if(metadata.TryGetValue(id,out var m))m.paths.Add(Cell(r,ch,"scene")+"\n"+Cell(r,ch,"hierarchy"));
            }
            bool Ancestor(string child,string owner)
            {
                if(!metadata.TryGetValue(child,out var c)||!metadata.TryGetValue(owner,out var p)||c.paths.Count!=1||p.paths.Count!=1)return false;
                var cp=c.paths.First().Split('\n');var pp=p.paths.First().Split('\n');
                return cp.Length==2&&pp.Length==2&&cp[0]!=""&&cp[0]==pp[0]&&pp[1]!=""&&cp[1].StartsWith(pp[1]+"/",StringComparison.Ordinal);
            }
            void Add(string child,string owner,string rule)
            {
                if(string.IsNullOrEmpty(child)||string.IsNullOrEmpty(owner)||child==owner)return;
                if(string.IsNullOrWhiteSpace(rule)||!Ancestor(child,owner)){bad.Add(child);return;}
                if(links.TryGetValue(child,out var old)&&old.owner!=owner){bad.Add(child);return;}
                links[child]=new Link{owner=owner,rule=rule};
            }
            foreach(var r in catalogue.Skip(1))Add(Cell(r,ch,"target_id"),Cell(r,ch,"statistics_owner_id"),Cell(r,ch,"statistics_owner_rule"));
            if(reviewed?.Value<string>("schema")=="gazeflow-reviewed-target-ownership-v1"&&reviewed.Value<string>("session_id")==session)
                foreach(var token in (reviewed["links"] as JArray)??new JArray())if(token is JObject l)Add(l.Value<string>("child_id"),l.Value<string>("owner_id"),l.Value<string>("rule"));
            var raw=File.Exists(rawPath)?StudySequenceAnalysis.Rows(rawPath).ToArray():Array.Empty<string[]>();
            var h=raw.Length>0?raw[0]:Array.Empty<string>();var list=new List<Sample>();int excluded=0;
            for(int i=1;i<raw.Length;i++)
            {
                var r=raw[i];if(r.Length!=h.Length)throw new InvalidDataException("Raw target CSV column mismatch at record "+i);
                if(!Number(Cell(r,h,"session_time_s"),out double time)||time<0||time>=duration){excluded++;continue;}
                string sid=Cell(r,h,"session_id");if(sid!=""&&sid!=session){excluded++;continue;}
                string id=Cell(r,h,"target_id");bool hit=Yes(Cell(r,h,"ray_valid"))&&Cell(r,h,"status")=="target_hit"&&id!="";
                var s=new Sample{r=r,index=i,t=time,raw=id,source=Cell(r,h,"source"),mount=Cell(r,h,"calibration_id"),correction=Cell(r,h,"correction_id"),hit=hit,
                    verified=Array.IndexOf(h,"mapping_verified")>=0?Yes(Cell(r,h,"mapping_verified")):new[]{"operator_verified","explicit_initial_approval","explicit_reconfirmation_after_head_rebind"}.Contains(Cell(r,h,"verification_status"))};
                if(hit)
                {
                    s.id=id;s.name=Cell(r,h,"target_name");s.group=Cell(r,h,"target_group");s.rule="raw_target_identity";s.status="raw_target_identity_no_extra_grouping";
                    if(Array.IndexOf(h,"statistics_owner_id")>=0)
                    {
                        string owner=Cell(r,h,"statistics_owner_id"),status=Cell(r,h,"statistics_owner_status"),rule=Cell(r,h,"statistics_owner_rule");
                        bool allowed=status=="raw_target_is_object"||status=="explicit_ancestor_owner"||status=="";
                        if(owner!=""&&allowed&&(owner==id||rule!="")&&(owner==id||metadata.ContainsKey(owner)))
                        {
                            s.id=owner;s.name=Cell(r,h,"statistics_owner_name");s.group=Cell(r,h,"statistics_owner_group");
                            s.rule=owner==id?"raw_target_identity":rule;s.status=status==""?"recorded_owner_legacy_status_unavailable":status;
                        }
                        else{s.id="";s.status="unresolved_recorded_owner";s.rule="no_fallback_from_invalid_live_owner";}
                    }
                    else
                    {
                        var seen=new HashSet<string>(StringComparer.Ordinal);string current=id,rule="";
                        while(links.TryGetValue(current,out var link))
                        {
                            if(bad.Contains(current)||!seen.Add(current)){current="";break;}
                            rule=rule==""?link.rule:rule+"; "+link.rule;current=link.owner;
                        }
                        if(current==""||bad.Contains(current)){s.id="";s.status="unresolved_conflicting_owner";s.rule="conflicting_or_invalid_structural_evidence";}
                        else if(current!=id){s.id=current;s.name="";s.group="";s.rule=rule;s.status="reviewed_or_catalogued_ancestor";}
                    }
                    if(s.id!=""&&metadata.TryGetValue(s.id,out var m)){if(s.name=="")s.name=m.name;if(s.group=="")s.group=m.group;}
                    if(s.name=="")s.name=id;
                }
                list.Add(s);
            }
            list=list.OrderBy(s=>s.t).ThenBy(s=>s.index).ToList();
            var header=new[]{"raw_callback_record","session_time_s","raw_target_id","raw_target_name","raw_target_group","object_id","object_name","object_group","ownership_status","ownership_rule","source","calibration_id","correction_id","mapping_verified","hit_geometry","hit_x","hit_y","hit_z","query_collider_path","statistics_key","raw_hit_x","raw_hit_y","raw_hit_z","session_id","participant_id"};
            var table=new JArray{new JArray(header)};var counts=new Dictionary<string,Stat>(StringComparer.Ordinal);Sample previous=null;
            bool Same(Sample a,Sample b)=>a.source==b.source&&a.mount==b.mount&&a.correction==b.correction&&a.verified==b.verified;
            string Key(Sample s)=>s.id!=""?s.id:"unresolved:"+s.raw;
            foreach(var s in list)
            {
                if(s.hit)
                {
                    string key=Key(s);
                    var record=new[]{s.index.ToString(CultureInfo.InvariantCulture),Text(s.t),s.raw,Cell(s.r,h,"target_name"),Cell(s.r,h,"target_group"),s.id,s.name,s.group,s.status,s.rule,s.source,s.mount,s.correction,s.verified?"True":"False",Cell(s.r,h,"hit_geometry"),Coordinate(Cell(s.r,h,"hit_x")),Coordinate(Cell(s.r,h,"hit_y")),Coordinate(Cell(s.r,h,"hit_z")),Cell(s.r,h,"query_collider_path"),key,Cell(s.r,h,"hit_x"),Cell(s.r,h,"hit_y"),Cell(s.r,h,"hit_z"),session,manifest.Value<string>("participantId")??""};
                    table.Add(new JArray(record));
                    if(!counts.TryGetValue(key,out var stat))counts[key]=stat=new Stat{id=s.id,name=s.name,group=s.group,status=s.status,first=s.t};
                    else if(stat.status!=s.status)stat.status="mixed_recorded_ownership_provenance";
                    stat.count++;stat.rawIds.Add(s.raw);
                    if(previous==null||!previous.hit||Key(previous)!=key||s.t-previous.t>.1||!Same(previous,s))stat.visits++;
                    if(previous!=null&&previous.hit&&Key(previous)==key&&s.t>previous.t&&s.t-previous.t<=.1&&Same(previous,s))stat.seconds+=s.t-previous.t;
                }
                previous=s;
            }
            var summary=new JArray{new JArray("statistics_key","object_id","object_name","object_group","first_hit_session_s","hit_callbacks","observed_visits","same_object_covered_s","raw_target_ids_json","ownership_status","start_s","end_s","quality_view","session_id","participant_id")};
            foreach(var kv in counts.OrderBy(k=>k.Key,StringComparer.Ordinal)){var v=kv.Value;summary.Add(new JArray(kv.Key,v.id,v.name,v.group,Text(v.first),v.count.ToString(),v.visits.ToString(),Text(v.seconds),JsonConvert.SerializeObject(v.rawIds),v.status,"0",Text(duration),"observed",session,manifest.Value<string>("participantId")??""));}
            var segments=StudySessionSegments.Describe(folder,manifest,duration);var segmentSummaries=new JObject();
            if(segments.Value<bool>("enabled"))
            {
                JArray SegmentSummary(double a,double b)
                {
                    var selectedCounts=new Dictionary<string,Stat>(StringComparer.Ordinal);Sample prior=null;
                    foreach(var s in list.Where(s=>s.t>=a&&s.t<b))
                    {
                        if(s.hit)
                        {
                            string key=Key(s);if(!selectedCounts.TryGetValue(key,out var v))selectedCounts[key]=v=new Stat{id=s.id,name=s.name,group=s.group,status=s.status,first=s.t};
                            else if(v.status!=s.status)v.status="mixed_recorded_ownership_provenance";
                            v.count++;v.rawIds.Add(s.raw);
                            if(prior==null||!prior.hit||Key(prior)!=key||s.t-prior.t>.1||!Same(prior,s))v.visits++;
                            if(prior!=null&&prior.hit&&Key(prior)==key&&s.t>prior.t&&s.t-prior.t<=.1&&Same(prior,s))v.seconds+=s.t-prior.t;
                        }
                        prior=s;
                    }
                    var tableForSegment=new JArray{summary[0].DeepClone()};
                    foreach(var kv in selectedCounts.OrderBy(k=>k.Key,StringComparer.Ordinal)){var v=kv.Value;tableForSegment.Add(new JArray(kv.Key,v.id,v.name,v.group,Text(v.first),v.count.ToString(),v.visits.ToString(),Text(v.seconds),JsonConvert.SerializeObject(v.rawIds),v.status,Text(a),Text(b),"observed",session,manifest.Value<string>("participantId")??""));}
                    return tableForSegment;
                }
                double boundary=segments.Value<double>("study_start_s");segmentSummaries["Tutorial"]=SegmentSummary(0,boundary);segmentSummaries["Study"]=SegmentSummary(boundary,duration);
            }
            string relative="Analysis/ObjectStatistics-"+outputTag,output=Path.Combine(folder,relative);StudyArchivePaths.RequireContainedUnlinked(folder,output);Directory.CreateDirectory(output);
            void Save(string name,JArray data)
            {
                string path=Path.Combine(output,name);StudyArchivePaths.RequireContainedUnlinked(folder,path);StudyArchivePaths.RequireContainedUnlinked(folder,path+".partial");
                var numeric=new HashSet<string>(new[]{"raw_callback_record","session_time_s","first_hit_session_s","hit_callbacks","observed_visits","same_object_covered_s","start_s","end_s","hit_x","hit_y","hit_z"},StringComparer.Ordinal);
                var text=new StringBuilder();int record=0;var columns=(JArray)data[0];foreach(JArray row in data)
                {
                    var values=row.Select((v,i)=>{string value=v.Type==JTokenType.Null?"":v.ToString();return record>0&&numeric.Contains(columns[i].ToString())&&Number(value,out double number)?(object)number:value;}).ToArray();
                    text.AppendLine(NeonStudyRecorder.Csv(values));record++;
                }
                StudyAtomicFile.Write(path,text.ToString());
            }
            Save("gaze_object_hits.csv",table);Save("gaze_object_summary.csv",summary);
            foreach(var segment in segmentSummaries.Properties())Save("gaze_object_summary_"+segment.Name.ToLowerInvariant()+".csv",(JArray)segment.Value);
            var result=new JObject{["schema"]="gazeflow-object-hit-export-v1",["session_id"]=session,["table"]=table,["summary"]=summary,
                ["hit_csv_relative"]="../"+relative+"/gaze_object_hits.csv",["summary_csv_relative"]="../"+relative+"/gaze_object_summary.csv",["segment_summaries"]=segmentSummaries,
                ["excluded_invalid_time_or_session_rows"]=excluded,["ownership_conflicts"]=new JArray(bad.OrderBy(v=>v,StringComparer.Ordinal)),
                ["semantics"]="Valid target_hit callbacks only. Raw target and canonical object identities retained. Unresolved ownership is explicit. CSV record index is not a physical line number. Callback time; not fixation or measured accuracy."};
            string report=Path.Combine(output,"export_checks.json");StudyArchivePaths.RequireContainedUnlinked(folder,report);
            StudyArchivePaths.RequireContainedUnlinked(folder,report+".partial");
            StudyAtomicFile.Write(report,new JObject{["schema"]=result["schema"],["session_id"]=session,["hit_records"]=table.Count-1,["summary_hit_callbacks"]=counts.Values.Sum(v=>v.count),["unresolved_hit_records"]=list.Count(v=>v.hit&&v.id==""),["excluded_invalid_time_or_session_rows"]=excluded,["ownership_conflicts"]=result["ownership_conflicts"].DeepClone(),["semantics"]=result["semantics"]}.ToString());
            return result;
        }
    }
}
