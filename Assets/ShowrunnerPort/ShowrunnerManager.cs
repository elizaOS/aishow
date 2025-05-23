using UnityEngine;
using ShowGenerator;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json; // For deep copy
using System.IO; // For File and Directory operations
using System.Text.RegularExpressions; // For parsing episode IDs
using System.Linq; // For OrderByDescending

public class ShowrunnerManager : MonoBehaviour
{
    [Header("Showrunner Components")]
    // Unified ShowConfig reference
    [Tooltip("The active show configuration being worked on. This will be used as a template for new generations.")]
    public ShowGenerator.ShowConfig ActiveShowConfig;
    public ShowrunnerGeneratorLLM generatorLLM;
    public ShowrunnerLoader loader; // Consider if this is still needed or if its functionality is elsewhere
    public ShowrunnerSpeaker speaker; // This seems unused given the focus on ElevenLabs
    public ShowrunnerSpeakerElevenLabs speakerElevenLabs;

    [Header("Showrunner Workflow Settings")]
    [Tooltip("If enabled, audio will be generated automatically after each episode is generated.")]
    public bool autoGenerateAudioAfterEpisode = false;

    [Header("API Configuration")]
    public ShowGenerator.ShowGeneratorApiKeys apiKeysConfig;

    [Tooltip("If true, uses the wrapper URLs defined in ApiKeysConfig. If false, uses direct API calls with API keys.")]
    public bool useWrapperEndpoints = true;

    // This list stores episodes generated during the current session, 
    // potentially for workflows that don't immediately save to ActiveShowConfig.episodes
    // or for UI display of "newly generated" items.
    public List<ShowEpisode> generatedEpisodesThisSession = new List<ShowEpisode>();

    // To temporarily hold the result of a generation before it's explicitly saved by the user.
    public ShowGenerator.ShowConfig LastGeneratedShowConfigCopy { get; private set; } 

    // Web app default voice mapping
    public static readonly Dictionary<string, string> DefaultVoiceMap = new Dictionary<string, string>
    {
        { "tv", "bqqaVfhKL1ieRzT6RneQ" },
        { "pepo", "PIx0FtBPXNpVzgTVfpYH" },
        { "sparty", "QFbF1ji5Znc2PzerwcaH" },
        { "eliza", "c0vk1lVZg53AttdoaYki" },
        { "marc", "v8BnZUxdzXDlja6wr0Ou" },
        { "shaw", "gYOKECHBoqupz2yMhZp1" }
    };

