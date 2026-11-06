// Independently extracted preview. The working EscapeRoom project is not referenced.
namespace NeonResearch
{
using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class NeonStudySettings
{
    public bool enabled = false;
    public bool recordOnPlay = false;
    public string condition = "baseline";
    public string outputRoot = "NeonResearchData";
    public bool archiveEnabled = true;
    public bool archiveAutoOpen = true; // Editor: open the finished session folder and offline HTML, not the archive dashboard.
    public string archivePythonExecutable = "";
    public bool mediaRecordingEnabled = false;
    public bool mediaRecordVideo = true;
    public bool mediaRecordMicrophone = true;
    public string mediaFfmpegExecutable = "";
    public string mediaMicrophoneDevice = "";
    public string mediaWindowTitle = "Oculus Mirror";
    public string mediaVideoSource = "window";
    public bool mediaAllowDesktopRecording = false;
    public int mediaScreenX = 0, mediaScreenY = 0;
    public int mediaVideoWidth = 1280, mediaVideoHeight = 720;
    public float mediaMaxMinutes = 90;
    public string participantId = "technical-test";
    public string companionRecordingId = "";
    public string companionIp = "";
    public int rtspPort = 8086;
    public int websocketPort = 8686;
    public int companionApiPort = 8080;
    public bool mountCalibrationVerified = false;
    public string calibrationId = "";
    public Vector3 mountPositionMeters;
    public Vector3 mountRotationDegrees;
    public int raycastLayerMask = -1;
    public float maxRayDistanceMeters = 30f;
    public float maxQueueAgeSeconds = 0.15f;
    public float maxDwellGapSeconds = 0.1f;

    public static string Folder
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UserSettings", "NeonResearch");
#else
            return Path.Combine(Application.persistentDataPath, "NeonResearchConfig");
#endif
        }
    }
    public static string FilePath => Path.Combine(Folder, "neon-study.json");

    public static NeonStudySettings Load()
    {
        Directory.CreateDirectory(Folder);
        if (!File.Exists(FilePath))
            File.WriteAllText(FilePath, JsonUtility.ToJson(new NeonStudySettings(), true));
        var value = JsonUtility.FromJson<NeonStudySettings>(File.ReadAllText(FilePath));
        if (value == null || value.rtspPort < 1 || value.rtspPort > 65535 || value.websocketPort < 1 || value.websocketPort > 65535 ||
            value.companionApiPort < 1 || value.companionApiPort > 65535 ||
            !(value.maxRayDistanceMeters > 0) || !(value.maxQueueAgeSeconds > 0) ||
            !(value.maxDwellGapSeconds > 0))
            throw new InvalidDataException("Invalid Neon study settings.");
        return value;
    }
}

}
