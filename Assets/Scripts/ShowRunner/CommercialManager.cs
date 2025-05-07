using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using ShowRunner.UI;

namespace ShowRunner 
{
    [Serializable]
    public class Commercial
    {
        public string name;
        public VideoClip videoClip;
    }

    [Serializable]
    public class CommercialBreak
    {
        public string breakName;
        public List<Commercial> commercials = new List<Commercial>();
        public bool skipThisBreak = false;
    }

    /// <summary>
    /// Manages the playback of commercials during scene transitions in the show.
    /// Hooks into ShowRunner's scene change events and pauses/resumes the show accordingly.
    /// </summary>
    public class CommercialManager : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private RawImage videoDisplay;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Canvas commercialCanvas;
        [SerializeField, Tooltip("Assign a UI Panel with a CanvasGroup for the fade-to-black effect after commercials.")] 
        private CanvasGroup blackFadePanel;
        private BackgroundMusicManager backgroundMusicManager;
        
        [Header("Configuration")]
        [Tooltip("List of commercial breaks that can be played sequentially or looped.")]
        public List<CommercialBreak> commercialBreaks = new List<CommercialBreak>();
        
        [Tooltip("Globally disable all commercial playback.")]
        public bool skipAllCommercials = false;
        
        [Tooltip("Skip commercials for the first N scene changes.")]
        public int skipFirstNSceneChanges = 0;
        
        [Header("Fade Settings")]
        [Tooltip("Duration of the fade-to-black after commercials finish.")]
        [SerializeField] private float fadeInDuration = 0.5f; 
        [Tooltip("Duration to hold the black screen before fading out.")]
        [SerializeField] private float holdDuration = 1.0f; 
        [Tooltip("Duration of the fade-from-black before the next scene appears.")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        private int currentBreakIndex = 0;
        private int sceneChangeCount = 0;
        private bool isPlayingCommercials = false;
        
        // Singleton instance
        public static CommercialManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of CommercialManager already exists. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Ensure canvas is initially disabled
            if (commercialCanvas != null)
            {
                commercialCanvas.gameObject.SetActive(false);
            }
            else
            {
                 Debug.LogError("CommercialManager: Commercial Canvas reference is not set!");
            }
            
            // Configure Video Player
            if (videoPlayer != null)
            {
                videoPlayer.playOnAwake = false;
                // Ensure a render texture exists if not assigned
                if (videoPlayer.targetTexture == null)
                {
                    Debug.LogWarning("CommercialManager: VideoPlayer targetTexture not set. Creating default RenderTexture (1920x1080).");
                    videoPlayer.targetTexture = new RenderTexture(1920, 1080, 24); 
                }
                // Assign the texture to the display image
                if (videoDisplay != null)
                {
                    videoDisplay.texture = videoPlayer.targetTexture;
                }
                 else
                {
                     Debug.LogError("CommercialManager: Video Display RawImage reference is not set!");
                }
            }
             else
            {
                 Debug.LogError("CommercialManager: Video Player reference is not set!");
            }

            // --- Find BackgroundMusicManager ---
            backgroundMusicManager = FindObjectOfType<BackgroundMusicManager>();
            if (backgroundMusicManager == null)
            {
                 Debug.LogWarning("CommercialManager: BackgroundMusicManager not found in scene. Music fading during commercials will not work.", this);
            }
            // --- End Find ---
        }
        
        /// <summary>
        /// Called when a scene change occurs in the ShowRunner.
        /// Determines if a commercial break should play based on configuration.
        /// </summary>
        public void TriggerCommercialBreak()
        {
            // Prevent overlapping commercial breaks
            if (isPlayingCommercials) 
            {
                Debug.Log("CommercialManager: TriggerCommercialBreak called while already playing commercials. Skipping.");
                return;
            }
            
            // Check global skip flag
            if (skipAllCommercials) 
            {
                 Debug.Log("CommercialManager: Skipping commercial break (skipAllCommercials is true).");
                 return;
            }
            
            // Check if we should skip the first few scene changes
            if (sceneChangeCount < skipFirstNSceneChanges)
            {
                sceneChangeCount++;
                 Debug.Log($"CommercialManager: Skipping commercial break for early scene change ({sceneChangeCount}/{skipFirstNSceneChanges}).");
                return;
            }
            
            sceneChangeCount++; // Increment regardless of whether a break plays
            
            // Check if there are any breaks configured
            if (commercialBreaks.Count == 0) 
            {
                 Debug.Log("CommercialManager: No commercial breaks configured. Skipping.");
                 return;
            }
            
            // Cycle through breaks
            if (currentBreakIndex >= commercialBreaks.Count)
            {
                currentBreakIndex = 0; // Loop back to the first break
                Debug.Log("CommercialManager: Reached end of commercial breaks, looping back to start.");
            }
                
            CommercialBreak currentBreak = commercialBreaks[currentBreakIndex];
            
            // Check if this specific break should be skipped
            if (currentBreak.skipThisBreak)
            {
                Debug.Log($"CommercialManager: Skipping commercial break '{currentBreak.breakName}' (skipThisBreak is true).");
                currentBreakIndex++; // Move to the next break for next time
                return;
            }
            
            // Check if the break has any actual commercials
            if (currentBreak.commercials == null || currentBreak.commercials.Count == 0)
            {
                 Debug.Log($"CommercialManager: Skipping commercial break '{currentBreak.breakName}' (no commercials assigned).");
                 currentBreakIndex++; // Move to the next break for next time
                 return;
            }
            
            // --- Play the Commercial Break ---
            Debug.Log($"CommercialManager: Starting commercial break '{currentBreak.breakName}'.");
            
            // Pause the main show
            if (ShowRunner.Instance != null)
            {
                ShowRunner.Instance.PauseShow(); // Pause the show via Singleton
            }
            else
            {
                Debug.LogError("CommercialManager: ShowRunner instance not found! Cannot pause show.");
                // Decide if we should proceed without pausing or abort
                return; // Abort if we can't pause
            }
            
            // Start the playback coroutine
            StartCoroutine(PlayCommercialBreakCoroutine(currentBreak));
            
            // Prepare for the next scene change
            currentBreakIndex++;
        }
        
