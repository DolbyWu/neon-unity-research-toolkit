using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
namespace NeonResearch
{
    /// <summary>Observer-marked intervals, not inferred cognitive truth. Independent of gaze/mapping quality.</summary>
    public static class StudyLiveObservations
    {
        [Serializable] public sealed class Record
        {
            public string observation_id,observer_id,label="Possible confusion",notes="",status="open",end_reason="";
            public string source="researcher_observation",created_utc,updated_utc;
            public double start_s;
            public double? end_s;
        }
        [Serializable] public sealed class Bundle
        {
            public string schema="gazeflow-live-observations-v1",session_id,time_base="session_seconds";
            public string method="Observer-selected intervals; interpretations are not automatically confirmed participant states. Player retrospective answers are saved separately.";
            public List<Record> records=new List<Record>();
        }
        private static Bundle bundle;
        public const int Limit=10;
        public static IReadOnlyList<Record> Records { get{Ensure();return bundle.records.AsReadOnly();} }
        public static Record Active { get{Ensure();return bundle.records.FirstOrDefault(r=>r.end_s==null);} }
        public static void Reset()=>bundle=null;
        private static void Ensure()
        {
            if(StudyExportSession.SessionId==null){bundle=new Bundle();return;}
            if(bundle?.session_id==StudyExportSession.SessionId)return;
            string file=Path.Combine(StudyExportSession.Folder,"live_observations.json");
            bundle=File.Exists(file)?JsonConvert.DeserializeObject<Bundle>(File.ReadAllText(file)):new Bundle{session_id=StudyExportSession.SessionId};
            if(bundle==null||bundle.schema!="gazeflow-live-observations-v1"||bundle.session_id!=StudyExportSession.SessionId||bundle.records==null||bundle.records.Count>Limit)throw new InvalidDataException("Observation session/file mismatch.");
        }
        private static void RequireRecording(){if(!StudyEvents.Recording)throw new InvalidOperationException("Start a recording session first.");}
        public static Record Begin(string observer)
        {
            RequireRecording();Ensure();if(Active!=null)throw new InvalidOperationException("End the current observation first.");
            if(bundle.records.Count>=Limit)throw new InvalidOperationException("Maximum 10 observation intervals per session.");
            if(string.IsNullOrWhiteSpace(observer)||observer.Length>120)throw new ArgumentException("Enter an anonymous observer ID (up to 120 characters).");
            var r=new Record{observation_id="obs-"+Guid.NewGuid().ToString("N"),observer_id=observer.Trim(),start_s=StudyExportSession.Seconds,created_utc=DateTime.UtcNow.ToString("O"),updated_utc=DateTime.UtcNow.ToString("O")};
            bundle.records.Add(r);try{Save();}catch{bundle.records.Remove(r);throw;}
            StudyEvents.Record("observation_interval_start",r.observation_id,"observer_marked","observer="+r.observer_id);return r;
        }
        public static Record End()
        {
            RequireRecording();var r=Active;if(r==null)throw new InvalidOperationException("No active observation.");
            r.end_s=StudyExportSession.Seconds;r.status="completed";r.end_reason="operator_end_button";r.updated_utc=DateTime.UtcNow.ToString("O");Save();
            StudyEvents.Record("observation_interval_end",r.observation_id,"observer_marked","start_s="+r.start_s+"; end_s="+r.end_s);return r;
        }
        public static void SaveNote(string id,string label,string notes)
        {
            RequireRecording();Ensure();if((label??"").Length>120||(notes??"").Length>10000)throw new ArgumentException("Observation text is too long.");
            var r=bundle.records.Single(x=>x.observation_id==id);if(r.end_s==null)throw new InvalidOperationException("End the interval before adding the note.");
            r.label=string.IsNullOrWhiteSpace(label)?"Possible confusion":label.Trim();r.notes=notes??"";r.updated_utc=DateTime.UtcNow.ToString("O");Save();
            StudyEvents.Record("observation_note_saved",id,"researcher_observation","Text stored separately in live_observations.json.");
        }
        public static void Finish()
        {
            Ensure();var r=Active;if(r==null)return;
            r.end_s=StudyExportSession.Seconds;r.status="interrupted";r.end_reason="session_ended_before_second_click";r.updated_utc=DateTime.UtcNow.ToString("O");Save();
            StudyEvents.Record("observation_interval_interrupted",r.observation_id,"session_end","Not a normal operator-ended interval.");
        }
        private static string Safe(string text)=>string.IsNullOrEmpty(text)?text:"=+-@".IndexOf(text.TrimStart().FirstOrDefault())>=0?"'"+text:text;
        private static void Save()
        {
            string root=StudyExportSession.Folder;if(root==null)throw new InvalidOperationException("No session folder.");
            StudyAtomicFile.Write(Path.Combine(root,"live_observations.json"),JsonConvert.SerializeObject(bundle,Formatting.Indented));
            var lines=new List<string>{"session_id,observation_id,start_s,end_s,status,observer_id,label,notes,source,end_reason,created_utc,updated_utc"};
            lines.AddRange(bundle.records.Select(r=>NeonStudyRecorder.Csv(bundle.session_id,r.observation_id,r.start_s,r.end_s.HasValue?(object)r.end_s.Value:"",r.status,Safe(r.observer_id),Safe(r.label),Safe(r.notes),r.source,r.end_reason,r.created_utc,r.updated_utc)));
            StudyAtomicFile.Write(Path.Combine(root,"live_observations.csv"),string.Join("\n",lines)+"\n");
        }
    }
}
