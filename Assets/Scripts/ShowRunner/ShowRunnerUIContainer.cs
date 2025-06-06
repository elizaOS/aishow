using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using ShowRunner.Utility;
#endif

namespace ShowRunner
{
    public class ShowRunnerUIContainer : MonoBehaviour
    {
        [Header("UI Canvas")]
        [SerializeField] private Canvas mainCanvas;

        [Header("UI Elements")]
        [SerializeField] private TMP_Dropdown showFileDropdown;
        [SerializeField] private TMP_Dropdown episodeDropdown;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Screenshot Controls")]
        [SerializeField] private Toggle autoScreenshotToggle;
        [SerializeField] private Toggle speakEventScreenshotToggle;

        [Header("Recording Controls")]
        [SerializeField] private Toggle showRecorderToggle;

        [Header("References")]
        [SerializeField] private ShowRunner showRunner;
        [SerializeField] private ShowRunnerUI uiController;
        [SerializeField] private ScreenshotManager screenshotManager;
#if UNITY_EDITOR
        [SerializeField] private ShowRecorder showRecorder;
#endif

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` key
        [SerializeField] private bool uiVisibleOnStart = true;

        private void Awake()
        {
            //Debug.Log("ShowRunnerUIContainer Awake called");
            
            // Check for status text component
            if (statusText == null)
            {
                //Debug.LogWarning("Status text component is not assigned in the Inspector. Trying to find it...");
                
                // Try to find it in children first
                statusText = GetComponentInChildren<TextMeshProUGUI>();
                
                // If not found in children, try to find it in the scene
                if (statusText == null)
                {
                    TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        if (text.name.Contains("Status") || text.name.Contains("status"))
                        {
                            statusText = text;
                            //Debug.Log("Found status text component in scene: " + statusText.name);
                            break;
                        }
                    }
                }
                
                if (statusText != null)
                {
                    //Debug.Log("Found status text component: " + statusText.name);
                }
                else
                {
                    //Debug.LogError("Could not find status text component. Please assign it manually.");
                }
            }
            else
            {
                //Debug.Log("Status text component is assigned: " + statusText.name);
            }
            
            // Check for other UI components
            if (showFileDropdown == null)
            {
                // Attempt to find it by name or tag if necessary
                // For simplicity, we assume it's assigned or easily findable
                // Example: showFileDropdown = GameObject.Find("ShowFileDropdownName").GetComponent<TMP_Dropdown>();
                // Debug.LogWarning("Show file dropdown not assigned."); 
            }
            if (episodeDropdown == null)
            {
                episodeDropdown = GetComponentInChildren<TMP_Dropdown>();
                if (episodeDropdown != null)
                {
                    //Debug.Log("Found episode dropdown in children: " + episodeDropdown.name);
                }
            }
            
            if (loadButton == null)
            {
                loadButton = GetComponentInChildren<Button>();
                if (loadButton != null)
                {
                    //Debug.Log("Found load button in children: " + loadButton.name);
                }
            }
            
            if (nextButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(); 
                foreach (Button button in buttons)
                {
                    if (button.name.Contains("Next"))
                    {
                        nextButton = button;
                        //Debug.Log("Found next button in children: " + nextButton.name);
                        break;
                    }
                }
            }
            
            if (playButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>();
                foreach (Button button in buttons)
                {
                    if (button.name.Contains("Play"))
                    {
                        playButton = button;
                        //Debug.Log("Found play button in children: " + playButton.name);
                        break;
                    }
                }
            }
            
            if (pauseButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>();
                foreach (Button button in buttons)
                {
                    if (button.name.Contains("Pause"))
                    {
                        pauseButton = button;
                        //Debug.Log("Found pause button in children: " + pauseButton.name);
                        break;
                    }
                }
            }
            
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

            if (uiController == null)
            {
                uiController = GetComponentInChildren<ShowRunnerUI>();
                if (uiController == null)
                {
                    //Debug.LogError("ShowRunnerUI not found! The UI won't function properly.");
                }
                else
                {
                    //Debug.Log("Found ShowRunnerUI in children: " + uiController.name);
                }
            }
            else
            {
                //Debug.Log("ShowRunnerUI is assigned: " + uiController.name);
            }
        }

        private void Start()
        {
            // Initialize UI state
            SetUIVisible(uiVisibleOnStart);
            SetUIInteractable(true); // Make UI interactable by default
            UpdateStatusText("Ready to load show data");
        }

        private void Update()
        {
            // Toggle UI visibility with the toggle key
            if (Input.GetKeyDown(toggleKey))
            {
                bool newVisibility = !mainCanvas.enabled;
                SetUIVisible(newVisibility);
                //Debug.Log($"UI visibility toggled: {(newVisibility ? "Visible" : "Hidden")}");
            }
        }

        private void OnEnable()
        {
            // Ensure status text is found when the component is enabled
            EnsureStatusTextComponent();
        }
        
        private void EnsureStatusTextComponent()
        {
            if (statusText == null)
            {
                //Debug.LogWarning("Status text component is null in OnEnable. Trying to find it...");
                
                // Try to find it in children
                statusText = GetComponentInChildren<TextMeshProUGUI>();
                
                // If not found in children, try to find it in the scene
                if (statusText == null)
                {
                    TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
                    foreach (TextMeshProUGUI text in allTexts)
                    {
                        if (text.name.Contains("Status") || text.name.Contains("status"))
                        {
                            statusText = text;
                            //Debug.Log("Found status text component in scene: " + statusText.name);
                            break;
                        }
                    }
                }
                
                if (statusText != null)
                {
                    //Debug.Log("Found status text component: " + statusText.name);
                }
                else
                {
                    //Debug.LogError("Could not find status text component. Please assign it manually.");
                }
            }
        }

        public void SetUIVisible(bool visible)
        {
            if (mainCanvas != null)
            {
                mainCanvas.enabled = visible;
            }
        }

        public void SetUIInteractable(bool interactable)
        {
            if (loadButton != null) loadButton.interactable = false; // Starts disabled until a show file and episode are selected
            if (nextButton != null) nextButton.interactable = false; // Always starts disabled
            if (playButton != null) playButton.interactable = false; // Always starts disabled
            if (pauseButton != null) pauseButton.interactable = false; // Always starts disabled
            if (showFileDropdown != null) showFileDropdown.interactable = interactable; // Show file dropdown should be interactable
            if (episodeDropdown != null) episodeDropdown.interactable = false; // Episode dropdown starts disabled
        }

        public void SetEpisodeSelectionInteractable(bool interactable)
        {
            if (episodeDropdown != null) episodeDropdown.interactable = interactable;
            if (loadButton != null) loadButton.interactable = interactable; // Enable load button when episodes are available
        }

        public void SetPlaybackControlsInteractable(bool interactable)
        {
            if (nextButton != null) nextButton.interactable = interactable;
            if (playButton != null) playButton.interactable = interactable;
        }

        public void SetAutoPlayControls(bool isAutoPlaying)
        {
            if (playButton != null) playButton.interactable = !isAutoPlaying;
            if (pauseButton != null) pauseButton.interactable = isAutoPlaying;
        }

        public void UpdateStatusText(string message)
        {
            //Debug.Log($"UpdateStatusText called with message: '{message}'");
            
            if (statusText == null)
            {
                //Debug.LogError("Status text component is null! Please assign it in the Inspector.");
                return;
            }
            
            statusText.text = message;
            //Debug.Log($"Status text updated to: '{message}'");
        }

        /// <summary>
        /// Populates the show file dropdown with discovered show archive names.
        /// </summary>
        /// <param name="showFileNames">List of show file names (without extension).</param>
        public void PopulateShowFileDropdown(List<string> showFileNames)
        {
            if (showFileDropdown == null) return;

            showFileDropdown.ClearOptions();
            if (showFileNames == null || showFileNames.Count == 0)
            {
                showFileDropdown.options.Add(new TMP_Dropdown.OptionData("-- No Shows Found --"));
                showFileDropdown.interactable = false; 
            }
            else
            {
                // Add a placeholder option first
                List<string> options = new List<string> { "-- Select Show File --" };
                options.AddRange(showFileNames);
                showFileDropdown.AddOptions(options);
                showFileDropdown.interactable = true;
            }
            showFileDropdown.value = 0; // Select the placeholder
            showFileDropdown.RefreshShownValue();
        }

        /// <summary>
        /// Gets the index of the selected item in the show file dropdown.
        /// Returns -1 if the placeholder is selected or dropdown is invalid.
        /// </summary>
        public int GetSelectedShowFileIndex()
        {
            if (showFileDropdown == null || showFileDropdown.value == 0) // Index 0 is the placeholder
                return -1; 
            return showFileDropdown.value;
        }

        /// <summary>
        /// Gets the text of the selected item in the show file dropdown.
        /// Returns null if the placeholder is selected or dropdown is invalid.
        /// </summary>
        public string GetSelectedShowFileText()
        {
             if (showFileDropdown == null || showFileDropdown.value == 0) // Index 0 is the placeholder
                return null; 
            return showFileDropdown.options[showFileDropdown.value].text;
        }

        public void PopulateEpisodeDropdown(System.Collections.Generic.List<string> episodeTitles)
        {
            if (episodeDropdown == null)
            {
                //Debug.LogError("PopulateEpisodeDropdown: episodeDropdown is null");
                return;
            }

            //Debug.Log($"PopulateEpisodeDropdown: Received {episodeTitles?.Count ?? 0} episode titles");
            if (episodeTitles != null)
            {
                foreach (var title in episodeTitles)
                {
                    //Debug.Log($"PopulateEpisodeDropdown: Processing title: {title}");
                }
            }

            episodeDropdown.ClearOptions();

            if (episodeTitles == null || episodeTitles.Count == 0)
            {
                //Debug.LogWarning("PopulateEpisodeDropdown: No episode titles provided");
                episodeDropdown.options.Add(new TMP_Dropdown.OptionData("-- No Episodes --"));
                SetEpisodeSelectionInteractable(false); // Disable dropdown and load button
            }
            else
            {
                // Add a placeholder first if needed, or just the titles
                List<string> options = new List<string> { "-- Select Episode --" };
                options.AddRange(episodeTitles);
                //Debug.Log($"PopulateEpisodeDropdown: Adding {options.Count} options to dropdown");
                episodeDropdown.AddOptions(options);
                SetEpisodeSelectionInteractable(true); // Enable dropdown and load button
            }
            
            episodeDropdown.value = 0; // Default to the first item (placeholder or first episode)
            episodeDropdown.RefreshShownValue();
            //Debug.Log($"PopulateEpisodeDropdown: Dropdown now has {episodeDropdown.options.Count} options");
        }

        public int GetSelectedEpisodeIndex()
        {
            if (episodeDropdown == null || episodeDropdown.value == 0) // Index 0 is placeholder
            {
                 //Debug.LogWarning("GetSelectedEpisodeIndex: Placeholder selected or dropdown invalid.");
                 return -1; 
            }
            // Adjust index because of the placeholder at index 0
            return episodeDropdown.value - 1; 
        }

        // Getters for UI components that need to be accessed by ShowRunnerUI
        public TMP_Dropdown GetShowFileDropdown() => showFileDropdown;
        public TMP_Dropdown GetEpisodeDropdown() => episodeDropdown;
        public Button GetLoadButton() => loadButton;
        public Button GetNextButton() => nextButton;
        public Button GetPlayButton() => playButton;
        public Button GetPauseButton() => pauseButton;
        public TextMeshProUGUI GetStatusText() => statusText;

        public void InitializeScreenshotToggles()
        {
            if (screenshotManager != null)
            {
                // Set initial toggle states based on ScreenshotManager settings
                if (autoScreenshotToggle != null)
                {
                    autoScreenshotToggle.isOn = screenshotManager.isAutoScreenshottingEnabled;
                }
                if (speakEventScreenshotToggle != null)
                {
                    speakEventScreenshotToggle.isOn = screenshotManager.enableSpeakEventScreenshots;
                }
            }

            // Initialize recorder toggle
            if (showRecorderToggle != null)
            {
                #if UNITY_EDITOR
                showRecorderToggle.interactable = true;
                // Set initial state based on ShowRecorder component's enabled state
                var recorder = FindObjectOfType<ShowRecorder>();
                if (recorder != null)
                {
                    showRecorderToggle.isOn = recorder.enabled;
                }
                #else
                showRecorderToggle.interactable = false;
                #endif
            }
        }

        // Getters for screenshot toggles
        public Toggle GetAutoScreenshotToggle() => autoScreenshotToggle;
        public Toggle GetSpeakEventScreenshotToggle() => speakEventScreenshotToggle;
        public Toggle GetShowRecorderToggle() => showRecorderToggle;
    }
} 