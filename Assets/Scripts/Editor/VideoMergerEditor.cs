#nullable enable
using UnityEngine;
using UnityEditor;
using System.IO;

public class VideoMergerEditor : EditorWindow
{
    private VideoMerger.MergeOptions mergeOptions = new VideoMerger.MergeOptions();
    private Vector2 scrollPosition;
    private bool isMerging = false;
    private string statusMessage = "";
    private float progress = 0f;
    private bool showAdvancedOptions = false;
    private bool showOptionalVideos = false;

    [MenuItem("Tools/Enhanced Video Merger")]
    public static void ShowWindow()
    {
        var window = GetWindow<VideoMergerEditor>("Enhanced Video Merger");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        VideoMerger.OnProgressUpdated += HandleProgressUpdate;
        
        // Set default values
        if (string.IsNullOrEmpty(mergeOptions.InputDirectoryPath))
        {
            mergeOptions.InputDirectoryPath = "";
        }
    }

    private void OnDisable()
    {
        VideoMerger.OnProgressUpdated -= HandleProgressUpdate;
    }

    private void HandleProgressUpdate(VideoMerger.MergeProgress progressInfo)
    {
        progress = progressInfo.Progress;
        statusMessage = progressInfo.CurrentOperation;
        
        if (progressInfo.HasError)
        {
            EditorUtility.DisplayDialog("Video Merge Error", progressInfo.ErrorMessage, "OK");
            isMerging = false;
        }
        else if (progressInfo.IsComplete)
        {
            EditorUtility.DisplayDialog("Video Merge Complete", 
                $"Videos merged successfully!\n\nOutput: {progressInfo.CurrentFile}\n\nYou can now find your merged video at the specified location.", 
                "OK");
            isMerging = false;
        }
        
        Repaint();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        EditorGUILayout.LabelField("Enhanced Video Merger", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool merges video segments with advanced codec standardization, audio processing, and progress monitoring. " +
            "It supports intro/outro videos and provides comprehensive format compatibility.", 
            MessageType.Info);
        EditorGUILayout.Space();

        // Basic Settings
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
        
        // Input Directory
        EditorGUILayout.BeginHorizontal();
        mergeOptions.InputDirectoryPath = EditorGUILayout.TextField("Input Video Folder", mergeOptions.InputDirectoryPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Input Video Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                mergeOptions.InputDirectoryPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Output File
        EditorGUILayout.BeginHorizontal();
        mergeOptions.OutputFilePath = EditorGUILayout.TextField("Output File", mergeOptions.OutputFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.SaveFilePanel("Save Merged Video", "", "merged_video.mp4", "mp4");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                mergeOptions.OutputFilePath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Auto-generate output path if not set
        if (string.IsNullOrEmpty(mergeOptions.OutputFilePath) && !string.IsNullOrEmpty(mergeOptions.InputDirectoryPath))
        {
            if (GUILayout.Button("Auto-Generate Output Path"))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(mergeOptions.InputDirectoryPath);
                string outputFileName = $"{dirInfo.Name}_Enhanced_Merged.mp4";
                string parentDir = dirInfo.Parent?.FullName ?? Path.GetPathRoot(dirInfo.FullName) ?? "";
                mergeOptions.OutputFilePath = Path.Combine(parentDir, outputFileName);
            }
        }

        EditorGUILayout.Space();

        // FFmpeg Path
        mergeOptions.FFmpegExecutablePath = EditorGUILayout.TextField("FFmpeg Executable", mergeOptions.FFmpegExecutablePath);
        EditorGUILayout.HelpBox("Leave as 'ffmpeg' if it's in your system PATH, or provide the full path to ffmpeg.exe", MessageType.Info);

        // Test FFmpeg button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test FFmpeg Installation"))
        {
            bool testResult = VideoMerger.TestFFmpegInstallation(mergeOptions.FFmpegExecutablePath);
            if (testResult)
            {
                EditorUtility.DisplayDialog("FFmpeg Test", "FFmpeg installation test successful!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("FFmpeg Test Failed", 
                    "FFmpeg test failed. Check the console for details and verify your FFmpeg installation.", "OK");
            }
        }
        if (GUILayout.Button("Test Simple Command"))
        {
            // Test a very basic FFmpeg command
            string testArgs = "-f lavfi -i testsrc=duration=1:size=320x240:rate=1 -t 1 -y test_output.mp4";
            bool testResult = VideoMerger.TestFFmpegCommand(mergeOptions.FFmpegExecutablePath, testArgs);
            if (testResult)
            {
                EditorUtility.DisplayDialog("Command Test", "Simple FFmpeg command test successful!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Command Test Failed", 
                    "Simple command test failed. Check console for details.", "OK");
            }
        }
        if (GUILayout.Button("Debug Filter Complex"))
        {
            // Test filter_complex syntax generation
            string debugOutput = VideoMerger.GenerateFilterComplexDebug(mergeOptions);
            Debug.Log($"Filter Complex Debug Output:\n{debugOutput}");
            EditorUtility.DisplayDialog("Filter Complex Debug", 
                "Filter complex syntax generated. Check Unity console for detailed output.", "OK");
        }
        if (GUILayout.Button("Browse FFmpeg", GUILayout.Width(100)))
        {
            string selectedPath = EditorUtility.OpenFilePanel("Select FFmpeg Executable", "", "exe");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                mergeOptions.FFmpegExecutablePath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Optional Videos Section
        showOptionalVideos = EditorGUILayout.Foldout(showOptionalVideos, "Optional Intro/Outro Videos", true, EditorStyles.foldoutHeader);
        if (showOptionalVideos)
        {
            EditorGUI.indentLevel++;
            
            // Intro Video
            EditorGUILayout.BeginHorizontal();
            mergeOptions.IntroVideoPath = EditorGUILayout.TextField("Intro Video", mergeOptions.IntroVideoPath ?? "");
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Intro Video", "", "mp4,mov,avi,mkv");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    mergeOptions.IntroVideoPath = selectedPath;
                }
            }
            if (!string.IsNullOrEmpty(mergeOptions.IntroVideoPath) && GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                mergeOptions.IntroVideoPath = null;
            }
            EditorGUILayout.EndHorizontal();

            // Outro Video
            EditorGUILayout.BeginHorizontal();
            mergeOptions.OutroVideoPath = EditorGUILayout.TextField("Outro Video", mergeOptions.OutroVideoPath ?? "");
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Outro Video", "", "mp4,mov,avi,mkv");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    mergeOptions.OutroVideoPath = selectedPath;
                }
            }
            if (!string.IsNullOrEmpty(mergeOptions.OutroVideoPath) && GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                mergeOptions.OutroVideoPath = null;
            }
            EditorGUILayout.EndHorizontal();

