#nullable enable
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Utility class for merging multiple video files into a single file using FFmpeg.
/// </summary>
public static class VideoMerger
{
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
    /// Merges video files from a specified directory that match the pattern "S<Season>E<Episode>_X_Y_new.mp4"
    /// into a single output video file using FFmpeg.
    /// </summary>
    /// <param name="inputDirectoryPath">The path to the directory containing the video files.</param>
    /// <param name="outputFilePath">The path (including filename) for the merged output video.</param>
    /// <param name="ffmpegExecutablePath">The path to the FFmpeg executable. Defaults to "ffmpeg" assuming it's in PATH.</param>
    /// <param name="introVideoPath">Optional. Path to an intro video file to prepend.</param>
    /// <param name="outroVideoPath">Optional. Path to an outro video file to append.</param>
    /// <returns>True if the videos were merged successfully, false otherwise.</returns>
    public static bool MergeVideos(string inputDirectoryPath, string outputFilePath, string ffmpegExecutablePath = "ffmpeg", string? introVideoPath = null, string? outroVideoPath = null)
    {
        UnityEngine.Debug.Log($"Starting video merge. Input: '{inputDirectoryPath}', Output: '{outputFilePath}'");
        if (!string.IsNullOrEmpty(introVideoPath)) UnityEngine.Debug.Log($"Intro video specified: {introVideoPath}");
        if (!string.IsNullOrEmpty(outroVideoPath)) UnityEngine.Debug.Log($"Outro video specified: {outroVideoPath}");

        if (!Directory.Exists(inputDirectoryPath))
        {
            UnityEngine.Debug.LogError($"Input directory does not exist: {inputDirectoryPath}");
            return false;
        }

        // Regex to match files like "S1E55_1_1_new.mp4" or "S1E56_1_2_new.mp4"
        // It captures Season (first number), Episode (second number), PartX (third number), and PartY (fourth number)
        Regex videoFileRegex = new Regex(@"^S(\d+)E(\d+)_(\d+)_(\d+)_new\.mp4$", RegexOptions.IgnoreCase);
        
        List<VideoSegmentInfo> videoSegments = new List<VideoSegmentInfo>();

        try
        {
            string[] files = Directory.GetFiles(inputDirectoryPath, "*.mp4");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                Match match = videoFileRegex.Match(fileName);
                if (match.Success)
                {
                    videoSegments.Add(new VideoSegmentInfo
                    {
                        FilePath = Path.GetFullPath(file), // Use full path
                        FileName = fileName,
                        Season = int.Parse(match.Groups[1].Value),    // Capture Season
                        Episode = int.Parse(match.Groups[2].Value),   // Capture Episode
                        PartX = int.Parse(match.Groups[3].Value),     // Capture PartX (was group 1)
                        PartY = int.Parse(match.Groups[4].Value)      // Capture PartY (was group 2)
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error reading files from directory '{inputDirectoryPath}': {ex.Message}");
            return false;
        }
        

        if (videoSegments.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No video files found matching the pattern 'S<Season>E<Episode>_X_Y_new.mp4' in the specified directory.");
            return false;
        }

        // Sort videos by Season, then Episode, then PartX, then by PartY
        videoSegments.Sort((s1, s2) => 
        {
            int compareSeason = s1.Season.CompareTo(s2.Season);
            if (compareSeason != 0)
            {
                return compareSeason;
            }
            int compareEpisode = s1.Episode.CompareTo(s2.Episode);
            if (compareEpisode != 0)
            {
                return compareEpisode;
            }
            int compareX = s1.PartX.CompareTo(s2.PartX);
            if (compareX != 0)
            {
                return compareX;
            }
            return s1.PartY.CompareTo(s2.PartY);
        });

        UnityEngine.Debug.Log($"Found {videoSegments.Count} video segments to merge. Order:");
        foreach(var segment in videoSegments)
        {
            UnityEngine.Debug.Log($"- {segment.FileName} (S:{segment.Season} E:{segment.Episode} X:{segment.PartX}, Y:{segment.PartY})");
        }

        string tempInputFilePath = Path.Combine(Path.GetTempPath(), "ffmpeg_video_list.txt");
        
        try
        {
            // Create the temporary file list for FFmpeg's concat demuxer
            // Paths in the list file need to use forward slashes and be quoted.
            StringBuilder fileListContent = new StringBuilder();

            // Add intro video if specified and valid
            if (!string.IsNullOrEmpty(introVideoPath))
            {
                if (File.Exists(introVideoPath))
                {
                    string formattedIntroPath = Path.GetFullPath(introVideoPath).Replace('\\', '/');
                    fileListContent.AppendLine($"file '{formattedIntroPath}'");
                    UnityEngine.Debug.Log($"Adding intro video to list: {introVideoPath}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Intro video path specified but file not found: {introVideoPath}. Skipping intro.");
                }
            }

            // Add main video segments
            foreach (var segment in videoSegments)
            {
                // FFmpeg concat demuxer expects 'file' directive and paths with forward slashes.
                // Quoting handles spaces or special characters in paths.
                if (string.IsNullOrEmpty(segment.FilePath))
                {
                    UnityEngine.Debug.LogWarning($"Segment with FileName '{segment.FileName ?? "Unknown"}' has a null or empty FilePath. Skipping.");
                    continue;
                }
                string formattedPath = segment.FilePath.Replace('\\', '/'); // FilePath is now known non-null/non-empty
                fileListContent.AppendLine($"file '{formattedPath}'");
            }

            // Add outro video if specified and valid
            if (!string.IsNullOrEmpty(outroVideoPath))
            {
                if (File.Exists(outroVideoPath))
                {
                    string formattedOutroPath = Path.GetFullPath(outroVideoPath).Replace('\\', '/');
                    fileListContent.AppendLine($"file '{formattedOutroPath}'");
                    UnityEngine.Debug.Log($"Adding outro video to list: {outroVideoPath}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Outro video path specified but file not found: {outroVideoPath}. Skipping outro.");
                }
            }

            File.WriteAllText(tempInputFilePath, fileListContent.ToString());
            UnityEngine.Debug.Log($"Temporary file list created at: {tempInputFilePath}");

            // Ensure output directory exists
             string outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Prepare FFmpeg command
            // -y: overwrite output file without asking
            // -f concat: use the concat demuxer
            // -safe 0: necessary when using absolute paths or paths not relative to the list file
            // -i: input file list
            // -c copy: stream copy, no re-encoding (fast and lossless if formats are compatible)
            string arguments = $"-y -f concat -safe 0 -i \"{tempInputFilePath}\" -c copy \"{outputFilePath}\"";
            
            UnityEngine.Debug.Log($"Executing FFmpeg: {ffmpegExecutablePath} {arguments}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = inputDirectoryPath // Set working directory if ffmpeg needs relative paths for some reason, though absolute paths are used
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, args) => { if(args.Data != null) outputBuilder.AppendLine(args.Data); };
                process.ErrorDataReceived += (sender, args) => { if(args.Data != null) errorBuilder.AppendLine(args.Data); };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // It's generally better to use an async wait or a timeout.
                // For simplicity in this example, a synchronous wait is used.
                // Consider a timeout for production code.
                process.WaitForExit(); 

                string ffmpegOutput = outputBuilder.ToString();
                string ffmpegError = errorBuilder.ToString();

                if (!string.IsNullOrWhiteSpace(ffmpegOutput))
                    UnityEngine.Debug.Log($"FFmpeg Output:\n{ffmpegOutput}");
                
                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log($"Videos merged successfully to: {outputFilePath}");
                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError($"FFmpeg failed with exit code {process.ExitCode}.\nFFmpeg Error Output:\n{ffmpegError}");
                    if (string.IsNullOrWhiteSpace(ffmpegError) && !string.IsNullOrWhiteSpace(ffmpegOutput))
                    {
                         UnityEngine.Debug.LogError($"FFmpeg Error (reported via stdout):\n{ffmpegOutput}");
                    }
                    return false;
                }
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
             UnityEngine.Debug.LogError($"FFmpeg execution error: {ex.Message}. Is FFmpeg installed and in PATH, or is '{ffmpegExecutablePath}' correct?");
             return false;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"An error occurred during video merging: {ex.Message}\nStackTrace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            // Clean up the temporary file list
            if (File.Exists(tempInputFilePath))
            {
                try
                {
                    File.Delete(tempInputFilePath);
                    UnityEngine.Debug.Log($"Temporary file list deleted: {tempInputFilePath}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to delete temporary file '{tempInputFilePath}': {ex.Message}");
                }
            }
        }
    }
}

// Example usage (you would call this from an Editor script or other utility):
//
// public class MyEditorScript : MonoBehaviour
// {
//     [UnityEditor.MenuItem("Tools/Merge Episode Videos S1E55")]
//     public static void MergeS1E55Videos()
//     {
//         string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
//         string inputDir = "C:/Users/dev/Documents/GitHub/aishow/Assets/Resources/Episodes/S1E55/hedra_processing/final_videos"; // Example
//         string outputFile = Path.Combine(desktopPath, "S1E55_Merged_Output.mp4");
//         string introSample = Path.Combine(desktopPath, "sample_intro.mp4"); // Example intro path
//         string outroSample = Path.Combine(desktopPath, "sample_outro.mp4"); // Example outro path
//
//         // Ensure paths are absolute if they come from user input or relative Unity paths
//         // For paths already absolute like above, GetFullPath is idempotent.
//         // If inputDir was, e.g., "Assets/MyVideos", Path.GetFullPath(inputDir) would resolve it.
//         // string absoluteInputDir = Path.GetFullPath(inputDir);
//         // string absoluteOutputFile = Path.GetFullPath(outputFile);
//
//         bool success = VideoMerger.MergeVideos(inputDir, outputFile, "ffmpeg", introSample, outroSample); // Updated call
//         if (success)
//         {
//             UnityEditor.EditorUtility.DisplayDialog("Video Merge", "Videos merged successfully!", "OK");
//         }
//         else
//         {
//             UnityEditor.EditorUtility.DisplayDialog("Video Merge", "Video merging failed. Check console for details.", "OK");
//         }
//     }
// } 