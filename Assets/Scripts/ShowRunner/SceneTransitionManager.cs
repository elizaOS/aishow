using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ShowRunner
{
    /// <summary>
    /// Manages scene transition animations that overlay between scene changes.
    /// Triggers a mask-based wipe transition before the next scene change.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [System.Serializable]
        public struct LocationTransitionMapping 
        {
            public string locationName;
            [Tooltip("The transition animation to use for this location")]
            public Animator transitionAnimator;
            [Tooltip("The name of the trigger parameter in the animator")]
            public string transitionTriggerParam;
            [Tooltip("The sound effect to play during transition")]
            public AudioClip transitionSound;
            [Range(0f, 1f)] public float volume; 
        }

        [Header("Transition Configuration")]
        [Tooltip("Map location names (from JSON) to specific transition animations and sounds.")]
        [SerializeField] private List<LocationTransitionMapping> transitionMappings = new List<LocationTransitionMapping>();
        [SerializeField] private Animator defaultTransitionAnimator; // Fallback transition
        [SerializeField] private string defaultTransitionTrigger = "StartTransition"; // Default trigger name
        [SerializeField] private AudioClip defaultTransitionSound; // Fallback sound
        [Range(0f, 1f)] [SerializeField] private float defaultVolume = 0.5f;

        [Header("Timing")]
        [SerializeField, Range(0f, 1f)] private float startTransitionAtAudioProgress = 0.8f;

        [Header("Behavior")]
        [Tooltip("If true, skips the transition animation/sound on the last scene of the episode (to avoid overlap with outro).")]
        [SerializeField] private bool ignoreLastSceneTransition = true;
        /// <summary>
        /// If true, skips the transition animation/sound on the last scene of the episode (to avoid overlap with outro).
        /// </summary>
        public bool IgnoreLastSceneTransition => ignoreLastSceneTransition;

        private Dictionary<string, LocationTransitionMapping> transitionMap = new Dictionary<string, LocationTransitionMapping>();
        private AudioSource transitionAudioSource;
        private ShowRunner showRunner;
        private bool isTransitioning = false;
        private Coroutine currentTransitionCoroutine;
        private string currentLocation = null;

        private void Awake()
        {
            showRunner = FindObjectOfType<ShowRunner>();

            if (showRunner == null)
            {
                Debug.LogError("SceneTransitionManager: ShowRunner not found!");
                return;
            }

            // Set up audio source if not assigned
            if (transitionAudioSource == null)
            {
                transitionAudioSource = gameObject.AddComponent<AudioSource>();
                transitionAudioSource.playOnAwake = false;
                transitionAudioSource.spatialBlend = 0f; // 2D sound
            }

            // Build the dictionary for faster lookups
            foreach (var mapping in transitionMappings)
            {
                if (!string.IsNullOrEmpty(mapping.locationName) && mapping.transitionAnimator != null)
                {
                    if (!transitionMap.ContainsKey(mapping.locationName))
                    {
                        transitionMap.Add(mapping.locationName, mapping);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate location name '{mapping.locationName}' found in transition mappings. Using the first entry.", this);
                    }
                }
            }

            // Subscribe to events
            showRunner.OnSceneChangedForDisplay += HandleSceneChanged;
        }

        private void OnDestroy()
        {
            if (showRunner != null)
            {
                showRunner.OnSceneChangedForDisplay -= HandleSceneChanged;
            }
        }

        private void HandleSceneChanged(string sceneName)
        {
            // Reset transition state when scene changes
            if (isTransitioning)
            {
                StopTransition();
            }

            // Update current location
            string newLocation = showRunner.GetCurrentSceneLocation();
            currentLocation = newLocation;
        }

        /// <summary>
        /// Called by ShowRunner when processing the last line of a scene.
        /// </summary>
        public void OnLastLineOfScene(EventData speakEvent, float audioLength, string nextLocation)
        {
            if (isTransitioning) return;

            // Calculate when to start the transition
            float startTime = audioLength * startTransitionAtAudioProgress;

            // Use the next location for the transition
            if (!string.IsNullOrEmpty(nextLocation))
            {
                currentLocation = nextLocation;
            }

            // Start the transition coroutine
            if (currentTransitionCoroutine != null)
            {
                StopCoroutine(currentTransitionCoroutine);
            }
            currentTransitionCoroutine = StartCoroutine(PlayTransitionCoroutine(startTime));
        }

        private IEnumerator PlayTransitionCoroutine(float startDelay)
        {
            // Wait until it's time to start the transition
            yield return new WaitForSeconds(startDelay);

            // Get the appropriate transition for current location
            LocationTransitionMapping mapping;
            Animator animatorToUse = defaultTransitionAnimator;
            string triggerToUse = defaultTransitionTrigger;
            AudioClip soundToUse = defaultTransitionSound;
            float volumeToUse = defaultVolume;

            if (!string.IsNullOrEmpty(currentLocation) && transitionMap.TryGetValue(currentLocation, out mapping))
            {
                Debug.Log($"[STM] Found mapping for location '{currentLocation}'. Using custom animator and trigger.");
                if (mapping.transitionAnimator != null)
                {
                    animatorToUse = mapping.transitionAnimator;
                }
                if (!string.IsNullOrEmpty(mapping.transitionTriggerParam))
                {
                    triggerToUse = mapping.transitionTriggerParam;
                }
                if (mapping.transitionSound != null)
                {
                    soundToUse = mapping.transitionSound;
                }
                volumeToUse = mapping.volume;
            }
            else
            {
                Debug.Log($"[STM] No mapping found for location '{currentLocation ?? "null"}'. Using default animator and trigger.");
            }

            // Log which animator and trigger will be used
            Debug.Log($"[STM] Using animator: '{animatorToUse?.name ?? "null"}', trigger: '{triggerToUse}' for location: '{currentLocation ?? "null"}'");

            // Start the transition animation
            isTransitioning = true;
            if (animatorToUse != null)
            {
                animatorToUse.SetTrigger(triggerToUse);
            }
            else
            {
                Debug.LogWarning("[STM] Animator to use is null! Transition will not play.");
            }

            // Play transition sound
            if (transitionAudioSource != null && soundToUse != null)
            {
                transitionAudioSource.volume = volumeToUse;
                transitionAudioSource.PlayOneShot(soundToUse);
            }
        }

        private void StopTransition()
        {
            isTransitioning = false;
            if (currentTransitionCoroutine != null)
            {
                StopCoroutine(currentTransitionCoroutine);
                currentTransitionCoroutine = null;
            }
        }
    }
} 