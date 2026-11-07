// Neon Research Toolkit: project-local research recording and review.
namespace NeonResearch
{
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only observer window: never drawn into the headset.</summary>
public sealed class StudyLiveMonitor : EditorWindow
{
    private StudyMonitorData.Point[] points=Array.Empty<StudyMonitorData.Point>();
    private StudyMonitorData.GameEvent[] events=Array.Empty<StudyMonitorData.GameEvent>();
    private Vector2 scroll;
    private double nextRefresh;
    private float seconds=30;
    private Task<string> zipTask;
    private string zipStatus="";
    [MenuItem("Tools/Neon Research/Open Live Monitor")]
    public static void Open() { var window=GetWindow<StudyLiveMonitor>("Neon Research Monitor"); window.minSize=new Vector2(620,650); }
    private void OnEnable() { EditorApplication.update+=Tick; }
    private void OnDisable() { EditorApplication.update-=Tick; }
    private void Tick()
    {
        if(EditorApplication.timeSinceStartup<nextRefresh)return;
        nextRefresh=EditorApplication.timeSinceStartup+0.1;
        points=StudyMonitorData.Points; events=StudyMonitorData.Events;
        if(zipTask!=null && zipTask.IsCompleted)
        {
            zipStatus=zipTask.IsFaulted?"ZIP failed: "+zipTask.Exception.GetBaseException().Message:"Saved ZIP: "+zipTask.Result;
            zipTask=null;
        }
        Repaint();
    }
    private void OnGUI()
    {
        scroll=EditorGUILayout.BeginScrollView(scroll);
        GUILayout.Label("NEON RESEARCH TOOLKIT  /  LIVE MONITOR",EditorStyles.boldLabel);
        if (GUILayout.Button("Settings / enable recording for this project")) NeonResearchSetup.Open();
        bool running=EditorApplication.isPlaying;
        double now=StudyMonitorData.Stopped?StudyMonitorData.EndSeconds:StudyExportSession.Seconds;
        double age=StudyMonitorData.LastSampleTime<0?double.PositiveInfinity:now-StudyMonitorData.LastSampleTime;
        var config=NeonStudySettings.Load();
        string state=!running?(StudyMonitorData.Stopped?"STOPPED — last session":"READY — enable recording in Settings before Play"):
            !StudyEvents.Recording?"RECORDING DISABLED / NO ACTIVE SESSION":!config.enabled?"NEON DISABLED — other channels may record":age>2?"NO RECENT EYE DATA":"RECEIVING EYE DATA";
        EditorGUILayout.HelpBox(state+"  |  Session "+TimeSpan.FromSeconds(Math.Max(0,now)).ToString(@"hh\:mm\:ss"),running&&age>2?MessageType.Warning:MessageType.Info);
        GUILayout.Label("Region: "+StudySequence.Region,EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Last recorded interaction (not continuous activity)",StudyMonitorData.LastInteraction,EditorStyles.wordWrappedLabel);
        GUILayout.Label("Receiver: "+NeonStudyRecorder.MonitorStatus);
        GUILayout.Label("Samples written: "+StudyMonitorData.Samples+"  |  Queue drops: "+NeonStudyRecorder.MonitorDropped);
        int recent=0,valid=0,synced=0;
        foreach(var p in points)if(p.time>=now-5 && p.time<=now) {recent++;if(p.valid)valid++;if(p.synced)synced++;}
        GUILayout.Label("Last 5s: "+recent+" samples  |  basic valid "+Percent(valid,recent)+"  |  clock aligned "+Percent(synced,recent));
        if(points.Length>0) {
            var p=points[points.Length-1];
            GUILayout.Label("Pupil L / R: "+Value(p.left)+" / "+Value(p.right)+"  (mm: documented wire units; not physically validated)");
        }
        if(!string.IsNullOrEmpty(StudyMonitorData.Error))EditorGUILayout.HelpBox(StudyMonitorData.Error,MessageType.Error);
        EditorGUILayout.HelpBox("Display uses PC receive-time (not corrected sample time). Event markers are approximate visual context only. Saved CSV retains corrected time and sync quality. Automatic fixation / saccade detection and cognitive-state inference are not implemented. Offline manual labels are supported.",MessageType.None);
        seconds=EditorGUILayout.Slider("Visible seconds",seconds,10,60);
        Plot("Gaze X / Y — scene-image pixels, not a VR target",now,false);
        Plot("Pupil L / R — mm (wire units; not physically validated)",now,true);
        GUILayout.Label("Recent game events",EditorStyles.boldLabel);
        for(int i=events.Length-1;i>=Math.Max(0,events.Length-8);i--)
            EditorGUILayout.LabelField(events[i].time.ToString("F2")+"s",events[i].description,EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);
        string folder=StudyExportSession.Folder;
        EditorGUILayout.LabelField("Data package folder",folder??"Created automatically when Play begins",EditorStyles.wordWrappedLabel);
        EditorGUILayout.BeginHorizontal();
        using(new EditorGUI.DisabledScope(!running || folder==null))
            if(GUILayout.Button("Mark observation")) { StudyExportSession.Event("observer_marker","Monitor button");StudyExportSession.Flush(); }
        using(new EditorGUI.DisabledScope(folder==null || !Directory.Exists(folder)))
            if(GUILayout.Button("Open data folder"))EditorUtility.RevealInFinder(folder);
        EditorGUILayout.EndHorizontal();
        using(new EditorGUI.DisabledScope(running || folder==null))
            if(GUILayout.Button("Open offline replay (finished session)"))
            {
                try { Application.OpenURL(new Uri(StudyOfflineReplay.Build(folder)).AbsoluteUri); }
                catch(Exception e) { EditorUtility.DisplayDialog("Offline replay",e.Message,"OK"); }
            }
        using(new EditorGUI.DisabledScope(running || zipTask!=null))
            if(GUILayout.Button(zipTask==null?"Package a finished session as ZIP":"Packaging…"))
            {
                string selected=EditorUtility.OpenFolderPanel("Select one completed session folder",folder??StudyExportSession.OutputRoot,"");
                if(!string.IsNullOrEmpty(selected)) { zipStatus="Packaging local data; no upload.";zipTask=Task.Run(()=>ZipSession(selected)); }
            }
        if(zipStatus.Length>0)EditorGUILayout.HelpBox(zipStatus,MessageType.Info);
        EditorGUILayout.HelpBox("Recording and saving run during an armed, approved Play session. Stop normally, then copy the session folder or create a ZIP. ZIP includes research data and device settings: store privately. This monitor is for the Unity Editor; standalone player builds are unverified.",MessageType.None);
        EditorGUILayout.EndScrollView();
    }
    private static string Percent(int n,int total)=>total==0?"N/A":(100.0*n/total).ToString("F1")+"%";
    private static string Value(float n)=>StudyMonitorData.FormatPupil(n);
    private void Plot(string title,double now,bool pupil)
    {
        GUILayout.Label(title,EditorStyles.boldLabel);
        Rect rect=GUILayoutUtility.GetRect(100,135,GUILayout.ExpandWidth(true));
        if(Event.current.type!=EventType.Repaint)return;
        EditorGUI.DrawRect(rect,new Color(0.075f,0.09f,0.12f));
        float min=float.PositiveInfinity,max=float.NegativeInfinity;
        foreach(var p in points)if(p.time>=now-seconds&&p.time<=now) {
            float a=pupil?p.left:p.x,b=pupil?p.right:p.y;
            if(!float.IsNaN(a)){min=Mathf.Min(min,a);max=Mathf.Max(max,a);}
            if(!float.IsNaN(b)){min=Mathf.Min(min,b);max=Mathf.Max(max,b);}
        }
        if(float.IsInfinity(min)){GUI.Label(rect,"  No valid data in this time window");return;}
        if(max-min<(pupil?0.0000001f:0.001f)) {
            float padding=pupil?Mathf.Max(Mathf.Abs(max)*0.01f,0.000001f):0.5f;
            min=pupil?Mathf.Max(0,min-padding):min-padding;max+=padding;
        }
        float gutter=pupil?84:52;
        Rect graph=new Rect(rect.x+gutter,rect.y+16,Mathf.Max(10,rect.width-gutter-10),rect.height-36);
        string axisFormat=pupil?"F6":"F2";
        GUI.Label(new Rect(rect.x+3,rect.y,gutter-6,20),max.ToString(axisFormat,System.Globalization.CultureInfo.InvariantCulture));
        GUI.Label(new Rect(rect.x+3,rect.yMax-23,gutter-6,20),min.ToString(axisFormat,System.Globalization.CultureInfo.InvariantCulture));
        Handles.BeginGUI();
        foreach(var e in events)if(e.time>=now-seconds&&e.time<=now){float x=graph.x+(float)((e.time-now+seconds)/seconds)*graph.width;Handles.color=new Color(1,1,1,0.15f);Handles.DrawLine(new Vector3(x,graph.y),new Vector3(x,graph.yMax));}
        for(int channel=0;channel<2;channel++) {
            Handles.color=channel==0?Color.cyan:new Color(1,0.65f,0.2f);
            bool has=false;Vector3 last=default;double lastTime=0;int lastEpoch=0;
            foreach(var p in points) {
                if(p.time<now-seconds||p.time>now)continue;
                float v=pupil?(channel==0?p.left:p.right):(channel==0?p.x:p.y);
                if(float.IsNaN(v)){has=false;continue;}
                var current=new Vector3(graph.x+(float)((p.time-now+seconds)/seconds)*graph.width,graph.yMax-(v-min)/(max-min)*graph.height);
                // Display-only pixel decimation; CSV and quality counters keep every sample.
                if(has && current.x-last.x<1 && p.time-lastTime<=0.1 && p.epoch==lastEpoch)continue;
                if(has&&p.time-lastTime<=0.1&&p.epoch==lastEpoch)Handles.DrawLine(last,current);
                last=current;lastTime=p.time;lastEpoch=p.epoch;has=true;
            }
        }
        Handles.EndGUI();
        GUI.Label(new Rect(graph.x,rect.yMax-20,graph.width,20),"-"+seconds.ToString("F0")+"s                     cyan "+(pupil?"L":"X")+" / orange "+(pupil?"R":"Y")+"   |   auto scale");
    }
    public static string ZipSession(string folder)
    {
        if(!(bool)JObject.Parse(File.ReadAllText(Path.Combine(folder,"export_manifest.json")))["closed"])throw new InvalidOperationException("Session is not closed.");
        foreach(string neon in Directory.GetDirectories(folder,"Neon-*"))
            if(!(bool)JObject.Parse(File.ReadAllText(Path.Combine(neon,"session.json")))["closed"])throw new InvalidOperationException("Neon recording is not closed.");
        foreach(string path in Directory.EnumerateFileSystemEntries(folder,"*",SearchOption.AllDirectories))
            if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidOperationException("Linked files/folders are not allowed in the data package.");
        string zip=folder.TrimEnd(Path.DirectorySeparatorChar)+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".zip";
        string temp=zip+".partial";
        ZipFile.CreateFromDirectory(folder,temp,System.IO.Compression.CompressionLevel.Fastest,true);
        File.Move(temp,zip);return zip;
    }
}

}
