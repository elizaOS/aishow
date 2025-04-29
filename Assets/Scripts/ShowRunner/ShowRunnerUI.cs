using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShowRunner
{
    /// <summary>
    /// Manages the user interface for the ShowRunner system.
    /// Handles UI interactions, state updates, and user input processing.
    /// </summary>
    public class ShowRunnerUI : MonoBehaviour
    {
        /// <summary>Reference to the main ShowRunner instance</summary>
        [Header("Components")]
        [SerializeField] private ShowRunner showRunner;
        [SerializeField] private ShowRunnerUIContainer uiContainer;

        private bool autoPlay = false;
        private bool isPlaying = false;
        private float autoPlayTimer = 0f;
        
        private void Awake()
        {
            Debug.Log("ShowRunnerUI Awake called");
            
            if (showRunner == null)
            {
                showRunner = FindObjectOfType<ShowRunner>();
                if (showRunner == null)
                {
                    Debug.LogError("ShowRunner not found! The UI won't function properly.");
                }
                else
                {
                    Debug.Log("Found ShowRunner: " + showRunner.name);
                }
            }
            else
            {
                Debug.Log("ShowRunner is assigned: " + showRunner.name);
            }

            if (uiContainer == null)
            {
                // First try to find it in parent
                uiContainer = GetComponentInParent<ShowRunnerUIContainer>();
                
                // If not found in parent, try to find it in the scene
                if (uiContainer == null)
                {
                    uiContainer = FindObjectOfType<ShowRunnerUIContainer>();
                    if (uiContainer != null)
                    {
                        Debug.Log("Found ShowRunnerUIContainer in scene: " + uiContainer.name);
                    }
                }
                
                if (uiContainer == null)
                {
                    Debug.LogError("ShowRunnerUIContainer not found! The UI won't function properly.");
                }
                else
                {
                    Debug.Log("Found ShowRunnerUIContainer: " + uiContainer.name);
                }
            }
            else
            {
                Debug.Log("ShowRunnerUIContainer is assigned: " + uiContainer.name);
            }
        }

        /// <summary>
        /// Initializes the UI components and sets up event listeners.
        /// </summary>
        private void Start()
        {
            // Set up event listeners
            if (uiContainer.GetLoadButton() != null) 
                uiContainer.GetLoadButton().onClick.AddListener(LoadSelectedEpisode);
            if (uiContainer.GetNextButton() != null) 
                uiContainer.GetNextButton().onClick.AddListener(NextStep);
            if (uiContainer.GetPlayButton() != null) 
                uiContainer.GetPlayButton().onClick.AddListener(StartAutoPlay);
            if (uiContainer.GetPauseButton() != null) 
                uiContainer.GetPauseButton().onClick.AddListener(StopAutoPlay);
            
            // Load show data to ensure episodes are available
            showRunner.LoadShowData();

            // Initialize UI components
            InitializeEpisodeDropdown();
            UpdateStatusText("Ready to load show data");
        }

        private void Update()
        {
            // Auto-advance when in autoplay mode
            if (autoPlay && !isPlaying)
            {
                autoPlayTimer -= Time.deltaTime;
                if (autoPlayTimer <= 0)
                {
                    isPlaying = true;
                    NextStep();
                    autoPlayTimer = 0.5f; // Small delay between auto-steps
                    isPlaying = false;
                }
            }
        }

        private void InitializeEpisodeDropdown()
        {
            // Get episode titles from ShowRunner
            var episodeTitles = showRunner.GetEpisodeTitles();
            uiContainer.PopulateEpisodeDropdown(episodeTitles);
        }

        /// <summary>
        /// Handles the episode selection from the dropdown.
        /// Updates UI state and triggers episode loading.
        /// </summary>
        public void LoadSelectedEpisode()
        {
            int selectedIndex = uiContainer.GetSelectedEpisodeIndex();
            Debug.Log($"LoadSelectedEpisode: Selected index is {selectedIndex}");
            
            if (selectedIndex >= 0)
            {
                // First update the status to show we're loading
                UpdateStatusText("Loading episode...");
                Debug.Log("LoadSelectedEpisode: Status updated to 'Loading episode...'");
                
                // Select the episode in the ShowRunner
                showRunner.SelectEpisode(selectedIndex);
                Debug.Log($"LoadSelectedEpisode: Episode selected at index {selectedIndex}");
                
                // Get the episode title directly from the ShowRunner
                string episodeTitle = showRunner.GetCurrentEpisodeTitle();
                Debug.Log($"LoadSelectedEpisode: Retrieved episode title from ShowRunner: '{episodeTitle}'");
                
                // Update the status with the loaded episode title
                UpdateStatusText($"Episode loaded: {episodeTitle}");
                Debug.Log($"LoadSelectedEpisode: Status updated to 'Episode loaded: {episodeTitle}'");
                
                // Enable the playback controls after episode is loaded
                uiContainer.SetPlaybackControlsInteractable(true);
                Debug.Log("LoadSelectedEpisode: Playback controls enabled");
            }
            else
            {
                Debug.LogWarning("LoadSelectedEpisode: Invalid episode index selected");
                UpdateStatusText("Please select a valid episode");
            }
        }

        /// <summary>
        /// Advances to the next scene or dialogue entry.
        /// Updates UI state based on playback mode.
        /// </summary>
        public void NextStep()
        {
            showRunner.NextStep();
            UpdateStatusText("Advancing to next step...");
        }

        /// <summary>
        /// Toggles between play and pause states.
        /// Updates UI elements and ShowRunner state.
        /// </summary>
        public void StartAutoPlay()
        {
            autoPlay = true;
            autoPlayTimer = 0.1f; // Start almost immediately
            UpdateStatusText("Auto playback started");
            // Switch ShowRunner into full auto mode
            showRunner.SetManualMode(false);
            
            // Update button states
            uiContainer.SetAutoPlayControls(true);
        }

        /// <summary>
        /// Toggles between play and pause states.
        /// Updates UI elements and ShowRunner state.
        /// </summary>
        public void StopAutoPlay()
        {
            autoPlay = false;
            UpdateStatusText("Auto playback paused");
            // Switch ShowRunner back into manual mode
            showRunner.SetManualMode(true);
            
            // Update button states
            uiContainer.SetAutoPlayControls(false);
        }

        // Example method to refresh data if show data is reloaded
        public void RefreshShowData()
        {
            showRunner.LoadShowData();
            InitializeEpisodeDropdown();
            UpdateStatusText("Show data refreshed");
        }

        /// <summary>
        /// Updates UI elements based on the current show state.
        /// </summary>
        private void UpdateStatusText(string message)
        {
            Debug.Log($"ShowRunnerUI.UpdateStatusText called with message: '{message}'");
            
            if (uiContainer == null)
            {
                Debug.LogError("UI Container reference is null! Trying to find it again...");
                uiContainer = GetComponentInParent<ShowRunnerUIContainer>();
                
                if (uiContainer == null)
                {
                    Debug.LogError("Could not find UI Container. Status text will not be updated: " + message);
                    return;
                }
                
                Debug.Log("Found UI Container: " + uiContainer.name);
            }
            
            uiContainer.UpdateStatusText(message);
            Debug.Log($"Status text update requested: '{message}'");
        }
    }
} 