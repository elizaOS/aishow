#nullable enable
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Events;

/// <summary>
/// Enhanced utility class for merging multiple video files into a single file using FFmpeg,
/// with options for intro/outro, audio level adjustments, codec standardization, and progress monitoring.
/// </summary>
public static class VideoMerger
{
    public class MergeProgress
    {
        public float Progress { get; set; }
        public string CurrentOperation { get; set; } = "";
        public string CurrentFile { get; set; } = "";
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public class MergeOptions
    {
        public string InputDirectoryPath { get; set; } = "";
        public string OutputFilePath { get; set; } = "";
        public string FFmpegExecutablePath { get; set; } = "ffmpeg";
        public string? IntroVideoPath { get; set; }
        public string? OutroVideoPath { get; set; }
        public bool StandardizeVideoCodec { get; set; } = true;
        public string TargetVideoCodec { get; set; } = "libx264";
        public string TargetResolution { get; set; } = "1920x1080";
        public string TargetFps { get; set; } = "30";
        public string TargetPixFmt { get; set; } = "yuv420p";
        public string TargetVideoBitrate { get; set; } = "5000k";
        public string TargetAudioCodec { get; set; } = "aac";
        public string TargetAudioSampleRate { get; set; } = "48000";
        public string TargetAudioChannels { get; set; } = "2";
        public string TargetAudioBitrate { get; set; } = "192k";
        public float IntroOutroVolume { get; set; } = 0.5f;
        public bool ApplyDynamicNormalization { get; set; } = true;
    }

    /// <summary>
    /// Represents a segment of a video, parsed from its filename.
    /// Used for sorting video files in the correct order before merging.
    /// </summary>
    private class VideoSegmentInfo
    {
        public string? FilePath { get; set; }
        public int Season { get; set; } 
        public int Episode { get; set; } 
        public int PartX { get; set; }
        public int PartY { get; set; }
        public string? FileName { get; set; }
    }

    /// <summary>
    /// Event that can be subscribed to for progress updates during the merge process
    /// </summary>
    public static event UnityAction<MergeProgress>? OnProgressUpdated;

    private static void ReportProgress(float progress, string operation, string currentFile = "", bool isComplete = false, bool hasError = false, string errorMessage = "")
    {
        var progressInfo = new MergeProgress
        {
            Progress = progress,
            CurrentOperation = operation,
            CurrentFile = currentFile,
            IsComplete = isComplete,
            HasError = hasError,
            ErrorMessage = errorMessage
        };
        
        OnProgressUpdated?.Invoke(progressInfo);
    }