            // Intro/Outro Volume
            mergeOptions.IntroOutroVolume = EditorGUILayout.Slider("Intro/Outro Volume", mergeOptions.IntroOutroVolume, 0f, 1f);
            EditorGUILayout.HelpBox("Volume level for intro/outro videos (0.5 = -6dB, recommended for background music)", MessageType.Info);
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Advanced Options Section
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Codec & Processing Options", true, EditorStyles.foldoutHeader);
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            
            // Video Settings
            EditorGUILayout.LabelField("Video Settings", EditorStyles.boldLabel);
            mergeOptions.StandardizeVideoCodec = EditorGUILayout.Toggle("Standardize Video Codec", mergeOptions.StandardizeVideoCodec);
            
            if (mergeOptions.StandardizeVideoCodec)
            {
                mergeOptions.TargetVideoCodec = EditorGUILayout.TextField("Target Video Codec", mergeOptions.TargetVideoCodec);
                mergeOptions.TargetResolution = EditorGUILayout.TextField("Target Resolution", mergeOptions.TargetResolution);
                mergeOptions.TargetFps = EditorGUILayout.TextField("Target FPS", mergeOptions.TargetFps);
                mergeOptions.TargetPixFmt = EditorGUILayout.TextField("Pixel Format", mergeOptions.TargetPixFmt);
                mergeOptions.TargetVideoBitrate = EditorGUILayout.TextField("Video Bitrate", mergeOptions.TargetVideoBitrate);
            }

            EditorGUILayout.Space();

            // Audio Settings
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            mergeOptions.TargetAudioCodec = EditorGUILayout.TextField("Audio Codec", mergeOptions.TargetAudioCodec);
            mergeOptions.TargetAudioSampleRate = EditorGUILayout.TextField("Sample Rate", mergeOptions.TargetAudioSampleRate);
            mergeOptions.TargetAudioChannels = EditorGUILayout.TextField("Audio Channels", mergeOptions.TargetAudioChannels);
            mergeOptions.TargetAudioBitrate = EditorGUILayout.TextField("Audio Bitrate", mergeOptions.TargetAudioBitrate);
            
