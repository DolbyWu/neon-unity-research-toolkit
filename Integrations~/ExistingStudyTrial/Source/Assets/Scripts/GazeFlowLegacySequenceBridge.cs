using System;
using System.Collections.Generic;
using UnityEngine;
using PromptAgentSystem;
using CoreSequence = NeonResearch.StudySequence;

/// <summary>Legacy API forwarding to ONE core timeline. Retains host region semantics and actual grab callbacks.</summary>
public static class StudySequence
{
    public static string Region => CoreSequence.Region;
    public static void Begin(string folder) => CoreSequence.Begin(folder);
    public static void Flush() => CoreSequence.Flush();
    public static void Close() => CoreSequence.Close();
    public static void SetRegion(string value) => CoreSequence.SetRegion(value);
    public static void Record(string kind,string puzzle,string objectId,string status,string outcome,string detail)
        => CoreSequence.Record(kind,puzzle,objectId,status,outcome,detail);
    public static void Grab(string hand, Component target)
    {
        var research=ResolveResearchTarget(target);
        // Use an authored ID only when configured and unique; no display-name identity guesses.
        CoreSequence.Grab(hand, target, research != null ? research.targetId : null);
    }
    public static NeonResearch.StudyResearchTarget ResolveResearchTarget(Component target)
    {
        if(target!=null)foreach(var candidate in target.GetComponentsInParent<NeonResearch.StudyResearchTarget>(true))
            if(candidate.isActiveAndEnabled&&candidate.HasUniqueId())return candidate;
        return null;
    }
    public static bool Contains(Collider collider, Vector3 point) => CoreSequence.Contains(collider, point);
    public static string Locate(Vector3 point)
    {
        var matches = new SortedSet<string>(StringComparer.Ordinal); bool configured=false;
        foreach (var agent in UnityEngine.Object.FindObjectsByType<EscapeRoomPromptAgent>(FindObjectsSortMode.None))
        {
            if(agent.StudyZones==null)continue;
            foreach(var zone in agent.StudyZones)
            { if(zone.triggerCollider==null||zone.puzzle==MainPuzzle.None)continue; configured=true; if(Contains(zone.triggerCollider,point))matches.Add(zone.puzzle.ToString()); }
        }
        foreach(var entrance in UnityEngine.Object.FindObjectsByType<EntranceController>(FindObjectsSortMode.None))
        {if(entrance.StudyEntrance==null)continue;configured=true;if(Contains(entrance.StudyEntrance,point))matches.Add("Pre-room");}
        if(!configured)return "Unknown_no_configured_zones";
        if(matches.Count==0)return "In-between";
        if(matches.Count==1){foreach(string match in matches)return match;}
        return "Ambiguous:"+string.Join("|",matches);
    }
}