    /// <summary>
    /// Executes an FFmpeg command with progress monitoring.
    /// </summary>
    private static bool ExecuteFFmpegCommand(string ffmpegPath, string arguments, string workingDirectory, out string output, out string error)
    {
        UnityEngine.Debug.Log($"Executing FFmpeg in '{workingDirectory}':\n{ffmpegPath} {arguments}");
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        StringBuilder outputBuilder = new StringBuilder();
        StringBuilder errorBuilder = new StringBuilder();
        int exitCode = -1;
        
        using (Process process = new Process { StartInfo = startInfo })
        {
            process.OutputDataReceived += (sender, args) => 
            { 
                if (args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                    // Parse FFmpeg progress if available
                    if (args.Data.Contains("time="))
                    {
                        try
                        {
                            var timeMatch = Regex.Match(args.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                            if (timeMatch.Success)
                            {
                                int hours = int.Parse(timeMatch.Groups[1].Value);
                                int minutes = int.Parse(timeMatch.Groups[2].Value);
                                int seconds = int.Parse(timeMatch.Groups[3].Value);
                                float totalSeconds = hours * 3600 + minutes * 60 + seconds;
                                // Estimate progress based on processing time
                                float progress = Mathf.Clamp01(totalSeconds / 300f);
                                ReportProgress(progress, "Processing video", "");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"Failed to parse FFmpeg progress: {ex.Message}");
                        }
                    }
                }
            };
            
            process.ErrorDataReceived += (sender, args) => 
            { 
                if (args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                string errorMsg = $"FFmpeg process failed to start: {ex.Message}. Is FFmpeg installed and accessible via '{ffmpegPath}'?";
                UnityEngine.Debug.LogError(errorMsg);
                output = outputBuilder.ToString();
                error = errorBuilder.ToString() + "\n" + ex.ToString();
                ReportProgress(0, "Error", "", false, true, errorMsg);
                return false;
            }
        }

        output = outputBuilder.ToString();
        error = errorBuilder.ToString();

        if (exitCode == 0)
        {
            UnityEngine.Debug.Log("FFmpeg command executed successfully.");
            return true;
        }
        else
        {
            string errorMsg = $"FFmpeg failed with exit code {exitCode}.\nFFmpeg Error Output:\n{error}";
            UnityEngine.Debug.LogError(errorMsg);
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public static bool MergeVideos(string inputDirectoryPath, string outputFilePath, string ffmpegExecutablePath = "ffmpeg", string? introVideoPath = null, string? outroVideoPath = null)
    {
        var options = new MergeOptions
        {
            InputDirectoryPath = inputDirectoryPath,
            OutputFilePath = outputFilePath,
            FFmpegExecutablePath = ffmpegExecutablePath,
            IntroVideoPath = introVideoPath,
            OutroVideoPath = outroVideoPath
        };
        
        return MergeVideosWithOptions(options);
    }

    /// <summary>
    /// Enhanced video merging with full options support and progress monitoring
    /// </summary>
    public static bool MergeVideosWithOptions(MergeOptions options)
    {
        UnityEngine.Debug.Log($"Starting enhanced video merge. Input Dir: '{options.InputDirectoryPath}', Output File: '{options.OutputFilePath}'");
        if (!string.IsNullOrEmpty(options.IntroVideoPath)) UnityEngine.Debug.Log($"Intro video: {options.IntroVideoPath}");
        if (!string.IsNullOrEmpty(options.OutroVideoPath)) UnityEngine.Debug.Log($"Outro video: {options.OutroVideoPath}");

        ReportProgress(0, "Starting merge process");

        // Validate inputs
        bool inputDirectoryExists = Directory.Exists(options.InputDirectoryPath);
        if (!inputDirectoryExists && string.IsNullOrEmpty(options.IntroVideoPath) && string.IsNullOrEmpty(options.OutroVideoPath))
        {
            string errorMsg = "No input sources available for merging.";
            UnityEngine.Debug.LogError(errorMsg);
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // Scan for video segments
        List<VideoSegmentInfo> videoSegments = new List<VideoSegmentInfo>();
        if (inputDirectoryExists)
        {
            ReportProgress(0.1f, "Scanning for video segments");
            videoSegments = ScanForVideoSegments(options.InputDirectoryPath);
        }

        if (videoSegments.Count == 0 && string.IsNullOrEmpty(options.IntroVideoPath) && string.IsNullOrEmpty(options.OutroVideoPath))
        {
            string errorMsg = "No video files found to merge.";
            UnityEngine.Debug.LogWarning(errorMsg);
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // Create temporary directory for processing
        string tempDir = Path.Combine(Path.GetTempPath(), $"ffmpeg_merge_{System.Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            string? mainContentPath = null;

            // Stage 1: Process main video segments if any
            if (videoSegments.Count > 0)
            {
                ReportProgress(0.2f, "Processing main video segments");
                mainContentPath = ProcessMainSegments(videoSegments, tempDir, options);
                if (mainContentPath == null) return false;
            }

            // Stage 2: Final merge with intro/outro and standardization
            ReportProgress(0.5f, "Preparing final merge");
            return ProcessFinalMerge(mainContentPath, tempDir, options);
        }
        catch (System.Exception ex)
        {
            string errorMsg = $"Unexpected error during video merging: {ex.Message}";
            UnityEngine.Debug.LogError($"{errorMsg}\nStackTrace: {ex.StackTrace}");
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                    UnityEngine.Debug.Log($"Cleaned up temporary directory: {tempDir}");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to cleanup temporary directory: {ex.Message}");
            }
        }
    }

    private static List<VideoSegmentInfo> ScanForVideoSegments(string inputDirectoryPath)
    {
        List<VideoSegmentInfo> segments = new List<VideoSegmentInfo>();
        Regex videoFileRegex = new Regex(@"^S(\d+)E(\d+)_(\d+)_(\d+)_new\.mp4$", RegexOptions.IgnoreCase);
        
        try
        {
            string[] files = Directory.GetFiles(inputDirectoryPath, "*.mp4");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                Match match = videoFileRegex.Match(fileName);
                if (match.Success)
                {
                    segments.Add(new VideoSegmentInfo
                    {
                        FilePath = Path.GetFullPath(file),
                        FileName = fileName,
                        Season = int.Parse(match.Groups[1].Value),
                        Episode = int.Parse(match.Groups[2].Value),
                        PartX = int.Parse(match.Groups[3].Value),
                        PartY = int.Parse(match.Groups[4].Value)
                    });
                }
            }

            // Sort segments in proper order
            segments.Sort((s1, s2) =>
            {
                int compareSeason = s1.Season.CompareTo(s2.Season);
                if (compareSeason != 0) return compareSeason;
                int compareEpisode = s1.Episode.CompareTo(s2.Episode);
                if (compareEpisode != 0) return compareEpisode;
                int compareX = s1.PartX.CompareTo(s2.PartX);
                if (compareX != 0) return compareX;
                return s1.PartY.CompareTo(s2.PartY);
            });

            UnityEngine.Debug.Log($"Found {segments.Count} video segments to merge:");
            foreach (var segment in segments)
            {
                UnityEngine.Debug.Log($"- {segment.FileName} (S:{segment.Season} E:{segment.Episode} X:{segment.PartX}, Y:{segment.PartY})");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error scanning for video segments: {ex.Message}");
        }

        return segments;
    }

    private static string? ProcessMainSegments(List<VideoSegmentInfo> segments, string tempDir, MergeOptions options)
    {
        string segmentsListPath = Path.Combine(tempDir, "segments_list.txt");
        string mergedPath = Path.Combine(tempDir, "main_merged.ts");

        // Create segments list file
        StringBuilder listContent = new StringBuilder();
        foreach (var segment in segments)
        {
            if (!string.IsNullOrEmpty(segment.FilePath))
            {
                string formattedPath = segment.FilePath.Replace('\\', '/');
                listContent.AppendLine($"file '{formattedPath}'");
            }
        }
        File.WriteAllText(segmentsListPath, listContent.ToString());

        // Build FFmpeg command for concatenation
        string arguments = string.Format(
            "-y -f concat -safe 0 -i \"{0}\" -c copy -bsf:a aac_adtstoasc \"{1}\"",
            segmentsListPath.Replace('\\', '/'),
            mergedPath.Replace('\\', '/')
        );

        if (!ExecuteFFmpegCommand(options.FFmpegExecutablePath, arguments, tempDir, out string output, out string error))
        {
            UnityEngine.Debug.LogError("Failed to merge main video segments.");
            return null;
        }

        UnityEngine.Debug.Log($"Main segments merged to: {mergedPath}");
        return mergedPath;
    }

    private static bool ProcessFinalMerge(string? mainContentPath, string tempDir, MergeOptions options)
    {
        List<string> inputs = new List<string>();
        StringBuilder filterComplex = new StringBuilder();
        List<string> videoStreams = new List<string>();
        List<string> audioStreams = new List<string>();
        int inputIndex = 0;

        // Count valid inputs first
        int validInputCount = 0;
        if (!string.IsNullOrEmpty(options.IntroVideoPath) && File.Exists(options.IntroVideoPath)) validInputCount++;
        if (!string.IsNullOrEmpty(mainContentPath) && File.Exists(mainContentPath)) validInputCount++;
        if (!string.IsNullOrEmpty(options.OutroVideoPath) && File.Exists(options.OutroVideoPath)) validInputCount++;

        if (validInputCount == 0)
        {
            string errorMsg = "No valid inputs found for final merge.";
            UnityEngine.Debug.LogError(errorMsg);
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // If only one input (main content), use simpler processing
        if (validInputCount == 1 && !string.IsNullOrEmpty(mainContentPath))
        {
            return ProcessSingleInput(mainContentPath, tempDir, options);
        }

        UnityEngine.Debug.Log($"Processing final merge with {validInputCount} inputs");

        // Helper method to add input and create filter chains
        void AddInput(string? videoPath, string type, float volume = 1.0f)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath)) return;

            string escapedPath = videoPath.Replace('\\', '/');
            inputs.Add($"-i \"{escapedPath}\"");
            
            UnityEngine.Debug.Log($"Adding input {inputIndex}: {videoPath} (type: {type}, volume: {volume})");

            // Parse target resolution into width and height
            string[] resolutionParts = options.TargetResolution.Split('x');
            string targetWidth = resolutionParts.Length > 0 ? resolutionParts[0] : "1920";
            string targetHeight = resolutionParts.Length > 1 ? resolutionParts[1] : "1080";

            // Simplified video processing - scale and pad with proper syntax
            string vIn = $"[{inputIndex}:v]";
            string vOut = $"[v{inputIndex}]";
            filterComplex.Append($"{vIn}scale={targetWidth}:{targetHeight}:force_original_aspect_ratio=decrease,pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2{vOut};");
            videoStreams.Add(vOut);

            // Simplified audio processing - just volume and resample
            string aIn = $"[{inputIndex}:a]";
            string aOut = $"[a{inputIndex}]";
            
            if (!Mathf.Approximately(volume, 1.0f))
            {
                filterComplex.Append($"{aIn}volume={volume:F1},aresample={options.TargetAudioSampleRate}{aOut};");
            }
            else
            {
                filterComplex.Append($"{aIn}aresample={options.TargetAudioSampleRate}{aOut};");
            }
            audioStreams.Add(aOut);

            inputIndex++;
        }

        // Add all inputs
        AddInput(options.IntroVideoPath, "intro", options.IntroOutroVolume);
        AddInput(mainContentPath, "main");
        AddInput(options.OutroVideoPath, "outro", options.IntroOutroVolume);

        // Validate we have streams to work with
        if (videoStreams.Count == 0 || audioStreams.Count == 0)
        {
            string errorMsg = "No valid video/audio streams found after input processing.";
            UnityEngine.Debug.LogError(errorMsg);
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // Build concatenation filters
        if (videoStreams.Count > 1)
        {
            // Concatenate video streams
            for (int i = 0; i < videoStreams.Count; i++)
                filterComplex.Append(videoStreams[i]);
            filterComplex.Append($"concat=n={videoStreams.Count}:v=1:a=0[outv];");

            // Concatenate audio streams
            for (int i = 0; i < audioStreams.Count; i++)
                filterComplex.Append(audioStreams[i]);
            filterComplex.Append($"concat=n={audioStreams.Count}:v=0:a=1[outa]");
        }
        else
        {
            // Single input, just rename the streams
            filterComplex.Append($"{videoStreams[0]}copy[outv];{audioStreams[0]}acopy[outa]");
        }

        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(options.OutputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        ReportProgress(0.7f, "Processing final output");

        // Log the complete filter_complex for debugging
        string filterComplexStr = filterComplex.ToString().TrimEnd(';');
        UnityEngine.Debug.Log($"Complete filter_complex: {filterComplexStr}");

        // Build final FFmpeg command with corrected stream mapping
        string finalArguments = string.Format(
            "-y {0} -filter_complex \"{1}\" -map \"[outv]\" -map \"[outa]\" " +
            "-c:v {2} -preset fast -crf 23 -pix_fmt {3} " +
            "-c:a {4} -b:a {5} \"{6}\"",
            string.Join(" ", inputs),
            filterComplexStr,
            options.TargetVideoCodec,
            options.TargetPixFmt,
            options.TargetAudioCodec,
            options.TargetAudioBitrate,
            options.OutputFilePath.Replace('\\', '/')
        );

        UnityEngine.Debug.Log($"Final FFmpeg command: {finalArguments}");

        if (!ExecuteFFmpegCommand(options.FFmpegExecutablePath, finalArguments, tempDir, out string output, out string error))
        {
            UnityEngine.Debug.LogError("Final merge failed.");
            return false;
        }

        UnityEngine.Debug.Log($"Videos merged successfully to: {options.OutputFilePath}");
        ReportProgress(1.0f, "Merge complete", options.OutputFilePath, true);
        return true;
    }

    /// <summary>
    /// Simplified processing for single input scenarios (no complex filter chains needed)
    /// </summary>
    private static bool ProcessSingleInput(string inputPath, string tempDir, MergeOptions options)
    {
        UnityEngine.Debug.Log("Using simplified single input processing");
        
        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(options.OutputFilePath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        ReportProgress(0.7f, "Processing single input");

        // Use the simplest possible re-encoding approach
        string escapedInputPath = inputPath.Replace('\\', '/');
        string escapedOutputPath = options.OutputFilePath.Replace('\\', '/');
        
        // Try simple re-encoding first
        string arguments = string.Format(
            "-y -i \"{0}\" -c:v {1} -preset fast -crf 23 -pix_fmt {2} -c:a {3} -b:a {4} \"{5}\"",
            escapedInputPath,
            options.TargetVideoCodec,
            options.TargetPixFmt,
            options.TargetAudioCodec,
            options.TargetAudioBitrate,
            escapedOutputPath
        );

        UnityEngine.Debug.Log($"Single input FFmpeg command: {arguments}");

        if (!ExecuteFFmpegCommand(options.FFmpegExecutablePath, arguments, tempDir, out string output, out string error))
        {
            UnityEngine.Debug.LogWarning("Re-encoding failed, trying simple copy...");
            
            // Fallback: try simple copy
            string copyArguments = string.Format(
                "-y -i \"{0}\" -c copy \"{1}\"",
                escapedInputPath,
                escapedOutputPath
            );
            
            UnityEngine.Debug.Log($"Fallback copy command: {copyArguments}");
            
            if (!ExecuteFFmpegCommand(options.FFmpegExecutablePath, copyArguments, tempDir, out string copyOutput, out string copyError))
            {
                UnityEngine.Debug.LogError("Both re-encoding and copy failed.");
                return false;
            }
            
            UnityEngine.Debug.Log("Fallback copy succeeded");
        }

        UnityEngine.Debug.Log($"Single input processed successfully to: {options.OutputFilePath}");
        ReportProgress(1.0f, "Merge complete", options.OutputFilePath, true);
        return true;
    }

    /// <summary>
    /// Test method to validate specific FFmpeg commands
    /// </summary>
    public static bool TestFFmpegCommand(string ffmpegPath, string testArguments)
    {
        UnityEngine.Debug.Log($"Testing FFmpeg command: {testArguments}");
        
        if (!ExecuteFFmpegCommand(ffmpegPath, testArguments, Path.GetTempPath(), out string output, out string error))
        {
            UnityEngine.Debug.LogError($"Test command failed. Arguments: {testArguments}");
            return false;
        }
        
        UnityEngine.Debug.Log("Test command succeeded");
        return true;
    }

    /// <summary>
    /// Test method to validate FFmpeg installation and basic functionality
    /// </summary>
    public static bool TestFFmpegInstallation(string ffmpegPath = "ffmpeg")
    {
        try
        {
            string arguments = "-version";
            if (!ExecuteFFmpegCommand(ffmpegPath, arguments, Path.GetTempPath(), out string output, out string error))
            {
                UnityEngine.Debug.LogError($"FFmpeg test failed. Output: {output}, Error: {error}");
                return false;
            }
            
            UnityEngine.Debug.Log($"FFmpeg test successful. Version info: {output.Split('\n')[0]}");
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"FFmpeg test exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Simplified merge method for debugging FFmpeg issues
    /// </summary>
    public static bool MergeVideosSimple(MergeOptions options)
    {
        UnityEngine.Debug.Log("Starting simplified video merge for debugging...");
        ReportProgress(0, "Testing FFmpeg installation");
        
        // First test FFmpeg
        if (!TestFFmpegInstallation(options.FFmpegExecutablePath))
        {
            string errorMsg = "FFmpeg installation test failed";
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // Scan for video segments
        List<VideoSegmentInfo> videoSegments = new List<VideoSegmentInfo>();
        if (Directory.Exists(options.InputDirectoryPath))
        {
            ReportProgress(0.1f, "Scanning for video segments");
            videoSegments = ScanForVideoSegments(options.InputDirectoryPath);
        }

        if (videoSegments.Count == 0)
        {
            string errorMsg = "No video segments found for simple merge test";
            ReportProgress(0, "Error", "", false, true, errorMsg);
            return false;
        }

        // Create temporary directory
        string tempDir = Path.Combine(Path.GetTempPath(), $"ffmpeg_simple_test_{System.Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            ReportProgress(0.3f, "Creating simple concatenation");
            
            // Simple concatenation without complex filters
            string segmentsListPath = Path.Combine(tempDir, "segments_list.txt");
            StringBuilder listContent = new StringBuilder();
            
            foreach (var segment in videoSegments)
            {
                if (!string.IsNullOrEmpty(segment.FilePath))
                {
                    string formattedPath = segment.FilePath.Replace('\\', '/');
                    listContent.AppendLine($"file '{formattedPath}'");
                }
            }
            File.WriteAllText(segmentsListPath, listContent.ToString());

            // Simple concat without re-encoding
            string outputPath = options.OutputFilePath.Replace('\\', '/');
            string arguments = string.Format(
                "-y -f concat -safe 0 -i \"{0}\" -c copy \"{1}\"",
                segmentsListPath.Replace('\\', '/'),
                outputPath
            );

            ReportProgress(0.5f, "Executing simple merge");
            if (!ExecuteFFmpegCommand(options.FFmpegExecutablePath, arguments, tempDir, out string output, out string error))
            {
                UnityEngine.Debug.LogError("Simple merge failed");
                return false;
            }

            ReportProgress(1.0f, "Simple merge complete", options.OutputFilePath, true);
            return true;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to cleanup temporary directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Debug method to test filter_complex syntax without actually processing
    /// </summary>
    public static string GenerateFilterComplexDebug(MergeOptions options)
    {
        List<string> inputs = new List<string>();
        StringBuilder filterComplex = new StringBuilder();
        List<string> videoStreams = new List<string>();
        List<string> audioStreams = new List<string>();
        int inputIndex = 0;

        // Helper method to simulate adding inputs
        void AddInputDebug(string? videoPath, string type, float volume = 1.0f)
        {
            if (string.IsNullOrEmpty(videoPath)) return;

            inputs.Add($"-i \"{videoPath}\"");

            // Parse target resolution into width and height
            string[] resolutionParts = options.TargetResolution.Split('x');
            string targetWidth = resolutionParts.Length > 0 ? resolutionParts[0] : "1920";
            string targetHeight = resolutionParts.Length > 1 ? resolutionParts[1] : "1080";

            // Video processing with corrected pad syntax
            string vIn = $"[{inputIndex}:v]";
            string vOut = $"[v{inputIndex}]";
            filterComplex.Append($"{vIn}scale={targetWidth}:{targetHeight}:force_original_aspect_ratio=decrease,pad={targetWidth}:{targetHeight}:(ow-iw)/2:(oh-ih)/2{vOut};");
            videoStreams.Add(vOut);

            // Audio processing
            string aIn = $"[{inputIndex}:a]";
            string aOut = $"[a{inputIndex}]";
            
            if (!Mathf.Approximately(volume, 1.0f))
            {
                filterComplex.Append($"{aIn}volume={volume:F1},aresample={options.TargetAudioSampleRate}{aOut};");
            }
            else
            {
                filterComplex.Append($"{aIn}aresample={options.TargetAudioSampleRate}{aOut};");
            }
            audioStreams.Add(aOut);

            inputIndex++;
        }

        // Add all inputs
        AddInputDebug(options.IntroVideoPath, "intro", options.IntroOutroVolume);
        AddInputDebug("MAIN_CONTENT_PATH", "main");
        AddInputDebug(options.OutroVideoPath, "outro", options.IntroOutroVolume);

        // Build concatenation filters
        if (videoStreams.Count > 1)
        {
            // Concatenate video streams
            for (int i = 0; i < videoStreams.Count; i++)
                filterComplex.Append(videoStreams[i]);
            filterComplex.Append($"concat=n={videoStreams.Count}:v=1:a=0[outv];");

            // Concatenate audio streams
            for (int i = 0; i < audioStreams.Count; i++)
                filterComplex.Append(audioStreams[i]);
            filterComplex.Append($"concat=n={audioStreams.Count}:v=0:a=1[outa]");
        }
        else
        {
            // Single input, just rename the streams
            filterComplex.Append($"{videoStreams[0]}copy[outv];{audioStreams[0]}acopy[outa]");
        }

        string debugOutput = $"Inputs: {string.Join(" ", inputs)}\n";
        debugOutput += $"Filter Complex: {filterComplex.ToString().TrimEnd(';')}\n";
        debugOutput += $"Video Streams: {string.Join(", ", videoStreams)}\n";
        debugOutput += $"Audio Streams: {string.Join(", ", audioStreams)}";
        
        return debugOutput;
    }
} 