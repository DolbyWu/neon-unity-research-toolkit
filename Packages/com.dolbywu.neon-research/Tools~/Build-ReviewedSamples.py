"""Offline, deterministic curation from a CLOSED session and human-reviewed annotation JSON.
No network, calibration fitting, interpolation, fixation detection, training or raw-file writes.
Usage: python Tools/Build-ReviewedSamples.py --session <folder> --annotations <json> --output <NEW-folder>
"""
import argparse
import bisect
import csv
import hashlib
import json
import math
import re
from pathlib import Path

SCHEMA = "gazeflow-reviewed-samples-v1"


def number(value):
    if isinstance(value, bool):
        return None
    try:
        n = float(value)
        return n if math.isfinite(n) else None
    except (ValueError, TypeError):
        return None


def canonical(value):
    return json.dumps(value, sort_keys=True, ensure_ascii=False, indent=2, allow_nan=False) + "\n"


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def table(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(stream))


def overlap(a, b, c, d):
    return max(a, c) < min(b, d)


def coverage(records, start, end, valid, key, max_gap):
    """Consecutive valid, same-stream pairs only; no borrowing outside [start,end)."""
    seconds = 0.0
    previous = None
    for t, row, source in records:
        good = valid(row)
        if previous is not None and good and valid(previous[1]):
            dt = t - previous[0]
            if 0 < dt <= max_gap and (source, key(row)) == (previous[2], key(previous[1])):
                seconds += dt
        previous = (t, row, source)
    return round(min(1.0, seconds / (end - start)), 12)


