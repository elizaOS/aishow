#nullable enable
using UnityEngine;
using UnityEditor;
using System.IO;

public class VideoMergerEditor : EditorWindow
{
    private string inputFolderPath = "";
    private string ffmpegPath = "ffmpeg"; // Default to ffmpeg in PATH
    private string introVideoPath = ""; // Path for the optional intro video
    private string outroVideoPath = ""; // Path for the optional outro video

    private bool showOptionalVideos = false; // Foldout state

    [MenuItem("Tools/Video Merger Util")]
    public static void ShowWindow()
    {
        GetWindow<VideoMergerEditor>("Video Merger");
    }

    void OnGUI()
    {
        GUILayout.Label("Video Merge Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Input Folder Path
        EditorGUILayout.BeginHorizontal();
        inputFolderPath = EditorGUILayout.TextField("Input Video Folder", inputFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Input Video Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                inputFolderPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal(); 

        EditorGUILayout.Space();

        // FFmpeg Path (optional)
        ffmpegPath = EditorGUILayout.TextField("FFmpeg Executable Path", ffmpegPath);
        EditorGUILayout.HelpBox("Leave as 'ffmpeg' if it is in your system's PATH, or provide the full path to ffmpeg.exe.", MessageType.Info);

        EditorGUILayout.Space();

        // Optional Intro/Outro Videos
        showOptionalVideos = EditorGUILayout.Foldout(showOptionalVideos, "Optional Intro/Outro Videos", true, EditorStyles.foldoutHeader);
        if (showOptionalVideos)
        {
            // Intro Video Path
            EditorGUILayout.BeginHorizontal();
            introVideoPath = EditorGUILayout.TextField("Intro Video (Optional)", introVideoPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Intro Video", "", "mp4,mov,avi,mkv");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    introVideoPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Outro Video Path
            EditorGUILayout.BeginHorizontal();
            outroVideoPath = EditorGUILayout.TextField("Outro Video (Optional)", outroVideoPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Outro Video", "", "mp4,mov,avi,mkv");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    outroVideoPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Merge Videos", GUILayout.Height(30)))
        {
            if (string.IsNullOrEmpty(inputFolderPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select an input video folder.", "OK");
                return;
            }

            if (!Directory.Exists(inputFolderPath))
            {
                EditorUtility.DisplayDialog("Error", $"Input folder does not exist: {inputFolderPath}", "OK");
                return;
            }

            // Determine output path
            DirectoryInfo dirInfo = new DirectoryInfo(inputFolderPath);
            string outputFileName = $"{dirInfo.Name}_Merged.mp4";
            string outputFilePath = Path.Combine(dirInfo.Parent != null ? dirInfo.Parent.FullName : Path.GetPathRoot(dirInfo.FullName) ?? "", outputFileName);
            
            UnityEngine.Debug.Log($"Attempting to merge videos from '{inputFolderPath}' to '{outputFilePath}'");
            UnityEngine.Debug.Log($"Intro video: {(string.IsNullOrEmpty(introVideoPath) ? "Not specified" : introVideoPath)}");
            UnityEngine.Debug.Log($"Outro video: {(string.IsNullOrEmpty(outroVideoPath) ? "Not specified" : outroVideoPath)}");

            // Pass null if paths are empty, otherwise pass the path.
            string? finalIntroPath = string.IsNullOrEmpty(introVideoPath) ? null : introVideoPath;
            string? finalOutroPath = string.IsNullOrEmpty(outroVideoPath) ? null : outroVideoPath;

            bool success = VideoMerger.MergeVideos(inputFolderPath, outputFilePath, ffmpegPath, finalIntroPath, finalOutroPath);

            if (success)
            {
                EditorUtility.DisplayDialog("Video Merge Success", $"Videos merged successfully!\nOutput: {outputFilePath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Video Merge Failed", "Video merging failed. Check Unity console for details and ensure FFmpeg is correctly configured.", "OK");
            }
        }
    }
} 