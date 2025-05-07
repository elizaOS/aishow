using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ShowRunner; // <<< Ensure ShowRunner namespace is included

namespace ShowRunner.UI // Assuming a UI namespace within ShowRunner
{
    /// <summary>
    /// Manages the animation states (show/hide) of multiple UI elements based on game events and timing.
    /// </summary>
    public class UXAnimationManager : MonoBehaviour
    {
        [System.Serializable]
        public class ManagedAnimatorInfo
        {
            public string description = "UI Element"; // For editor clarity
            public Animator animator;
            public string stateParameterName = "IsVisible"; // Name of the bool parameter in the Animator
            public string specificLocation = ""; // Location that triggers the specific animation
        }

        [Header("Managed Animators")]
        [Tooltip("List of UI animators managed by this system.")]
        [SerializeField] private List<ManagedAnimatorInfo> managedAnimators = new List<ManagedAnimatorInfo>();

        [Header("Auto-Hide Settings")]
        [Tooltip("Enable automatically hiding the UI after a delay.")]
        [SerializeField] private bool autoHideEnabled = true;
        [Tooltip("Delay in seconds before automatically hiding the UI.")]
        [SerializeField] private float autoHideDelay = 20.0f;

        // Singleton instance
        public static UXAnimationManager Instance { get; private set; }

        private Coroutine autoHideCoroutine = null;
        private bool isHidden = true; // Start assuming hidden

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of UXAnimationManager already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Initially show all UX elements when the scene starts (or manager becomes active)
            // You might adjust this depending on the desired initial state.
            // ShowAllManagedUX(); // <<< REMOVED

            // Start the auto-hide timer if enabled
            // StartAutoHideTimer(); // <<< REMOVED (Timer will start on first explicit Show or relevant SceneChange)
        }

        /// <summary>
        /// Sets the state parameter to true for all managed animators.
        /// </summary>
        public void ShowAllManagedUX()
        {
            Debug.Log("UXAnimationManager: Setting all managed UX to VISIBLE.");
            isHidden = false;
            CancelAutoHideTimer(); // Cancel auto-hide when explicitly shown

            foreach (var info in managedAnimators)
            {
                if (info.animator != null && !string.IsNullOrEmpty(info.stateParameterName))
                {
                    info.animator.SetBool(info.stateParameterName, true);
                }
                 else if(info.animator == null)
                {
                     Debug.LogWarning($"UXAnimationManager: Animator is null for '{info.description}'. Cannot set state parameter '{info.stateParameterName}'.");
                }
                else if (string.IsNullOrEmpty(info.stateParameterName))
                {
                    Debug.LogWarning($"UXAnimationManager: State Parameter Name is empty for '{info.description}'. Cannot set state.");
                }
            }

            // Restart the timer if auto-hide is enabled
            StartAutoHideTimer();
        }

        /// <summary>
        /// Sets the state parameter to false for all managed animators.
        /// </summary>
        public void HideAllManagedUX()
        {
            Debug.Log("UXAnimationManager: Setting all managed UX to HIDDEN.");
            isHidden = true;
            // Do not cancel the auto-hide timer here, as this might BE the auto-hide action.

            foreach (var info in managedAnimators)
            {
                if (info.animator != null && !string.IsNullOrEmpty(info.stateParameterName))
                {
                    info.animator.SetBool(info.stateParameterName, false);
                }
                 else if(info.animator == null)
                {
                     Debug.LogWarning($"UXAnimationManager: Animator is null for '{info.description}'. Cannot set state parameter '{info.stateParameterName}'.");
                }
                 else if (string.IsNullOrEmpty(info.stateParameterName))
                {
                    Debug.LogWarning($"UXAnimationManager: State Parameter Name is empty for '{info.description}'. Cannot set state.");
                }
            }
        }

        /// <summary>
        /// Starts the auto-hide timer if enabled and not already running.
        /// </summary>
        private void StartAutoHideTimer()
        {
            // Only start if enabled and not already hidden
            if (autoHideEnabled && !isHidden && autoHideCoroutine == null)
            {
                autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
            }
        }

