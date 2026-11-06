// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>One shared gate, independent of archive/media event registration order.</summary>
[InitializeOnLoad]
public static class StudyParticipantStartFlow
{
    private const string IdKey = "NeonResearch.Participant.ApprovedId";
    private const string RootKey = "NeonResearch.Participant.ApprovedRoot";
    private static bool queued;
    static StudyParticipantStartFlow()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            { SessionState.EraseString(IdKey); SessionState.EraseString(RootKey); }
            if (state == PlayModeStateChange.EnteredEditMode) Clear();
        };
    }

    public static bool ApprovedFor(string root, string id) =>
        SessionState.GetString(IdKey, "") == id &&
        string.Equals(SessionState.GetString(RootKey, ""), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase);

    public static void Approve(string root, string id, string intake)
    {
        SessionState.SetString(IdKey, id);
        SessionState.SetString(RootKey, Path.GetFullPath(root));
        StudyParticipantIntake.Set(intake);
    }

    public static void Clear()
    {
        SessionState.EraseString(IdKey); SessionState.EraseString(RootKey);
        StudyParticipantIntake.Set("");
    }

    public static bool AllowOrRequest()
    {
        if (Application.isBatchMode) return true;
        var settings = NeonStudySettings.Load();
        if (!settings.recordOnPlay || !settings.archiveEnabled) return true;
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return false;
        string root = StudyExportSession.OutputRoot;
        if (ApprovedFor(root, settings.participantId)) return true;
        EditorApplication.isPlaying = false;
        Clear();
        if (!queued)
        {
            queued = true;
            EditorApplication.delayCall += OpenWhenIdle;
        }
        return false;
    }

    private static void OpenWhenIdle()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += OpenWhenIdle; return; }
        queued = false;
        StudyParticipantStartWindow.Open();
    }
}

public sealed class StudyParticipantStartWindow : EditorWindow
{
    [SerializeField] private string root, id, profileHash = "", background = "", ageRange = "", gender = "", handedness = "",
        vrExperience = "", researcher = "", sessionNotes = "", error = "";
    [SerializeField] private bool returning, submitted;
    [SerializeField] private string[] existing = Array.Empty<string>();
    [SerializeField] private int existingIndex;
    [SerializeField] private Vector2 scroll;

