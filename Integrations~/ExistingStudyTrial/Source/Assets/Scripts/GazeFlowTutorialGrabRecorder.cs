#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NeonResearch;
using Oculus.Interaction;
using UnityEngine;
/// <summary>Actual tutorial pointer callbacks only. Mapping/eye quality never gates behavioral events.</summary>
public sealed class GazeFlowTutorialGrabRecorder:MonoBehaviour
{
    private readonly Dictionary<Grabbable,Action<PointerEvent>> handlers=new Dictionary<Grabbable,Action<PointerEvent>>();
    private readonly HashSet<string> selections=new HashSet<string>();
    private double nextScan;
    private void Update()
    {
        if(Time.realtimeSinceStartupAsDouble<nextScan)return;nextScan=Time.realtimeSinceStartupAsDouble+.5;
        foreach(var g in FindObjectsByType<Grabbable>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(g.gameObject.scene!=gameObject.scene||handlers.ContainsKey(g))continue;
            var captured=g;Action<PointerEvent> handler=e=>RecordPointer(captured,e);g.WhenPointerEventRaised+=handler;handlers.Add(g,handler);
        }
    }
    public void RecordPointer(Grabbable target,PointerEvent evt)
    {
        if(target==null||!StudyEvents.Recording)return;
        string key=target.GetInstanceID()+":"+evt.Identifier;
        string kind;
        if(evt.Type==PointerEventType.Select){if(!selections.Add(key))return;kind="grab_start";}
        else if(evt.Type==PointerEventType.Unselect||evt.Type==PointerEventType.Cancel){if(!selections.Remove(key))return;kind="grab_end";}
        else return;
        if(evt.Type==PointerEventType.Select)
            foreach(var gate in FindObjectsByType<HandTutorialNextGate>(FindObjectsSortMode.None))
                if(gate.gameObject.scene==target.gameObject.scene)gate.ObserveActualGrab(target,evt);
        var research=StudySequence.ResolveResearchTarget(target);
        string id=research!=null?research.targetId:"unresolved-runtime-instance:"+target.GetInstanceID();
        StudyEvents.Record(kind,id,"actual_meta_pointer_callback","object_name="+target.name+"; group="+(research==null?"unresolved":research.targetGroup)+"; pointer_id="+evt.Identifier+"; hand=unresolved; source=tutorial_grab_bridge; identity="+(research==null?"unresolved_not_stable":"configured_unique_target"));
    }
    private void OnDisable()
    {
        foreach(var item in handlers)if(item.Key!=null)item.Key.WhenPointerEventRaised-=item.Value;
        handlers.Clear();selections.Clear();
    }
}
#endif