        /// <summary>
        /// Coroutine to play all commercials within a given break sequentially.
        /// </summary>
        private IEnumerator PlayCommercialBreakCoroutine(CommercialBreak commercialBreak)
        {
            isPlayingCommercials = true;
            
            // Activate the commercial UI
            if (commercialCanvas != null)
            {
                commercialCanvas.gameObject.SetActive(true);
            }
            
            // --- Fade out background music ---
            if (backgroundMusicManager != null)
            {
                backgroundMusicManager.FadeOutForCommercials();
            }
            // --- End Fade out ---

            // Play each commercial clip
            for (int i = 0; i < commercialBreak.commercials.Count; i++)
            {
                Commercial commercial = commercialBreak.commercials[i];
                bool isLastCommercial = (i == commercialBreak.commercials.Count - 1);

                if (commercial.videoClip == null || videoPlayer == null) 
                {
                     Debug.LogWarning($"Skipping commercial '{commercial.name}' due to missing VideoClip or VideoPlayer reference.");
                     continue;
                }
                
                Debug.Log($"Playing commercial: {commercial.name} ({commercial.videoClip.name})");
                
                // Setup VideoPlayer
                videoPlayer.clip = commercial.videoClip;
                videoPlayer.Prepare();
                
                // Wait until prepared
                yield return new WaitUntil(() => videoPlayer.isPrepared);
                
                // Play the video
                videoPlayer.Play();
                
                // Wait for video completion (with safety timeout)
                if (!isLastCommercial)
                {
                    // --- Standard wait for non-last commercials ---
                    float startTime = Time.time;
                    float timeout = videoPlayer.clip.length > 0 ? (float)videoPlayer.clip.length + 5.0f : 60.0f; 
                    while (videoPlayer.isPlaying && (Time.time - startTime) < timeout)
                    {
                        yield return null; // Wait for the next frame
                    }
                    if (videoPlayer.isPlaying) { Debug.LogWarning($"Commercial '{commercial.name}' timed out."); videoPlayer.Stop(); }
                     else { Debug.Log($"Finished playing non-last commercial: {commercial.name}"); }
                }
                else
                {
                    // --- Overlap fade with the end of the LAST commercial ---
                    float videoLength = (float)videoPlayer.clip.length;
                    float timeToStartFade = videoLength - fadeInDuration; // When to START the fade

                    // 1. Wait until it's time to start fading
                    if (timeToStartFade > 0)
                    {
                        // Wait until it's time to start fading
                        yield return new WaitForSeconds(timeToStartFade);
                    }
                    else
                    {
                         Debug.LogWarning($"Last commercial '{commercial.name}' is shorter than fade duration. Fading immediately.");
                         // Don't wait, start fade now
                    }

                    // Start the fade coroutine
                    // 2. Start the fade-in (runs in parallel)
                    if (blackFadePanel != null)
                    {
                        Debug.Log("Starting fade-in during last commercial...");
                        StartCoroutine(FadeCanvasGroup(blackFadePanel, 1f, fadeInDuration)); // NO YIELD RETURN
                    }
                    
                    // 3. Wait for the remaining duration of the video clip
                    // Calculate how much video time *should* be left after the wait above
                    float expectedRemainingVideoTime = videoLength - timeToStartFade; 
                    if (timeToStartFade <= 0) expectedRemainingVideoTime = videoLength; // Full video length if fade started immediately
                    
                    if (expectedRemainingVideoTime > 0)
                    {
                        yield return new WaitForSeconds(expectedRemainingVideoTime);
                    }
                    
                    // Wait for the fade to complete (or slightly longer than remaining video time)
                    
                    // Ensure video is stopped
                    videoPlayer.Stop();
                    Debug.Log($"Finished playing LAST commercial: {commercial.name} and fade should be complete.");
                    
                    // --- Resume ShowRunner IMMEDIATELY after last video --- 
                    // 4. Resume ShowRunner NOW 
                    if (ShowRunner.Instance != null)
                    {
                        Debug.Log("Resuming ShowRunner immediately after last video finished.");
                        ShowRunner.Instance.ResumeShow(); 
                    }
                    else
                    {
                         Debug.LogError("CommercialManager: ShowRunner instance not found! Cannot resume show.");
                    }
                    // -----------------------------------------------------
                }
            }
            
            // --- Commercial Break Finished Playing Videos ---
            Debug.Log($"Commercial break '{commercialBreak.breakName}' finished playing videos.");
            
            // Hide the video canvas immediately
            if (commercialCanvas != null)
            {
                commercialCanvas.gameObject.SetActive(false);
            }
            
            // --- Fade to Black and Hold ---
            if (blackFadePanel != null)
            {
                Debug.Log("Fading to black...");
                Debug.Log($"[CommercialManager] Holding black screen at {Time.time}");
                yield return new WaitForSeconds(holdDuration); // Use variable for hold duration
                Debug.Log($"[CommercialManager] Finished hold at {Time.time}");

                Debug.Log("Starting fade-out black...");
                StartCoroutine(FadeCanvasGroup(blackFadePanel, 0f, fadeOutDuration)); // Use variable - NO YIELD RETURN
            }
            else
            {
                // No fade panel? Just resume immediately after commercials.
                 Debug.LogWarning("Black Fade Panel not assigned. Resuming ShowRunner immediately after commercials.");
                 if (ShowRunner.Instance != null)
                 {
                     ShowRunner.Instance.ResumeShow();
                 }
                  else
                 {
                      Debug.LogError("CommercialManager: ShowRunner instance not found! Cannot resume show.");
                 }
            }
            
            // --- Resume ShowRunner DURING or AFTER fade out (depending on if fade panel exists) --- 
            if (ShowRunner.Instance != null)
            {
                // MOVED: Resume is now definitively AFTER fade out.
            }
             else
            {
                 Debug.LogError("CommercialManager: ShowRunner instance not found! Cannot resume show AFTER fade.");
            }
            // -----------------------------------------

            // --- Resume ShowRunner AFTER fade out is complete --- 
            if (ShowRunner.Instance != null)
            {
                Debug.Log("Resuming ShowRunner AFTER black fade.");
                ShowRunner.Instance.ResumeShow(); 
            }
            else
            {
                 Debug.LogError("CommercialManager: ShowRunner instance not found! Cannot resume show AFTER fade.");
            }
            // -----------------------------------------------------

            // --- Resume Background Music AFTER fade is complete ---
            if (backgroundMusicManager != null)
            {
                 backgroundMusicManager.ResumeAfterCommercials();
            }
            // --- End Resume Music ---
            
            isPlayingCommercials = false;

            // Notify UX Manager that commercials have ended
            // UXAnimationManager.Instance?.OnCommercialEnd();
        }
        
