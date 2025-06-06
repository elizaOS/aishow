#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ShowGenerator;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Added for dictionary editing
using System.Threading.Tasks; // Added for async Task
using System;
using Newtonsoft.Json;
using System.Threading;

[CustomEditor(typeof(ShowrunnerManager))]
public class ShowrunnerManagerEditor : Editor
{
    // Remove unused foldout fields
    // private bool showPrompts = true;
    // private bool showActors = false;
    // private bool showLocations = false;
    // private bool showVoiceMap = false;

    private bool isGeneratingEpisode = false;
    private bool isGeneratingAudio = false;
    private double episodeGenerationStartTime;
    private double audioGenerationStartTime;

    // For manual dictionary key editing (currently used by DrawStringStringDictionary)
    // private Dictionary<string, string> tempPromptKeys = new Dictionary<string, string>(); // This seems unused, consider removing if confirmed

    private CancellationTokenSource audioCancelTokenSource = null;

    private bool showBasicInfo = true;
    private bool showPromptsSummary = false;
    private bool showActorsSummary = false;
    private bool showLocationsSummary = false;
    private bool showEpisodesSummary = false;
    private bool showVoiceMapSummary = false; 

    private bool showX23ApiSettings = false; // Added for X23 API section

    private void OnEnable()
    {
        // Reset generation state flags when the editor is enabled
        isGeneratingEpisode = false;
        isGeneratingAudio = false;
        // Reset start times to current time to prevent stale timer values on re-enable
        episodeGenerationStartTime = EditorApplication.timeSinceStartup;
        audioGenerationStartTime = EditorApplication.timeSinceStartup;
        
        EditorApplication.update += RepaintEditorIfBusy; // Renamed for clarity
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintEditorIfBusy;
    }

    // Method to be called by EditorApplication.update
    private void RepaintEditorIfBusy()
    {
        if (isGeneratingEpisode || isGeneratingAudio)
        {
            Repaint(); // Repaint to update the timer display
        }
    }