    private ShowGenerator.ShowConfig DeepCopyShowConfig(ShowGenerator.ShowConfig source)
    {
        if (source == null)
        {
            Debug.LogWarning("[DeepCopyShowConfig] Source config is null. Returning null.");
            return null;
        }
        string serialized = JsonConvert.SerializeObject(source, Formatting.None, 
                            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        
        Debug.Log($"[DeepCopyShowConfig] Serialized source: {serialized.Substring(0, Mathf.Min(serialized.Length, 500))}..."); // Log preview

        var deserializedCopy = JsonConvert.DeserializeObject<ShowGenerator.ShowConfig>(serialized);
        if (deserializedCopy == null)
        {
            Debug.LogError("[DeepCopyShowConfig] Deserialization resulted in a null object!");
        }
        else
        {
            Debug.Log($"[DeepCopyShowConfig] Successfully deserialized. Copy Name: {deserializedCopy.name}");
        }
        return deserializedCopy;
    }

    private void OnValidate()
    {
        if (ActiveShowConfig != null && (ActiveShowConfig.actorVoiceMap == null || ActiveShowConfig.actorVoiceMap.Count == 0))
        {
            PrepopulateVoiceMap();
        }
    }

    public void PrepopulateVoiceMap()
    {
        if (ActiveShowConfig == null) return;
        if (ActiveShowConfig.actorVoiceMap == null)
            ActiveShowConfig.actorVoiceMap = new Dictionary<string, string>();
        foreach (var kvp in DefaultVoiceMap)
        {
            if (!ActiveShowConfig.actorVoiceMap.ContainsKey(kvp.Key))
                ActiveShowConfig.actorVoiceMap[kvp.Key] = kvp.Value;
        }
    }

    [ContextMenu("Reapply Web App Default Voice Map")]
    public void ReapplyDefaultVoiceMap()
    {
        PrepopulateVoiceMap();
        Debug.Log("Voice map prepopulated with web app defaults on ActiveShowConfig.");
    }

    private string GetNextEpisodeId(ShowGenerator.ShowConfig config)
    {
        int currentSeason = 1;
        int latestEpisodeNumber = -1; // Start at -1 so E0 becomes E1 after increment if pilot is S1E0

        List<ShowEpisode> allEpisodes = new List<ShowEpisode>();
        if (config.pilot != null)
        {
            allEpisodes.Add(config.pilot);
        }
        if (config.episodes != null)
        {
            allEpisodes.AddRange(config.episodes);
        }

        if (allEpisodes.Any())
        {
            foreach (var ep in allEpisodes)
            {
                if (string.IsNullOrEmpty(ep.id)) continue;
                Match match = Regex.Match(ep.id, @"S(\d+)E(\d+)");
                if (match.Success)
                {
                    int season = int.Parse(match.Groups[1].Value);
                    int episode = int.Parse(match.Groups[2].Value);
                    
                    if (season > currentSeason) // Prioritize highest season found
                    {
                        currentSeason = season;
                        latestEpisodeNumber = episode;
                    }
                    else if (season == currentSeason && episode > latestEpisodeNumber)
                    {
                        latestEpisodeNumber = episode;
                    }
                }
            }
        }
        // If pilot was S1E0, latestEpisodeNumber is 0. Increment to 1.
        // If last ep was S1E5, latestEpisodeNumber is 5. Increment to 6.
        // If no episodes or pilot, latestEpisodeNumber remains -1, increments to 0 (S1E0).
        // However, we generally want the first *generated* episode to be E1 if pilot is E0.
        // If latestEpisodeNumber remains -1 (no valid IDs found), start with S1E1.
        if (latestEpisodeNumber == -1 && allEpisodes.Any(ep => ep.id == "S1E0")) // Specifically if pilot is S1E0
        {
             latestEpisodeNumber = 0; // So it becomes S1E1
        }
        else if (latestEpisodeNumber == -1) // No episodes at all, or no parseable IDs
        {
            latestEpisodeNumber = 0; // Will become S1E1 (or S{currentSeason}E1 if currentSeason was updated by a different format)
                                     // To be safe, if no valid IDs, let's force S1E1.
            currentSeason = 1;
            latestEpisodeNumber = 0; // Will be incremented to 1
        }


        return $"S{currentSeason}E{latestEpisodeNumber + 1}";
    }

    [ContextMenu("Generate Episode (LLM) - Non-destructive, creates copy")]
    public async void GenerateEpisodeContextNonDestructive() 
    {
        if (generatorLLM != null && ActiveShowConfig != null && apiKeysConfig != null)
        {
            Debug.Log($"Attempting to generate episode using ActiveShowConfig as template: {ActiveShowConfig.name}. Using wrapper: {useWrapperEndpoints}");
            ShowEpisode newEpisode = await generatorLLM.GenerateEpisode(ActiveShowConfig, apiKeysConfig, useWrapperEndpoints);

            if (newEpisode != null)
            {
                // Type of LastGeneratedShowConfigCopy will be ShowGenerator.ShowConfig due to DeepCopyShowConfig return type
                LastGeneratedShowConfigCopy = DeepCopyShowConfig(ActiveShowConfig);
                if (LastGeneratedShowConfigCopy != null) // Check if copy was successful
                {
                    if (LastGeneratedShowConfigCopy.episodes == null) LastGeneratedShowConfigCopy.episodes = new List<ShowEpisode>();
                    
                    // Assign a new ID to the generated episode
                    newEpisode.id = GetNextEpisodeId(ActiveShowConfig); // Get ID based on ActiveShowConfig's state
                                                                        // It's important to get this *before* adding it to any list if the list is used by GetNextEpisodeId

                    LastGeneratedShowConfigCopy.episodes.Add(newEpisode);
                    Debug.Log($"Generated Episode: {newEpisode.name} (ID: {newEpisode.id}). A new ShowConfig copy has been prepared with this episode.");
                    
                    // Save the LastGeneratedShowConfigCopy (which now contains the new episode) to disk
                    SaveShowConfigToFile(LastGeneratedShowConfigCopy, newEpisode.id);

                    if (autoGenerateAudioAfterEpisode && speakerElevenLabs != null && LastGeneratedShowConfigCopy.episodes.Count > 0)
                    {
                        await speakerElevenLabs.GenerateAndSaveAudioForEpisode(
                            LastGeneratedShowConfigCopy.episodes[LastGeneratedShowConfigCopy.episodes.Count - 1],
                            LastGeneratedShowConfigCopy.actorVoiceMap,
                            apiKeysConfig,
                            useWrapperEndpoints,
                            System.Threading.CancellationToken.None);
                    }
                }
                else
                {
                     Debug.LogError("Failed to create a deep copy of ActiveShowConfig during ContextMenu generation.");
                }
            }
            else { Debug.LogError("Failed to generate episode (ContextMenu)."); }
        }
        else { Debug.LogWarning("LLM Generator, ActiveShowConfig, or ApiKeysConfig not set."); }
    }

    [ContextMenu("Generate Audio for Last Episode - Uses ActiveShowConfig")]
    public async Task GenerateAudioForLastEpisodeContext() 
    {
        if (speakerElevenLabs != null && ActiveShowConfig != null && ActiveShowConfig.episodes != null && ActiveShowConfig.episodes.Count > 0 && apiKeysConfig != null)
        {
            var lastEp = ActiveShowConfig.episodes[ActiveShowConfig.episodes.Count - 1];
            Debug.Log($"Attempting to generate audio for episode: {lastEp.name} from ActiveShowConfig. Using wrapper endpoints: {useWrapperEndpoints}");
            await speakerElevenLabs.GenerateAndSaveAudioForEpisode(lastEp, ActiveShowConfig.actorVoiceMap, apiKeysConfig, useWrapperEndpoints, System.Threading.CancellationToken.None);
        }
        else
        {
            Debug.LogWarning("[ShowrunnerManager ContextMenu] ElevenLabs Speaker, ActiveShowConfig, episodes, or ApiKeysConfig not set/available.");
        }
    }
    
    private void SaveShowConfigToFile(ShowGenerator.ShowConfig configToSave, string episodeIdForFilename)
    {
        if (configToSave == null)
        {
            Debug.LogError("[SaveShowConfigToFile] Config to save is null.");
            return;
        }
        if (string.IsNullOrEmpty(configToSave.id))
        {
            Debug.LogError("[SaveShowConfigToFile] ShowConfig ID is null or empty, cannot determine filename prefix.");
            return;
        }
        if (string.IsNullOrEmpty(episodeIdForFilename))
        {
            Debug.LogError("[SaveShowConfigToFile] Episode ID for filename is null or empty.");
            // Fallback or default naming could be added here if desired
            episodeIdForFilename = "UnknownEpisode";
        }

        try
        {
            string directoryPath = Path.Combine(Application.dataPath, "Resources", "Episodes");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"[SaveShowConfigToFile] Created directory: {directoryPath}");
            }

            // Filename: ShowID_EpisodeID.json (e.g., aipodcast_S1E1.json)
            // This saves the *entire ShowConfig* containing the new episode.
            string sanitizedShowName = configToSave.name.Replace(" ", "_"); // New: Sanitize name
            sanitizedShowName = Regex.Replace(sanitizedShowName, @"[^a-zA-Z0-9_]", ""); // New: Remove invalid chars
            string filename = $"{sanitizedShowName}_{episodeIdForFilename}.json"; // New: Use sanitized name
            string filePath = Path.Combine(directoryPath, filename);

            // Use the loader's SaveToJson, which now wraps with 'config'
            ShowGeneratorConfigLoader.SaveToJson(configToSave, filePath);
            Debug.Log($"[SaveShowConfigToFile] Successfully saved ShowConfig with new episode to: {filePath}");
            
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveShowConfigToFile] Error saving ShowConfig to file: {e.Message}");
        }
    }

    // Methods called from ShowrunnerManagerEditor UI buttons
    public async Task<bool> GenerateEpisodeFromEditor()
    {
        if (ActiveShowConfig == null) { Debug.LogError("ActiveShowConfig is null before generation."); return false; }
        Debug.Log($"[GenerateEpisodeFromEditor] ActiveShowConfig Name before copy: {ActiveShowConfig.name}, Episode Count: {ActiveShowConfig.episodes?.Count ?? 0}");
        
        LastGeneratedShowConfigCopy = null; 
        Debug.Log($"[ShowrunnerManager] Attempting to call LLM.GenerateEpisode. Use Wrapper: {useWrapperEndpoints}, API Key Config Valid: {apiKeysConfig != null}");
        ShowEpisode newEpisode = await generatorLLM.GenerateEpisode(ActiveShowConfig, apiKeysConfig, useWrapperEndpoints);

        if (newEpisode != null)
        {
            LastGeneratedShowConfigCopy = DeepCopyShowConfig(ActiveShowConfig);
            if (LastGeneratedShowConfigCopy == null)
            {
                Debug.LogError("Failed to create a deep copy of ActiveShowConfig. LastGeneratedShowConfigCopy is null.");
                return false;
            }
            Debug.Log($"[GenerateEpisodeFromEditor] LastGeneratedShowConfigCopy Name after copy: {LastGeneratedShowConfigCopy.name}, Episode Count: {LastGeneratedShowConfigCopy.episodes?.Count ?? 0}");

            // Assign a new ID to the generated episode
            newEpisode.id = GetNextEpisodeId(ActiveShowConfig); // Get ID based on ActiveShowConfig's state

            // Ensure episodes list in the copy contains ONLY the new episode
            LastGeneratedShowConfigCopy.episodes = new List<ShowEpisode> { newEpisode };
            Debug.Log($"[GenerateEpisodeFromEditor] Set new episode. LastGeneratedShowConfigCopy Episode Count: {LastGeneratedShowConfigCopy.episodes.Count}, New Episode Name: {newEpisode.name}, New ID: {newEpisode.id}");
            
            Debug.Log($"Episode Generation Successful: {newEpisode.name}. A new ShowConfig copy has been prepared.");
            
            // Automatically save this new ShowConfig copy (which includes the new episode) to disk
            SaveShowConfigToFile(LastGeneratedShowConfigCopy, newEpisode.id);

            if (autoGenerateAudioAfterEpisode && speakerElevenLabs != null && LastGeneratedShowConfigCopy.episodes.Count > 0)
            {
                // Generate audio for the newly generated episode within the copy
                await speakerElevenLabs.GenerateAndSaveAudioForEpisode(
                    LastGeneratedShowConfigCopy.episodes[0], // It's the only episode in this list 
                    LastGeneratedShowConfigCopy.actorVoiceMap, 
                    apiKeysConfig, 
                    useWrapperEndpoints,
                    System.Threading.CancellationToken.None);
            }
            return true; // Success
        }
        else
        {
            Debug.LogError("Failed to generate episode (LLM returned null).");
            LastGeneratedShowConfigCopy = null; 
            return false; // Failure
        }
    }

    public async Task<bool> GenerateAudioForLastEpisodeFromEditor(System.Threading.CancellationToken cancellationToken)
    {
        Debug.Log("[ShowrunnerManager] Entered GenerateAudioForLastEpisodeFromEditor");
        Debug.Log($"ActiveShowConfig: {(ActiveShowConfig != null ? ActiveShowConfig.name : "null")}, Episodes: {ActiveShowConfig?.episodes?.Count ?? 0}");
        ShowEpisode episodeToProcess = null; // ADDED: Declare variable
        Dictionary<string, string> voiceMapToUse = null; // ADDED: Declare variable

        if (ActiveShowConfig != null && ActiveShowConfig.episodes != null && ActiveShowConfig.episodes.Count > 0)
        {
            episodeToProcess = ActiveShowConfig.episodes[ActiveShowConfig.episodes.Count - 1];
            voiceMapToUse = ActiveShowConfig.actorVoiceMap;
            Debug.Log($"[DEBUG] Selected episode: {episodeToProcess?.name}, VoiceMap count: {voiceMapToUse?.Count ?? 0}");
        }
        else
        {
            Debug.LogError("[DEBUG] No episodes available in 'ActiveShowConfig.episodes' to generate audio for.");
            return false;
        }
        
        if (episodeToProcess != null && voiceMapToUse != null && speakerElevenLabs != null && apiKeysConfig != null)
        {
            Debug.Log("[DEBUG] Calling GenerateAndSaveAudioForEpisode...");
            await speakerElevenLabs.GenerateAndSaveAudioForEpisode(episodeToProcess, voiceMapToUse, apiKeysConfig, useWrapperEndpoints, cancellationToken);
            return true; 
        }
        Debug.LogError($"[DEBUG] Failed to generate audio: missing components or episode data. episodeToProcess: {(episodeToProcess != null ? episodeToProcess.name : "null")}, voiceMapToUse: {(voiceMapToUse != null ? voiceMapToUse.Count.ToString() : "null")}, speakerElevenLabs: {(speakerElevenLabs != null ? "set" : "null")}, apiKeysConfig: {(apiKeysConfig != null ? "set" : "null")}");
        return false;
    }

    public void LoadShowConfig(ShowGenerator.ShowConfig configToLoad)
    {
        ActiveShowConfig = configToLoad;
        LastGeneratedShowConfigCopy = null; // Clear any pending generated copy when a new config is loaded.
        generatedEpisodesThisSession.Clear(); 
        if (ActiveShowConfig != null && ActiveShowConfig.episodes != null)
        {
            // If you want generatedEpisodesThisSession to mirror the loaded config's episodes initially:
            // generatedEpisodesThisSession.AddRange(ActiveShowConfig.episodes);
        }
        if (ActiveShowConfig != null)
        {
            Debug.Log($"Loaded ShowConfig into ActiveShowConfig: {ActiveShowConfig.name}");
        }
        else
        {
            Debug.LogError("Attempted to load a null ShowConfig into ActiveShowConfig.");
        }
    }

    public void ClearLastGeneratedShowConfigCopy()
    {
        LastGeneratedShowConfigCopy = null;
        Debug.Log("LastGeneratedShowConfigCopy has been cleared.");
    }

    public async Task<string> PingLLMEndpointAsync()
    {
        if (generatorLLM == null)
        {
            Debug.LogError("LLM Generator is not assigned.");
            return "Error: LLM Generator not assigned.";
        }
        if (apiKeysConfig == null)
        {
             Debug.LogError("API Keys Config is not assigned.");
            return "Error: API Keys Config not assigned.";
        }
        // Pass the necessary parts from apiKeysConfig for the test
        return await generatorLLM.TestClaudeEndpointAsync(apiKeysConfig.claudeWrapperUrl, apiKeysConfig.llmApiKey, useWrapperEndpoints, apiKeysConfig);
    }
} 