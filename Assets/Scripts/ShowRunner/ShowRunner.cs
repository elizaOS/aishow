using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using ShowRunner.UI;

namespace ShowRunner
{
    // Define the data structure for episode completion events within this namespace
    [System.Serializable] // Keep serializable if needed elsewhere
    public struct EpisodeCompletionData
    {
        public string JsonFilePath;
        public string EpisodeId;
    }

    /// <summary>
    /// Core controller class for managing show playback, scene preparation, and event processing.
    /// This class orchestrates the entire show experience, from loading episodes to managing scene transitions.
    /// </summary>
    public class ShowRunner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string episodesRootPath = "Episodes";
        [SerializeField] private float dialogueDelay = 0.5f; // Delay between dialogues
        [SerializeField] private bool playAudioFromActors = true; // Whether to play audio from actor positions

        [Header("References")]
        [SerializeField] private EventProcessor eventProcessor;
        [SerializeField] private AudioSource defaultAudioSource;
        [SerializeField] private ScenePreperationManager scenePreparationManager;
        [SerializeField] private SceneTransitionManager sceneTransitionManager;

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

        // Public static instance for Singleton pattern
        public static ShowRunner Instance { get; private set; }

        // Dictionary to cache actor audio sources
        private Dictionary<string, AudioSource> actorAudioSources = new Dictionary<string, AudioSource>();
        
        // Name of the currently loaded show file (without extension)
        private string loadedShowFileName = null; 
        private ShowData showData;
        private Episode currentEpisode;
        private int currentSceneIndex = -1;
        private int currentDialogueIndex = -1;
        private string playbackState = "init";
        private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

        // State to track if we're waiting for scene preparation
        private bool waitingForScenePreparation = false;
        private string pendingSceneName = null;

        // Event fired AFTER the last dialogue line of the last scene has completed playback
        // and ShowRunner has internally marked the episode as unloaded.
        // Passes data about the completed episode (relative JSON path, episode ID).
        public event Action<EpisodeCompletionData> OnLastDialogueComplete;

        // Event fired when an episode is selected, passing the episode description/premise.
        public event Action<string, string> OnEpisodeSelectedForDisplay;

        // Event fired when an episode is selected and considered "started" for playback purposes
        public event Action OnEpisodeActualStart; 

        // Event fired when the current scene changes, passing the scene name/location.
        public event Action<string> OnSceneChangedForDisplay;

        // Internal pause state (set by CommercialManager)
        private bool isPaused = false;

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                // Debug.LogWarning("Another instance of ShowRunner already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (defaultAudioSource == null)
            {
                defaultAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (eventProcessor == null)
            {
                eventProcessor = FindObjectOfType<EventProcessor>();
                if (eventProcessor == null)
                {
                    // Debug.LogError("EventProcessor not found! The ShowRunner won't function properly.");
                }
            }
            
            if (scenePreparationManager == null)
            {
                scenePreparationManager = FindObjectOfType<ScenePreperationManager>();
                if (scenePreparationManager == null)
                {
                    // Debug.LogError("ScenePreparationManager not found! The ShowRunner won't function properly.");
                }
            }
            
            if (scenePreparationManager != null)
            {
                // Subscribe to the scene preparation complete event
                scenePreparationManager.OnScenePreparationComplete += OnScenePreparationComplete;
            }
             else
            {
                 // Debug.LogError("ShowRunner Awake: Could not find ScenePreparationManager to subscribe to OnScenePreparationComplete!");
            }

            // Subscribe to UXAnimationManager events if the manager exists
            if (UXAnimationManager.Instance != null)
            {
                this.OnLastDialogueComplete += UXAnimationManager.Instance.OnEpisodeEnd;
            }
            else
            {
                // Debug.LogWarning("ShowRunner Awake: UXAnimationManager instance not found. Episode End UX events won't fire.");
            }

            if (sceneTransitionManager == null)
            {
                sceneTransitionManager = FindObjectOfType<SceneTransitionManager>();
                if (sceneTransitionManager == null)
                {
                    // Debug.LogWarning("SceneTransitionManager not found! Scene transitions will not be available.");
                }
            }
        }

        private void Start()
        {
            // Don't automatically load data on start. 
            // Loading should be triggered after discovering and selecting a file.
            // Example: FindObjectOfType<ShowRunnerUI>().InitializeShowSelection();
            // Consider loading the first discovered file by default if needed.
        }

