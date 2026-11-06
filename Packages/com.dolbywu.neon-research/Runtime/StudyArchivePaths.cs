// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

/// <summary>Participant grouping is opt-in; rootOverride in tests retains legacy flat layout.</summary>
public static class StudyArchivePaths
{
    public static string ParticipantFolder(string root, string participantId)
    {
        if (string.IsNullOrWhiteSpace(participantId) || !Regex.IsMatch(participantId, @"\A[A-Za-z0-9][A-Za-z0-9_-]{0,47}\z"))
            throw new InvalidDataException("Use an anonymous ID: 1-48 letters, digits, underscores or hyphens; start with a letter or digit.");
        string fullRoot = Path.GetFullPath(root);
        string participant = Path.GetFullPath(Path.Combine(fullRoot, "Participants", participantId));
        RequireContainedUnlinked(fullRoot, participant);
        return participant;
    }

    public static string SessionFolder(string root, string participantId, string sessionId)
    {
        if (!Regex.IsMatch(sessionId ?? "", @"\A\d{8}T\d{9}Z-[a-f0-9]{8}\z"))
            throw new InvalidDataException("Invalid study session ID.");
        string fullRoot = Path.GetFullPath(root);
        string participant = ParticipantFolder(root, participantId);
        if (!File.Exists(Path.Combine(participant, "participant.json")))
            throw new InvalidDataException("Create/select this participant in the study archive before Play.");
        if ((string)JObject.Parse(File.ReadAllText(Path.Combine(participant, "participant.json")))["participantId"] != participantId)
            throw new InvalidDataException("Participant profile ID does not match its folder.");
        string folder = Path.GetFullPath(Path.Combine(participant, "Sessions", sessionId));
        RequireContainedUnlinked(fullRoot, folder);
        return folder;
    }

    public static void RequireContainedUnlinked(string root, string folder)
    {
        string fullRoot = Path.GetFullPath(root);
        folder = Path.GetFullPath(folder);
        if (!folder.StartsWith(fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Archive path escapes configured root.");
        for (string path = folder; path != null; path = Path.GetDirectoryName(path))
        {
            if ((Directory.Exists(path) || File.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked archive directories are not supported.");
            if (string.Equals(path, fullRoot, StringComparison.OrdinalIgnoreCase)) break;
        }
    }
}

public static class StudyArchiveNotifications
{
    public static event Action<string> SessionReady;
    public static void Publish(string folder) => SessionReady?.Invoke(folder);
}

}
