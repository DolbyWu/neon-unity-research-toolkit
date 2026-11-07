// Neon Research Toolkit: project-local research recording and review.
namespace NeonResearch
{
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>Local, anonymous participant profiles and immutable per-session intake snapshots.</summary>
public static class StudyParticipantProfiles
{
    public static string Fingerprint(string text)
    {
        using (var hash = SHA256.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
    }

    public static JObject Read(string root, string id)
    {
        string file = Path.Combine(StudyArchivePaths.ParticipantFolder(root, id), "participant.json");
        StudyArchivePaths.RequireContainedUnlinked(root, file);
        if (!File.Exists(file)) throw new InvalidDataException("Participant does not exist. Choose New participant first.");
        var profile = JObject.Parse(File.ReadAllText(file));
        if ((string)profile["participantId"] != id) throw new InvalidDataException("Participant ID does not match its folder.");
        return profile;
    }

    public static string SuggestId(string root)
    {
        // Never silently reuse the last configured ID, including the old P001 pilot.
        for (int n = 1; n < 1000000; n++)
        {
            string id = "P" + n.ToString("D3");
            string folder = StudyArchivePaths.ParticipantFolder(root, id);
            if (!Directory.Exists(folder) && !File.Exists(folder)) return id;
        }
        throw new InvalidDataException("Enter an unused anonymous participant ID manually.");
    }

    public static void RequireNoOpenSessions(string root)
    {
        string participants = Path.Combine(Path.GetFullPath(root), "Participants");
        StudyArchivePaths.RequireContainedUnlinked(root, participants);
        if (!Directory.Exists(participants)) return;
        foreach (string participant in Directory.GetDirectories(participants))
        {
            StudyArchivePaths.RequireContainedUnlinked(root, participant);
            string sessions = Path.Combine(participant, "Sessions");
            StudyArchivePaths.RequireContainedUnlinked(root, sessions);
            if (!Directory.Exists(sessions)) continue;
            foreach (string folder in Directory.GetDirectories(sessions))
            {
                string manifest = Path.Combine(folder, "export_manifest.json");
                StudyArchivePaths.RequireContainedUnlinked(root, manifest);
                if (File.Exists(manifest) && JObject.Parse(File.ReadAllText(manifest))["closed"]?.Value<bool>() != true)
                    throw new InvalidDataException("A previous session is still marked open. Stop/finalize it before starting another; do not edit its closed flag.");
            }
        }
    }

    public static JObject Save(string root, string id, bool returning, string expectedProfileHash,
        string background, JObject details)
    {
        string folder = StudyArchivePaths.ParticipantFolder(root, id);
        string file = Path.Combine(folder, "participant.json");
        StudyArchivePaths.RequireContainedUnlinked(root, file);
        RequireNoOpenSessions(root);
        JObject profile;
        if (File.Exists(file))
        {
            if (!returning) throw new InvalidDataException("This ID already exists. Select Returning participant explicitly, or use a new ID.");
            string text = File.ReadAllText(file);
            if (string.IsNullOrEmpty(expectedProfileHash) || Fingerprint(text) != expectedProfileHash)
                throw new InvalidDataException("Participant details changed elsewhere. Select the participant again before saving.");
            profile = Read(root, id); // Preserve original creation time and unknown fields.
        }
        else
        {
            if (returning) throw new InvalidDataException("Select an existing participant, or choose New participant.");
            if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
                throw new InvalidDataException("This folder already contains files without a profile. Use a different ID; no existing data was changed.");
            profile = new JObject { ["participantId"] = id, ["createdUtc"] = DateTimeOffset.UtcNow.ToString("O") };
        }
        if ((background ?? "").Length > 50000) throw new InvalidDataException("Background notes are too long.");
        details = details ?? new JObject();
        foreach (var property in details.Properties())
            if (property.Value.Type != JTokenType.String || ((string)property.Value ?? "").Length > 2000)
                throw new InvalidDataException("Participant detail fields must be text, at most 2000 characters each.");
        profile["updatedUtc"] = DateTimeOffset.UtcNow.ToString("O");
        profile["background"] = background ?? "";
        var merged = profile["details"] as JObject ?? new JObject();
        foreach (var property in details.Properties()) merged[property.Name] = property.Value.DeepClone();
        profile["details"] = merged;
        profile["privacyNote"] = "Anonymous ID. Optional, voluntary self-report only. Keep names, contacts and consent identity mappings separately.";
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "Sessions"));
        StudyArchivePaths.RequireContainedUnlinked(root, Path.Combine(folder, "participant.txt"));
        StudyAtomicFile.Write(file, profile.ToString(Formatting.Indented));
        StudyAtomicFile.Write(Path.Combine(folder, "participant.txt"), Summary(profile));
        return profile;
    }

