#nullable enable
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
using ShowRunner;

// Add new using statement for TranscriptTranslator if it's in a different namespace
// Assuming TranscriptTranslator is in the global namespace or a commonly accessible one.
// If not, we'll need: using YourNamespace;

// --- JSON Data Structures ---
// These classes represent the structure of the episode JSON file.
// Adjust them if your JSON structure differs significantly.

[Serializable]
public class DialogueLine
{
    public string? actor;
    public string? line;
    public string? action; 
}

[Serializable]
public class Scene
{
    public string? location; 
    public string? description;
    public string? @in; // Use @ to allow 'in' as a variable name
    public string? @out; // Use @ to allow 'out' as a variable name
    public Dictionary<string, string>? cast;
    public List<DialogueLine>? dialogue;
}

[Serializable]
public class Episode
{
    public string? id;
    public string? name;
    public string? premise;
    public string? summary;
    public List<Scene>? scenes;
}

[Serializable]
public class ShowConfig // Simplified for transcript generation
{
     public string? id;
     // Add other config fields if needed later
}


[Serializable]
public class RootEpisodeData
{
    public ShowConfig? config;
    public List<Episode>? episodes;
}

// --- Transcript Generator ---

public class YouTubeTranscriptGenerator : MonoBehaviour
{
    // New fields for translation
    public TranscriptTranslator? transcriptTranslator; // Assign in Inspector
    public List<string> targetTranslationLanguages = new List<string> { "Chinese (Simplified)", "Korean" }; // Default, editable in Inspector
    public string translationCustomInstructions = "Ensure the translation is natural and fluent. Prioritize accuracy for technical terms if present.";

    // Placeholder text and links for the transcript header
    private const string HEADER_TEMPLATE = @"AI generated daily updates from the ai16z GitHub highlighting contributions and development updates

Date Generated: {0}

https://github.com/elizaOS/eliza
https://x.com/ai16zdao
"; // Removed --- TRANSCRIPT --- from here