        /// <summary>
        /// Discovers all .json show archive files in the Resources/Episodes directory.
        /// </summary>
        /// <returns>A list of show file names (without extension) found.</returns>
        public List<string> DiscoverShowFiles()
        {
            List<string> showFiles = new List<string>();
            string searchPath = Path.Combine(Application.dataPath, "Resources", episodesRootPath);
            
            try
            {
                if (Directory.Exists(searchPath))
                {
                    // Find all .json files, excluding .meta files
                    string[] files = Directory.GetFiles(searchPath, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        // Get the filename without extension for Resources.Load compatibility
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                        showFiles.Add(fileNameWithoutExtension);
                        // Debug.Log($"Discovered show file: {fileNameWithoutExtension}");
                    }
                }
                else
                {
                    // Debug.LogWarning($"Show discovery path not found: {searchPath}");
                }
            }
            catch (Exception)
            {
                // Debug.LogError($"Error discovering show files in {searchPath}: {ex.Message}");
            }
            
            // Also attempt to load directly from Resources in case files are there but not found by Directory.GetFiles (e.g., in a build)
            // This part might need refinement based on how Resources are packed.
            // A common approach is to have a manifest file or rely solely on the file system search in Editor.
            // For simplicity here, we primarily rely on the file system search. Consider Resources.LoadAll<TextAsset>(episodesRootPath) as an alternative.

            return showFiles;
        }

