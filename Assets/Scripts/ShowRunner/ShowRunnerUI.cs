using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShowRunner
{
    public class ShowRunnerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown episodeDropdown;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Components")]
        [SerializeField] private ShowRunner showRunner;

        private bool autoPlay = false;
        private bool isPlaying = false;
        private float autoPlayTimer = 0f;
        
        private void Awake()
        {
            if (showRunner == null)
            {
                showRunner = FindObjectOfType<ShowRunner>();
                if (showRunner == null)
                {
                    Debug.LogError("ShowRunner not found! The UI won't function properly.");
                }
            }
        }

        private void Start()
        {
            // Set up event listeners
            if (loadButton != null) loadButton.onClick.AddListener(LoadSelectedEpisode);
            if (nextButton != null) nextButton.onClick.AddListener(NextStep);
            if (playButton != null) playButton.onClick.AddListener(StartAutoPlay);
            if (pauseButton != null) pauseButton.onClick.AddListener(StopAutoPlay);
            
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
            if (episodeDropdown != null)
            {
                // Get episode titles from ShowRunner
                List<string> episodeTitles = showRunner.GetEpisodeTitles();
                
                if (episodeTitles.Count > 0)
                {
                    episodeDropdown.ClearOptions();
                    episodeDropdown.AddOptions(episodeTitles);
                    episodeDropdown.value = 0;
                }
                else
                {
                    Debug.LogWarning("No episodes available in the show data");
                }
            }
        }

        public void LoadSelectedEpisode()
        {
            if (episodeDropdown != null)
            {
                int selectedIndex = episodeDropdown.value;
                showRunner.SelectEpisode(selectedIndex);
                UpdateStatusText($"Episode loaded: {showRunner.GetCurrentEpisodeTitle()}");
                
                // Enable the next button after episode is loaded
                if (nextButton != null) nextButton.interactable = true;
                if (playButton != null) playButton.interactable = true;
            }
        }

        public void NextStep()
        {
            showRunner.NextStep();
            UpdateStatusText("Advancing to next step...");
        }

        public void StartAutoPlay()
        {
            autoPlay = true;
            autoPlayTimer = 0.1f; // Start almost immediately
            UpdateStatusText("Auto playback started");
            // Switch ShowRunner into full auto mode
            showRunner.SetManualMode(false);
            
            // Update button states
            if (playButton != null) playButton.interactable = false;
            if (pauseButton != null) pauseButton.interactable = true;
        }

        public void StopAutoPlay()
        {
            autoPlay = false;
            UpdateStatusText("Auto playback paused");
            // Switch ShowRunner back into manual mode
            showRunner.SetManualMode(true);
            
            // Update button states
            if (playButton != null) playButton.interactable = true;
            if (pauseButton != null) pauseButton.interactable = false;
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        // Example method to refresh data if show data is reloaded
        public void RefreshShowData()
        {
            showRunner.LoadShowData();
            InitializeEpisodeDropdown();
            UpdateStatusText("Show data refreshed");
        }
    }
} 