        /// <summary>
        /// Cancels the currently running auto-hide timer, if any.
        /// </summary>
        private void CancelAutoHideTimer()
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
                 Debug.Log("UXAnimationManager: Auto-hide timer cancelled.");
            }
        }

        /// <summary>
        /// Coroutine that waits for the specified delay and then calls HideAllManagedUX.
        /// </summary>
        private IEnumerator AutoHideCoroutine()
        {
             Debug.Log($"UXAnimationManager: Starting auto-hide timer for {autoHideDelay} seconds.");
            yield return new WaitForSeconds(autoHideDelay);
             Debug.Log("UXAnimationManager: Auto-hide timer finished.");
            HideAllManagedUX();
            autoHideCoroutine = null; // Reset coroutine handle
        }

        // --- Event Handlers (to be called by other systems) ---

        /// <summary>
        /// Called when a new episode starts playing.
        /// </summary>
        public void OnEpisodeStart()
        {
            Debug.Log("UXAnimationManager: Handling Episode Start.");
            // UI will be shown by the first OnSceneChanged call.
        }

        /// <summary>
        /// Called when the current episode finishes.
        /// Sets the state to hidden.
        /// </summary>
        public void OnEpisodeEnd(EpisodeCompletionData completionData)
        {
            Debug.Log($"UXAnimationManager: Handling Episode End ({completionData.EpisodeId}).");
            HideAllManagedUX(); // Hide UX when episode is done
        }

        /// <summary>
        /// Called when the scene changes.
        /// Sets the boolean state parameter on each animator based on location or overall state.
        /// </summary>
        /// <param name="newLocation">The location name of the newly loaded scene.</param>
        public void OnSceneChanged(string newLocation)
        {
            // Log entry into the function to detect multiple calls
            Debug.Log($"====== UXAnimationManager: OnSceneChanged START for location: '{newLocation}'. Time: {Time.time} ======");
            CancelAutoHideTimer(); // Always cancel timer first

            bool wasHiddenBeforeSceneChange = isHidden; // Store the previous overall state
            bool anyElementIsVisible = false; // Track if any element should be visible after this update

            foreach (var info in managedAnimators)
            {
                if (info.animator == null) continue; 
                if (string.IsNullOrEmpty(info.stateParameterName))
                {
                    Debug.LogWarning($"UXAnimationManager: State Parameter Name is empty for '{info.description}'. Cannot set state.");
                    continue;
                }

                bool shouldBeVisible = false;
                bool isLocationSpecific = !string.IsNullOrEmpty(info.specificLocation);

                // Determine desired state for this element
                if (isLocationSpecific)
                {
                    // Location-specific element: visible only if location matches
                    shouldBeVisible = info.specificLocation.Equals(newLocation, System.StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // General element: Should be visible only if the overall UI was already visible before this change.
                    // If the UI was hidden, general elements remain hidden initially.
                    shouldBeVisible = !wasHiddenBeforeSceneChange;
                    if(shouldBeVisible)
                         Debug.Log($"UX '{info.description}': General element, UI was visible. Should remain visible.");
                    else
                         Debug.Log($"UX '{info.description}': General element, UI was hidden. Should remain hidden.");
                }

                // Get the current state from the animator
                bool currentState = info.animator.GetBool(info.stateParameterName);

                // Log the values BEFORE the comparison
                Debug.Log($"UX '{info.description}': Checking state. ShouldBeVisible={shouldBeVisible}, CurrentState={currentState}");

                // Only set the bool if the desired state is different from the current state
                if (shouldBeVisible != currentState)
                {
                    Debug.Log($"    ----> STATE MISMATCH! Setting '{info.stateParameterName}' to {shouldBeVisible}.");
                    info.animator.SetBool(info.stateParameterName, shouldBeVisible);
                }
                
                // Track if any element is now set to be visible (use the final intended state)
                if (shouldBeVisible) 
                {
                    anyElementIsVisible = true;
                }
            }

            // Update the overall hidden state
            isHidden = !anyElementIsVisible;
            Debug.Log($"UXAnimationManager: Finished scene change processing. Final isHidden state: {isHidden}");

            // Start the timer only if the UI is now considered visible
            if (!isHidden)
            {
                StartAutoHideTimer();
            }
            Debug.Log($"====== UXAnimationManager: OnSceneChanged END for location: '{newLocation}'. Time: {Time.time} ======");
        }

        /// <summary>
        /// Called when a commercial break starts.
        /// Typically hides the main game UX.
        /// </summary>
        public void OnCommercialStart()
        {
            Debug.Log("UXAnimationManager: Handling Commercial Start.");
            CancelAutoHideTimer(); // Don't auto-hide during commercials
            HideAllManagedUX(); // Hide standard UX during commercials
        }

        /// <summary>
        /// Called when a commercial break ends.
        /// Typically restores the main game UX.
        /// </summary>
        public void OnCommercialEnd()
        {
            Debug.Log("UXAnimationManager: Handling Commercial End.");
            ShowAllManagedUX(); // Show standard UX after commercials
            // Auto-hide timer will be restarted by ShowAllManagedUX if needed
        }

        private void OnDestroy()
        {
            // Clean up singleton instance
            if (Instance == this)
            {
                Instance = null;
            }
            // Stop any running coroutines
            StopAllCoroutines();
        }
    }

    // Define outside the main class if used elsewhere, or keep nested if only used here.
    // Placeholder struct definition matching the one in ShowRunner
    // Ensure this matches the actual definition in ShowRunner.cs
    // [System.Serializable] // <<< REMOVED this struct definition
    // public struct EpisodeCompletionData
    // {
    //    public string JsonFilePath;
    //    public string EpisodeId;
    // }
} 