    [MenuItem("Tools/Neon Research/Participant Details Before Play")]
    public static void Open()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var window = GetWindow<StudyParticipantStartWindow>(true, "Neon Research — participant details", true);
        window.minSize = new Vector2(620, 610);
        window.root = StudyExportSession.OutputRoot;
        window.existing = Directory.Exists(Path.Combine(window.root, "Participants"))
            ? Directory.GetDirectories(Path.Combine(window.root, "Participants")).Select(Path.GetFileName)
                .Where(p => File.Exists(Path.Combine(window.root, "Participants", p, "participant.json"))).OrderBy(p => p).ToArray()
            : Array.Empty<string>();
        window.returning = false; window.existingIndex = 0; window.submitted = false;
        window.id = StudyParticipantProfiles.SuggestId(window.root);
        window.ShowUtility(); window.Focus();
    }

    private void OnDestroy() { if (!submitted) StudyParticipantStartFlow.Clear(); }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(root)) { EditorGUILayout.HelpBox("Reopen this form from Tools > Study > Participant Details Before Play.", MessageType.Info); return; }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Participant details", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play is paused before entry. No game data, microphone or screen recording has started. Use an anonymous ID; keep names and contact details separately.", MessageType.Info);
        EditorGUILayout.LabelField("Archive root", root, EditorStyles.wordWrappedLabel);
        bool newReturning = GUILayout.Toolbar(returning ? 1 : 0, new[] { "New participant", "Returning participant" }) == 1;
        if (newReturning != returning)
        {
            returning = newReturning; existingIndex = 0; profileHash = "";
            id = returning ? "" : StudyParticipantProfiles.SuggestId(root);
            background = ageRange = gender = handedness = vrExperience = ""; error = "";
        }
        if (returning)
        {
            string[] options = new[] { "Select participant..." }.Concat(existing).ToArray();
            int selected = EditorGUILayout.Popup("Existing participant", existingIndex, options);
            if (selected != existingIndex)
            {
                existingIndex = selected;
                if (selected > 0) LoadExisting(existing[selected - 1]);
                else { id = ""; profileHash = ""; }
            }
        }
        using (new EditorGUI.DisabledScope(returning)) id = EditorGUILayout.TextField("Anonymous participant ID *", id ?? "");
        EditorGUILayout.LabelField("Optional, voluntary self-report", EditorStyles.boldLabel);
        ageRange = EditorGUILayout.TextField("Age / age range", ageRange);
        gender = EditorGUILayout.TextField("Gender / self-description", gender);
        handedness = EditorGUILayout.TextField("Handedness", handedness);
        vrExperience = EditorGUILayout.TextField("Previous VR experience", vrExperience);
        EditorGUILayout.LabelField("Background / field notes");
        background = EditorGUILayout.TextArea(background, GUILayout.MinHeight(65));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("This session only", EditorStyles.boldLabel);
        researcher = EditorGUILayout.TextField("Researcher ID (optional)", researcher);
        EditorGUILayout.LabelField("Session / setup notes (optional)");
        sessionNotes = EditorGUILayout.TextArea(sessionNotes, GUILayout.MinHeight(55));
        // Display only. Do not pass placeholders or partially typed IDs to Windows path APIs.
        EditorGUILayout.LabelField("Save location", root.TrimEnd('\\', '/') + "\\Participants\\" +
            (id ?? "[ID]") + "\\Sessions\\[new session]", EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("Saving confirms the selected ID for this Play only. The recording-consent dialog follows. Existing sessions are never renamed or overwritten. Cancel does not start Play or recording.", MessageType.None);
        if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel", GUILayout.Height(32))) { StudyParticipantStartFlow.Clear(); Close(); }
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(id) || (returning && existingIndex == 0)))
            if (GUILayout.Button("Save participant and continue", GUILayout.Height(32))) Submit();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void LoadExisting(string participantId)
    {
        try
        {
            var profile = StudyParticipantProfiles.Read(root, participantId);
            id = participantId;
            string file = Path.Combine(StudyArchivePaths.ParticipantFolder(root, id), "participant.json");
            profileHash = StudyParticipantProfiles.Fingerprint(File.ReadAllText(file));
            background = (string)profile["background"] ?? "";
            var details = profile["details"] as JObject ?? new JObject();
            ageRange = (string)details["ageRange"] ?? ""; gender = (string)details["genderSelfDescription"] ?? "";
            handedness = (string)details["handedness"] ?? ""; vrExperience = (string)details["vrExperience"] ?? "";
            error = "";
        }
        catch (Exception exception) { id = ""; profileHash = ""; error = exception.Message; }
    }

    private void Submit()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before confirming participant details.");
            var settings = NeonStudySettings.Load();
            if (!settings.recordOnPlay || !settings.archiveEnabled || !string.Equals(root, StudyExportSession.OutputRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Archive settings changed. Close and reopen the participant form.");
            if (researcher.Length > 2000 || sessionNotes.Length > 10000) throw new InvalidDataException("Session notes are too long.");
            id = id.Trim();
            StudyParticipantProfiles.Save(root, id, returning, profileHash, background,
                new JObject { ["ageRange"] = ageRange, ["genderSelfDescription"] = gender,
                    ["handedness"] = handedness, ["vrExperience"] = vrExperience });
            // In case a later step fails, this form now explicitly edits the profile just saved.
            returning = true;
            existing = Directory.GetDirectories(Path.Combine(root, "Participants")).Select(Path.GetFileName)
                .Where(p => File.Exists(Path.Combine(root, "Participants", p, "participant.json"))).OrderBy(p => p).ToArray();
            existingIndex = Array.IndexOf(existing, id) + 1;
            profileHash = StudyParticipantProfiles.Fingerprint(File.ReadAllText(Path.Combine(StudyArchivePaths.ParticipantFolder(root, id), "participant.json")));
            StudyParticipantProfiles.SelectInConfig(NeonStudySettings.FilePath, root, id);
            var intake = new JObject { ["participantId"] = id, ["archiveRoot"] = root, ["profileHash"] = profileHash,
                ["submittedUtc"] = DateTimeOffset.UtcNow.ToString("O"), ["researcherId"] = researcher,
                ["sessionNotes"] = sessionNotes, ["source"] = "operator_entry_before_Play" };
            StudyParticipantStartFlow.Approve(root, id, intake.ToString(Formatting.None));
            submitted = true; Close();
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true; };
        }
        catch (Exception exception) { error = exception.Message; Repaint(); }
    }
}

}
