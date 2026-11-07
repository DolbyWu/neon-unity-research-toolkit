// Neon Research Toolkit: project-local research recording and review.
namespace NeonResearch
{
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Main-thread, bounded display cache. Never replaces raw logging.</summary>
public static class StudyMonitorData
{
    public struct Point { public double time; public float x,y,left,right; public bool valid,synced; public int epoch; }
    public struct GameEvent { public double time; public string description; }
    private static readonly Queue<Point> points = new Queue<Point>();
    private static readonly Queue<GameEvent> events = new Queue<GameEvent>();
    public static string LastInteraction { get; private set; } = "No recorded interaction";
    public static string Error = "";
    public static bool Stopped { get; private set; }
    public static double EndSeconds { get; private set; }
    public static double LastSampleTime { get; private set; } = -1;
    public static long Samples { get; private set; }
    public static Point[] Points => points.ToArray();
    public static GameEvent[] Events => events.ToArray();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        points.Clear(); events.Clear(); Samples=0; LastSampleTime=-1; Error="";
        LastInteraction="No recorded interaction"; Stopped=false; EndSeconds=0;
    }
    public static float Pupil(float value, bool available) => available && value > 0 && !float.IsNaN(value) && !float.IsInfinity(value) ? value : float.NaN;
    public static string FormatPupil(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value)
        ? value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "No data";
    public static void Sample(double receivedTime,float x,float y,float left,float right,bool eyes,bool valid,bool synced,int epoch)
    {
        LastSampleTime=receivedTime; Samples++;
        points.Enqueue(new Point {time=receivedTime,x=valid?x:float.NaN,y=valid?y:float.NaN,
            left=Pupil(left,eyes&&valid),right=Pupil(right,eyes&&valid),valid=valid,synced=synced,epoch=epoch});
        while(points.Count>16000 || (points.Count>0 && receivedTime-points.Peek().time>60))points.Dequeue();
    }
    public static void Event(double time,string kind,string puzzle,string objectId,string outcome)
    {
        string detail=kind+" | "+puzzle+" | "+objectId+" | "+outcome;
        events.Enqueue(new GameEvent {time=time,description=detail});
        while(events.Count>80)events.Dequeue();
        if(kind!="region_change" && kind!="observer_marker")LastInteraction=detail;
    }
    public static void Stop(double seconds) { Stopped=true; EndSeconds=seconds; }
}

}
