using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace ShowRunner
{
    public class ShowRunner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string showFileName = "aipodcast-archive-2025-04-25T06-29-11-478Z.json";
        [SerializeField] private string episodesRootPath = "Episodes";
        [SerializeField] private float dialogueDelay = 0.5f; // Delay between dialogues
        [SerializeField] private bool playAudioFromActors = true; // Whether to play audio from actor positions

        [Header("References")]
        [SerializeField] private EventProcessor eventProcessor;
        [SerializeField] private AudioSource defaultAudioSource;

        [Header("Playback Settings")]
        [SerializeField, Tooltip("If enabled, steps only when UI triggers NextStep; disable for full auto-play.")]
        private bool manualMode = true;

        [System.Serializable]
        public struct ActorAudioMapping
        {
            public string actorName;
            public AudioSource audioSource;
        }

        [Header("Audio Mappings")]
        [Tooltip("Assign each actor's AudioSource here to drive visemes.")]
        [SerializeField] private List<ActorAudioMapping> actorAudioMappings = new List<ActorAudioMapping>();

        // Dictionary to cache actor audio sources
        private Dictionary<string, AudioSource> actorAudioSources = new Dictionary<string, AudioSource>();
        
        private ShowData showData;
        private Episode currentEpisode;
        private int currentSceneIndex = -1;
        private int currentDialogueIndex = -1;
        private string playbackState = "init";
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (defaultAudioSource == null)
            {
                defaultAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (eventProcessor == null)
            {
                eventProcessor = FindObjectOfType<EventProcessor>();
                if (eventProcessor == null)
                {
                    Debug.LogError("EventProcessor not found! The ShowRunner won't function properly.");
                }
            }
        }

        private void Start()
        {
            LoadShowData();
            PreloadAudio();
        }

        public void LoadShowData()
        {
            try
            {
                // Load JSON file from Resources folder
                // Build resource path and normalize separators to forward slashes
                string rawPath = Path.Combine(episodesRootPath, showFileName);
                string resourcePath = rawPath.Replace(Path.DirectorySeparatorChar, '/').Replace(".json", "");
                TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
                
                if (jsonAsset != null)
                {
                    showData = JsonConvert.DeserializeObject<ShowData>(jsonAsset.text);
                    Debug.Log($"Show data loaded successfully: {showData.Config.Name} with {showData.Episodes.Count} episodes");
                }
                else
                {
                    // Try loading from disk Resources folder if not in Resources.Load
                    string absolutePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, showFileName);
                    // Normalize path separators to forward slashes for consistency
                    absolutePath = absolutePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (File.Exists(absolutePath))
                    {
                        string jsonContent = File.ReadAllText(absolutePath);
                        showData = JsonConvert.DeserializeObject<ShowData>(jsonContent);
                        Debug.Log($"Show data loaded from file system: {showData.Config.Name} with {showData.Episodes.Count} episodes");
                    }
                    else
                    {
                        Debug.LogError($"Show file not found at: {absolutePath} or in Resources");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading show data: {ex.Message}");
            }
        }

        private void PreloadAudio()
        {
            if (showData == null || showData.Episodes == null)
                return;

            StartCoroutine(PreloadAudioCoroutine());
        }

        private IEnumerator PreloadAudioCoroutine()
        {
            int totalFiles = 0;
            int loadedFiles = 0;

            // Count total files first
            foreach (var episode in showData.Episodes)
            {
                for (int sceneIndex = 0; sceneIndex < episode.Scenes.Count; sceneIndex++)
                {
                    var scene = episode.Scenes[sceneIndex];
                    totalFiles += scene.Dialogue.Count;
                }
            }

            Debug.Log($"Starting preload of {totalFiles} audio files...");

            // Now preload the files
            foreach (var episode in showData.Episodes)
            {
                for (int sceneIndex = 0; sceneIndex < episode.Scenes.Count; sceneIndex++)
                {
                    var scene = episode.Scenes[sceneIndex];
                    
                    for (int dialogueIndex = 0; dialogueIndex < scene.Dialogue.Count; dialogueIndex++)
                    {
                        // Construct the audio file path and key
                        string audioFileName = $"{episode.Id}_{sceneIndex + 1}_{dialogueIndex + 1}.mp3";
                        string audioKey = $"{episode.Id}_{sceneIndex + 1}_{dialogueIndex + 1}";
                        
                        // Try loading from Resources first
                        string resourcePath = $"{episodesRootPath}/{episode.Id}/audio/{audioFileName}".Replace(".mp3", "");
                        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
                        
                        if (clip == null)
                        {
                            // Try loading directly from Resources folder on disk if not in Resources.Load
                            string filePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, episode.Id, "audio", audioFileName);
                            if (File.Exists(filePath))
                            {
                                // Use Unity's audio loading API for files in Assets folder
                                string assetPath = $"Assets/Resources/{episodesRootPath}/{episode.Id}/audio/{audioFileName}";
                                #if UNITY_EDITOR
                                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                                #endif
                            }
                        }
                        
                        if (clip != null)
                        {
                            audioCache[audioKey] = clip;
                            loadedFiles++;
                            
                            // Log progress periodically
                            if (loadedFiles % 10 == 0 || loadedFiles == totalFiles)
                            {
                                Debug.Log($"Preloaded {loadedFiles}/{totalFiles} audio files");
                            }
                        }
                        
                        // Yield every few files to avoid freezing the editor
                        if (loadedFiles % 5 == 0)
                            yield return null;
                    }
                }
            }
            
            Debug.Log($"Audio preload complete. Loaded {loadedFiles}/{totalFiles} files.");
        }

        public void SelectEpisode(int episodeIndex)
        {
            if (showData != null && showData.Episodes != null && episodeIndex >= 0 && episodeIndex < showData.Episodes.Count)
            {
                currentEpisode = showData.Episodes[episodeIndex];
                currentSceneIndex = -1;
                currentDialogueIndex = -1;
                playbackState = "init";
                Debug.Log($"Selected episode: {currentEpisode.Title} (ID: {currentEpisode.Id})");
            }
            else
            {
                Debug.LogError($"Invalid episode index: {episodeIndex}");
            }
        }

        public void NextStep()
        {
            if (currentEpisode == null)
            {
                Debug.LogWarning("No episode selected. Please select an episode first.");
                return;
            }

            switch (playbackState)
            {
                case "init":
                    Debug.Log("Starting episode playback");
                    playbackState = "episode-loaded";
                    break;

                case "episode-loaded":
                    currentSceneIndex++;
                    if (currentSceneIndex < currentEpisode.Scenes.Count)
                    {
                        var scene = currentEpisode.Scenes[currentSceneIndex];
                        Debug.Log($"Loading scene {currentSceneIndex + 1}: {scene.Location}");
                        
                        // Create a prepareScene event
                        EventData sceneEvent = new EventData
                        {
                            type = "prepareScene",
                            location = scene.Location,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                        
                        eventProcessor.ProcessEvent(sceneEvent);
                        currentDialogueIndex = -1;
                        playbackState = "scene-loaded";
                    }
                    else
                    {
                        Debug.Log("Episode completed");
                        playbackState = "episode-unloaded";
                    }
                    break;

                case "scene-loaded":
                    currentDialogueIndex++;
                    var currentScene = currentEpisode.Scenes[currentSceneIndex];
                    
                    if (currentDialogueIndex < currentScene.Dialogue.Count)
                    {
                        var dialogue = currentScene.Dialogue[currentDialogueIndex];
                        Debug.Log($"{dialogue.Actor}: \"{dialogue.Line}\"");
                        
                        // Create a speak event
                        EventData speakEvent = new EventData
                        {
                            type = "speak",
                            actor = dialogue.Actor,
                            line = dialogue.Line,
                            action = dialogue.Action ?? "normal",
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            // Add episode ID and scene/dialogue numbers for reference
                            episode = currentEpisode.Id,
                            additionalData = $"scene:{currentSceneIndex + 1},dialogue:{currentDialogueIndex + 1}"
                        };
                        
                        // Load and play the corresponding audio file
                        StartCoroutine(PlayDialogueAudio(speakEvent));
                        
                        playbackState = "dialogue";
                    }
                    else
                    {
                        Debug.Log("Scene completed");
                        playbackState = "scene-unloaded";
                    }
                    break;

                case "dialogue":
                    // Waiting for audio to finish
                    break;

                case "scene-unloaded":
                    playbackState = "episode-loaded";
                    // Auto-advance if not in manual stepping mode
                    if (!manualMode)
                        NextStep();
                    break;

                case "episode-unloaded":
                    Debug.Log("Show playback complete");
                    break;
            }
        }

        // Find the AudioSource component for an actor
        private AudioSource GetAudioSourceForActor(string actorName)
        {
            AudioSource audioSource = null;
            // 1) Check inspector-provided mappings
            if (actorAudioMappings != null)
            {
                foreach (var map in actorAudioMappings)
                {
                    if (map.actorName == actorName && map.audioSource != null)
                    {
                        audioSource = map.audioSource;
                        break;
                    }
                }
                if (audioSource != null)
                {
                    if (!audioSource.enabled) audioSource.enabled = true;
                    return audioSource;
                }
            }
            // 2) Check cache
            if (actorAudioSources.TryGetValue(actorName, out audioSource))
            {
                if (audioSource != null)
                {
                    if (!audioSource.enabled) audioSource.enabled = true;
                    return audioSource;
                }
                actorAudioSources.Remove(actorName);
            }
            // 3) Try to find the actor GameObject
            GameObject actorObject = GameObject.Find(actorName);
            if (actorObject != null)
            {
                // Prefer ActorAudioSourceAssigner component
                var assigner = actorObject.GetComponent<ActorAudioSourceAssigner>();
                if (assigner != null)
                    audioSource = assigner.GetAudioSource();
                else
                    audioSource = actorObject.GetComponent<AudioSource>() ?? actorObject.AddComponent<AudioSource>();
                // Ensure audio source is enabled
                if (!audioSource.enabled) audioSource.enabled = true;
                // Cache for future
                actorAudioSources[actorName] = audioSource;
                return audioSource;
            }
            // 4) Fallback
            Debug.LogWarning($"Could not find actor '{actorName}' for audio playback. Using default audio source.");
            if (defaultAudioSource != null && !defaultAudioSource.enabled)
                defaultAudioSource.enabled = true;
            return defaultAudioSource;
        }
        
        private IEnumerator PlayDialogueAudio(EventData speakEvent)
        {
            // Get the audio clip key
            string audioKey = $"{currentEpisode.Id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}";
            AudioClip dialogueClip = null;
            
            // Try to get from cache first
            if (audioCache.TryGetValue(audioKey, out dialogueClip))
            {
                Debug.Log($"Using cached audio: {audioKey}");
            }
            else
            {
                // Try loading from Resources 
                string resourcePath = $"{episodesRootPath}/{currentEpisode.Id}/audio/{currentEpisode.Id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}".Replace(".mp3", "");
                dialogueClip = Resources.Load<AudioClip>(resourcePath);
                
                if (dialogueClip == null)
                {
                    // Try loading from disk Resources folder if not in Resources.Load
                    string filePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, currentEpisode.Id, "audio", $"{currentEpisode.Id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}.mp3");
                    if (File.Exists(filePath))
                    {
                        // Use Unity's audio loading API for files in Assets folder
                        string assetPath = $"Assets/Resources/{episodesRootPath}/{currentEpisode.Id}/audio/{currentEpisode.Id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}.mp3";
                        #if UNITY_EDITOR
                        dialogueClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                        #endif
                    }
                }
                
                // Cache it if we found it
                if (dialogueClip != null)
                {
                    audioCache[audioKey] = dialogueClip;
                }
            }
            
            // Process the speak event first (this shows caption, handles camera, etc.)
            eventProcessor.ProcessEvent(speakEvent);
            
            if (dialogueClip != null)
            {
                // Determine which audio source to use
                AudioSource audioSource = defaultAudioSource;
                
                if (playAudioFromActors)
                {
                    // Get the audio source from the actor GameObject
                    audioSource = GetAudioSourceForActor(speakEvent.actor);
                }
                
                // Play the audio (ensure source is enabled for visemes)
                audioSource.clip = dialogueClip;
                audioSource.enabled = true;
                audioSource.Play();
                
                // Wait for the audio to finish (plus a small delay)
                float waitTime = dialogueClip.length + dialogueDelay;
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                Debug.LogWarning($"Audio clip not found for: {audioKey}. Proceeding without audio.");
                // Default wait time if no audio
                yield return new WaitForSeconds(3.0f);
            }
            
            // Scene dialogue ended
            playbackState = "scene-loaded";
            // Auto-advance if not manual
            if (!manualMode)
                NextStep();
        }

        // Utility functions for UI
        public string GetCurrentEpisodeTitle()
        {
            return currentEpisode?.Title ?? "No Episode Selected";
        }
        
        public int GetTotalEpisodeCount()
        {
            return showData?.Episodes?.Count ?? 0;
        }
        
        public List<string> GetEpisodeTitles()
        {
            List<string> titles = new List<string>();
            if (showData?.Episodes != null)
            {
                foreach (var episode in showData.Episodes)
                {
                    titles.Add($"{episode.Id}: {episode.Title}");
                }
            }
            return titles;
        }
        
        public ShowData GetShowData()
        {
            return showData;
        }

        // Clear all actor audio sources when the component is destroyed
        private void OnDestroy()
        {
            foreach (var actorAudioSource in actorAudioSources.Values)
            {
                if (actorAudioSource != null && actorAudioSource != defaultAudioSource)
                {
                    Destroy(actorAudioSource);
                }
            }
            
            actorAudioSources.Clear();
        }

        /// <summary>
        /// Toggle between manual stepping and full auto-play.
        /// </summary>
        public void SetManualMode(bool manual)
        {
            manualMode = manual;
        }
    }
} 