    public static void SelectInConfig(string configPath, string root, string id)
    {
        Read(root, id);
        RequireNoOpenSessions(root);
        var config = JObject.Parse(File.ReadAllText(configPath));
        string configuredRoot = StudyExportSession.ResolveOutputRoot(Directory.GetParent(UnityEngine.Application.dataPath).FullName,
            (string)config["outputRoot"]);
        if (!string.Equals(Path.GetFullPath(root), configuredRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Archive root changed. Reopen the participant form.");
        config["participantId"] = id; // Never rewrite microphone, network or credential fields from a form.
        StudyAtomicFile.Write(configPath, config.ToString(Formatting.Indented));
    }

    public static void WriteSessionSnapshot(string root, string id, string sessionId, string sessionFolder, JObject intake)
    {
        string expected = StudyArchivePaths.SessionFolder(root, id, sessionId);
        if (!string.Equals(Path.GetFullPath(sessionFolder), expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Participant snapshot destination does not match this session.");
        var profile = Read(root, id);
        if (intake != null && ((string)intake["participantId"] != id ||
            !string.Equals(Path.GetFullPath((string)intake["archiveRoot"]), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) ||
            (string)intake["profileHash"] != Fingerprint(File.ReadAllText(Path.Combine(StudyArchivePaths.ParticipantFolder(root, id), "participant.json")))))
            throw new InvalidDataException("Confirmed participant details changed before recording. Stop and confirm them again.");
        Directory.CreateDirectory(sessionFolder);
        var snapshot = new JObject { ["sessionId"] = sessionId, ["participantId"] = id,
            ["savedUtc"] = DateTimeOffset.UtcNow.ToString("O"), ["profile"] = profile,
            ["intake"] = intake?.DeepClone(), ["source"] = intake == null ? "configured_profile_no_editor_intake" : "operator_confirmed_before_Play" };
        // CreateNew refuses accidental replacement of any existing session evidence.
        using (var stream = new FileStream(Path.Combine(sessionFolder, "participant_snapshot.json"), FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(snapshot.ToString(Formatting.Indented));
        using (var stream = new FileStream(Path.Combine(sessionFolder, "participant_summary.txt"), FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            writer.Write("Session: " + sessionId + "\n" + Summary(profile) + "\nResearcher ID: " + (string)(intake?["researcherId"]) +
                "\nSession notes: " + (string)(intake?["sessionNotes"]) + "\n");
    }

    private static string Summary(JObject profile)
    {
        var text = new StringBuilder("PARTICIPANT DETAILS — LOCAL RESEARCH RECORD\n");
        text.AppendLine("Participant ID: " + (string)profile["participantId"]);
        text.AppendLine("Created: " + (string)profile["createdUtc"]);
        text.AppendLine("Updated: " + (string)profile["updatedUtc"]);
        foreach (var item in (profile["details"] as JObject ?? new JObject()).Properties())
            text.AppendLine(item.Name + ": " + (string)item.Value);
        text.AppendLine("Background / field notes:\n" + (string)profile["background"]);
        text.AppendLine("Privacy: optional self-report only; keep names and contact mappings separately.");
        return text.ToString();
    }
}

/// <summary>SessionState survives Editor domain reload; no data is sent to a server.</summary>
public static class StudyParticipantIntake
{
    private const string Key = "NeonResearch.Participant.Intake";
    public static void Set(string json)
    {
#if UNITY_EDITOR
        UnityEditor.SessionState.SetString(Key, json ?? "");
#endif
    }
    public static JObject Consume()
    {
#if UNITY_EDITOR
        string json = UnityEditor.SessionState.GetString(Key, "");
        UnityEditor.SessionState.EraseString(Key);
        return string.IsNullOrEmpty(json) ? null : JObject.Parse(json);
#else
        return null;
#endif
    }
}

}