        /// <summary>
        /// Loads show data from a specific JSON file in the Resources/Episodes directory.
        /// Clears previous show data and resets playback state before loading.
        /// </summary>
        /// <param name="showFileNameToLoad">The name of the show file (without .json extension) to load.</param>
        public void LoadShowData(string showFileNameToLoad)
        {
            // Clear previous data and state
            ResetShowState();
            
            if (string.IsNullOrEmpty(showFileNameToLoad))
            {
                // Debug.LogError("LoadShowData: Provided show file name is null or empty.");
                return;
            }

            loadedShowFileName = showFileNameToLoad; // Store the name of the loaded file 
            
            try
            {
                // Debug.Log($"LoadShowData: Starting to load show data for '{showFileNameToLoad}'");
                
                // Build resource path (relative to Resources folder)
                string resourcePath = Path.Combine(episodesRootPath, showFileNameToLoad).Replace(Path.DirectorySeparatorChar, '/');
                // Debug.Log($"LoadShowData: Looking for JSON at resource path: {resourcePath}");
                
                TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
                
                if (jsonAsset != null)
                {
                    // Debug.Log($"LoadShowData: Found JSON asset via Resources.Load: {jsonAsset.name}, size: {jsonAsset.text.Length} bytes");
                    ProcessLoadedJson(jsonAsset.text, showFileNameToLoad);
                    PreloadAudio(); // Preload audio after successful load
                }
                else
                {
                    // Fallback: Try loading from the file system (primarily for Editor convenience)
                    string absolutePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, showFileNameToLoad + ".json");
                    absolutePath = absolutePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    // Debug.Log($"LoadShowData: Resources.Load failed. Looking for JSON at absolute path: {absolutePath}");
                    
                    if (File.Exists(absolutePath))
                    {
                        string jsonContent = File.ReadAllText(absolutePath);
                        // Debug.Log($"LoadShowData: Found JSON file via File.Exists: {absolutePath}, size: {jsonContent.Length} bytes");
                        ProcessLoadedJson(jsonContent, showFileNameToLoad);
                        PreloadAudio(); // Preload audio after successful load
                    }
                    else
                    {
                        // Debug.LogError($"Show file not found at: {absolutePath} or via Resources.Load for path: {resourcePath}");
                        // Ensure state is reset if load fails
                        ResetShowState(); 
                    }
                }
            }
            catch (Exception)
            {
                // Debug.LogError($"Error loading show data for '{showFileNameToLoad}': {ex.Message}");
                // Debug.LogError($"Stack trace: {ex.StackTrace}");
                // Ensure state is reset on error
                ResetShowState();
            }
        }

        // Helper method to process the loaded JSON content
        private void ProcessLoadedJson(string jsonContent, string sourceFileName)
        {
            showData = JsonConvert.DeserializeObject<ShowData>(jsonContent);
            if (showData?.Config != null && showData.Episodes != null)
            {
                // Debug.Log($"Show data loaded successfully from '{sourceFileName}': {showData.Config.name} with {showData.Episodes.Count} episodes");
                // Log episode details
                for (int i = 0; i < showData.Episodes.Count; i++)
                {
                    var episode = showData.Episodes[i];
                    // Debug.Log($"Episode {i}: id = '{episode.id}', name = '{episode.name}', scenes = {episode.scenes?.Count ?? 0}");
                }
                // Set initial state after successful load
                playbackState = "init"; 
            }
            else
            {
                 // Debug.LogError($"Failed to deserialize valid ShowData from '{sourceFileName}'. JSON content might be malformed or missing expected structure.");
                 ResetShowState(); // Ensure state is reset if deserialization fails
            }
        }
        
        // Helper method to reset show-related state
        public void ResetShowState()
        {
             // Debug.Log("Resetting show state.");
             showData = null;
             currentEpisode = null;
             currentSceneIndex = -1;
             currentDialogueIndex = -1;
             playbackState = "unloaded"; // Or some appropriate initial state
             loadedShowFileName = null;
             audioCache.Clear(); 
             // Consider clearing actorAudioSources if actors change per show file, though current logic re-finds them.
        }

        private void PreloadAudio()
        {
            if (showData == null || showData.Episodes == null)
            {
                 // Debug.LogWarning("PreloadAudio: Cannot preload, showData is null or has no episodes.");
                 return;
            }

            // Clear existing cache before preloading new audio
            audioCache.Clear();
            // Debug.Log("Cleared existing audio cache before preloading.");

            StartCoroutine(PreloadAudioCoroutine());
        }

        private IEnumerator PreloadAudioCoroutine()
        {
            int totalFiles = 0;
            int loadedFiles = 0;

            // Count total files first
            foreach (var episode in showData.Episodes)
            {
                for (int sceneIndex = 0; sceneIndex < episode.scenes.Count; sceneIndex++)
                {
                    var scene = episode.scenes[sceneIndex];
                    totalFiles += scene.dialogue.Count;
                }
            }

            // Debug.Log($"Starting preload of {totalFiles} audio files...");

            // Now preload the files
            foreach (var episode in showData.Episodes)
            {
                for (int sceneIndex = 0; sceneIndex < episode.scenes.Count; sceneIndex++)
                {
                    var scene = episode.scenes[sceneIndex];
                    
                    for (int dialogueIndex = 0; dialogueIndex < scene.dialogue.Count; dialogueIndex++)
                    {
                        // Construct the audio file path and key
                        string audioFileName = $"{episode.id}_{sceneIndex + 1}_{dialogueIndex + 1}.mp3";
                        string audioKey = $"{episode.id}_{sceneIndex + 1}_{dialogueIndex + 1}";
                        
                        // Try loading from Resources first
                        string resourcePath = $"{episodesRootPath}/{episode.id}/audio/{audioFileName}".Replace(".mp3", "");
                        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
                        
                        if (clip == null)
                        {
                            // Try loading directly from Resources folder on disk if not in Resources.Load
                            string filePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, episode.id, "audio", audioFileName);
                            if (File.Exists(filePath))
                            {
                                // Use Unity's audio loading API for files in Assets folder
                                string assetPath = $"Assets/Resources/{episodesRootPath}/{episode.id}/audio/{audioFileName}";
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
                                // Debug.Log($"Preloaded {loadedFiles}/{totalFiles} audio files");
                            }
                        }
                        
                        // Yield every few files to avoid freezing the editor
                        if (loadedFiles % 5 == 0)
                            yield return null;
                    }
                }
            }
            
            // Debug.Log($"Audio preload complete. Loaded {loadedFiles}/{totalFiles} files.");
        }

        /// <summary>
        /// Handles the selection of an episode from the dropdown.
        /// Updates UI state and prepares for episode playback.
        /// </summary>
        /// <param name="index">Index of the selected episode in the dropdown</param>
        public void SelectEpisode(int index)
        {
            // Debug.Log($"SelectEpisode: Selecting episode at index {index}");
            
            if (showData != null && showData.Episodes != null)
            {
                // Debug.Log($"SelectEpisode: Show data has {showData.Episodes.Count} episodes");
                
                if (index >= 0 && index < showData.Episodes.Count)
                {
                    currentEpisode = showData.Episodes[index];
                    currentSceneIndex = -1;
                    currentDialogueIndex = -1;
                    playbackState = "init"; // Reset playback state when selecting a new episode
                    
                    // Reset the OutroCaller state to allow the next outro to play
                    OutroCaller outroCaller = FindObjectOfType<OutroCaller>();
                    if (outroCaller != null)
                    {
                        outroCaller.ResetOutroState();
                    }
                    else
                    {
                         // Debug.LogWarning("SelectEpisode could not find OutroCaller to reset its state.");
                    }

                    // Notify UX Manager that an episode has been selected/started
                    UXAnimationManager.Instance?.OnEpisodeStart();

                    // Debug.Log($"SelectEpisode: Selected episode: id = '{currentEpisode.id}', name = '{currentEpisode.name}'");
                    
                    // Invoke the OnEpisodeSelectedForDisplay event with premise or summary
                    string episodeName = !string.IsNullOrEmpty(currentEpisode.name) ? currentEpisode.name : currentEpisode.id;
                    string premise = !string.IsNullOrEmpty(currentEpisode.premise) ? currentEpisode.premise :
                                     !string.IsNullOrEmpty(currentEpisode.summary) ? currentEpisode.summary :
                                     "No description available.";
                    OnEpisodeSelectedForDisplay?.Invoke(episodeName, premise);
                    
                    // Verify the episode data
                    if (string.IsNullOrEmpty(currentEpisode.name))
                    {
                        // Debug.LogWarning($"SelectEpisode: Episode at index {index} has an empty name");
                    }
                    
                    if (currentEpisode.scenes == null || currentEpisode.scenes.Count == 0)
                    {
                        // Debug.LogWarning($"SelectEpisode: Episode at index {index} has no scenes");
                    }
                    else
                    {
                        // Debug.Log($"SelectEpisode: Episode has {currentEpisode.scenes.Count} scenes");
                    }
                }
                else
                {
                    // Debug.LogError($"SelectEpisode: Invalid episode index: {index}. Valid range: 0-{showData.Episodes.Count - 1}");
                }
            }
            else
            {
                // Debug.LogError("SelectEpisode: Show data is null or has no episodes. Call LoadShowData() first.");
            }
        }

        public void NextStep()
        {
            // Prevent advancing if paused or waiting for scene preparation
            if (isPaused || waitingForScenePreparation)
            {
                // Debug.LogWarning($"NextStep called but ShowRunner is paused ({isPaused}) or waiting for scene prep ({waitingForScenePreparation}). Ignoring.");
                return;
            }

            if (currentEpisode == null)
            {
                // Debug.LogWarning("No episode selected. Please select an episode first.");
                return;
            }

            switch (playbackState)
            {
                case "init":
                    // Debug.Log("Starting episode playback");
                    playbackState = "episode-loaded";
                    // THIS IS A GOOD SPOT FOR "ACTUAL START" if currentEpisode is valid
                    if (currentEpisode != null)
                    {
                        Debug.Log($"ShowRunner: Invoking OnEpisodeActualStart from NextStep (init state) for episode {currentEpisode.id}");
                        OnEpisodeActualStart?.Invoke(); 
                    }
                    break;

                case "episode-loaded":
                    currentSceneIndex++;
                    if (currentSceneIndex < currentEpisode.scenes.Count)
                    {
                        // --- Trigger Commercial Break Check ---
                        // Check if commercials should play before preparing the next scene.
                        // CommercialManager handles pausing/resuming the ShowRunner internally.
                        if (CommercialManager.Instance != null)
                        {
                            CommercialManager.Instance.TriggerCommercialBreak();
                        }
                        else
                        {
                             // Debug.LogWarning("CommercialManager instance not found. Cannot trigger commercial break check.");
                        }
                        // ---------------------------------------
                        
                        // Proceed with scene preparation
                        var scene = currentEpisode.scenes[currentSceneIndex];
                        // Debug.Log($"Loading scene {currentSceneIndex + 1}: {scene.location}");
                        
                        // Invoke the OnSceneChangedForDisplay event
                        OnSceneChangedForDisplay?.Invoke(scene.location ?? "Unnamed Scene");
                        
                        // Create a prepareScene event
                        EventData sceneEvent = new EventData
                        {
                            type = "prepareScene",
                            location = scene.location,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                        
                        // Set waiting state before processing the event
                        waitingForScenePreparation = true;
                        pendingSceneName = scene.location;
                        playbackState = "scene-preparing";
                        
                        eventProcessor.ProcessEvent(sceneEvent);
                        currentDialogueIndex = -1;
                        
                        // Note: we'll transition to scene-loaded when OnScenePreparationComplete is called
                    }
                    else
                    {
                        // --- Episode Finished --- 
                        // Debug.Log("All scenes in the episode completed.");
                        playbackState = "episode-unloaded"; // Final state
                        
                        // Create the completion data
                        EpisodeCompletionData completionData = new EpisodeCompletionData
                        {
                            // Construct the relative path used by YouTubeTranscriptGenerator
                            JsonFilePath = Path.Combine("Assets", "Resources", episodesRootPath, loadedShowFileName + ".json").Replace('\\', '/'),
                            EpisodeId = currentEpisode.id
                        };

                        // Invoke the completion event AFTER setting the final state
                        // Debug.Log($"Invoking OnLastDialogueComplete event for {completionData.EpisodeId} from {completionData.JsonFilePath}.");
                        OnLastDialogueComplete?.Invoke(completionData); 

                        // Note: Any outro logic (like the previously rejected OutroSequenceManager)
                        // would likely be triggered by a listener to OnLastDialogueComplete now,
                        // rather than being directly started here.
                    }
                    break;

                case "scene-preparing":
                    // Still preparing scene - we'll be notified when it's done
                    // Debug.Log("Scene is still being prepared. Waiting...");
                    break;

                case "scene-loaded":
                    currentDialogueIndex++;
                    var currentScene = currentEpisode.scenes[currentSceneIndex];
                    
                    if (currentDialogueIndex < currentScene.dialogue.Count)
                    {
                        var dialogue = currentScene.dialogue[currentDialogueIndex];
                        // Debug.Log($"{dialogue.actor}: \"{dialogue.line}\"");
                        
                        // Create a speak event
                        EventData speakEvent = new EventData
                        {
                            type = "speak",
                            actor = dialogue.actor,
                            line = dialogue.line,
                            action = dialogue.action ?? "normal",
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            // Add episode ID and scene/dialogue numbers for reference
                            episode = currentEpisode.id,
                            additionalData = $"scene:{currentSceneIndex + 1},dialogue:{currentDialogueIndex + 1}"
                        };
                        
                        // Check if the actor is 'tv'
                        if (dialogue.actor == "tv")
                        {
                             // Special handling for 'tv' actor: Process the event (e.g., for media display via SpeakPayloadManager)
                             // but skip audio playback and associated wait time.
                             eventProcessor.ProcessEvent(speakEvent);
                             
                             // If audio/wait is needed for 'tv' in the future, uncomment the lines below
                             // and potentially remove the direct state transition and NextStep() call.
                             // StartCoroutine(PlayDialogueAudio(speakEvent)); 
                             // playbackState = "dialogue"; 
                             
                             // Since there's no audio, transition state immediately back as if dialogue finished.
                             playbackState = "scene-loaded"; 
                             
                             // Auto-advance if not in manual mode, as there's no audio wait.
                             if (!manualMode)
                             {
                                 NextStep();
                             }
                        }
                        else
                        {
                            // Normal dialogue: Load and play the corresponding audio file
                            StartCoroutine(PlayDialogueAudio(speakEvent));
                            playbackState = "dialogue"; // Set state to wait for audio completion
                        }
                    }
                    else
                    {
                        // Debug.Log("Scene completed");
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
                    // Debug.Log("Show playback complete");
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
            // Debug.LogWarning($"Could not find actor '{actorName}' for audio playback. Using default audio source.");
            if (defaultAudioSource != null && !defaultAudioSource.enabled)
                defaultAudioSource.enabled = true;
            return defaultAudioSource;
        }
        
        private IEnumerator PlayDialogueAudio(EventData speakEvent)
        {
            // Wait indefinitely if paused
            while (isPaused)
            {
                yield return null;
            }
            
            // Get the audio clip key
            string audioKey = $"{currentEpisode.id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}";
            AudioClip dialogueClip = null;
            
            // Try to get from cache first
            if (audioCache.TryGetValue(audioKey, out dialogueClip))
            {
                // Debug.Log($"Using cached audio: {audioKey}");
            }
            else
            {
                // Try loading from Resources 
                string resourcePath = $"{episodesRootPath}/{currentEpisode.id}/audio/{currentEpisode.id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}".Replace(".mp3", "");
                dialogueClip = Resources.Load<AudioClip>(resourcePath);
                
                if (dialogueClip == null)
                {
                    // Try loading from disk Resources folder if not in Resources.Load
                    string filePath = Path.Combine(Application.dataPath, "Resources", episodesRootPath, currentEpisode.id, "audio", $"{currentEpisode.id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}.mp3");
                    if (File.Exists(filePath))
                    {
                        // Use Unity's audio loading API for files in Assets folder
                        string assetPath = $"Assets/Resources/{episodesRootPath}/{currentEpisode.id}/audio/{currentEpisode.id}_{currentSceneIndex + 1}_{currentDialogueIndex + 1}.mp3";
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

                // Check if this is the last line of the scene
                bool isLastLine = currentDialogueIndex == currentEpisode.scenes[currentSceneIndex].dialogue.Count - 1;
                // Check if this is the last scene in the episode
                bool isLastScene = currentSceneIndex == currentEpisode.scenes.Count - 1;
                // If it's the last line and we have a transition manager, notify it
                // Only trigger transition if NOT the last scene, or if IgnoreLastSceneTransition is false
                if (isLastLine && sceneTransitionManager != null)
                {
                    // Look ahead to the next scene for transition
                    string nextLocation = null;
                    if (currentSceneIndex + 1 < currentEpisode.scenes.Count)
                        nextLocation = currentEpisode.scenes[currentSceneIndex + 1].location;
                    // Only play transition if not the last scene, or if IgnoreLastSceneTransition is false
                    if (!isLastScene || !sceneTransitionManager.IgnoreLastSceneTransition)
                    {
                        sceneTransitionManager.OnLastLineOfScene(speakEvent, dialogueClip.length, nextLocation);
                    }
                }
                
                // Wait for the audio to finish (plus a small delay)
                float waitTime = dialogueClip.length + dialogueDelay;
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                // Debug.LogWarning($"Audio clip not found for: {audioKey}. Proceeding without audio.");
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
            // Debug.Log("GetCurrentEpisodeTitle called");
            
            if (currentEpisode == null)
            {
                // Debug.LogWarning("GetCurrentEpisodeTitle: currentEpisode is null");
                return "No Episode Selected";
            }
            
            // Debug.Log($"GetCurrentEpisodeTitle: currentEpisode.id = '{currentEpisode.id}'");
            // Debug.Log($"GetCurrentEpisodeTitle: currentEpisode.name = '{currentEpisode.name}'");
            
            if (string.IsNullOrEmpty(currentEpisode.name))
            {
                // Debug.LogWarning("GetCurrentEpisodeTitle: currentEpisode.name is null or empty, using id instead");
                return currentEpisode.id;
            }
            
            // Debug.Log($"GetCurrentEpisodeTitle: Returning name '{currentEpisode.name}' for episode id {currentEpisode.id}");
            return currentEpisode.name;
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
                // Debug.Log($"GetEpisodeTitles: Found {showData.Episodes.Count} episodes");
                foreach (var episode in showData.Episodes)
                {
                    // Debug.Log($"GetEpisodeTitles: Processing episode {episode.id} - {episode.name}");
                    string name = string.IsNullOrEmpty(episode.name) ? episode.id : episode.name;
                    titles.Add($"{episode.id}: {name}");
                }
            }
            else
            {
                // Debug.LogWarning("GetEpisodeTitles: showData or Episodes is null");
            }
            return titles;
        }
        
        public ShowData GetShowData()
        {
            return showData;
        }

        // Handle scene preparation complete event
        private void OnScenePreparationComplete(string sceneName)
        {
            // Only proceed if we were waiting for this specific scene
            if (waitingForScenePreparation && sceneName == pendingSceneName)
            {
                waitingForScenePreparation = false;
                pendingSceneName = null;
                playbackState = "scene-loaded";
                // Debug.Log("ShowRunner state updated to 'scene-loaded' after scene preparation completed.");
                
                // Notify UX Manager about the scene change
                UXAnimationManager.Instance?.OnSceneChanged(sceneName);

                // Now check if we should resume the ShowRunner
                // Resume only if we were paused waiting for a commercial break AND that break is now finished
                if (isPaused)
                {
                     // Scene is ready and we weren't paused for commercials, proceed if auto-play
                    if (!manualMode)
                    {
                        NextStep();
                    }
                }
            }
            else
            {
                 // Debug.LogWarning($"Received OnScenePreparationComplete for {sceneName}, but was not waiting for it or waiting for {pendingSceneName}. State: {playbackState}");
            }
        }

        // Clear all actor audio sources when the component is destroyed
        private void OnDestroy()
        {
            if (scenePreparationManager != null)
            {
                // Unsubscribe to prevent memory leaks
                scenePreparationManager.OnScenePreparationComplete -= OnScenePreparationComplete;
            }

            // Unsubscribe from UXAnimationManager event
            if (UXAnimationManager.Instance != null)
            {
                this.OnLastDialogueComplete -= UXAnimationManager.Instance.OnEpisodeEnd;
            }

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

        /// <summary>
        /// Creates and sends a scene preparation event to the EventProcessor.
        /// This triggers the scene loading and preparation process.
        /// </summary>
        /// <param name="sceneName">Name of the scene to prepare</param>
        private void PrepareScene(string sceneName)
        {
            // ... existing code ...
        }

        /// <summary>
        /// Creates and sends a speak event to the EventProcessor.
        /// This triggers character dialogue and animations.
        /// </summary>
        /// <param name="actor">Name of the speaking character</param>
        /// <param name="line">Dialogue line to speak</param>
        /// <param name="action">Action to perform while speaking</param>
        private void SendSpeakEvent(string actor, string line, string action)
        {
            // ... existing code ...
        }

        /// <summary>
        /// Gets the name of the currently loaded show file.
        /// </summary>
        /// <returns>The filename (without extension) or null if no file is loaded.</returns>
        public string GetLoadedShowFileName()
        {
            return loadedShowFileName;
        }

        // Helper method to get current episode ID for the recorder
        public string GetCurrentEpisodeId()
        {
            return currentEpisode?.id;
        }

        // --- Pause/Resume Functionality ---

        /// <summary>
        /// Pauses the ShowRunner's internal state progression.
        /// Prevents NextStep() from advancing and causes waiting coroutines to halt.
        /// Does NOT change Time.timeScale.
        /// </summary>
        public void PauseShow()
        {
            if (!isPaused)
            {
                isPaused = true;
                // Time.timeScale = 0f; // REMOVED: Don't pause global time
                // TODO: Consider pausing specific audio sources if necessary (e.g., background music)
                // Debug.Log("ShowRunner: Internal state PAUSED");
            }
            else
            {
                // Debug.LogWarning("ShowRunner: PauseShow called but already paused.");
            }
        }

        /// <summary>
        /// Resumes the ShowRunner's internal state progression.
        /// Allows NextStep() to advance and waiting coroutines to continue.
        /// Does NOT change Time.timeScale.
        /// </summary>
        public void ResumeShow()
        {
            if (isPaused)
            {
                isPaused = false;
                // Time.timeScale = 1f; // REMOVED: Don't resume global time
                 // TODO: Consider resuming specific audio sources if necessary
                // Debug.Log("ShowRunner: Internal state RESUMED");
            }
             else
            {
                 // Debug.LogWarning("ShowRunner: ResumeShow called but not paused.");
            }
        }

        /// <summary>
        /// Gets the location name defined in the JSON for the currently loaded scene.
        /// </summary>
        /// <returns>The location string (e.g., "podcast_desk") or null if no scene is loaded or index is invalid.</returns>
        public string GetCurrentSceneLocation()
        {
            if (currentEpisode != null && 
                currentSceneIndex >= 0 && 
                currentSceneIndex < currentEpisode.scenes.Count)
            {
                return currentEpisode.scenes[currentSceneIndex].location;
            }
            return null; // No valid scene loaded or index out of bounds
        }
    }
} 