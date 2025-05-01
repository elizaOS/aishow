using UnityEngine;
using UnityEngine.Video; // Required for VideoPlayer
using System.Collections; // Required for Coroutine
using UnityEngine.UI; // Required for UI components

namespace ShowRunner
{
    /// <summary>
    /// Listens for the episode completion event and triggers 
    /// the playback of an outro video GameObject.
    /// Ensures the video starts from the beginning.
    /// </summary>
    public class OutroCaller : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The GameObject containing the VideoPlayer for the outro. It will be activated by this script.")]
        [SerializeField] private GameObject outroVideoObject;

        [Tooltip("Reference to the EpisodeCompletionNotifier. If null, will try to find it in the scene.")]
        [SerializeField] private EpisodeCompletionNotifier completionNotifier;

        [Tooltip("The captions textbox GameObject that should be disabled before the outro plays.")]
        [SerializeField] private GameObject captionsTextbox;

        [Header("Fade Out")]
        [Tooltip("Panel used for fading to black after the video.")]
        [SerializeField] private Image fadePanel;
        
        [Tooltip("Duration of the fade to black effect in seconds.")]
        [SerializeField] private float fadeDuration = 1.5f;

        private VideoPlayer videoPlayer;

        private void Awake()
        {
            // Find Notifier if not assigned
            if (completionNotifier == null)
            {
                completionNotifier = FindObjectOfType<EpisodeCompletionNotifier>();
                if (completionNotifier == null)
                {
                    Debug.LogError("OutroCaller could not find EpisodeCompletionNotifier! Outro will not play.", this);
                    enabled = false;
                    return;
                }
            }

            // Validate the outro object
            if (outroVideoObject == null)
            {
                Debug.LogError("Outro Video Object is not assigned in OutroCaller! Outro will not play.", this);
                enabled = false;
                return;
            }

            // Try to get the VideoPlayer component early
            videoPlayer = outroVideoObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                 Debug.LogError($"No VideoPlayer component found on the assigned Outro Video Object ({outroVideoObject.name}). Outro cannot play.", this);
                 enabled = false;
                 return;
            }

            // Validate the fade panel
            if (fadePanel != null)
            {
                // Initialize fade panel to be transparent
                Color panelColor = fadePanel.color;
                panelColor.a = 0f;
                fadePanel.color = panelColor;
                fadePanel.gameObject.SetActive(true);
            }

            // Ensure the outro object is initially inactive
            outroVideoObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (completionNotifier != null)
            {
                Debug.Log("OutroCaller subscribing to EpisodeCompletionNotifier.OnEpisodePlaybackFinished.", this);
                completionNotifier.OnEpisodePlaybackFinished += StartOutroVideo;
            }
        }

        private void OnDisable()
        {
            if (completionNotifier != null)
            {
                Debug.Log("OutroCaller unsubscribing from EpisodeCompletionNotifier.OnEpisodePlaybackFinished.", this);
                completionNotifier.OnEpisodePlaybackFinished -= StartOutroVideo;
            }
        }

        /// <summary>
        /// Called when the EpisodeCompletionNotifier signals the end of the episode.
        /// Activates the outro video object and plays the video from the start.
        /// </summary>
        private void StartOutroVideo()
        {
            Debug.Log("Received episode completion signal. Starting outro video...", this);

            if (outroVideoObject == null || videoPlayer == null)
            {
                Debug.LogError("Cannot start outro video because references are missing.", this);
                return;
            }

            // Disable captions textbox if assigned
            if (captionsTextbox != null)
            {
                captionsTextbox.SetActive(false);
                Debug.Log("Disabled captions textbox.", this);
            }

            // Activate the parent GameObject
            outroVideoObject.SetActive(true);

            // Start the coroutine to handle video preparation and playback
            StartCoroutine(PlayVideoFromStart());
        }

        private IEnumerator PlayVideoFromStart()
        {
             // Ensure the player is stopped before modifying frame/preparing
             if (videoPlayer.isPlaying)
             {
                 videoPlayer.Stop();
             }

             // Reset to the beginning
             videoPlayer.frame = 0;
             Debug.Log("VideoPlayer frame reset to 0.", this);

            // Optional: Clear the target texture to avoid showing the last frame briefly
            if (videoPlayer.targetTexture != null)
            {
                RenderTexture rt = videoPlayer.targetTexture;
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.black); // Or your desired background color
                RenderTexture.active = null;
                 Debug.Log("Cleared VideoPlayer target texture.", this);
            }

             // Prepare the video (preloads data)
             Debug.Log("Preparing video...", this);
             videoPlayer.Prepare();

             // Wait until prepared
             float prepareTimeout = 10f; // Safety timeout
             float prepareTimer = 0f;
             while (!videoPlayer.isPrepared && prepareTimer < prepareTimeout)
             {
                 prepareTimer += Time.deltaTime;
                 yield return null; // Wait for next frame
             }

             if (!videoPlayer.isPrepared)
             {
                 Debug.LogError($"Video preparation timed out for {outroVideoObject.name}! Cannot play outro.", this);
                 outroVideoObject.SetActive(false); // Deactivate if prep fails
                 yield break; // Exit coroutine
             }

            // Preparation complete, play the video
            Debug.Log("Video prepared. Playing outro.", this);
            videoPlayer.Play();

            // Wait for video to finish before fading out
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
            
            Debug.Log("Outro video finished. Starting fade out.", this);
            
            // Start fade out if panel is assigned
            if (fadePanel != null)
            {
                yield return StartCoroutine(FadeToBlack());
            }
        }
        
        /// <summary>
        /// Fades the panel to black over the specified duration.
        /// </summary>
        private IEnumerator FadeToBlack()
        {
            if (fadePanel == null)
            {
                Debug.LogWarning("Cannot fade out - fade panel is not assigned.", this);
                yield break;
            }
            
            // Ensure the panel is visible
            fadePanel.gameObject.SetActive(true);
            
            // Get the current color
            Color panelColor = fadePanel.color;
            float startAlpha = panelColor.a;
            
            // Fade from current alpha to 1 (fully opaque)
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);
                
                panelColor.a = Mathf.Lerp(startAlpha, 1f, normalizedTime);
                fadePanel.color = panelColor;
                
                yield return null;
            }
            
            // Ensure we end at fully opaque
            panelColor.a = 1f;
            fadePanel.color = panelColor;
            
            Debug.Log("Fade out complete.", this);
            
            // Optional: You could signal completion here with an event if needed
        }
    }
} 