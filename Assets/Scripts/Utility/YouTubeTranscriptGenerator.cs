using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;

// --- JSON Data Structures ---
// These classes represent the structure of the episode JSON file.
// Adjust them if your JSON structure differs significantly.

[Serializable]
public class DialogueLine
{
    public string actor;
    public string line;
    public string action;
}

[Serializable]
public class Scene
{
    public string location;
    public string description;
    public string @in; // Use @ to allow 'in' as a variable name
    public string @out; // Use @ to allow 'out' as a variable name
    public Dictionary<string, string> cast;
    public List<DialogueLine> dialogue;
}

[Serializable]
public class Episode
{
    public string id;
    public string name;
    public string premise;
    public string summary;
    public List<Scene> scenes;
}

[Serializable]
public class ShowConfig // Simplified for transcript generation
{
     public string id;
     // Add other config fields if needed later
}


[Serializable]
public class RootEpisodeData
{
    public ShowConfig config;
    public List<Episode> episodes;
}

// --- Transcript Generator ---

public class YouTubeTranscriptGenerator : MonoBehaviour
{
    // Placeholder text and links for the transcript header
    private const string HEADER_TEMPLATE = @"AI generated daily updates from the ai16z GitHub highlighting contributions and development updates

Date Generated: {0}

https://github.com/elizaOS/eliza
https://x.com/ai16zdao

--- TRANSCRIPT ---
";

    /// <summary>
    /// Generates a YouTube transcript for a specific episode from a JSON file.
    /// </summary>
    /// <param name="jsonFilePathRelative">Relative path to the episode JSON file (e.g., Assets/Resources/Episodes/show.json).</param>
    /// <param name="episodeIdToGenerate">The ID of the episode to generate the transcript for (e.g., "S1E1").</param>
    public void GenerateTranscript(string jsonFilePathRelative, string episodeIdToGenerate)
    {
        string fullJsonPath = Path.Combine(Application.dataPath, "..", jsonFilePathRelative); // Navigate up from Assets and then use relative path
        string outputDirectory = Path.Combine(Application.dataPath, "Resources", "Transcripts");
        string showId = "unknown_show"; // Default value

        try
        {
            if (!File.Exists(fullJsonPath))
            {
                Debug.LogError($"JSON file not found at path: {fullJsonPath}");
                return;
            }

            string jsonContent = File.ReadAllText(fullJsonPath);
            RootEpisodeData rootData = JsonConvert.DeserializeObject<RootEpisodeData>(jsonContent);

            if (rootData == null || rootData.episodes == null)
            {
                Debug.LogError("Failed to parse JSON data or no episodes found.");
                return;
            }

            if (rootData.config != null && !string.IsNullOrEmpty(rootData.config.id))
            {
                 showId = rootData.config.id;
            }
            else
            {
                 Debug.LogWarning("Show ID not found in config, using default.");
            }


            Episode targetEpisode = rootData.episodes.FirstOrDefault(ep => ep.id == episodeIdToGenerate);

            if (targetEpisode == null)
            {
                Debug.LogError($"Episode with ID '{episodeIdToGenerate}' not found in the JSON file.");
                return;
            }

            // Ensure the output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                Debug.Log($"Created directory: {outputDirectory}");
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh(); // Refresh AssetDatabase if in editor
                #endif
            }

            // Construct filename: ShowID_EpisodeID_youtubetranscript.txt
            string outputFileName = $"{showId}_{targetEpisode.id}_youtubetranscript.txt";
            // Output path relative to Assets/Resources/
            string outputFilePathRelative = Path.Combine("Transcripts", outputFileName);
            string fullOutputFilePath = Path.Combine(outputDirectory, outputFileName);


            StringBuilder transcriptBuilder = new StringBuilder();

            // Add Header
            transcriptBuilder.AppendLine(string.Format(HEADER_TEMPLATE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            // Add Dialogue - Simplified Formatting
            foreach (Scene scene in targetEpisode.scenes)
            {
                // transcriptBuilder.AppendLine($"--- Scene: {scene.description} ({scene.location}) ---"); // Removed scene header
                if (scene.dialogue != null)
                {
                    foreach (DialogueLine dialogue in scene.dialogue)
                    {
                        // Skip 'tv' actor lines meant for image display
                        if (dialogue.actor.ToLower() == "tv") continue;

                        transcriptBuilder.AppendLine($"{dialogue.actor}: {dialogue.line}");
                    }
                }
                // transcriptBuilder.AppendLine(); // Removed blank line between scenes
            }

            // Write to file
            File.WriteAllText(fullOutputFilePath, transcriptBuilder.ToString());

            Debug.Log($"Successfully generated transcript for episode '{episodeIdToGenerate}' at: Assets/Resources/{outputFilePathRelative}");

            #if UNITY_EDITOR
            // Import or refresh the asset in the Unity Editor so it appears
            UnityEditor.AssetDatabase.ImportAsset(Path.Combine("Assets", "Resources", outputFilePathRelative));
            UnityEditor.AssetDatabase.Refresh();
            #endif

        }
        catch (JsonException jsonEx)
        {
            Debug.LogError($"Error parsing JSON file '{fullJsonPath}': {jsonEx.Message}");
        }
        catch (IOException ioEx)
        {
            Debug.LogError($"File I/O error: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"An unexpected error occurred: {ex.Message}");
        }
    }

    // --- Event Subscription ---

    private void OnEnable()
    {
        Debug.Log("YouTubeTranscriptGenerator subscribing to EventManager.OnEpisodeComplete.", this);
        EventManager.OnEpisodeComplete += HandleEpisodeCompletion;
    }

    private void OnDisable()
    {
        Debug.Log("YouTubeTranscriptGenerator unsubscribing from EventManager.OnEpisodeComplete.", this);
        EventManager.OnEpisodeComplete -= HandleEpisodeCompletion;
    }

    /// <summary>
    /// Handles the event fired when an episode completes.
    /// </summary>
    private void HandleEpisodeCompletion(EpisodeCompletionData completionData)
    {
        Debug.Log($"Received OnEpisodeComplete event for {completionData.EpisodeId}. Generating transcript.", this);
        // Call the existing generation method with data from the event
        GenerateTranscript(completionData.JsonFilePath, completionData.EpisodeId);
    }

    // --- Original TODO comment (now handled by events) ---
    // TODO: Implement event listener for automatic generation
    // Example:
    // private void OnEnable() { // Subscribe to ShowRunner's episode complete event }
    // private void OnDisable() { // Unsubscribe }
    // private void HandleEpisodeCompleted(string jsonPath, string episodeId)
    // {
    //     GenerateTranscript(jsonPath, episodeId);
    // }
} 