            mergeOptions.ApplyDynamicNormalization = EditorGUILayout.Toggle("Apply Dynamic Audio Normalization", mergeOptions.ApplyDynamicNormalization);
            EditorGUILayout.HelpBox("Dynamic normalization helps balance audio levels across different segments", MessageType.Info);
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // Progress Bar
        if (isMerging)
        {
            EditorGUILayout.LabelField("Merge Progress", EditorStyles.boldLabel);
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20f), progress, statusMessage);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("Merging in progress... Please wait. This may take several minutes depending on video size and complexity.", MessageType.Info);
            
            if (GUILayout.Button("Cancel Merge"))
            {
                // Note: In a real implementation, you'd want to add cancellation support
                EditorUtility.DisplayDialog("Cancel", "Merge cancellation is not yet implemented. Please wait for the current operation to complete.", "OK");
            }
        }
        else
        {
            // Validation
            bool canMerge = true;
            string validationMessage = "";

            if (string.IsNullOrEmpty(mergeOptions.OutputFilePath))
            {
                canMerge = false;
                validationMessage = "Please specify an output file path.";
            }
            else if (string.IsNullOrEmpty(mergeOptions.InputDirectoryPath) && 
                     string.IsNullOrEmpty(mergeOptions.IntroVideoPath) && 
                     string.IsNullOrEmpty(mergeOptions.OutroVideoPath))
            {
                canMerge = false;
                validationMessage = "Please specify at least one input source (folder, intro, or outro video).";
            }

            if (!canMerge)
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            }

            // Merge Button
            EditorGUI.BeginDisabledGroup(!canMerge);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Simple Merge (Debug)", GUILayout.Height(30)))
            {
                StartSimpleMerge();
            }
            if (GUILayout.Button("Advanced Merge", GUILayout.Height(30)))
            {
                StartMerge();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Simple Merge: Basic concatenation without complex filters (for debugging)\n" +
                "Advanced Merge: Full processing with intro/outro, codec standardization, and audio processing", 
                MessageType.Info);
            
            EditorGUI.EndDisabledGroup();
        }

        // Quick Presets
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("High Quality"))
        {
            ApplyHighQualityPreset();
        }
        if (GUILayout.Button("Balanced"))
        {
            ApplyBalancedPreset();
        }
        if (GUILayout.Button("Fast/Small"))
        {
            ApplyFastPreset();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void StartMerge()
    {
        isMerging = true;
        progress = 0f;
        statusMessage = "Initializing...";
        
        // Start the merge process
        bool success = VideoMerger.MergeVideosWithOptions(mergeOptions);
        
        if (!success && !isMerging) // If it failed immediately
        {
            EditorUtility.DisplayDialog("Video Merge Failed", 
                "Video merging failed immediately. Check Unity console for details and ensure FFmpeg is correctly configured.", 
                "OK");
            isMerging = false;
        }
    }

    private void StartSimpleMerge()
    {
        isMerging = true;
        progress = 0f;
        statusMessage = "Initializing simple merge...";
        
        // Start the simple merge process for debugging
        bool success = VideoMerger.MergeVideosSimple(mergeOptions);
        
        if (!success && !isMerging) // If it failed immediately
        {
            EditorUtility.DisplayDialog("Simple Merge Failed", 
                "Simple video merging failed. Check Unity console for details and ensure FFmpeg is correctly configured.", 
                "OK");
            isMerging = false;
        }
    }

    private void ApplyHighQualityPreset()
    {
        mergeOptions.TargetVideoBitrate = "8000k";
        mergeOptions.TargetAudioBitrate = "320k";
        mergeOptions.TargetResolution = "1920x1080";
        mergeOptions.TargetFps = "60";
        mergeOptions.ApplyDynamicNormalization = true;
        UnityEngine.Debug.Log("Applied High Quality preset");
    }

    private void ApplyBalancedPreset()
    {
        mergeOptions.TargetVideoBitrate = "5000k";
        mergeOptions.TargetAudioBitrate = "192k";
        mergeOptions.TargetResolution = "1920x1080";
        mergeOptions.TargetFps = "30";
        mergeOptions.ApplyDynamicNormalization = true;
        UnityEngine.Debug.Log("Applied Balanced preset");
    }

    private void ApplyFastPreset()
    {
        mergeOptions.TargetVideoBitrate = "2000k";
        mergeOptions.TargetAudioBitrate = "128k";
        mergeOptions.TargetResolution = "1280x720";
        mergeOptions.TargetFps = "30";
        mergeOptions.ApplyDynamicNormalization = false;
        UnityEngine.Debug.Log("Applied Fast/Small preset");
    }
} 