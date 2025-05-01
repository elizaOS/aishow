using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
            //Debug.Log("ShowRunnerUI Awake called");
            
            if (showRunner == null)
            {
                showRunner = FindObjectOfType<ShowRunner>();
                if (showRunner == null)
                {
                    //Debug.LogError("ShowRunner not found! The UI won't function properly.");
                }
                else
                {
                    //Debug.Log("Found ShowRunner: " + showRunner.name);
                }
            }
            else
            {
                //Debug.Log("ShowRunner is assigned: " + showRunner.name);
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
                        //Debug.Log("Found ShowRunnerUIContainer in scene: " + uiContainer.name);
                    }
                }
                
                if (uiContainer == null)
                {
                    //Debug.LogError("ShowRunnerUIContainer not found! The UI won't function properly.");
                }
                else
                {
                    //Debug.Log("Found ShowRunnerUIContainer: " + uiContainer.name);
                }
            }
            else
            {
                //Debug.Log("ShowRunnerUIContainer is assigned: " + uiContainer.name);
            }
        }

        /// <summary>
        /// Initializes the UI components and sets up event listeners.
        /// </summary>
        private void Start()
        {
            // Set up event listeners (excluding Load button initially)
            // Load button listener is added after show file is selected
            // if (uiContainer.GetLoadButton() != null) 
            //     uiContainer.GetLoadButton().onClick.AddListener(LoadSelectedEpisode); 
            if (uiContainer.GetNextButton() != null) 
                uiContainer.GetNextButton().onClick.AddListener(NextStep);
            if (uiContainer.GetPlayButton() != null) 
                uiContainer.GetPlayButton().onClick.AddListener(StartAutoPlay);
            if (uiContainer.GetPauseButton() != null) 
                uiContainer.GetPauseButton().onClick.AddListener(StopAutoPlay);
            
            // REMOVED: showRunner.LoadShowData(); - Loading is now dynamic

            // Initialize UI components
            // InitializeEpisodeDropdown(); // Called after a show file is loaded
            InitializeShowFileSelection(); // Start by selecting a show file
            UpdateStatusText("Please select a show file.");
            uiContainer.SetPlaybackControlsInteractable(false); // Ensure playback is disabled initially
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

        /// <summary>
        /// Populates the episode dropdown based on the currently loaded show data.
        /// </summary>
        private void InitializeEpisodeDropdown()
        {
            if (showRunner == null || uiContainer == null) return;
            
            // Get episode titles from ShowRunner for the *currently loaded* show
            var episodeTitles = showRunner.GetEpisodeTitles(); 
            uiContainer.PopulateEpisodeDropdown(episodeTitles);

            // Add listener for the Load Episode button *only* if episodes are available
            var loadButton = uiContainer.GetLoadButton();
            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners(); // Clear previous listeners
                if (episodeTitles != null && episodeTitles.Count > 0)
                {
                    loadButton.onClick.AddListener(LoadSelectedEpisode);
                    // Interactability is handled by PopulateEpisodeDropdown
                }
            }
        }

        /// <summary>
        /// Initializes the show file selection dropdown.
        /// </summary>
        private void InitializeShowFileSelection()
        {
             if (showRunner == null || uiContainer == null) return;

             var showFileDropdown = uiContainer.GetShowFileDropdown();
             if (showFileDropdown == null)
             {
                 Debug.LogError("ShowFileDropdown reference is missing in UIContainer.");
                 return;
             }
             
             List<string> showFiles = showRunner.DiscoverShowFiles();
             uiContainer.PopulateShowFileDropdown(showFiles);

             // Add listener for selection changes
             showFileDropdown.onValueChanged.RemoveAllListeners(); // Clear previous listeners
             showFileDropdown.onValueChanged.AddListener(OnShowFileSelected); 

             // Initially clear episode dropdown and disable related controls
             uiContainer.PopulateEpisodeDropdown(new List<string>()); // Clear episodes
             uiContainer.SetEpisodeSelectionInteractable(false);
             uiContainer.SetPlaybackControlsInteractable(false);
        }
        
        /// <summary>
        /// Handles the selection of a show file from the dropdown.
        /// Loads the selected show data and updates the episode dropdown.
        /// </summary>
        /// <param name="index">The index of the selected show file.</param>
        private void OnShowFileSelected(int index)
        {
            if (showRunner == null || uiContainer == null) return;

            string selectedShowFileName = uiContainer.GetSelectedShowFileText();

            if (!string.IsNullOrEmpty(selectedShowFileName))
            {
                UpdateStatusText($"Loading show file: {selectedShowFileName}...");
                showRunner.LoadShowData(selectedShowFileName);
                
                // Check if data loaded successfully (ShowData is not null)
                if (showRunner.GetShowData() != null) 
                { 
                    UpdateStatusText("Show file loaded. Please select an episode.");
                    InitializeEpisodeDropdown(); // Populate episodes for the loaded show
                    // Episode dropdown interactability is handled by InitializeEpisodeDropdown
                    uiContainer.SetPlaybackControlsInteractable(false); // Keep playback disabled until episode loaded
                }
                else
                {
                     // LoadShowData failed (error logged by ShowRunner)
                     UpdateStatusText("Failed to load show file. Check console.");
                     uiContainer.PopulateEpisodeDropdown(new List<string>()); // Clear episodes
                     uiContainer.SetEpisodeSelectionInteractable(false);
                     uiContainer.SetPlaybackControlsInteractable(false);
                }
            }
            else
            { 
                // Placeholder selected
                UpdateStatusText("Please select a show file.");
                showRunner.ResetShowState(); // Clear any previously loaded show data
                uiContainer.PopulateEpisodeDropdown(new List<string>()); // Clear episodes
                uiContainer.SetEpisodeSelectionInteractable(false);
                uiContainer.SetPlaybackControlsInteractable(false);
            }
        }

        /// <summary>
        /// Handles the episode selection from the dropdown.
        /// Updates UI state and triggers episode loading.
        /// </summary>
        public void LoadSelectedEpisode()
        {
            int selectedIndex = uiContainer.GetSelectedEpisodeIndex();
            // Debug.Log($"LoadSelectedEpisode: Selected dropdown value index = {uiContainer.GetEpisodeDropdown()?.value}, Adjusted index = {selectedIndex}");
            
            if (selectedIndex >= 0) // Index is already adjusted for placeholder by GetSelectedEpisodeIndex
            {
                // First update the status to show we're loading
                UpdateStatusText("Loading episode...");
                //Debug.Log("LoadSelectedEpisode: Status updated to 'Loading episode...'");
                
                // Select the episode in the ShowRunner
                showRunner.SelectEpisode(selectedIndex);
                //Debug.Log($"LoadSelectedEpisode: Episode selected at index {selectedIndex}");
                
                // Get the episode title directly from the ShowRunner
                string episodeTitle = showRunner.GetCurrentEpisodeTitle();
                //Debug.Log($"LoadSelectedEpisode: Retrieved episode title from ShowRunner: '{episodeTitle}'");
                
                // Update the status with the loaded episode title
                UpdateStatusText($"Episode loaded: {episodeTitle}");
                //Debug.Log($"LoadSelectedEpisode: Status updated to 'Episode loaded: {episodeTitle}'");
                
                // Enable the playback controls after episode is loaded
                uiContainer.SetPlaybackControlsInteractable(true);
                //Debug.Log("LoadSelectedEpisode: Playback controls enabled");
            }
            else
            {
                // Debug.LogWarning("LoadSelectedEpisode: Invalid episode index selected (placeholder or error).");
                UpdateStatusText("Please select a valid episode from the list.");
                uiContainer.SetPlaybackControlsInteractable(false); // Disable playback controls
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
        // This might need adjustment depending on desired behavior (re-discover or just reload current)
        public void RefreshShowData()
        {
            // Option 1: Re-discover and reload the currently selected show file
            string currentShowFile = showRunner.GetLoadedShowFileName();
            InitializeShowFileSelection(); // Re-discovers files and sets up dropdown
            if (!string.IsNullOrEmpty(currentShowFile))
            {
                // Try to re-select the previously loaded file if it still exists
                // (Requires finding the index based on the name in the new list)
                // For simplicity, we might just reset to the default state:
                 UpdateStatusText("Show files re-discovered. Please select a show file.");
            }
            else
            {
                 UpdateStatusText("Show files re-discovered. Please select a show file.");
            }

            // Option 2: Just reload the currently loaded file (if any)
            // string currentShowFile = showRunner.GetLoadedShowFileName();
            // if (!string.IsNullOrEmpty(currentShowFile))
            // {
            //     OnShowFileSelected(uiContainer.GetShowFileDropdown().value); // Reload current
            // }
            // else
            // {
            //     InitializeShowFileSelection(); // If nothing was loaded, re-discover
            // }
            
        }

        /// <summary>
        /// Updates UI elements based on the current show state.
        /// </summary>
        private void UpdateStatusText(string message)
        {
            //Debug.Log($"ShowRunnerUI.UpdateStatusText called with message: '{message}'");
            
            if (uiContainer == null)
            {
                //Debug.LogError("UI Container reference is null! Trying to find it again...");
                uiContainer = GetComponentInParent<ShowRunnerUIContainer>();
                
                if (uiContainer == null)
                {
                    //Debug.LogError("Could not find UI Container. Status text will not be updated: " + message);
                    return;
                }
                
                //Debug.Log("Found UI Container: " + uiContainer.name);
            }
            
            uiContainer.UpdateStatusText(message);
            //Debug.Log($"Status text update requested: '{message}'");
        }
    }
} 