        /// <summary>
        /// Coroutine to fade a CanvasGroup's alpha.
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration)
        {
            if (cg == null) yield break;

            float startAlpha = cg.alpha;
            float time = 0f;

            // Ensure the CanvasGroup is interactable and blocks raycasts during fade-in, 
            // and disable them during fade-out to prevent blocking UI underneath.
            cg.interactable = targetAlpha > 0.5f; 
            cg.blocksRaycasts = targetAlpha > 0.5f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime; // Use unscaled time in case Time.timeScale was 0 briefly
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                yield return null;
            }

            cg.alpha = targetAlpha;
            // Final state after fade
            cg.interactable = targetAlpha > 0.5f; 
            cg.blocksRaycasts = targetAlpha > 0.5f;
        }
        
        /// <summary>
        /// Allows external calls (e.g., from a UI button) to immediately stop 
        /// the currently playing commercial break and resume the show.
        /// </summary>
        public void SkipCurrentCommercials()
        {
            if (isPlayingCommercials)
            {
                Debug.Log("Skipping current commercial break...");
                
                // Stop the playback coroutine
                StopAllCoroutines(); // Be careful if this manager runs other coroutines
                
                // Stop the video player if it's running
                if (videoPlayer != null && videoPlayer.isPlaying)
                {
                    videoPlayer.Stop();
                }
                
                // Hide the canvas
                if (commercialCanvas != null)
                {
                    commercialCanvas.gameObject.SetActive(false);
                }
                
                // Resume the show
                if (ShowRunner.Instance != null)
                {
                    ShowRunner.Instance.ResumeShow();
                }
                
                // Reset the flag
                isPlayingCommercials = false;
            }
             else
            {
                 Debug.Log("SkipCurrentCommercials called, but no commercials are currently playing.");
            }
        }
    }
} 