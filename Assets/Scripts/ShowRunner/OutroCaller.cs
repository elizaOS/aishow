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

        [Tooltip("The AudioSource component that will play the video's audio.")]
        [SerializeField] private AudioSource videoAudioSource;

        [Header("Fade Out")]
        [Tooltip("Panel used for fading to black after the video.")]
        [SerializeField] private Image fadePanel;
        
        [Tooltip("Duration of the fade to black effect in seconds.")]
        [SerializeField] private float fadeDuration = .3f;

        private VideoPlayer videoPlayer;
        private bool hasOutroStarted = false; // Flag to prevent double execution

        private void Awake()
        {
            Debug.Log("OutroCaller: Starting initialization...", this);

            // Find Notifier if not assigned
            if (completionNotifier == null)
            {
                completionNotifier = FindObjectOfType<EpisodeCompletionNotifier>();
                if (completionNotifier == null)
                {
                    Debug.LogWarning("OutroCaller could not find EpisodeCompletionNotifier. Will try to continue anyway.", this);
                }
            }

            // Validate the outro object
            if (outroVideoObject == null)
            {
                Debug.LogError("Outro Video Object is not assigned in OutroCaller! Outro will not play.", this);
                enabled = false;
                return;
            }

            // Get the VideoPlayer component
            videoPlayer = outroVideoObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError($"No VideoPlayer component found on {outroVideoObject.name}!", this);
                enabled = false;
                return;
            }

            // Configure audio settings
            if (videoAudioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.controlledAudioTrackCount = 1;
                videoPlayer.SetTargetAudioSource(0, videoAudioSource);
                
                // Optimize audio source
                videoAudioSource.playOnAwake = false;
                videoAudioSource.bypassEffects = true;
                videoAudioSource.bypassListenerEffects = true;
                videoAudioSource.bypassReverbZones = true;
                videoAudioSource.priority = 0;
                videoAudioSource.spatialBlend = 0f;
            }

            // Ensure VideoPlayer is properly configured
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.frameReady += OnFrameReady;

            // Ensure the outro object is initially inactive
            outroVideoObject.SetActive(false);
            
            Debug.Log("OutroCaller: Initialization complete", this);
        }

        private void OnEnable()
        {
            Debug.Log("OutroCaller: OnEnable called", this);
            if (completionNotifier != null)
            {
                Debug.Log("OutroCaller: Subscribing to EpisodeCompletionNotifier", this);
                completionNotifier.OnEpisodePlaybackFinished += StartOutroVideo;
            }
            else
            {
                Debug.LogWarning("OutroCaller: No EpisodeCompletionNotifier found to subscribe to!", this);
            }
        }

        private void OnDisable()
        {
            Debug.Log("OutroCaller: OnDisable called", this);
            if (completionNotifier != null)
            {
                Debug.Log("OutroCaller: Unsubscribing from EpisodeCompletionNotifier", this);
                completionNotifier.OnEpisodePlaybackFinished -= StartOutroVideo;
            }
        }

        /// <summary>
        /// Called when the EpisodeCompletionNotifier signals the end of the episode.
        /// Activates the outro video object and plays the video from the start.
        /// </summary>
        private void StartOutroVideo()
        {
            Debug.Log("OutroCaller: Starting outro video sequence...", this);
            
            if (hasOutroStarted)
            {
                Debug.LogWarning("Outro already started. Ignoring duplicate trigger.", this);
                return;
            }

            if (outroVideoObject == null || videoPlayer == null)
            {
                Debug.LogError("Critical components missing. Cannot start outro.", this);
                return;
            }

            hasOutroStarted = true;

            // Disable captions if present
            if (captionsTextbox != null)
            {
                captionsTextbox.SetActive(false);
            }

            // Activate and prepare video
            Debug.Log("Activating video object", this);
            outroVideoObject.SetActive(true);
            
            // Reset video player
            videoPlayer.Stop();
            videoPlayer.frame = 0;

            StartCoroutine(DelayedPlayVideoFromStart(fadeDuration));
        }

        private IEnumerator DelayedPlayVideoFromStart(float delay)
        {
            Debug.Log($"Waiting {delay} seconds before starting video...", this);
            yield return new WaitForSeconds(delay);

            if (!videoPlayer.isPrepared)
            {
                Debug.Log("Preparing video...", this);
                videoPlayer.Prepare();
                
                float prepareTimeout = 10f;
                float prepareTimer = 0f;
                
                while (!videoPlayer.isPrepared && prepareTimer < prepareTimeout)
                {
                    prepareTimer += Time.deltaTime;
                    yield return null;
                }

                if (!videoPlayer.isPrepared)
                {
                    Debug.LogError("Video preparation timed out!", this);
                    yield break;
                }
            }

            Debug.Log("Starting video playback...", this);
            videoPlayer.Play();

            while (videoPlayer.isPlaying)
            {
                yield return null;
            }

            Debug.Log("Video playback complete. Starting fade out...", this);
            
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

        /// <summary>
        /// Resets the state to allow the outro to play again for a new episode.
        /// </summary>
        public void ResetOutroState()
        {
            hasOutroStarted = false;
            // Optional: Stop video player if it was somehow left running
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                 videoPlayer.Stop();
                 outroVideoObject?.SetActive(false); // Ensure object is inactive too
            }
             // Ensure fade panel is reset
            if (fadePanel != null)
            {
                Color panelColor = fadePanel.color;
                panelColor.a = 0f;
                fadePanel.color = panelColor;
                fadePanel.gameObject.SetActive(true); // Keep it active but transparent
            }
            Debug.Log("OutroCaller state reset.", this);
        }

        private void OnFrameReady(VideoPlayer source, long frameIdx)
        {
            if (frameIdx == 0)
            {
                Debug.Log("First frame ready", this);
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.frameReady -= OnFrameReady;
            }
            
            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(false);
            }
        }
    }
} 