    public override void OnInspectorGUI()
    {
        ShowrunnerManager mgr = (ShowrunnerManager)target;
        serializedObject.Update();

        // Core Components Section
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "Core Components", true);
        if (showBasicInfo)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ActiveShowConfig"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generatorLLM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loader"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speaker"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speakerElevenLabs"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Workflow Settings Section
        EditorGUILayout.LabelField("Workflow Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoGenerateAudioAfterEpisode"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        // API Configuration Section
        EditorGUILayout.LabelField("API Configuration", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("apiKeysConfig"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useWrapperEndpoints"));
        EditorGUI.indentLevel--;

        // Custom Prompt Affixes Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Prompt Affixes", EditorStyles.boldLabel);
        mgr.useCustomPromptAffixes = EditorGUILayout.Toggle(
            new GUIContent("Use Custom Prompt Affixes", "Enable to add custom prefix and suffix to the LLM prompt."),
            mgr.useCustomPromptAffixes);
        if (mgr.useCustomPromptAffixes)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(new GUIContent("Custom Prompt Prefix", "Text to prepend to the LLM prompt."));
            mgr.customPromptPrefix = EditorGUILayout.TextArea(mgr.customPromptPrefix, GUILayout.Height(EditorGUIUtility.singleLineHeight * 3));
            EditorGUILayout.LabelField(new GUIContent("Custom Prompt Suffix", "Text to append to the LLM prompt."));
            mgr.customPromptSuffix = EditorGUILayout.TextArea(mgr.customPromptSuffix, GUILayout.Height(EditorGUIUtility.singleLineHeight * 3));
            EditorGUI.indentLevel--;
        }

        // X23.ai API Data Injection Section
        EditorGUILayout.Space();
        showX23ApiSettings = EditorGUILayout.Foldout(showX23ApiSettings, "X23.ai API Data Injection", true);
        if (showX23ApiSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            mgr.useX23ApiData = EditorGUILayout.Toggle(
                new GUIContent("Use X23.ai Data", "Enable to fetch and inject data from x23.ai API into the LLM prompt."),
                mgr.useX23ApiData);
            if (mgr.useX23ApiData)
            {
                EditorGUI.indentLevel++;
                mgr.x23ApiRequestType = (X23ApiRequestType)EditorGUILayout.EnumPopup(
                    new GUIContent("X23 API Request Type", "Select the x23.ai endpoint to use."),
                    mgr.x23ApiRequestType);

                EditorGUILayout.LabelField("General Parameters", EditorStyles.boldLabel);
                mgr.x23SearchQuery = EditorGUILayout.TextField(
                    new GUIContent("Search Query", "For Keyword, RAG, Hybrid search."),
                    mgr.x23SearchQuery);
                mgr.x23Limit = EditorGUILayout.IntField(
                    new GUIContent("Limit", "Max items to fetch."),
                    mgr.x23Limit);
                mgr.x23ProtocolsToFilter = EditorGUILayout.TextField(
                    new GUIContent("Protocols (comma-sep)", "e.g., aave,optimism"),
                    mgr.x23ProtocolsToFilter);
                mgr.x23ItemTypesToFilter = EditorGUILayout.TextField(
                    new GUIContent("Item Types (comma-sep)", "e.g., discussion,snapshot"),
                    mgr.x23ItemTypesToFilter);

                switch (mgr.x23ApiRequestType)
                {
                    case X23ApiRequestType.KeywordSearch:
                        EditorGUILayout.LabelField("Keyword Search Specific", EditorStyles.boldLabel);
                        mgr.x23ExactMatchForKeyword = EditorGUILayout.Toggle(
                            new GUIContent("Exact Match", "Keyword search exact match."),
                            mgr.x23ExactMatchForKeyword);
                        mgr.x23SortByRelevanceForKeyword = EditorGUILayout.Toggle(
                            new GUIContent("Sort by Relevance", "Keyword search sort by relevance."),
                            mgr.x23SortByRelevanceForKeyword);
                        break;
                    case X23ApiRequestType.RagSearch:
                    case X23ApiRequestType.HybridSearch:
                        EditorGUILayout.LabelField("Similarity Search Specific", EditorStyles.boldLabel);
                        mgr.x23SimilarityThreshold = EditorGUILayout.Slider(
                            new GUIContent("Similarity Threshold", "For RAG/Hybrid search."),
                            mgr.x23SimilarityThreshold, 0f, 1f);
                        break;
                    case X23ApiRequestType.RecentFeed:
                    case X23ApiRequestType.TopScoredFeed:
                        EditorGUILayout.LabelField("Feed Specific", EditorStyles.boldLabel);
                        mgr.x23UnixTimestamp = EditorGUILayout.LongField(
                            new GUIContent("Unix Timestamp", "0 for default/current."),
                            mgr.x23UnixTimestamp);
                        if (mgr.x23ApiRequestType == X23ApiRequestType.TopScoredFeed)
                        {
                            mgr.x23ScoreThresholdForTopScored = EditorGUILayout.DoubleField(
                                new GUIContent("Score Threshold", "Min score for TopScoredFeed."),
                                mgr.x23ScoreThresholdForTopScored);
                        }
                        break;
                    case X23ApiRequestType.DigestFeed:
                        EditorGUILayout.LabelField("Digest Feed Specific", EditorStyles.boldLabel);
                        mgr.x23UnixTimestamp = EditorGUILayout.LongField(
                            new GUIContent("Unix Timestamp", "0 for default/current."),
                            mgr.x23UnixTimestamp);
                        mgr.x23TimePeriodForDigest = EditorGUILayout.TextField(
                            new GUIContent("Time Period", "'daily', 'weekly', or 'monthly'."),
                            mgr.x23TimePeriodForDigest);
                        break;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        // Claude API Settings Section
        if (mgr.apiKeysConfig != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Claude API Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            string newModelName = EditorGUILayout.TextField(
                new GUIContent("Model Name", "e.g., claude-3-opus-20240229, claude-3-sonnet-20240229"),
                mgr.apiKeysConfig.claudeModelName);
            int newMaxTokens = EditorGUILayout.IntField(
                new GUIContent("Max Tokens", "Max tokens for the LLM response"),
                mgr.apiKeysConfig.claudeMaxTokens);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mgr.apiKeysConfig, "Change Claude API Settings");
                mgr.apiKeysConfig.claudeModelName = newModelName;
                mgr.apiKeysConfig.claudeMaxTokens = newMaxTokens;
                EditorUtility.SetDirty(mgr.apiKeysConfig);
            }
            EditorGUI.indentLevel--;
        }

        // Show Config Management Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Show Config Management", EditorStyles.boldLabel);
        if (GUILayout.Button("Load Show Config from JSON"))
        {
            string path = EditorUtility.OpenFilePanel("Load Show Config", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                var loadedConfig = ShowGeneratorConfigLoader.LoadFromJson(path);
                if (loadedConfig != null)
                {
                    Undo.RecordObject(mgr, "Load Show Config");
                    mgr.LoadShowConfig(loadedConfig);
                    EditorUtility.SetDirty(mgr);
                }
            }
        }

        if (mgr.ActiveShowConfig != null)
        {
            if (GUILayout.Button("Save ActiveShowConfig to JSON"))
            {
                string path = EditorUtility.SaveFilePanel("Save ActiveShowConfig", Application.dataPath, mgr.ActiveShowConfig.name + ".json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    ShowGeneratorConfigLoader.SaveToJson(mgr.ActiveShowConfig, path);
                    AssetDatabase.Refresh();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Active Show Config is not assigned! Load or create one to see detailed editors and actions.", MessageType.Warning);
        }

        // Showrunner Actions Section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Showrunner Actions", EditorStyles.boldLabel);
        
        bool activeConfigMissing = mgr.ActiveShowConfig == null;
        bool apiKeysMissing = mgr.apiKeysConfig == null;

        if (activeConfigMissing)
        {
            EditorGUILayout.HelpBox("Actions are disabled: 'Active Show Config' is not assigned. Please load one using the button above or assign it directly.", MessageType.Warning);
        }
        if (apiKeysMissing)
        {
            EditorGUILayout.HelpBox("Actions are disabled: 'Api Keys Config' is not assigned. Please assign it in the slot provided in the 'API Configuration' section.", MessageType.Warning);
        }

        bool baseCanDoActions = !activeConfigMissing && !apiKeysMissing;
        GUI.enabled = baseCanDoActions && !isGeneratingEpisode;

        string generateEpisodeButtonText = isGeneratingEpisode ? 
            $"Generating Episode... ({TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - episodeGenerationStartTime):mm\\:ss})" : 
            "Generate Episode (LLM) - Creates New Copy";
        if (GUILayout.Button(generateEpisodeButtonText))
        {
            GenerateEpisodeAsync(mgr);
        }
        GUI.enabled = true;

        bool hasEpisodes = baseCanDoActions && mgr.ActiveShowConfig != null && 
            (mgr.ActiveShowConfig.episodes?.Count > 0 || mgr.generatedEpisodesThisSession?.Count > 0);
        bool canGenerateAudioButtonBeEnabled = baseCanDoActions && hasEpisodes && !isGeneratingAudio;

        GUI.enabled = canGenerateAudioButtonBeEnabled;
        string generateAudioButtonText = "Generate Audio from Loaded Config";
        if (isGeneratingAudio)
        {
            double elapsedTime = EditorApplication.timeSinceStartup - audioGenerationStartTime;
            generateAudioButtonText = $"Generating Audio... ({TimeSpan.FromSeconds(elapsedTime):mm\\:ss})";
        }

        if (baseCanDoActions && !hasEpisodes && !isGeneratingAudio)
        {
            EditorGUILayout.HelpBox("Generate Audio button is disabled: No episodes available in ActiveShowConfig or generated this session. Load a config first.", MessageType.Info);
        }

        if (GUILayout.Button(generateAudioButtonText))
        {
            Debug.Log("[ShowrunnerManagerEditor] About to call GenerateAudioForLastEpisodeFromEditor");
            audioCancelTokenSource = new CancellationTokenSource();
            isGeneratingAudio = true;
            audioGenerationStartTime = EditorApplication.timeSinceStartup;
            _ = Task.Run(async () => 
            {
                try
                {
                    bool success = await mgr.GenerateAudioForLastEpisodeFromEditor(audioCancelTokenSource.Token);
                    EditorApplication.delayCall += () => 
                    {
                        isGeneratingAudio = false;
                        audioGenerationStartTime = EditorApplication.timeSinceStartup;
                        audioCancelTokenSource = null;
                        if (success)
                        {
                            ShowNotification("Audio generation process completed successfully. Check console.");
                        }
                        else
                        {
                            ShowNotification("Audio generation process failed or was cancelled. Check console.");
                        }
                        Repaint();
                    };
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ShowrunnerManagerEditor] Exception in audio generation task: " + ex);
                }
            });
        }

        GUI.enabled = isGeneratingAudio;
        if (isGeneratingAudio && GUILayout.Button("Stop Generating Audio"))
        {
            audioCancelTokenSource?.Cancel();
            isGeneratingAudio = false;
            audioGenerationStartTime = EditorApplication.timeSinceStartup;
            ShowNotification("Audio generation cancellation requested.");
            Repaint();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Reset Audio Generation State (Debug)"))
        {
            isGeneratingAudio = false;
            Repaint();
        }

        EditorGUILayout.Space();
        GUI.enabled = baseCanDoActions && !isGeneratingEpisode && !isGeneratingAudio;
        if (GUILayout.Button("Reapply Web App Default Voice Map (Global)"))
        {
            Undo.RecordObject(mgr, "Reapply Default Voice Map");
            mgr.ReapplyDefaultVoiceMap();
            EditorUtility.SetDirty(mgr);
            ShowNotification("Default voice map reapplied.");
        }

        GUI.enabled = baseCanDoActions && !isGeneratingEpisode && !isGeneratingAudio;
        if (GUILayout.Button("Ping LLM Endpoint"))
        {
            PingLLMEndpointAsync(mgr);
        }
        GUI.enabled = true;

        // Show Config Details Section
        EditorGUILayout.Space();
        DrawActiveShowConfigDetails(mgr);

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            EditorUtility.SetDirty(mgr);
        }
    }

    // New async void method to handle episode generation on the main thread
    private async void GenerateEpisodeAsync(ShowrunnerManager mgr)
    {
        if (isGeneratingEpisode) return; // Prevent re-entrancy

        Debug.Log("[ShowrunnerManagerEditor] \'Generate Episode\' button clicked. Initiating asynchronous generation on main thread.");
        isGeneratingEpisode = true;
        episodeGenerationStartTime = EditorApplication.timeSinceStartup;
        Repaint(); // Update button text/state

        bool success = false;
        try
        {
            Debug.Log("[ShowrunnerManagerEditor] Async Task on Main Thread: Starting episode generation via ShowrunnerManager.GenerateEpisodeFromEditor().");
            success = await mgr.GenerateEpisodeFromEditor();

            if (success)
            {
                Debug.Log("[ShowrunnerManagerEditor] Async Task on Main Thread: Episode generation task completed. Success: True");
                
                // Automatic saving logic - uses mgr.LastGeneratedShowConfigCopy directly
                if (mgr.LastGeneratedShowConfigCopy != null && mgr.LastGeneratedShowConfigCopy.episodes != null && mgr.LastGeneratedShowConfigCopy.episodes.Count > 0)
                {
                    string targetDirectory = Path.Combine(Application.dataPath, "Resources", "Episodes");
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                        Debug.Log($"[ShowrunnerManagerEditor] Created directory: {targetDirectory}");
                    }

                    ShowEpisode generatedEpisode = mgr.LastGeneratedShowConfigCopy.episodes[0]; // Assuming the new episode is the only one
                    string showName = mgr.LastGeneratedShowConfigCopy.name.Replace(" ", "_").Replace(":", "_").Replace("/", "_"); // Sanitize
                    string episodeId = generatedEpisode.id.Replace(" ", "_").Replace(":", "_").Replace("/", "_"); // Sanitize
                    string fileName = $"{showName}_{episodeId}.json";
                    string savePath = Path.Combine(targetDirectory, fileName);

                    ShowGeneratorConfigLoader.SaveToJson(mgr.LastGeneratedShowConfigCopy, savePath);
                    AssetDatabase.Refresh(); // Important to make Unity recognize the new file

                    string userFriendlyPath = $"Assets/Resources/Episodes/{fileName}";
                    ShowNotification($"Episode saved to: {userFriendlyPath}");
                    Debug.Log($"[ShowrunnerManagerEditor] Episode automatically saved to: {savePath}");
                    
                    mgr.ClearLastGeneratedShowConfigCopy(); // Clear the copy after saving
                }
                else
                {
                    Debug.LogError("[ShowrunnerManagerEditor] LastGeneratedShowConfigCopy or its episodes list was null/empty after successful generation. Cannot auto-save.");
                }
            }
            else
            {
                Debug.LogError("[ShowrunnerManagerEditor] Async Task on Main Thread: Episode generation task completed. Success: False");
                ShowNotification("Episode generation failed. Check console.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ShowrunnerManagerEditor] Exception during episode generation: {ex.Message}\n{ex.StackTrace}");
            ShowNotification("Episode generation failed with exception. Check console.");
            success = false; // Ensure success is false on exception
        }
        finally
        {
            isGeneratingEpisode = false;
            episodeGenerationStartTime = EditorApplication.timeSinceStartup; // Reset timer
            Repaint(); // Update UI
        }
    }

    // New async void method to handle LLM ping
    private async void PingLLMEndpointAsync(ShowrunnerManager mgr)
    {
        if (isGeneratingEpisode || isGeneratingAudio) 
        {
            Debug.LogWarning("[LLM Ping] Cannot ping while another operation is in progress.");
            return;
        }

        Debug.Log("[LLM Ping] Initiating LLM endpoint test...");
        // Optionally set a flag like isPinging = true; if you want to show a status in UI
        // For now, just log and show notification.

        try
        {
            string response = await mgr.PingLLMEndpointAsync();
            ShowNotification("LLM Ping Sent. Response: " + (response.Length > 100 ? response.Substring(0, 100) + "..." : response));
            Debug.Log($"[LLM Ping] Result: {response}");
        }
        catch (Exception ex)
        {
            ShowNotification("LLM Ping failed. Exception: " + ex.Message);
            Debug.LogError($"[LLM Ping] Exception: {ex.Message}");
        }
        finally
        {
            // if (isPinging) isPinging = false;
            Repaint(); // Repaint if needed to update UI state
        }
    }

    private void DrawActiveShowConfigDetails(ShowrunnerManager mgr)
    {
        var cfg = mgr.ActiveShowConfig;
        if (cfg == null)
        {
            EditorGUILayout.HelpBox("No ActiveShowConfig loaded.", MessageType.Info);
            return;
        }

        // Basic Info Section
        showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "Basic Information", true);
        if (showBasicInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Show ID", cfg.id ?? "<null>");
            if (string.IsNullOrEmpty(cfg.id))
                EditorGUILayout.HelpBox("ShowConfig 'id' is missing or empty!", MessageType.Warning);

            EditorGUILayout.LabelField("Show Name", cfg.name ?? "<null>");
            if (string.IsNullOrEmpty(cfg.name))
                EditorGUILayout.HelpBox("ShowConfig 'name' is missing or empty!", MessageType.Warning);

            EditorGUILayout.LabelField("Description", cfg.description ?? "<null>");
            if (string.IsNullOrEmpty(cfg.description))
                EditorGUILayout.HelpBox("ShowConfig 'description' is missing or empty!", MessageType.Warning);

            EditorGUILayout.LabelField("Creator", cfg.creator ?? "<null>");
            if (string.IsNullOrEmpty(cfg.creator))
                EditorGUILayout.HelpBox("ShowConfig 'creator' is missing or empty!", MessageType.Warning);

            EditorGUILayout.EndVertical();
        }

        // Prompts Section
        showPromptsSummary = EditorGUILayout.Foldout(showPromptsSummary, $"Prompts ({cfg.prompts?.Count ?? 0} entries)", true);
        if (showPromptsSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (cfg.prompts == null || cfg.prompts.Count == 0)
            {
                EditorGUILayout.HelpBox("No prompts defined!", MessageType.Warning);
            }
            else
            {
                foreach (var prompt in cfg.prompts)
                {
                    EditorGUILayout.LabelField(prompt.Key, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(prompt.Value, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space(5);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Actors Section
        showActorsSummary = EditorGUILayout.Foldout(showActorsSummary, $"Actors ({cfg.actors?.Count ?? 0} characters)", true);
        if (showActorsSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (cfg.actors == null || cfg.actors.Count == 0)
            {
                EditorGUILayout.HelpBox("No actors defined!", MessageType.Warning);
            }
            else
            {
                foreach (var actor in cfg.actors)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Actor: {actor.Key}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Name:", actor.Value.name);
                    EditorGUILayout.LabelField("Gender:", actor.Value.gender);
                    EditorGUILayout.LabelField("Voice:", actor.Value.voice);
                    EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(actor.Value.description, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Locations Section
        showLocationsSummary = EditorGUILayout.Foldout(showLocationsSummary, $"Locations ({cfg.locations?.Count ?? 0} sets)", true);
        if (showLocationsSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (cfg.locations == null || cfg.locations.Count == 0)
            {
                EditorGUILayout.HelpBox("No locations defined!", MessageType.Warning);
            }
            else
            {
                foreach (var location in cfg.locations)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Location: {location.Key}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Name:", location.Value.name);
                    EditorGUILayout.LabelField("Description:", location.Value.description, EditorStyles.wordWrappedLabel);
                    
                    EditorGUILayout.LabelField("Slots:", EditorStyles.boldLabel);
                    if (location.Value.slots != null && location.Value.slots.Count > 0)
                    {
                        foreach (var slot in location.Value.slots)
                        {
                            EditorGUILayout.LabelField($"  • {slot.Key}: {slot.Value}");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("  No slots defined");
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Episodes Section
        showEpisodesSummary = EditorGUILayout.Foldout(showEpisodesSummary, $"Episodes ({cfg.episodes?.Count ?? 0} episodes)", true);
        if (showEpisodesSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Pilot Episode
            EditorGUILayout.LabelField("Pilot Episode:", EditorStyles.boldLabel);
            if (cfg.pilot == null)
            {
                EditorGUILayout.HelpBox("No pilot episode defined!", MessageType.Warning);
                if (GUILayout.Button("Initialize Pilot"))
                {
                    Undo.RecordObject(mgr, "Initialize Pilot");
                    cfg.pilot = new ShowEpisode();
                    EditorUtility.SetDirty(mgr);
                }
            }
            else
            {
                EditorGUILayout.LabelField($"ID: {cfg.pilot.id ?? "<null>"}");
                EditorGUILayout.LabelField($"Name: {cfg.pilot.name ?? "<null>"}");
                EditorGUILayout.LabelField($"Premise: {cfg.pilot.premise ?? "<null>"}");
                EditorGUILayout.LabelField($"Scenes: {cfg.pilot.scenes?.Count ?? 0}");
            }
            
            EditorGUILayout.Space(10);
            
            // Regular Episodes
            EditorGUILayout.LabelField("Regular Episodes:", EditorStyles.boldLabel);
            if (cfg.episodes == null || cfg.episodes.Count == 0)
            {
                EditorGUILayout.HelpBox("No regular episodes yet.", MessageType.Info);
            }
            else
            {
                foreach (var episode in cfg.episodes)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Episode: {episode.id}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Name:", episode.name);
                    EditorGUILayout.LabelField("Premise:", episode.premise, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Summary:", episode.summary, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"Scenes: {episode.scenes?.Count ?? 0}");
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }
            EditorGUILayout.EndVertical();
        }

        // Voice Map Section
        showVoiceMapSummary = EditorGUILayout.Foldout(showVoiceMapSummary, $"Voice Map ({cfg.actorVoiceMap?.Count ?? 0} mappings)", true);
        if (showVoiceMapSummary)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (cfg.actorVoiceMap == null || cfg.actorVoiceMap.Count == 0)
            {
                EditorGUILayout.HelpBox("No voice mappings defined!", MessageType.Warning);
            }
            else
            {
                foreach (var mapping in cfg.actorVoiceMap)
                {
                    EditorGUILayout.LabelField($"{mapping.Key}: {mapping.Value}");
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    // Helper to show notification on the editor window
    private void ShowNotification(string message)
    {
        var window = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
        if (window != null) window.ShowNotification(new GUIContent(message));
    }

    // Helper for string-string dictionary (like Prompts, ActorVoiceMap)
    private bool DrawStringStringDictionary(Dictionary<string, string> dictionary, string keyHeaderText, string valueHeaderText)
    {
        if (dictionary == null) return false;
        bool changed = false;

        List<string> keysToRemove = new List<string>();
        string keyToUpdate = null;      
        string newKeyForEntry = null;   
        string valueForNewKey = null; 
        string valueToUpdate = null;    

        foreach (var kvp in dictionary.ToList()) 
        {
            EditorGUILayout.BeginHorizontal();
            string displayedKey = EditorGUILayout.TextField(kvp.Key, GUILayout.Width(150));
            string displayedValue = EditorGUILayout.TextArea(kvp.Value, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 1.5f), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                keysToRemove.Add(kvp.Key);
            }
            EditorGUILayout.EndHorizontal();

            if (displayedKey != kvp.Key) 
            {
                if (!string.IsNullOrEmpty(displayedKey) && !dictionary.ContainsKey(displayedKey))
                {
                    keysToRemove.Add(kvp.Key); 
                    newKeyForEntry = displayedKey;    
                    valueForNewKey = displayedValue; // Use the current displayed value, even if key changed
                }
                 // If key change causes conflict or is empty, do nothing this iteration
            }
            else if (displayedValue != kvp.Value) 
            {
                keyToUpdate = kvp.Key;
                valueToUpdate = displayedValue;
            }
        }

        foreach (string key in keysToRemove)
        {
            if (dictionary.Remove(key)) changed = true;
        }
        
        if (newKeyForEntry != null && valueForNewKey !=null) // Check valueForNewKey too
        {
            dictionary[newKeyForEntry] = valueForNewKey;
            changed = true;
        }
        else if (keyToUpdate != null && valueToUpdate != null) // Check valueToUpdate too
        {
            dictionary[keyToUpdate] = valueToUpdate;
            changed = true;
        }

        if (GUILayout.Button($"Add New {keyHeaderText.Replace(" Key","")}"))
        {
            string baseKey = "new_" + keyHeaderText.ToLower().Replace(" key", "").Replace(" ", "_");
            string newKeyToAdd = baseKey;
            int k = 0;
            while (dictionary.ContainsKey(newKeyToAdd)) { newKeyToAdd = $"{baseKey}_{++k}"; }
            dictionary.Add(newKeyToAdd, "");
            changed = true;
        }
        return changed;
    }
    
    private bool DrawActorsDictionary(Dictionary<string, ShowActor> dictionary)
    {
        if (dictionary == null) return false;
        bool changedOverall = false;

        List<string> keysToRemove = new List<string>();
        string keyToUpdate = null;      
        ShowActor valueToUpdate = null; 
        string newKeyForEntry = null;   
        ShowActor valueForNewKeyEntry = null; 

        foreach (var kvp in dictionary.ToList()) 
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool actorFieldsChanged = false;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Actor Key:", GUILayout.Width(70));
            string displayedKey = EditorGUILayout.TextField(kvp.Key);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                keysToRemove.Add(kvp.Key);
            }
            EditorGUILayout.EndHorizontal();

            ShowActor actor = kvp.Value ?? new ShowActor(); // Ensure actor is not null

            string oldName = actor.name;
            string oldGender = actor.gender;
            string oldDescription = actor.description;
            string oldVoice = actor.voice;

            actor.name = EditorGUILayout.TextField("Name", actor.name);
            actor.gender = EditorGUILayout.TextField("Gender", actor.gender);
            EditorGUILayout.LabelField("Description");
            actor.description = EditorGUILayout.TextArea(actor.description, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            actor.voice = EditorGUILayout.TextField("Voice (ID/Desc)", actor.voice);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);

            if (oldName != actor.name || oldGender != actor.gender || oldDescription != actor.description || oldVoice != actor.voice)
            {
                actorFieldsChanged = true;
            }

            if (displayedKey != kvp.Key)
            {
                if (!string.IsNullOrEmpty(displayedKey) && !dictionary.ContainsKey(displayedKey))
                {
                    keysToRemove.Add(kvp.Key); 
                    newKeyForEntry = displayedKey;    
                    valueForNewKeyEntry = actor; 
                }
            }
            else if (actorFieldsChanged)
            {
                keyToUpdate = kvp.Key;
                valueToUpdate = actor;
            }
        }

        foreach (string key in keysToRemove)
        {
            if (dictionary.Remove(key)) changedOverall = true;
        }
        
        if (newKeyForEntry != null && valueForNewKeyEntry != null)
        {
            dictionary[newKeyForEntry] = valueForNewKeyEntry;
            newKeyForEntry = null; // Reset after processing
            changedOverall = true;
        }
        else if (keyToUpdate != null && valueToUpdate != null)
        {
            dictionary[keyToUpdate] = valueToUpdate;
            keyToUpdate = null; // Reset after processing
            changedOverall = true;
        }

        if (GUILayout.Button("Add New Actor"))
        {
            string baseKey = "new_actor";
            string newKeyToAdd = baseKey;
            int k = 0;
            while (dictionary.ContainsKey(newKeyToAdd)) { newKeyToAdd = $"{baseKey}_{++k}"; }
            dictionary.Add(newKeyToAdd, new ShowActor { name = newKeyToAdd });
            changedOverall = true;
        }
        return changedOverall;
    }

    private bool DrawLocationsDictionary(Dictionary<string, ShowLocation> dictionary)
    {
        if (dictionary == null) return false;
        bool changedOverall = false;

        List<string> keysToRemove = new List<string>();
        string keyToUpdate = null;      
        ShowLocation valueToUpdate = null; 
        string newKeyForEntry = null;   
        ShowLocation valueForNewKeyEntry = null; 

        foreach (var kvp in dictionary.ToList()) 
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool locationFieldsChanged = false;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Location Key:", GUILayout.Width(85));
            string displayedKey = EditorGUILayout.TextField(kvp.Key);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                keysToRemove.Add(kvp.Key);
            }
            EditorGUILayout.EndHorizontal();

            ShowLocation location = kvp.Value ?? new ShowLocation();
            if (location.slots == null) location.slots = new Dictionary<string, string>();

            string oldName = location.name;
            string oldDescription = location.description;

            location.name = EditorGUILayout.TextField("Name", location.name);
            EditorGUILayout.LabelField("Description");
            location.description = EditorGUILayout.TextArea(location.description, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
            
            EditorGUILayout.LabelField("Slots", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            if (DrawStringStringDictionary(location.slots, "Slot Name", "Slot Value"))
            {
                locationFieldsChanged = true; // If slots changed, the location is considered changed
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);

            if (oldName != location.name || oldDescription != location.description)
            {
                locationFieldsChanged = true;
            }
            
            if (displayedKey != kvp.Key)
            {
                if (!string.IsNullOrEmpty(displayedKey) && !dictionary.ContainsKey(displayedKey))
                {
                    keysToRemove.Add(kvp.Key); 
                    newKeyForEntry = displayedKey;    
                    valueForNewKeyEntry = location; 
                }
            }
            else if (locationFieldsChanged)
            {
                keyToUpdate = kvp.Key;
                valueToUpdate = location;
            }
        }

        foreach (string key in keysToRemove)
        {
            if (dictionary.Remove(key)) changedOverall = true;
        }
        
        if (newKeyForEntry != null && valueForNewKeyEntry != null)
        {
            dictionary[newKeyForEntry] = valueForNewKeyEntry;
            newKeyForEntry = null; // Reset after processing
            changedOverall = true;
        }
        else if (keyToUpdate != null && valueToUpdate != null)
        {
            dictionary[keyToUpdate] = valueToUpdate;
            keyToUpdate = null; // Reset after processing
            changedOverall = true;
        }

        if (GUILayout.Button("Add New Location"))
        {
            string baseKey = "new_location";
            string newKeyToAdd = baseKey;
            int k = 0;
            while (dictionary.ContainsKey(newKeyToAdd)) { newKeyToAdd = $"{baseKey}_{++k}"; }
            dictionary.Add(newKeyToAdd, new ShowLocation { name = newKeyToAdd, slots = new Dictionary<string, string>() });
            changedOverall = true;
        }
        return changedOverall;
    }
    // Removed DrawDictionaryManuallyWithComplexValue as it's replaced by specific dictionary drawers
}
#endif 