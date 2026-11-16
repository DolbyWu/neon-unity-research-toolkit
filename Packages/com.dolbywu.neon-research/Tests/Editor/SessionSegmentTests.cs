using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace NeonResearch.Tests
{
    public sealed class SessionSegmentTests
    {
        private string root;
        private JObject manifest;
        [SetUp] public void Before(){root=Path.GetFullPath("Library/SessionSegmentTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);manifest=new JObject{["sessionId"]="SYNTHETIC",["closed"]=true,["analysisStartRegion"]="Pre-room"};}
        [TearDown] public void After(){if(Directory.Exists(root))Directory.Delete(root,true);}
        private void Regions()=>File.WriteAllText(Path.Combine(root,"region_intervals.csv"),"start_session_s,end_session_s,region,source\n0,4,Tutorial,test\n4,6,Pre-room,test\n6,10,Study,test\n");
        [Test] public void FirstPreRoomEntryIncludesPreRoomInStudyAndPreservesSessionClock()
        {
            Regions();var result=StudySessionSegments.Describe(root,manifest,10);
            Assert.AreEqual(4,result.Value<double>("study_start_s"));Assert.IsTrue(result.Value<bool>("primary_analysis_available"));
            Assert.AreEqual(4,result["tutorial"].Value<double>("end_s"));Assert.AreEqual(4,result["study"].Value<double>("start_s"));
        }
        [Test] public void ConfiguredMissingEntryIsTutorialOnlyButGenericUnconfiguredProjectKeepsFullSession()
        {
            var result=StudySessionSegments.Describe(root,manifest,10);Assert.AreEqual(10,result.Value<double>("study_start_s"));Assert.IsFalse(result.Value<bool>("primary_analysis_available"));
            manifest.Remove("analysisStartRegion");result=StudySessionSegments.Describe(root,manifest,10);Assert.IsFalse(result.Value<bool>("enabled"));Assert.IsTrue(result.Value<bool>("primary_analysis_available"));
            Regions();result=StudySessionSegments.Describe(root,manifest,10);Assert.IsFalse(result.Value<bool>("enabled"));Assert.AreEqual(0,result.Value<double>("study_start_s"),"Region names alone must never enable segmentation.");
        }
        [Test] public void PartitionsReconcileWithHalfOpenBoundaryUnalignedEyeAndQuotedPayloadPreserved()
        {
            Regions();File.WriteAllText(Path.Combine(root,"game_events.csv"),"session_time_s,detail,session_id\n3.9,\"a,b\",SYNTHETIC\n4,study,SYNTHETIC\n10,end,SYNTHETIC\n5,foreign,OTHER\n");
            File.WriteAllText(Path.Combine(root,"head_pose.csv"),"session_time_s,rotation_x\n4,-0.123456\n");
            string neon=Path.Combine(root,"Neon-synthetic");Directory.CreateDirectory(neon);
            File.WriteAllText(Path.Combine(neon,"NeonGaze.csv"),"aligned_session_time_s,time_alignment_status,receive_session_time_s\n3.9,time_echo_estimated,4.1\n4,time_echo_estimated,4.2\n,time_unavailable,5\n");
            var result=StudySessionSegments.Build(root,manifest,10,new JObject());string output=Path.Combine(root,result.Value<string>("session_relative_folder"));
            Assert.IsTrue(result.Value<bool>("complete"));Assert.IsTrue(result["files"].All(f=>f.Value<bool>("partition_reconciles")));
            var tutorial=StudySequenceAnalysis.Rows(Path.Combine(output,"Tutorial/game_events.csv")).ToArray();var study=StudySequenceAnalysis.Rows(Path.Combine(output,"Study/game_events.csv")).ToArray();
            Assert.AreEqual(2,tutorial.Length);Assert.AreEqual("a,b",tutorial[1][1]);Assert.AreEqual("1",tutorial[1][3]);Assert.AreEqual("4",study[1][0]);Assert.AreEqual("2",study[1][3]);
            Assert.AreEqual(3,StudySequenceAnalysis.Rows(Path.Combine(output,"Unassigned/game_events.csv")).Count());
            Assert.AreEqual(2,StudySequenceAnalysis.Rows(Path.Combine(output,"Unassigned/Neon-synthetic/NeonGaze.csv")).Count());
            Assert.AreEqual(2,StudySequenceAnalysis.Rows(Path.Combine(output,"Study/Neon-synthetic/NeonGaze.csv")).Count());
            Assert.AreEqual("-0.123456",StudySequenceAnalysis.Rows(Path.Combine(output,"Study/head_pose.csv")).ToArray()[1][1]);
            Assert.IsTrue(File.ReadAllText(Path.Combine(root,"game_events.csv")).Contains("\"a,b\""));
        }
        [Test] public void SegmentSummariesMatchPartitionedHitsAndDoNotBridgeTutorialBoundaryOrInvalidRay()
        {
            Regions();manifest["durationSeconds"]=10;File.WriteAllText(Path.Combine(root,"export_manifest.json"),manifest.ToString());
            File.WriteAllText(Path.Combine(root,"gaze_targets.csv"),"session_time_s,source,ray_valid,status,target_id,target_name,target_group,session_id\n3.95,test,True,target_hit,ball,Ball,props,SYNTHETIC\n4,test,True,target_hit,ball,Ball,props,SYNTHETIC\n4.05,test,False,invalid_gaze,,,,SYNTHETIC\n4.1,test,True,target_hit,ball,Ball,props,SYNTHETIC\n4.15,test,True,target_hit,ball,Ball,props,SYNTHETIC\n");
            var objects=StudyObjectHitExport.Build(root,"SYNTHETIC",10,new JObject(),"synthetic");var study=(JArray)objects["segment_summaries"]["Study"];var h=study[0].Values<string>().ToArray();
            Assert.AreEqual(3,(int)study[1][Array.IndexOf(h,"hit_callbacks")]);Assert.AreEqual(2,(int)study[1][Array.IndexOf(h,"observed_visits")]);
            Assert.AreEqual(.05,(double)study[1][Array.IndexOf(h,"same_object_covered_s")],1e-9);
            var segments=StudySessionSegments.Build(root,manifest,10,objects);string output=Path.Combine(root,segments.Value<string>("session_relative_folder"));
            Assert.AreEqual(4,StudySequenceAnalysis.Rows(Path.Combine(output,"Study/gaze_object_hits.csv")).Count());Assert.IsTrue(File.Exists(Path.Combine(output,"Study/gaze_object_summary.csv")));
        }
        [Test] public void SegmentExportRejectsOpenRecording(){manifest["closed"]=false;Assert.Throws<InvalidOperationException>(()=>StudySessionSegments.Build(root,manifest,10,new JObject()));}
        [Test] public void ArbitraryConfiguredRegionIsTrimmedAndDoesNotGuessFromFamiliarNames()
        {
            Regions();manifest["analysisStartRegion"]="  MyExperimentStart  ";
            var missing=StudySessionSegments.Describe(root,manifest,10);Assert.IsFalse(missing.Value<bool>("primary_analysis_available"));
            File.AppendAllText(Path.Combine(root,"region_intervals.csv"),"6,10,MyExperimentStart,test\n");
            var result=StudySessionSegments.Describe(root,manifest,10);Assert.AreEqual(6,result.Value<double>("study_start_s"));Assert.AreEqual("MyExperimentStart",result.Value<string>("boundary_region"));
        }
    }
}