    /// <summary>
    /// Generates a YouTube transcript for a specific episode from a JSON file.
    /// </summary>
    /// <param name="jsonFilePathRelativeOrAbsolute">Relative or absolute path to the episode JSON file (e.g., Assets/Resources/Episodes/show.json).</param>
    /// <param name="episodeIdToGenerate">The ID of the episode to generate the transcript for (e.g., "S1E1").</param>
    public void GenerateTranscript(string jsonFilePathRelativeOrAbsolute, string episodeIdToGenerate)
    {
        string fullJsonPath;
        if (Path.IsPathRooted(jsonFilePathRelativeOrAbsolute))
        {
            fullJsonPath = jsonFilePathRelativeOrAbsolute;
        }
        else
        {
            // Assumes relative to project root like "Assets/Resources/Episodes/show.json"
            fullJsonPath = Path.Combine(Application.dataPath, "..", jsonFilePathRelativeOrAbsolute);
        }

        // New: Define base path for the specific episode's assets
        string baseEpisodePath = Path.Combine(Application.dataPath, "Resources", "Episodes", episodeIdToGenerate);
        // New: Define the output directory for transcripts within the episode's folder
        string outputDirectory = Path.Combine(baseEpisodePath, "transcript");
        string showId = "unknown_show"; // Default value

        try
        {
            if (!File.Exists(fullJsonPath))
            {
                Debug.LogError($"JSON file not found at path: {fullJsonPath}");
                return;
            }

            string jsonContent = File.ReadAllText(fullJsonPath);
            RootEpisodeData? rootData = JsonConvert.DeserializeObject<RootEpisodeData>(jsonContent);

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


            Episode? targetEpisode = rootData.episodes.FirstOrDefault(ep => ep.id == episodeIdToGenerate);

            if (targetEpisode == null)
            {
                //Debug.LogError($"Episode with ID '{episodeIdToGenerate}' not found in the JSON file.");
                return;
            }

            // Ensure the new output directory exists
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
            // New: Full path to the output file in the new directory
            string fullOutputFilePath = Path.Combine(outputDirectory, outputFileName);
            // New: Relative path for AssetDatabase import
            string relativePathForImport = Path.Combine("Resources", "Episodes", episodeIdToGenerate, "transcript", outputFileName);


            StringBuilder transcriptBuilder = new StringBuilder();

            // Add Header
            transcriptBuilder.AppendLine(string.Format(HEADER_TEMPLATE, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            // Add START_TRANSCRIPT marker
            transcriptBuilder.AppendLine(); // Blank line for separation
            transcriptBuilder.AppendLine("START_TRANSCRIPT");
            transcriptBuilder.AppendLine(); // Blank line for separation

            // Add Dialogue - Simplified Formatting
            if (targetEpisode.scenes != null)
            {
                foreach (Scene scene in targetEpisode.scenes)
                {
                    // transcriptBuilder.AppendLine($"--- Scene: {scene.description} ({scene.location}) ---"); // Removed scene header
                    if (scene.dialogue != null)
                    {
                        foreach (DialogueLine dialogue in scene.dialogue)
                        {
                            // Skip 'tv' actor lines meant for image display
                            if (dialogue.actor?.ToLower() == "tv") continue;

                            transcriptBuilder.AppendLine($"{dialogue.actor}: {dialogue.line}");
                        }
                    }
                    // transcriptBuilder.AppendLine(); // Removed blank line between scenes
                }
            }

            // Add END_TRANSCRIPT marker
            transcriptBuilder.AppendLine(); // Blank line for separation
            transcriptBuilder.AppendLine("END_TRANSCRIPT");

            // Write to file
            string transcriptContent = transcriptBuilder.ToString();
            File.WriteAllText(fullOutputFilePath, transcriptContent);

            Debug.Log($"Successfully generated transcript for episode '{episodeIdToGenerate}' at: Assets/{relativePathForImport}");

            // FIRE EVENT for original transcript completion
            // Ensure OriginalTranscriptData struct and EventManager.OnOriginalTranscriptGenerated are defined elsewhere (e.g., in EventManager.cs)
            OriginalTranscriptData originalTranscriptEventData = new OriginalTranscriptData
            {
                EpisodeId = targetEpisode.id!, // or episodeIdToGenerate, should be the same
                OriginalTranscriptFilePath = fullOutputFilePath,
                EpisodeJsonFilePath = fullJsonPath // This is the path to the main show JSON that was read
            };
            EventManager.RaiseOriginalTranscriptGenerated(originalTranscriptEventData); // Assuming a static method for raising the event
            // If EventManager is an instance or events are invoked directly:
            // EventManager.Instance?.OnOriginalTranscriptGenerated?.Invoke(originalTranscriptEventData);
            // or simply: EventManager.OnOriginalTranscriptGenerated?.Invoke(originalTranscriptEventData);
            Debug.Log($"[YouTubeTranscriptGenerator] Fired OnOriginalTranscriptGenerated event for {targetEpisode.id}");

            #if UNITY_EDITOR
            // Import or refresh the asset in the Unity Editor so it appears
            UnityEditor.AssetDatabase.ImportAsset(Path.Combine("Assets", relativePathForImport));
            // UnityEditor.AssetDatabase.Refresh(); // Refresh can be done once after all translations if needed
            #endif

            // New: Translate the generated transcript if translator is available
            if (transcriptTranslator != null && targetTranslationLanguages != null && targetTranslationLanguages.Count > 0)
            {
                // string originalTranscriptContent = transcriptBuilder.ToString(); // Use the content we just built
                // Or read from file if preferred: string originalTranscriptContent = File.ReadAllText(fullOutputFilePath);
                string originalTranscriptContent = transcriptContent; // Use the already stringified content

                foreach (string targetLanguage in targetTranslationLanguages)
                {
                    if (string.IsNullOrEmpty(targetLanguage)) continue;

                    // Make a local copy for the lambda
                    string currentTargetLanguage = targetLanguage; 
                    string? currentEpisodeId = targetEpisode.id;
                    string currentShowId = showId;
                    string currentOutputDirectory = outputDirectory;
                    string currentBaseEpisodeAssetPath = Path.Combine("Resources", "Episodes", episodeIdToGenerate, "transcript"); // Renamed for clarity
                    string currentFullJsonPath = fullJsonPath; // Capture fullJsonPath for the event

                    Debug.Log($"Attempting to translate episode '{currentEpisodeId}' to '{currentTargetLanguage}'.");

                    transcriptTranslator.CallTranslateText(
                        originalTranscriptContent,
                        currentTargetLanguage,
                        translationCustomInstructions,
                        (translatedText, error, success) =>
                        {
                            if (success && !string.IsNullOrEmpty(translatedText))
                            {
                                string langCode = GetLanguageCode(currentTargetLanguage);
                                string translatedFileName = $"{currentShowId}_{currentEpisodeId}_youtubetranscript_{langCode}.txt";
                                string translatedFilePath = Path.Combine(currentOutputDirectory, translatedFileName);
                                
                                try
                                {
                                    File.WriteAllText(translatedFilePath, translatedText);
                                    string relativeTranslatedPathForImport = Path.Combine(currentBaseEpisodeAssetPath, translatedFileName);
                                    Debug.Log($"Successfully translated and saved transcript for episode '{currentEpisodeId}' to '{currentTargetLanguage}' ({langCode}) at: Assets/{relativeTranslatedPathForImport}");

                                    // FIRE EVENT for translated transcript completion
                                    TranslatedTranscriptData translatedTranscriptEventData = new TranslatedTranscriptData
                                    {
                                        EpisodeId = currentEpisodeId,
                                        TranslatedTranscriptFilePath = translatedFilePath,
                                        EpisodeJsonFilePath = currentFullJsonPath, // Use the captured fullJsonPath
                                        LanguageName = currentTargetLanguage,
                                        LanguageCode = langCode
                                    };
                                    EventManager.RaiseTranslatedTranscriptGenerated(translatedTranscriptEventData);
                                    Debug.Log($"[YouTubeTranscriptGenerator] Fired OnTranslatedTranscriptGenerated for {currentEpisodeId} - {langCode}");

                                    #if UNITY_EDITOR
                                    UnityEditor.AssetDatabase.ImportAsset(Path.Combine("Assets", relativeTranslatedPathForImport));
                                    #endif
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"Error writing translated file for episode '{currentEpisodeId}' to '{currentTargetLanguage}': {ex.Message}");
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to translate episode '{currentEpisodeId}' to '{currentTargetLanguage}'. Error: {error}");
                            }
                        }
                    );
                }
                #if UNITY_EDITOR
                // Perform a single refresh after initiating all translations
                UnityEditor.AssetDatabase.Refresh();
                #endif
            }
            else if (transcriptTranslator == null)
            {
                Debug.LogWarning("TranscriptTranslator not assigned in YouTubeTranscriptGenerator. Skipping translations.");
            }

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
        //Debug.Log("YouTubeTranscriptGenerator subscribing to EventManager.OnEpisodeComplete.", this);
        EventManager.OnEpisodeComplete += HandleEpisodeCompletion;
    }

    private void OnDisable()
    {
        //Debug.Log("YouTubeTranscriptGenerator unsubscribing from EventManager.OnEpisodeComplete.", this);
        EventManager.OnEpisodeComplete -= HandleEpisodeCompletion;
    }

    /// <summary>
    /// Handles the event fired when an episode completes.
    /// </summary>
    private void HandleEpisodeCompletion(EpisodeCompletionData completionData)
    {
        Debug.Log($"Received OnEpisodeComplete event for {completionData.EpisodeId}. Generating transcript.", this);
        // Call the existing generation method with data from the event
        // Ensure completionData.JsonFilePath is absolute or correctly handled by GenerateTranscript
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

    // Helper to get language codes for filenames
    private static readonly Dictionary<string, string> LanguageToCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Add common languages and ensure user-requested ones are present
        { "English", "en" },
        { "Spanish", "es" },
        { "French", "fr" },
        { "German", "de" },
        { "Japanese", "ja" },
        { "Korean", "ko" }, // User specified _ko
        { "Chinese (Simplified)", "ch" }, // User specified _ch
        { "Chinese Traditional", "zh-TW" }, // Example
        { "Italian", "it" },
        { "Portuguese", "pt" },
        { "Russian", "ru" },
        { "Arabic", "ar" },
        { "Hindi", "hi" },
        { "Dutch", "nl" },
        { "Swedish", "sv" },
        { "Norwegian", "no" },
        { "Danish", "da" },
        { "Finnish", "fi" }
        // Can be expanded based on _supportedLanguages in TranscriptTranslatorInspector
    };

    public string GetLanguageCode(string languageName)
    {
        if (string.IsNullOrEmpty(languageName)) return "unk";

        if (LanguageToCodeMap.TryGetValue(languageName, out string? code))
        {
            return code;
        }

        // Fallback for unmapped languages: e.g., first 2 letters.
        // This can be made more robust if needed.
        string langPart = languageName.Split(' ')[0]; // "Chinese" from "Chinese (Simplified)"
        string potentialCode = new string(langPart.Where(char.IsLetter).Take(2).ToArray()).ToLowerInvariant();
        Debug.LogWarning($"Language code not found for '{languageName}'. Using fallback code: '{potentialCode}'. Consider adding to LanguageToCodeMap.");
        return potentialCode;
    }
} 