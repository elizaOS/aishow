using UnityEngine;
using System;

namespace ShowRunner
{
    /// <summary>
    /// Listens for the completion event from ShowRunner 
    /// and notifies other systems that the episode playback is finished.
    /// This acts as a decoupled notification layer.
    /// </summary>
    public class EpisodeCompletionNotifier : MonoBehaviour
    {
        [Tooltip("Optional reference to the ShowRunner. If null, will attempt to find it automatically.")]
        [SerializeField] private ShowRunner showRunnerInstance;

        /// <summary>
        /// Event fired when this notifier detects that the ShowRunner has completed the episode.
        /// Other systems (like an outro video player) should listen to this.
        /// </summary>
        public event Action OnEpisodePlaybackFinished;

        private bool isSubscribed = false;

        private void Awake()
        {
            // Find ShowRunner if not assigned in Inspector
            if (showRunnerInstance == null)
            {
                showRunnerInstance = FindObjectOfType<ShowRunner>();
                 if (showRunnerInstance == null)
                 {
                    //Debug.LogError("EpisodeCompletionNotifier could not find the ShowRunner instance! Cannot subscribe to completion event.", this);
                    enabled = false; // Disable self if ShowRunner not found
                 }
            }
        }

        private void OnEnable()
        {
            // Subscribe to the ShowRunner event if we have an instance and haven't already
            if (showRunnerInstance != null && !isSubscribed)
            {
                //Debug.Log("EpisodeCompletionNotifier subscribing to ShowRunner.OnLastDialogueComplete.", this);
                showRunnerInstance.OnLastDialogueComplete += HandleEpisodeComplete;
                isSubscribed = true;
            }
            else if(showRunnerInstance == null)
            {
                //Debug.LogWarning("EpisodeCompletionNotifier OnEnable: ShowRunner instance is still null. Ensure it exists in the scene before this component enables.", this);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            if (showRunnerInstance != null && isSubscribed)
            {
                 //Debug.Log("EpisodeCompletionNotifier unsubscribing from ShowRunner.OnLastDialogueComplete.", this);
                showRunnerInstance.OnLastDialogueComplete -= HandleEpisodeComplete;
                isSubscribed = false;
            }
        }

        /// <summary>
        /// Handler method called when ShowRunner finishes the last dialogue.
        /// Expects completion data (JsonFilePath, EpisodeId) from ShowRunner.
        /// </summary>
        private void HandleEpisodeComplete(EpisodeCompletionData completionData)
        {
            //Debug.Log($"EpisodeCompletionNotifier received OnLastDialogueComplete from ShowRunner for Episode {completionData.EpisodeId}! Firing OnEpisodePlaybackFinished.", this);

            // Notify any listeners that the episode playback is truly finished.
            OnEpisodePlaybackFinished?.Invoke();

            // Now, invoke the EventManager event with the received data
            EventManager.InvokeEpisodeComplete(completionData);

            // --- TODO: Add your outro video trigger logic here --- 
            // Example: FindObjectOfType<OutroVideoPlayer>()?.PlayOutro();
        } 
    }
} 