def write_csv(path, rows, columns):
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def curate(session, annotations, output, *, window=2.0, min_coverage=.7, max_gap=.1,
           require_aoi=False, require_video=False, video_duration=None):
    session, annotations, output = map(lambda p: Path(p).resolve(), (session, annotations, output))
    if output == session or session in output.parents:
        raise ValueError("Output must be outside the immutable session folder.")
    if output.exists():
        raise ValueError("Output already exists; use a NEW directory. Earlier exports are immutable.")
    if not all(number(n) is not None for n in (window, min_coverage, max_gap)) or window <= 0 or not 0 <= min_coverage <= 1 or max_gap <= 0:
        raise ValueError("Invalid processing parameters.")
    if video_duration is not None and (number(video_duration) is None or video_duration <= 0):
        raise ValueError("Video duration must be positive, in seconds.")
    used = {}

    def read_json(path, name=None):
        used[name or path.relative_to(session).as_posix()] = digest(path)
        return json.loads(path.read_text(encoding="utf-8-sig"))

    manifest = read_json(session / "export_manifest.json")
    bundle = read_json(annotations, "reviewed_annotations.json")
    duration = number(manifest.get("durationSeconds"))
    if manifest.get("closed") is not True or duration is None or duration < 0:
        raise ValueError("A valid closed session is required.")
    if bundle.get("schema") not in ("neon-study-annotations-v1", "neon-study-annotations-v2") or bundle.get("session") != manifest.get("sessionId") or bundle.get("participant_id") != manifest.get("participantId", "") or bundle.get("time_base") != "session_seconds" or number(bundle.get("duration_s")) != duration:
        raise ValueError("Annotation schema/session/participant/duration/time basis mismatch.")
    labels = bundle.get("annotations")
    if not isinstance(labels, list) or len(labels) > 10000:
        raise ValueError("Invalid annotation list.")
    ids = set()
    for a in labels:
        if not isinstance(a, dict) or not isinstance(a.get("id"), str) or not a["id"] or a["id"] in ids:
            raise ValueError("Missing or duplicate annotation identity.")
        ids.add(a["id"])
        start, end = number(a.get("start_s")), number(a.get("end_s"))
        if start is None or end is None or not 0 <= start <= end <= duration:
            raise ValueError("Invalid annotation interval.")
        if not all(isinstance(a.get(k), str) and a[k].strip() for k in ("label", "annotator_id", "source", "confidence")):
            raise ValueError("Missing human-label provenance.")
        intensity = a.get("participant_intensity")
        if intensity is not None and (isinstance(intensity, bool) or number(intensity) is None or not 0 <= float(intensity) <= 10):
            raise ValueError("Invalid participant intensity.")
        confusion = a.get("confusion_rating_1_5")
        confusion_source = a.get("confusion_rating_source")
        if confusion is not None:
            if isinstance(confusion, bool) or not isinstance(confusion, (int, float)) or number(confusion) is None or not float(confusion).is_integer() or not 1 <= confusion <= 5:
                raise ValueError("Confusion rating must be an optional integer from 1 to 5.")
            if confusion_source != "participant_retrospective_self_report":
                raise ValueError("Confusion rating requires explicit participant retrospective self-report provenance.")
        elif confusion_source not in (None, ""):
            raise ValueError("Confusion rating provenance cannot be supplied without a rating.")
        observation = a.get("related_observation_id", "")
        if not isinstance(observation, str) or (observation and re.fullmatch(r"obs-[a-f0-9]{32}", observation) is None):
            raise ValueError("Invalid linked live observation identity.")
        if a.get("review_status", "draft") not in ("draft", "reviewed", "disputed", "excluded"):
            raise ValueError("Invalid review status.")
    gaze, aoi = [], []
    unaligned = 0

    def read_csv(path):
        if not path.exists():
            return []
        used[path.relative_to(session).as_posix()] = digest(path)
        return table(path)

    for folder in sorted(session.glob("Neon-*")):
        metadata = folder / "session.json"
        if not metadata.exists() or read_json(metadata).get("closed") is not True:
            raise ValueError("Every raw Neon stream must be closed.")
        for row in read_csv(folder / "NeonGaze.csv"):
            t = number(row.get("aligned_session_time_s"))
            if t is None or row.get("time_alignment_status") != "time_echo_estimated":
                unaligned += 1
                continue
            gaze.append((t, row, folder.name))
    for row in read_csv(session / "gaze_targets.csv"):
        t = number(row.get("session_time_s"))
        if t is not None:
            aoi.append((t, row, row.get("source", "")))
    events = read_csv(session / "game_events.csv")
    poses = read_csv(session / "head_pose.csv")
    reports = {}
    for file in sorted((session / "Calibration").glob("*/validation.json")):
        report = read_json(file)
        reports[report.get("id")] = report
    validation_intervals = []
    validation_starts = {}
    for row in events:
        t = number(row.get("session_time_s"))
        if t is None:
            continue
        if row.get("event_type") == "gaze_validation_start":
            validation_starts[row.get("object_id", "")] = t
        elif row.get("event_type") == "gaze_validation_end" and row.get("object_id", "") in validation_starts:
            validation_intervals.append((validation_starts.pop(row.get("object_id", "")), t))
    validation_intervals.extend((start, duration) for start in validation_starts.values())
    for report in reports.values():
        a, b = number(report.get("start_session_s")), number(report.get("end_session_s"))
        if a is not None and b is not None and b >= a:
            validation_intervals.append((a,b))
    # Invalid/out-of-order rows must not be repaired by sorting sensor data into plausibility.
    def monotonic(records):
        last = {}
        for t, row, stream in records:
            if stream in last and t <= last[stream]:
                row["_invalid_time_order"] = "true"
            last[stream] = t
        return sorted(records, key=lambda item: (item[0], item[2]))
    gaze, aoi = monotonic(gaze), monotonic(aoi)
    gaze_times, aoi_times = [r[0] for r in gaze], [r[0] for r in aoi]

    def inside(records, times, start, end):
        return records[bisect.bisect_left(times, start):bisect.bisect_left(times, end)]

    def eye_valid(row):
        return not row.get("_invalid_time_order") and row.get("worn", "").lower() == "true" and row.get("sample_quality", "").startswith("basic_valid") and number(row.get("gaze_x_px")) is not None and number(row.get("gaze_y_px")) is not None

    def aoi_valid(row):
        report = reports.get(row.get("verification_id"))
        return (not row.get("_invalid_time_order") and row.get("ray_valid", "").lower() == "true" and row.get("status") in ("target_hit", "no_hit", "non_target_occluder") and row.get("verification_status") == "measured_pass_operator_approval_separate" and report is not None and report.get("passed") is True and report.get("aborted") is False and report.get("session_id") == manifest["sessionId"] and report.get("calibration_id") == row.get("calibration_id"))

    params = dict(window_s=window, stride_s=window, min_coverage=min_coverage, max_gap_s=max_gap, require_aoi=require_aoi, require_video=require_video, video_duration_s=video_duration,
                  time_basis="time_echo_estimated_aligned_session_seconds", interval="[start,end)", overlap_rule="exclude_any_active_overlapping_annotation_until_reconciled", trailing_rule="audit_and_reject", unlabeled_rule="audit_not_negative_class", validation_rule="exclude_validation_intervals_missing_end_extends_to_session_end", aoi_time_basis="Unity_callback_seconds_not_device_sync", eye_coverage="consecutive_basic_valid_same_stream_and_epoch_pairs_no_interpolation", aoi_coverage="consecutive_measured_pass_operator_approved_same_source_calibration_verification_pairs", cross_source_join="shared_session_seconds_only_NOT_hardware_sync")
    params_id = hashlib.sha256(canonical(params).encode()).hexdigest()[:16]
    sample_rows, audit, interval_rows = [], [], []
    video = bundle.get("video", {})
    offset = number(video.get("offset_s"))
    for a in sorted(labels, key=lambda row: (row["start_s"], row["end_s"], row["id"])):
        start, end = float(a["start_s"]), float(a["end_s"])
        status = a.get("review_status", "draft")
        base = dict(participant_id=manifest.get("participantId", ""), session_id=manifest["sessionId"], annotation_id=a["id"], label=a["label"], annotator_id=a["annotator_id"], evidence_source=a["source"], annotator_confidence=a["confidence"], participant_intensity=a.get("participant_intensity"), intensity_source="participant_self_report" if a.get("participant_intensity") is not None else "", confusion_rating_1_5=a.get("confusion_rating_1_5"), confusion_rating_source=a.get("confusion_rating_source") or "", related_observation_id=a.get("related_observation_id", ""), review_status=status, annotation_revision=a.get("record_revision", 1), related_ids=json.dumps(a.get("related_ids", []), separators=(",", ":")), time_basis=params["time_basis"], processing_id=params_id)
        interval_rows.append(dict(base, start_s=start, end_s=end))
        if start == end:
            audit.append(dict(annotation_id=a["id"], start_s=start, end_s=end, decision="rejected", reasons="point_annotation_not_a_training_interval"))
            continue
        cursor = start
        while cursor < end - 1e-9:
            stop = round(min(end, cursor + window), 9)
            reasons = []
            if stop - cursor < window - 1e-8:
                reasons.append("short_trailing_window")
            if status != "reviewed":
                reasons.append("annotation_" + status)
            if any(overlap(cursor, stop, a, b) for a,b in validation_intervals):
                reasons.append("overlaps_spatial_validation_not_task_data")
            if a["label"].strip().lower() in ("unlabeled", "uncertain", "uncertainty"):
                reasons.append("no_resolved_target_label")
            conflicting = [b["id"] for b in labels if b["id"] != a["id"] and b.get("review_status", "draft") != "excluded" and overlap(cursor, stop, b["start_s"], b["end_s"])]
            if conflicting:
                reasons.append("overlapping_annotations_require_reconciliation:" + ",".join(sorted(conflicting)))
            eyes = inside(gaze, gaze_times, cursor, stop)
            hits = inside(aoi, aoi_times, cursor, stop)
            eye_cov = coverage(eyes, cursor, stop, eye_valid, lambda r: r.get("connection_epoch", ""), max_gap)
            aoi_cov = coverage(hits, cursor, stop, aoi_valid, lambda r: (r.get("calibration_id"), r.get("verification_id")), max_gap)
            if eye_cov < min_coverage or not any(eye_valid(row) for _, row, _ in eyes):
                reasons.append("insufficient_aligned_valid_eye_coverage")
            if require_aoi and (aoi_cov < min_coverage or not any(aoi_valid(row) for _, row, _ in hits)):
                reasons.append("insufficient_verified_aoi_coverage")
            video_start, video_end = (None, None) if offset is None else (cursor-offset, stop-offset)
            video_covered = video_duration is not None and video_start is not None and 0 <= video_start <= video_end <= video_duration
            if require_video and not video_covered:
                reasons.append("video_offset_duration_or_coverage_unavailable")
            decision = "rejected" if reasons else "included"
            audit.append(dict(annotation_id=a["id"], start_s=cursor, end_s=stop, decision=decision, reasons=";".join(reasons)))
            valid_rows = [row for _, row, _ in eyes if eye_valid(row)]
            pupil = {}
            for name in ("pupil_left_mm", "pupil_right_mm"):
                values = [number(row.get(name)) for row in valid_rows if row.get("eye_state_available", "").lower() == "true"]
                values = [n for n in values if n is not None and n > 0]
                pupil[name+"_wire_mean"] = round(sum(values)/len(values), 12) if values else None
                pupil[name+"_available_count"] = len(values)
            if not reasons:
                identity = canonical([manifest["sessionId"], a["id"], cursor, stop, params_id])
                sample_rows.append(dict(base, sample_id=hashlib.sha256(identity.encode()).hexdigest()[:24], start_s=cursor, end_s=stop, eye_count=len(eyes), eye_valid_count=len(valid_rows), eye_coverage=eye_cov, eye_missing_fraction=round(1-eye_cov,12), aoi_coverage=aoi_cov, aoi_missing_fraction=round(1-aoi_cov,12), aoi_callback_count=len(hits), target_hit_callbacks=sum(row.get("status")=="target_hit" and aoi_valid(row) for _,row,_ in hits), calibration_ids=json.dumps(sorted({row.get("calibration_id", "") for _,row,_ in hits})), verification_ids=json.dumps(sorted({row.get("verification_id", "") for _,row,_ in hits})), aoi_statuses=json.dumps(sorted({row.get("status", "") for _,row,_ in hits})), video_start_s=video_start, video_end_s=video_end, video_coverage_confirmed=video_covered, video_timing_method=video.get("timing_method", "unavailable"), behavior_event_count=sum(cursor <= t < stop for row in events if (t:=number(row.get("session_time_s"))) is not None), head_pose_count=sum(cursor <= t < stop for row in poses if (t:=number(row.get("session_time_s"))) is not None), **pupil))
            cursor = stop
    # Audit unannotated time separately; it is never converted to a negative class.
    intervals = sorted((float(a["start_s"]), float(a["end_s"])) for a in labels if a["end_s"] > a["start_s"])
    cursor = 0.0
    for start, end in intervals:
        if start > cursor:
            audit.append(dict(annotation_id="", start_s=cursor, end_s=start, decision="unlabeled", reasons="not_a_negative_class"))
        cursor = max(cursor, end)
    if cursor < duration:
        audit.append(dict(annotation_id="", start_s=cursor, end_s=duration, decision="unlabeled", reasons="not_a_negative_class"))
    # Check immutability again before writing any result.
    for name, expected in used.items():
        source = annotations if name == "reviewed_annotations.json" else session/name
        if digest(source) != expected:
            raise ValueError("Input changed during curation; no output written.")
    output.mkdir(parents=True)
    base_columns = list(dict(participant_id=0, session_id=0, annotation_id=0, label=0, annotator_id=0, evidence_source=0, annotator_confidence=0, participant_intensity=0, intensity_source=0, confusion_rating_1_5=0, confusion_rating_source=0, related_observation_id=0, review_status=0, annotation_revision=0, related_ids=0, time_basis=0, processing_id=0))
    extra = ["sample_id","start_s","end_s","eye_count","eye_valid_count","eye_coverage","eye_missing_fraction","aoi_coverage","aoi_missing_fraction","aoi_callback_count","target_hit_callbacks","calibration_ids","verification_ids","aoi_statuses","video_start_s","video_end_s","video_coverage_confirmed","video_timing_method","behavior_event_count","head_pose_count","pupil_left_mm_wire_mean","pupil_left_mm_available_count","pupil_right_mm_wire_mean","pupil_right_mm_available_count"]
    write_csv(output/"samples.csv", sample_rows, base_columns+extra)
    write_csv(output/"curated_intervals.csv", interval_rows, base_columns+["start_s","end_s"])
    write_csv(output/"audit.csv", audit, ["annotation_id","start_s","end_s","decision","reasons"])
    outputs = {name: digest(output/name) for name in ("samples.csv","curated_intervals.csv","audit.csv")}
    result = dict(schema=SCHEMA, processing_id=params_id, parameters=params, input_sha256=dict(sorted(used.items())), output_sha256=outputs, builder_sha256=digest(Path(__file__)), included_samples=len(sample_rows), audit_rows=len(audit), unaligned_raw_rows_not_joined=unaligned,
                  human_measure_policy={"participant_intensity": "optional legacy 0-10 self-report; never converted", "confusion_rating_1_5": "optional integer 1-5 participant retrospective report; distinct from legacy intensity and annotator confidence", "related_observation_id": "optional syntactically validated original observer reference; not proof of cognitive truth", "analysis_warning": "Human ratings and labels are potential outcomes, not automatically predictors. Avoid target leakage."},
                  limitations=["Basic valid is not full eye-quality validation; pupil wire units are not physically validated millimetres.", "No interpolation or missing-value zero filling. AOI coverage is optional and distinct from raw eye quality.", "Video coverage uses supplied duration and saved offset, not measured hardware synchronization.", "Event/head counts are context, not validated cognitive features. Prevent task/outcome leakage when selecting predictors.", "Split by participant/session/task before model tuning; do not randomly split adjacent windows. Fit preprocessing only on training folds. Single-participant data cannot establish cross-participant generalization."])
    (output/"manifest.json").write_text(canonical(result), encoding="utf-8")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--session", required=True)
    parser.add_argument("--annotations", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--window", type=float, default=2)
    parser.add_argument("--min-coverage", type=float, default=.7)
    parser.add_argument("--max-gap", type=float, default=.1)
    parser.add_argument("--require-aoi", action="store_true")
    parser.add_argument("--require-video", action="store_true")
    parser.add_argument("--video-duration", type=float)
    args = parser.parse_args()
    try:
        result = curate(args.session,args.annotations,args.output,window=args.window,min_coverage=args.min_coverage,max_gap=args.max_gap,require_aoi=args.require_aoi,require_video=args.require_video,video_duration=args.video_duration)
        print("Included samples:",result["included_samples"],"; audit rows:",result["audit_rows"])
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError) as error:
        parser.exit(1, "Curation rejected: "+str(error)+"\n")
