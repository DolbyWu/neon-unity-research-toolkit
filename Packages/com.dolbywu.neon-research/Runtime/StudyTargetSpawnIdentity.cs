using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Explicit deterministic identity for spawned instances; callers own stable spawn-site and instance keys.</summary>
    public static class StudyTargetSpawnIdentity
    {
        public static void Register(GameObject instance, string authoredSpawnSiteId, string instanceKey)
        {
            if(instance==null || string.IsNullOrWhiteSpace(authoredSpawnSiteId) || string.IsNullOrWhiteSpace(instanceKey))
                throw new ArgumentException("Spawned targets require an instance, authored spawn-site ID and nonempty instance key.");
            foreach(var target in instance.GetComponentsInChildren<StudyResearchTarget>(true))
            {
                if(!target.requiresSpawnIdentity) continue;
                string templateId=target.targetId;
                using(var sha=SHA256.Create())
                    target.targetId="spawn-"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(authoredSpawnSiteId+"\n"+instanceKey+"\n"+templateId))).Replace("-","").Substring(0,24).ToLowerInvariant();
                target.requiresSpawnIdentity=false;
                StudyEvents.Record("research_target_spawn",target.targetId,"registered", "spawn_site="+authoredSpawnSiteId+"; instance_key="+instanceKey+"; template="+templateId);
            }
        }
    }
}
