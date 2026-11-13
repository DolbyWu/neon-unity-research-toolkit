"""Synthetic-only acceptance tests; never opens a participant archive or contacts a network."""
import csv
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec=importlib.util.spec_from_file_location("builder",Path(__file__).with_name("Build-ReviewedSamples.py"))
builder=importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class ReviewedSamplesTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="gazeflow-synthetic-")
        self.root=Path(self.temp.name)
        self.session=self.root/"session"
        self.session.mkdir()
        self.annotations=self.root/"annotations.json"
        self.manifest={"sessionId":"SYNTHETIC", "participantId":"SYNTHETIC-ONLY", "durationSeconds":14, "closed":True}
        (self.session/"export_manifest.json").write_text(json.dumps(self.manifest))
        folder=self.session/"Neon-synthetic"
        folder.mkdir()
        (folder/"session.json").write_text('{"closed":true}')
        rows=[]
        for i in range(280):
            t=i*.05
            if 2<=t<4:
                continue
            rows.append({"aligned_session_time_s":round(t,4), "time_alignment_status":"time_echo_estimated", "connection_epoch":"1", "sample_quality":"basic_valid_not_full_eye_quality_assessment", "worn":"True", "gaze_x_px":500, "gaze_y_px":350, "eye_state_available":"True", "pupil_left_mm":.0034,"pupil_right_mm":""})
        rows.append({**rows[0],"aligned_session_time_s":"","time_alignment_status":"unavailable"})
        self.csv(folder/"NeonGaze.csv",rows)
        self.csv(self.session/"game_events.csv",[{"session_time_s":4,"event_type":"grab_start"},{"session_time_s":6,"event_type":"boundary_not_inside_previous"}])
        def label(id,start,end,text,status="reviewed"):
            return dict(id=id,start_s=start,end_s=end,label=text,annotator_id="R1",source="retrospective_interview",confidence="medium",review_status=status,participant_intensity=0,record_revision=2,related_ids=[])
        self.bundle={"schema":"neon-study-annotations-v2","session":"SYNTHETIC","participant_id":"SYNTHETIC-ONLY","duration_s":14,"time_base":"session_seconds","video":{"offset_s":1,"timing_method":"capture_launch_estimate"},"annotations":[label("a",0,4,"Confusion"),label("b",4,6,"Insight"),label("c",6,8,"Stuck","draft"),label("d",8,10,"Confusion"),label("e",9,10,"Insight"),label("f",10,11,"Insight")]}
        self.save()
    def tearDown(self):
        self.temp.cleanup()
    @staticmethod
    def csv(path,rows):
        with path.open("w",newline="") as stream:
            writer=csv.DictWriter(stream,fieldnames=list(rows[0]))
            writer.writeheader();writer.writerows(rows)
    def save(self):
        self.annotations.write_text(json.dumps(self.bundle))
    def build(self,name,**kwargs):
        return builder.curate(self.session,self.annotations,self.root/name,**kwargs)
    def test_identical_outputs_and_auditable_missing_overlap_adjacent_and_tail(self):
        first=self.build("one")
        second=self.build("two")
        self.assertEqual(first,second)
        for name in ("samples.csv","curated_intervals.csv","audit.csv","manifest.json"):
            self.assertEqual((self.root/"one"/name).read_bytes(),(self.root/"two"/name).read_bytes())
        self.assertEqual(first["included_samples"],2)
        self.assertEqual(first["unaligned_raw_rows_not_joined"],1)
        audit=(self.root/"one/audit.csv").read_text()
        for reason in ("insufficient_aligned_valid_eye_coverage","overlapping_annotations_require_reconciliation","short_trailing_window","annotation_draft","not_a_negative_class"):
            self.assertIn(reason,audit)
        samples=builder.table(self.root/"one/samples.csv")
        self.assertEqual([r["annotation_id"] for r in samples],["a","b"])
        self.assertEqual(samples[1]["behavior_event_count"],"1")
        self.assertEqual(samples[0]["participant_intensity"],"0")
        self.assertEqual(samples[0]["pupil_right_mm_wire_mean"],"")
    def test_aoi_and_video_are_optional_independent_quality_requirements(self):
        self.assertEqual(self.build("raw")["included_samples"],2)
        self.assertEqual(self.build("aoi",require_aoi=True)["included_samples"],0)
        self.assertEqual(self.build("video",require_video=True)["included_samples"],0)
        self.assertEqual(self.build("covered-video",require_video=True,video_duration=5)["included_samples"],1)
    def test_confusion_rating_and_observer_link_preserved_without_legacy_scale_conversion(self):
        first=self.bundle["annotations"][0]
        first.update(confusion_rating_1_5=4,confusion_rating_source="participant_retrospective_self_report",related_observation_id="obs-"+"a"*32)
        self.save()
        result=self.build("ratings")
        for name in ("samples.csv","curated_intervals.csv"):
            rows=builder.table(self.root/"ratings"/name)
            row=next(r for r in rows if r["annotation_id"]=="a")
            self.assertEqual(row["confusion_rating_1_5"],"4")
            self.assertEqual(row["confusion_rating_source"],"participant_retrospective_self_report")
            self.assertEqual(row["related_observation_id"],"obs-"+"a"*32)
            self.assertEqual(row["participant_intensity"],"0")
            absent=next(r for r in rows if r["annotation_id"]=="b")
            self.assertEqual(absent["confusion_rating_1_5"],"")
            self.assertEqual(absent["related_observation_id"],"")
        self.assertIn("never converted",result["human_measure_policy"]["participant_intensity"])
    def test_invalid_confusion_rating_provenance_and_observation_link_are_rejected(self):
        first=self.bundle["annotations"][0]
        first["confusion_rating_source"]="participant_retrospective_self_report"
        for index,value in enumerate((0,6,2.5,True,"4",float("nan"))):
            with self.subTest(value=value):
                first["confusion_rating_1_5"]=value;self.save()
                with self.assertRaises(ValueError):self.build("bad-rating-"+str(index))
        first["confusion_rating_1_5"]=3
        for index,value in enumerate((None,"researcher_observation","participant_self_report")):
            first["confusion_rating_source"]=value;self.save()
            with self.assertRaises(ValueError):self.build("bad-provenance-"+str(index))
        first["confusion_rating_source"]="participant_retrospective_self_report"
        for index,value in enumerate(("unknown",None,"obs-"+"G"*32)):
            first["related_observation_id"]=value;self.save()
            with self.assertRaises(ValueError):self.build("bad-link-"+str(index))
        first["related_observation_id"]="";first["confusion_rating_1_5"]=None;self.save()
        with self.assertRaises(ValueError):self.build("source-without-rating")
    def test_legacy_does_not_silently_become_reviewed(self):
        self.bundle["schema"]="neon-study-annotations-v1"
        for a in self.bundle["annotations"]:
            a.pop("review_status")
        self.save()
        self.assertEqual(self.build("legacy")["included_samples"],0)
    def test_immutable_inputs_and_refuse_overwrite_or_nested_output(self):
        before={p:builder.digest(p) for p in self.session.rglob("*") if p.is_file()}
        self.build("one")
        self.assertEqual(before,{p:builder.digest(p) for p in before})
        with self.assertRaises(ValueError): self.build("one")
        with self.assertRaises(ValueError): builder.curate(self.session,self.annotations,self.session/"derived")
    def test_wrong_identity_invalid_intensity_and_unclosed_session_fail(self):
        self.bundle["participant_id"]="OTHER"
        self.save()
        with self.assertRaises(ValueError): self.build("bad")
        self.bundle["participant_id"]="SYNTHETIC-ONLY"
        self.bundle["annotations"][0]["participant_intensity"]=11
        self.save()
        with self.assertRaises(ValueError): self.build("bad2")
        self.bundle["annotations"][0]["participant_intensity"]=None
        self.save()
        self.manifest["closed"]=False
        (self.session/"export_manifest.json").write_text(json.dumps(self.manifest))
        with self.assertRaises(ValueError): self.build("bad3")
    def test_nonfinite_parameters_and_invalid_time_order_never_pass(self):
        with self.assertRaises(ValueError): self.build("nan",window=float("nan"))
        self.assertIsNone(builder.number(True))
        records=[(0,{"ok":True},"s"),(1,{"ok":True},"s")]
        self.assertEqual(builder.coverage(records,0,2,lambda r:r["ok"],lambda r:1,.1),0)
    def test_validation_phase_is_excluded_and_aoi_requires_matching_session_report(self):
        self.csv(self.session/"game_events.csv",[{"session_time_s":0,"event_type":"gaze_validation_start","object_id":"v"},{"session_time_s":1,"event_type":"gaze_validation_end","object_id":"v"}])
        self.assertEqual(self.build("validation-phase")["included_samples"],1)
        report_dir=self.session/"Calibration/v"
        report_dir.mkdir(parents=True)
        report={"id":"v","calibration_id":"c","session_id":"SYNTHETIC","passed":True,"aborted":False}
        (report_dir/"validation.json").write_text(json.dumps(report))
        rows=[{"session_time_s":round(i*.05,4),"source":"synthetic","ray_valid":"True","status":"no_hit","calibration_id":"c","verification_id":"v","verification_status":"measured_pass_operator_approval_separate"} for i in range(280)]
        self.csv(self.session/"gaze_targets.csv",rows)
        self.assertEqual(self.build("verified-aoi",require_aoi=True)["included_samples"],1)
        report["session_id"]="OTHER"
        (report_dir/"validation.json").write_text(json.dumps(report))
        self.assertEqual(self.build("wrong-report",require_aoi=True)["included_samples"],0)


if __name__=="__main__": unittest.main(verbosity=2)
