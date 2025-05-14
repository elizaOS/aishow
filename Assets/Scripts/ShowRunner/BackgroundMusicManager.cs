using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ShowRunner
{
    /// <summary>
    /// Manages background music playback based on scene location changes during the show.
    /// Plays appropriate audio clips concurrently with dialogue and handles transitions.
    /// </summary>
    public class BackgroundMusicManager : MonoBehaviour
    {
        // --- Injected Dependencies ---
        // These fields are populated by BackgroundMusicManagerSetup using reflection.
        // Ensure they match the field names used in the Setup script.
        private AudioSource backgroundAudioSource;
        private ScenePreperationManager scenePreparationManager;
        // private EpisodeCompletionNotifier episodeCompletionNotifier; // No longer used directly for episode end
        // --- End Injected Dependencies ---

        [System.Serializable]
        public struct LocationMusicMapping
        {
            public string locationName;
            [Tooltip("List of possible music clips for this location. One will be chosen at random each time.")]
            public List<AudioClip> musicClips; // Changed from single AudioClip to list
            [Range(0f, 1f)] public float volume;
        }

        [Header("Music Configuration")]
        [Tooltip("Map location names (from JSON) to specific background music clips and volumes.")]
        [SerializeField] private List<LocationMusicMapping> musicMappings = new List<LocationMusicMapping>();
        [SerializeField] private AudioClip defaultMusicClip; // Fallback music if location not found
        [Range(0f, 1f)] [SerializeField] private float defaultVolume = 0.3f;

        [Header("Transition Settings")]
        [Tooltip("Duration (in seconds) for the background music to fade IN.")]
        [SerializeField] private float fadeInDuration = 2.0f; // Default fade-in time
        [Tooltip("Duration (in seconds) for the background music to fade OUT.")]
        [SerializeField] private float fadeOutDuration = 3.0f; // Default fade-out time

        private Dictionary<string, LocationMusicMapping> musicMap = new Dictionary<string, LocationMusicMapping>();
        private Coroutine currentFadeCoroutine = null;
        private string currentLocation = null; // Track the current location for music
        private bool isSubscribed = false;
        private bool isFadedForCommercials = false; // Flag to track if music is muted for commercials
        private string locationBeforeCommercials = null; // Store location before commercials

        private void Awake()
        {
            // Build the dictionary for faster lookups
            foreach (var mapping in musicMappings)
            {
                // Accept mapping if it has a name and at least one valid clip
                if (!string.IsNullOrEmpty(mapping.locationName) && mapping.musicClips != null && mapping.musicClips.Count > 0)
                {
                    if (!musicMap.ContainsKey(mapping.locationName))
                    {
                        musicMap.Add(mapping.locationName, mapping);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate location name '{mapping.locationName}' found in music mappings. Using the first entry.", this);
                    }
                }
            }
        }

        // Subscribe in Start to ensure dependencies from Setup.Awake are ready 
        // and subscription happens before the first scene prep potentially finishes.
        private void Start()
        {
            // Check dependencies before subscribing
            // Dependencies should be injected by Setup script in Awake
            if (scenePreparationManager != null && ShowRunner.Instance != null && backgroundAudioSource != null && !isSubscribed)
            {
                //Debug.Log("BackgroundMusicManager subscribing to events.", this);
                scenePreparationManager.OnScenePreparationComplete += HandleScenePreparationComplete;
                // Subscribe to ShowRunner's event for precise end-of-dialogue timing
                ShowRunner.Instance.OnLastDialogueComplete += HandleLastDialogueComplete;
                isSubscribed = true;
            }
            else if (isSubscribed)
            {
                // Already subscribed
            }
            else
            {
                 // Dependencies might not be ready yet if Setup script hasn't run.
                 // Consider using StartCoroutine(WaitForDependenciesAndSubscribe()); if issues persist.
                Debug.LogWarning("BackgroundMusicManager Start: Dependencies not met. Subscription will be attempted later or might fail if Setup doesn't run.", this);
            }
        }

        // Unsubscribe in OnDisable as usual
        private void OnDisable()
        {
            if (isSubscribed)
            {
                Debug.Log("BackgroundMusicManager unsubscribing from events.", this);
                if (scenePreparationManager != null)
                {
                    scenePreparationManager.OnScenePreparationComplete -= HandleScenePreparationComplete;
                }
                // Unsubscribe from ShowRunner's event
                if (ShowRunner.Instance != null)
                {
                    ShowRunner.Instance.OnLastDialogueComplete -= HandleLastDialogueComplete;
                }
                isSubscribed = false;
            }
        }

        private void HandleScenePreparationComplete(string sceneName)
        {
            Debug.Log($"[BMM] HandleScenePreparationComplete triggered for Unity scene: '{sceneName}'");
            // If music is faded for commercials, don't process scene changes
            if (isFadedForCommercials)
            {
                Debug.Log("BackgroundMusicManager: Ignoring scene preparation complete event because commercials are active.", this);
                return;
            }

            // Note: The sceneName parameter here is the Unity scene name being loaded,
            // which might not directly match the 'location' property in the JSON.
            // We need the *location* from the ShowRunner's current scene data.
            
            ShowRunner showRunner = ShowRunner.Instance; // Assuming Singleton pattern
            if (showRunner == null)
            {
                Debug.LogError("[BMM] ShowRunner instance not found! Cannot determine current location.", this);
                return;
            }
            
            string newLocation = showRunner.GetCurrentSceneLocation();

            // === Log the result ===
            if (string.IsNullOrEmpty(newLocation))
            {
                // Updated log: We don't know the index for sure here, just that location is missing.
                Debug.LogWarning($"[BMM] GetCurrentSceneLocation() returned NULL or EMPTY when preparing Unity scene '{sceneName}'. Using default music.", this);
                PlayMusicForLocation(null); // Explicitly pass null if location is invalid
                return;
            }
            else
            {
                Debug.Log($"[BMM] GetCurrentSceneLocation() returned: '{newLocation}'");
            }

            Debug.Log($"[BMM] Scene preparation complete for location '{newLocation}' (Unity scene: '{sceneName}'). Current BMM location is '{currentLocation ?? "null"}'", this);

            // Only play if the location actually changed
            if (newLocation != currentLocation)
            {
                PlayMusicForLocation(newLocation);
            }
            else
            {
                // Location hasn't changed, maybe continue playing or restart?
                // For now, let existing music continue if it's the same location.
                Debug.Log($"BackgroundMusicManager: Location '{newLocation}' is the same as current. Music state unchanged.", this);
            }
        }

        // Renamed handler to reflect the event it's subscribed to
        private void HandleLastDialogueComplete(EpisodeCompletionData completionData)
        {
            Debug.Log($"BackgroundMusicManager: Last dialogue complete for episode '{completionData.EpisodeId}'. Fading out background music.", this);
            FadeOutMusic();
            currentLocation = null; // Reset location tracking
            isFadedForCommercials = false; // Ensure flag is reset if episode ends abruptly
        }

        private void PlayMusicForLocation(string locationName)
        {
            currentLocation = locationName; // Update current location tracking

            LocationMusicMapping mapping;
            AudioClip clipToPlay = defaultMusicClip;
            float targetVolume = defaultVolume;

            if (!string.IsNullOrEmpty(locationName) && musicMap.TryGetValue(locationName, out mapping))
            {
                // Randomly pick a clip from the list for this location
                if (mapping.musicClips != null && mapping.musicClips.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, mapping.musicClips.Count);
                    clipToPlay = mapping.musicClips[randomIndex];
                    targetVolume = mapping.volume;
                    Debug.Log($"BackgroundMusicManager: Randomly selected clip '{clipToPlay.name}' for location '{locationName}', Volume {targetVolume}", this);
                }
                else
                {
                    Debug.LogWarning($"BackgroundMusicManager: No music clips found in mapping for location '{locationName}'. Using default.", this);
                }
            }
            else
            {
                 Debug.Log($"BackgroundMusicManager: No specific music found for location '{locationName ?? "null"}'. Using default: '{defaultMusicClip?.name ?? "None"}', Volume {defaultVolume}", this);
            }

            // --- Determine if we need to fade out the *current* music --- 
            bool shouldFadeOutCurrent = backgroundAudioSource.isPlaying && 
                                       (backgroundAudioSource.clip != clipToPlay || clipToPlay == null);

            // Start fade out if needed
            if (shouldFadeOutCurrent)
            {
                Debug.Log($"BackgroundMusicManager: Fading out current clip '{backgroundAudioSource.clip?.name ?? "None"}'.", this);
                FadeOutMusic(); // Uses fadeOutDuration
            }

            // --- Play or adjust the new music --- 
            if (clipToPlay != null)
            {
                // Always randomize, so even if the same location is triggered, a new clip may be chosen
                Debug.Log($"BackgroundMusicManager: Starting fade in for new/randomized clip '{clipToPlay.name}'.", this);
                FadeInMusic(clipToPlay, targetVolume, true); // Always restart for new random
            }
            else
            {
                // No new clip to play, ensure fade out continues/completes if it wasn't triggered above
                if (!shouldFadeOutCurrent && backgroundAudioSource.isPlaying)
                {
                     Debug.LogWarning("BackgroundMusicManager: No music clip to play (neither specific nor default). Fading out existing.", this);
                     FadeOutMusic(); // Uses fadeOutDuration
                }
                else if (!shouldFadeOutCurrent)
                {
                     Debug.Log("BackgroundMusicManager: No music clip to play, and nothing was playing.", this);
                }
            }
        }

        // Added forceRestart parameter
        private void FadeInMusic(AudioClip clip, float targetVolume, bool forceRestart = true)
        {
            if (backgroundAudioSource == null) return;

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }

            // Only change clip and restart if necessary or forced
            if (forceRestart || backgroundAudioSource.clip != clip || !backgroundAudioSource.isPlaying)
            {
                backgroundAudioSource.clip = clip;
                // Don't reset volume to 0 if we're just adjusting the volume of an already playing clip
                if(forceRestart || !backgroundAudioSource.isPlaying) 
                    backgroundAudioSource.volume = 0f; 
                    
                backgroundAudioSource.Play(); 
                 Debug.Log($"BackgroundMusicManager: Starting/Restarting playback for clip '{clip.name}'", this);
            }
             else
            {
                 Debug.Log($"BackgroundMusicManager: Clip '{clip.name}' already playing, adjusting volume only.", this);
            }

            currentFadeCoroutine = StartCoroutine(FadeAudio(backgroundAudioSource, fadeInDuration, targetVolume));
        }

        private void FadeOutMusic()
        {
            if (backgroundAudioSource == null || !backgroundAudioSource.isPlaying)
            {
                 if (currentFadeCoroutine != null)
                 {
                      StopCoroutine(currentFadeCoroutine);
                      currentFadeCoroutine = null;
                 }
                 if(backgroundAudioSource != null) backgroundAudioSource.volume = 0f;
                 return;
            }

            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }
            currentFadeCoroutine = StartCoroutine(FadeAudio(backgroundAudioSource, fadeOutDuration, 0f, true));
        }

        /// <summary>
        /// Fades out the background music specifically for a commercial break.
        /// </summary>
        public void FadeOutForCommercials()
        {
            if (isFadedForCommercials) return; // Already fading/faded for commercials

            Debug.Log("BackgroundMusicManager: Fading out music for commercials.", this);
            isFadedForCommercials = true;
            locationBeforeCommercials = currentLocation; // Store the current location
            FadeOutMusic(); // Use the standard fade-out duration/logic
        }

        /// <summary>
        /// Resumes background music playback after commercials finish, returning to the pre-commercial state.
        /// </summary>
        public void ResumeAfterCommercials()
        {
            if (!isFadedForCommercials) return; // Not faded for commercials, nothing to resume

            Debug.Log($"BackgroundMusicManager: Resuming music after commercials.", this);
            isFadedForCommercials = false;

            // --- Get the CURRENT location after commercials --- 
            string currentLocationAfterCommercials = null;
            ShowRunner showRunner = ShowRunner.Instance; 
            if (showRunner != null)
            {
                currentLocationAfterCommercials = showRunner.GetCurrentSceneLocation();
                 Debug.Log($"BackgroundMusicManager: Current location after commercials is '{currentLocationAfterCommercials ?? "null"}'.", this);
            }
            else
            {
                 Debug.LogError("BackgroundMusicManager: ShowRunner instance not found when resuming after commercials! Cannot determine correct music.", this);
            }
            // --- End Get Current Location ---

            // Re-initiate music playback for the *current* location
            PlayMusicForLocation(currentLocationAfterCommercials);
            
            locationBeforeCommercials = null; // Clear the stored location
        }

        private IEnumerator FadeAudio(AudioSource audioSource, float duration, float targetVolume, bool stopWhenDone = false)
        {
             if (duration <= 0) // Handle zero or negative duration
            {
                audioSource.volume = targetVolume;
                if (stopWhenDone && targetVolume <= 0.01f) audioSource.Stop();
                Debug.Log($"BackgroundMusicManager: Fade duration <= 0. Set volume directly to {targetVolume} and {(stopWhenDone && targetVolume <= 0.01f ? "stopped." : "continued.")}", this);
                currentFadeCoroutine = null;
                yield break; 
            }
            
            float currentTime = 0;
            float startVolume = audioSource.volume;

            // Ensure the audio source is playing if fading in to a volume > 0 and it's not already playing
            if (!audioSource.isPlaying && targetVolume > 0.01f && audioSource.clip != null)
            {
                Debug.LogWarning("BackgroundMusicManager: FadeAudio detected audio source stopped unexpectedly during fade-in attempt. Restarting playback.", this);
                audioSource.volume = 0f; // Ensure starting from 0 if we have to restart
                startVolume = 0f;
                audioSource.Play();
            }

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
                yield return null;
            }

            audioSource.volume = targetVolume; // Ensure target volume is set precisely

            if (stopWhenDone && targetVolume <= 0.01f) // Use a small threshold for float comparison
            { 
                // Check volume again in case it changed slightly after the loop
                if(audioSource.volume <= 0.01f)
                {
                    audioSource.Stop();
                    Debug.Log("BackgroundMusicManager: Fade out complete. AudioSource stopped.", this);
                }
                else
                {
                    Debug.LogWarning($"BackgroundMusicManager: Fade out complete, but volume ({audioSource.volume}) was slightly above threshold. Stopping anyway.", this);
                     audioSource.Stop();
                }
            }
            else if (stopWhenDone)
            {
                 // Fade out finished, but target volume wasn't near zero
                 Debug.LogWarning($"FadeAudio completed fade out with stopWhenDone=true, but targetVolume ({targetVolume}) wasn't near zero. Stopping audio source.", this);
                 audioSource.Stop();
            }
            else
            {
                 // Fade in complete
                 Debug.Log($"BackgroundMusicManager: Fade complete. Volume at {audioSource.volume}", this);
            }

            currentFadeCoroutine = null; // Mark coroutine as finished
        }
    }
} 