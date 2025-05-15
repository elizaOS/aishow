using UnityEngine;
using System;

namespace ShowRunner
{
    /// <summary>
    /// Manages splitscreen functionality by tracking location changes and activating/deactivating
    /// the splitscreen GameObject when appropriate.
    /// </summary>
    public class SplitScreenManager : MonoBehaviour
    {
        [Tooltip("The GameObject that contains the splitscreen UI elements")]
        [SerializeField] private GameObject splitScreenObject;

        [Tooltip("The name of the location that should trigger splitscreen mode")]
        [SerializeField] private string splitScreenLocationName = "splitscreen";

        private ShowRunner showRunner;
        private bool isSubscribed = false;
        private string currentLocation = null;

        private void Awake()
        {
            // Find ShowRunner instance
            showRunner = FindObjectOfType<ShowRunner>();
            if (showRunner == null)
            {
                Debug.LogError("SplitScreenManager: ShowRunner not found in scene!", this);
                enabled = false;
                return;
            }

            // Validate splitscreen object
            if (splitScreenObject == null)
            {
                Debug.LogError("SplitScreenManager: SplitScreen GameObject not assigned in inspector!", this);
                enabled = false;
                return;
            }

            // Ensure splitscreen is initially disabled
            splitScreenObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (showRunner != null && !isSubscribed)
            {
                showRunner.OnSceneChangedForDisplay += HandleSceneChanged;
                isSubscribed = true;
            }
        }

        private void OnDisable()
        {
            if (showRunner != null && isSubscribed)
            {
                showRunner.OnSceneChangedForDisplay -= HandleSceneChanged;
                isSubscribed = false;
            }
        }

        /// <summary>
        /// Handles scene changes and updates splitscreen state accordingly
        /// </summary>
        private void HandleSceneChanged(string newLocation)
        {
            if (string.IsNullOrEmpty(newLocation))
            {
                Debug.LogWarning("SplitScreenManager: Received null or empty location name", this);
                return;
            }

            // Update current location
            currentLocation = newLocation;

            // Check if we should be in splitscreen mode
            bool shouldBeSplitScreen = newLocation.Equals(splitScreenLocationName, StringComparison.OrdinalIgnoreCase);

            // Update splitscreen object state
            if (splitScreenObject.activeSelf != shouldBeSplitScreen)
            {
                splitScreenObject.SetActive(shouldBeSplitScreen);
                Debug.Log($"SplitScreenManager: SplitScreen {(shouldBeSplitScreen ? "activated" : "deactivated")} for location: {newLocation}", this);
            }
        }

        /// <summary>
        /// Gets the current location being displayed
        /// </summary>
        public string GetCurrentLocation()
        {
            return currentLocation;
        }

        /// <summary>
        /// Checks if splitscreen is currently active
        /// </summary>
        public bool IsSplitScreenActive()
        {
            return splitScreenObject != null && splitScreenObject.activeSelf;
        }
    }
} 