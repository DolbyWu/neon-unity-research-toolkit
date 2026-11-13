using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonResearch
{
    /// <summary>Explicit local offline curation; never uploads data or starts model training.</summary>
    public sealed class ReviewedSamplesWindow : EditorWindow
    {
        private string session="", annotations="", output="", python="", message="Choose a finished session and reviewed annotation JSON. No audio/video upload or model training.";
        private float windowSeconds=2, minimumCoverage=.7f;
        private bool requireAoi;
        private Process process;
        [MenuItem("Tools/GazeFlow/Build Reviewed Samples (Offline)")]
        public static void Open() { var w=GetWindow<ReviewedSamplesWindow>("Reviewed samples");w.minSize=new Vector2(560,350);w.Show(); }
        private void OnEnable() { python=NeonStudySettings.Load().archivePythonExecutable??"";EditorApplication.update+=Poll; }
        private void OnDisable() { EditorApplication.update-=Poll; }
        private void Poll()
        {
            if(process==null || !process.HasExited)return;
            message=(process.ExitCode==0?"BUILD COMPLETE\n":"BUILD REJECTED\n")+process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();
            process.Dispose();process=null;Repaint();
        }
        private static string Quote(string value) => "\""+value.Replace('\\','/').Replace("\"","\\\"")+"\"";
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(message,MessageType.Info);
            using(new EditorGUI.DisabledScope(process!=null))
            {
                python=EditorGUILayout.TextField("Python executable",python);
                session=EditorGUILayout.TextField("Closed session folder",session);
                if(GUILayout.Button("Choose finished session folder")) { var selected=EditorUtility.OpenFolderPanel("Choose finished session",StudyExportSession.OutputRoot,""); if(!string.IsNullOrEmpty(selected)) { session=selected;output=Path.Combine(StudyExportSession.OutputRoot,"CuratedSamples","curation-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ")); } }
                annotations=EditorGUILayout.TextField("Reviewed annotation JSON",annotations);
                if(GUILayout.Button("Choose annotation JSON")) { var selected=EditorUtility.OpenFilePanel("Choose reviewed annotations",session,"json");if(!string.IsNullOrEmpty(selected))annotations=selected; }
                output=EditorGUILayout.TextField("NEW output folder",output);
                windowSeconds=EditorGUILayout.FloatField("Window seconds (non-overlap)",windowSeconds);
                minimumCoverage=EditorGUILayout.FloatField("Minimum valid eye coverage",minimumCoverage);
                requireAoi=EditorGUILayout.Toggle("Also require measured AOI quality",requireAoi);
                EditorGUILayout.HelpBox("Only reviewed, resolved intervals can become samples. Conflicts, draft labels, short tails and gaps remain in audit.csv. Old v1 labels import as draft. Thresholds are configurable engineering policy. These files are not a trained model.",MessageType.Warning);
                using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if(GUILayout.Button("Build reviewed samples locally",GUILayout.Height(38)))
                {
                    try
                    {
                        if(!File.Exists(python))throw new FileNotFoundException("Choose a valid Python executable.");
                        string script=Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ReviewedSamplesWindow).Assembly).resolvedPath,"Tools~","Build-ReviewedSamples.py");
                        if(!File.Exists(script))throw new FileNotFoundException("Packaged offline builder missing.");
                        if(!Directory.Exists(session)||!File.Exists(annotations)||string.IsNullOrWhiteSpace(output))throw new InvalidOperationException("Choose a session, annotation file and NEW output folder.");
                        string arguments=Quote(script)+" --session "+Quote(session)+" --annotations "+Quote(annotations)+" --output "+Quote(output)+" --window "+windowSeconds.ToString("R",CultureInfo.InvariantCulture)+" --min-coverage "+minimumCoverage.ToString("R",CultureInfo.InvariantCulture)+(requireAoi?" --require-aoi":"");
                        process=Process.Start(new ProcessStartInfo(python,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true});
                        message="Building locally. Original session files remain unchanged. No network request.";
                    }catch(Exception e){message=e.Message;}
                }
            }
            if(!string.IsNullOrWhiteSpace(output)&&Directory.Exists(output)&&GUILayout.Button("Open curated output folder"))EditorUtility.RevealInFinder(Path.Combine(output,"manifest.json"));
        }
    }
}
