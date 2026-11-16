using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace NeonResearch.Tests
{
    public sealed class ObjectHitExportTests
    {
        private string root;
        private const string Session="synthetic-object-export";
        private readonly string[] catalogue={"target_id","target_name","target_group","scene","hierarchy","active"};
        private readonly string[] rawHeader={"session_time_s","source","ray_valid","status","target_id","target_name","target_group","calibration_id","correction_id","mapping_verified","session_id","hit_x"};
        [SetUp]public void Before(){root=Path.GetFullPath("Library/ObjectHitExportTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"export_manifest.json"),new JObject{["sessionId"]=Session,["closed"]=true,["durationSeconds"]=5}.ToString());}
        [TearDown]public void After(){Assert.That(root.StartsWith(Path.GetFullPath("Library/ObjectHitExportTests")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));Directory.Delete(root,true);}
        private void Write(string name,string[][] records)=>File.WriteAllText(Path.Combine(root,name),string.Join("\n",records.Select(r=>NeonStudyRecorder.Csv(r.Cast<object>().ToArray()))));
        private string[] Hit(double t,string id,string status="target_hit",bool valid=true)=>new[]{t.ToString("R",System.Globalization.CultureInfo.InvariantCulture),"synthetic",valid?"True":"False",status,id,id,"raw","mount","","False",Session,"-1"};
        private void Catalog()=>Write("target_catalog.csv",new[]{catalogue,new[]{"p","Diary","notes","Scene","Diary","False"},new[]{"a","Visuals","mesh","Scene","Diary/Visuals","False"},new[]{"b","Mesh","mesh","Scene","Diary/Mesh","False"},new[]{"lid","Lid","parts","Scene","Diary/Lid","False"},new[]{"q","Other object","other","Scene","Other","True"}});
        private JObject Reviewed(params string[][] pairs)=>new JObject{["schema"]="gazeflow-reviewed-target-ownership-v1",["session_id"]=Session,["links"]=new JArray(pairs.Select(p=>new JObject{["child_id"]=p[0],["owner_id"]=p[1],["rule"]="explicit_review"}))};
        private static string Cell(JArray table,JArray row,string name)=>row[table[0].Select(v=>v.ToString()).ToList().IndexOf(name)].ToString();
        [Test]public void HitsOnlyCsvAndHtmlUseTheSameExactChildObjectPairsAndConservativeSummary()
        {
            Catalog();Write("gaze_targets.csv",new[]{rawHeader,Hit(1,"a"),Hit(1.05,"b"),Hit(1.1,"p"),Hit(1.15,"lid"),Hit(1.16,"",valid:false),Hit(1.17,"",status:"non_target_occluder"),Hit(2,"a")});
            string before=File.ReadAllText(Path.Combine(root,"gaze_targets.csv"));
            var result=StudyObjectHitExport.Build(root,Session,5,Reviewed(new[]{"a","p"},new[]{"b","p"}),"test");var table=(JArray)result["table"];var summary=(JArray)result["summary"];
            Assert.That(table.Count-1,Is.EqualTo(5));Assert.That(Cell(table,(JArray)table[1],"raw_target_id"),Is.EqualTo("a"));Assert.That(Cell(table,(JArray)table[1],"object_id"),Is.EqualTo("p"));
            Assert.That(Cell(table,(JArray)table[4],"object_id"),Is.EqualTo("lid"),"Independent nested parts stay separate.");
            var diary=summary.Skip(1).Cast<JArray>().Single(r=>Cell(summary,r,"object_id")=="p");Assert.That(Cell(summary,diary,"hit_callbacks"),Is.EqualTo("4"));Assert.That(Cell(summary,diary,"observed_visits"),Is.EqualTo("2"));Assert.That(double.Parse(Cell(summary,diary,"same_object_covered_s"),System.Globalization.CultureInfo.InvariantCulture),Is.EqualTo(.1).Within(1e-9));
            var csv=StudySequenceAnalysis.Rows(Path.Combine(root,"Analysis/ObjectStatistics-test/gaze_object_hits.csv")).ToArray();Assert.That(csv.Length,Is.EqualTo(table.Count));Assert.That(csv[1][Array.IndexOf(csv[0],"hit_x")],Is.EqualTo("-1"),"Numeric negative coordinates must not gain a formula-escape apostrophe.");Assert.That(File.ReadAllText(Path.Combine(root,"gaze_targets.csv")),Is.EqualTo(before));
        }
        [Test]public void PerCallbackOwnerOverridesStaticCatalogueAndUnavailableOwnerRemainsExplicit()
        {
            Catalog();var h=rawHeader.Concat(new[]{"statistics_owner_id","statistics_owner_name","statistics_owner_group","statistics_owner_rule","statistics_owner_status"}).ToArray();
            Write("gaze_targets.csv",new[]{h,Hit(1,"a").Concat(new[]{"p","Recorded diary","notes","live_visual_link","explicit_ancestor_owner"}).ToArray(),Hit(1.05,"a").Concat(new[]{"q","Recorded other","other","live_reparent_link","explicit_ancestor_owner"}).ToArray(),Hit(1.1,"a").Concat(new[]{"","","","","owner_unavailable_or_not_unique"}).ToArray()});
            var result=StudyObjectHitExport.Build(root,Session,5,Reviewed(new[]{"a","p"}),"live");var t=(JArray)result["table"];Assert.That(Cell(t,(JArray)t[1],"object_id"),Is.EqualTo("p"));Assert.That(Cell(t,(JArray)t[2],"object_id"),Is.EqualTo("q"));Assert.That(Cell(t,(JArray)t[2],"object_name"),Is.EqualTo("Recorded other"));Assert.That(Cell(t,(JArray)t[3],"object_id"),Is.Empty);Assert.That(Cell(t,(JArray)t[3],"ownership_status"),Is.EqualTo("unresolved_recorded_owner"));
        }
        [Test]public void UnrelatedHistoricalOwnerIsRejectedAndCatalogueNameOrInactivityNeverCreatesAHitsRow()
        {
            Catalog();Write("gaze_targets.csv",new[]{rawHeader,Hit(1,"a")});var result=StudyObjectHitExport.Build(root,Session,5,Reviewed(new[]{"a","q"}),"reject");var t=(JArray)result["table"];Assert.That(t.Count-1,Is.EqualTo(1));Assert.That(Cell(t,(JArray)t[1],"object_id"),Is.Empty);Assert.That(Cell(t,(JArray)t[1],"ownership_status"),Is.EqualTo("unresolved_conflicting_owner"));
            var unlinked=StudyObjectHitExport.Build(root,Session,5,new JObject(),"unlinked");Assert.That(Cell((JArray)unlinked["table"],(JArray)unlinked["table"][1],"object_id"),Is.EqualTo("a"),"No automatic ancestor merge.");
        }
        [Test]public void WrongSessionOutOfRangeAndInvalidTimeAreExcludedAndOutputPathIsBounded()
        {
            Catalog();var foreign=Hit(2,"a");foreign[10]="foreign";var nan=Hit(2,"a");nan[0]="NaN";Write("gaze_targets.csv",new[]{rawHeader,Hit(1,"a"),foreign,Hit(9,"a"),nan});
            var result=StudyObjectHitExport.Build(root,Session,5,new JObject(),"bounded");Assert.That(result.Value<int>("excluded_invalid_time_or_session_rows"),Is.EqualTo(3));Assert.That(((JArray)result["table"]).Count-1,Is.EqualTo(1));Assert.Throws<ArgumentException>(()=>StudyObjectHitExport.Build(root,Session,5,new JObject(),"../elsewhere"));
        }
    }
}
