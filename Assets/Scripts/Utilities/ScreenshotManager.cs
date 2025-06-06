using UnityEngine;
using System.IO;
using System.Collections;
using System; // Required for DateTime
using ShowRunner; // Required for EventData
using System.Text.RegularExpressions; // Required for Sanitizing filenames

/// <summary>
/// Manages automatic and manual screenshot capture.
/// Saves screenshots to a folder within the Unity project's Assets directory.
/// </summary>
public class ScreenshotManager : MonoBehaviour
{
    [Header("Automatic Screenshots")]
    [Tooltip("Enable or disable automatic screenshots.")]
    public bool isAutoScreenshottingEnabled = false;

    [Tooltip("Interval in seconds between automatic screenshots.")]
    public float autoScreenshotInterval = 30.0f;

    [Header("Manual Screenshots")]
    [Tooltip("Key to trigger a manual screenshot.")]
    public KeyCode manualScreenshotKey = KeyCode.F12;

    [Header("Speak Event Screenshots")]
    [Tooltip("Enable or disable screenshots on speak events.")]
    public bool enableSpeakEventScreenshots = false;

    [Tooltip("Subfolder name within the main screenshot folder for speak event screenshots.")]
    public string speakEventSubFolderName = "SpeakEvents";

    [Header("Settings")]
    [Tooltip("Subfolder name within the Assets directory to save screenshots.")]
    public string screenshotFolderName = "Screenshots";
    
    [Tooltip("JPG quality for encoding (1-100).")]
    [Range(1, 100)]
    public int jpgQuality = 75;

    private string baseScreenshotPath; // Renamed from screenshotPath
    private string speakEventScreenshotPath;
    private Coroutine autoScreenshotCoroutine;
    private ShowRunner.ShowRunner showRunnerInstance; // Reference to ShowRunner

    private const string EPISODE_SCREENSHOTS_SUBFOLDER_NAME = "screenshots";

    /// <summary>
    /// Initializes the ScreenshotManager.
    /// Creates the screenshot directories within Assets and starts auto-screenshot/event listening if enabled.
    /// </summary>
    void Start()
    {
        // Get ShowRunner instance
        showRunnerInstance = ShowRunner.ShowRunner.Instance;
        if (showRunnerInstance == null)
        {
            Debug.LogWarning("ScreenshotManager: ShowRunner instance not found. Speak event screenshots might not have full naming data, and episode-specific paths for auto/manual screenshots won't be used.");
        }

        // Define the full base path for saving screenshots within the Assets folder (fallback)
        baseScreenshotPath = Path.Combine(Application.dataPath, screenshotFolderName);

        // Create the base screenshot directory if it doesn't exist
        if (!Directory.Exists(baseScreenshotPath))
        {
            Directory.CreateDirectory(baseScreenshotPath);
            Debug.Log($"Fallback base screenshot directory created at: {baseScreenshotPath}");
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }

        // Define the full path for speak event screenshots, as a subfolder of the base path (fallback)
        speakEventScreenshotPath = Path.Combine(baseScreenshotPath, speakEventSubFolderName);

        // Create the speak event screenshot directory if it doesn't exist (fallback)
        if (!Directory.Exists(speakEventScreenshotPath))
        {
            Directory.CreateDirectory(speakEventScreenshotPath);
            Debug.Log($"Fallback speak event screenshot directory created at: {speakEventScreenshotPath}");
            #if UNITY_EDITOR
            // Refresh the AssetDatabase to show the new folder in the Unity Editor
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }

        // Start the automatic screenshot coroutine if initially enabled
        if (isAutoScreenshottingEnabled)
        {
            StartAutoScreenshotCoroutine();
        }

        // Subscribe to speak events if initially enabled
        ToggleSpeakEventScreenshotMode(enableSpeakEventScreenshots); // Call the toggle to handle subscription
    }

    /// <summary>
    /// Called when the MonoBehaviour will be destroyed.
    /// Unsubscribes from events to prevent memory leaks.
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from speak events if we were subscribed
        if (enableSpeakEventScreenshots) // Or a dedicated flag if you prefer more robust tracking
        {
            ShowRunner.EventProcessor.OnSpeakEventProcessed -= HandleSpeakEvent;
        }
    }

    /// <summary>
    /// Checks for manual screenshot input every frame.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(manualScreenshotKey))
        {
            StartCoroutine(CaptureAndSaveScreenshot());
            Debug.Log("Manual screenshot triggered.");
        }
    }

    /// <summary>
    /// Toggles the automatic screenshotting feature.
    /// </summary>
    public void ToggleAutoScreenshot(bool enable)
    {
        isAutoScreenshottingEnabled = enable;
        if (isAutoScreenshottingEnabled)
        {
            StartAutoScreenshotCoroutine();
            Debug.Log("Automatic screenshotting ENABLED.");
        }
        else
        {
            StopAutoScreenshotCoroutine();
            Debug.Log("Automatic screenshotting DISABLED.");
        }
    }
    
    /// <summary>
    /// Starts the automatic screenshot coroutine if it's not already running.
    /// </summary>
    private void StartAutoScreenshotCoroutine()
    {
        if (autoScreenshotCoroutine == null)
        {
            autoScreenshotCoroutine = StartCoroutine(AutoScreenshotCoroutine());
        }
    }

    /// <summary>
    /// Stops the automatic screenshot coroutine if it's running.
    /// </summary>
    private void StopAutoScreenshotCoroutine()
    {
        if (autoScreenshotCoroutine != null)
        {
            StopCoroutine(autoScreenshotCoroutine);
            autoScreenshotCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine for taking screenshots at regular intervals.
    /// </summary>
    private IEnumerator AutoScreenshotCoroutine()
    {
        while (true) // Loop indefinitely
        {
            // Wait for the specified interval
            yield return new WaitForSeconds(autoScreenshotInterval);

            if (isAutoScreenshottingEnabled)
            {
                Debug.Log("Automatic screenshot triggered.");
                yield return StartCoroutine(CaptureAndSaveScreenshot());
            }
        }
    }

    /// <summary>
    /// Toggles the speak event screenshotting feature.
    /// Subscribes or unsubscribes from the EventProcessor.OnSpeakEventProcessed event.
    /// </summary>
    public void ToggleSpeakEventScreenshotMode(bool enable)
    {
        enableSpeakEventScreenshots = enable;
        if (enableSpeakEventScreenshots)
        {
            ShowRunner.EventProcessor.OnSpeakEventProcessed += HandleSpeakEvent;
            Debug.Log("Speak Event Screenshotting ENABLED.");
        }
        else
        {
            ShowRunner.EventProcessor.OnSpeakEventProcessed -= HandleSpeakEvent;
            Debug.Log("Speak Event Screenshotting DISABLED.");
        }
    }

    /// <summary>
    /// Handles the speak event triggered by EventProcessor.
    /// Captures a screenshot if speak event screenshotting is enabled.
    /// </summary>
    /// <param name="eventData">The data from the speak event, containing actor info.</param>
    private void HandleSpeakEvent(EventData eventData)
    {
        if (enableSpeakEventScreenshots && eventData != null && !string.IsNullOrEmpty(eventData.actor))
        {
            string episodeId = "UnknownEpisode";
            int sceneIndex = -1;
            int dialogueIndex = -1;

            if (showRunnerInstance != null)
            {
                episodeId = showRunnerInstance.GetCurrentEpisodeId() ?? "NoEpisode";
                sceneIndex = showRunnerInstance.GetCurrentSceneIndex(); // 0-based
                dialogueIndex = showRunnerInstance.GetCurrentDialogueIndex(); // 0-based
            }
            else
            {
                Debug.LogWarning("ScreenshotManager: ShowRunner instance not available for HandleSpeakEvent. Filename will lack episode/scene/dialogue info.");
            }

            Debug.Log($"Speak event received for actor: {eventData.actor}. Episode: {episodeId}, Scene: {sceneIndex + 1}, Dialogue: {dialogueIndex + 1}. Triggering screenshot.");
            StartCoroutine(CaptureAndSaveScreenshot(eventData.actor, episodeId, sceneIndex, dialogueIndex));
        }
        else if (enableSpeakEventScreenshots)
        {
            Debug.LogWarning("Speak event received, but actor name is missing. Cannot take speak event screenshot.");
        }
    }
    
    /// <summary>
    /// Captures the screen, encodes it to JPG, and saves it to a file in the Assets folder.
    /// This runs at the end of the frame to ensure all rendering is complete.
    /// </summary>
    /// <param name="actorName">Optional: The name of the actor speaking, for speak event screenshots.</param>
    /// <param name="episodeIdFromEvent">Optional: The ID of the current episode for speak event screenshots.</param>
    /// <param name="sceneIndexFromEvent">Optional: The 0-based index of the current scene for speak event screenshots.</param>
    /// <param name="dialogueIndexFromEvent">Optional: The 0-based index of the current dialogue for speak event screenshots.</param>
    private IEnumerator CaptureAndSaveScreenshot(string actorName = null, string episodeIdFromEvent = null, int sceneIndexFromEvent = -1, int dialogueIndexFromEvent = -1)
    {
        // If this is a speak event (actorName is provided), add a 1.5-second delay
        if (!string.IsNullOrEmpty(actorName))
        {
            yield return new WaitForSeconds(1.5f);
        }

        // ALWAYS wait for the end of the frame immediately before attempting to capture
        // This ensures that all rendering for the current frame (after any delays) is complete.
        yield return new WaitForEndOfFrame();

        try
        {
            Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] jpgBytes = screenTexture.EncodeToJPG(jpgQuality);
            Destroy(screenTexture);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string fileName;
            string targetDirectoryPath; // This will be the directory where the file is saved
            string relativePathForAssetDB; // Relative path from "Assets/" for AssetDatabase.ImportAsset

            // Determine if this call is from a speak event
            bool isSpeakEventCall = !string.IsNullOrEmpty(actorName);
            string currentShowrunnerEpisodeId = null;
            if (showRunnerInstance != null)
            {
                currentShowrunnerEpisodeId = showRunnerInstance.GetCurrentEpisodeId();
            }

            // --- CASE 1: Speak event with full context passed from HandleSpeakEvent ---
            // This is the most specific case, using episodeId, sceneIndex, and dialogueIndex from the event.
            bool hasFullContextFromEvent = isSpeakEventCall &&
                                           !string.IsNullOrEmpty(episodeIdFromEvent) &&
                                           episodeIdFromEvent != "UnknownEpisode" &&
                                           episodeIdFromEvent != "NoEpisode" &&
                                           sceneIndexFromEvent != -1 &&
                                           dialogueIndexFromEvent != -1;

            if (hasFullContextFromEvent)
            {
                Debug.Log($"Screenshot Case: Speak Event with full context (Episode: {episodeIdFromEvent}, Scene: {sceneIndexFromEvent + 1}, Dialogue: {dialogueIndexFromEvent + 1}).");
                string episodeSpecificBaseDir = Path.Combine(Application.dataPath, "Resources", "Episodes", episodeIdFromEvent);
                targetDirectoryPath = Path.Combine(episodeSpecificBaseDir, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                relativePathForAssetDB = Path.Combine("Resources", "Episodes", episodeIdFromEvent, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                fileName = $"{episodeIdFromEvent}_{sceneIndexFromEvent + 1}_{dialogueIndexFromEvent + 1}.jpg";
            }
            // --- CASE 2: Speak event, but relying on ShowRunner for episodeId (scene/dialogue from event might be incomplete/missing) ---
            else if (isSpeakEventCall && !string.IsNullOrEmpty(currentShowrunnerEpisodeId) && currentShowrunnerEpisodeId != "UnknownEpisode" && currentShowrunnerEpisodeId != "NoEpisode")
            {
                Debug.Log($"Screenshot Case: Speak Event with ShowRunner episode context (Episode: {currentShowrunnerEpisodeId}). Fallback naming used.");
                string episodeSpecificBaseDir = Path.Combine(Application.dataPath, "Resources", "Episodes", currentShowrunnerEpisodeId);
                targetDirectoryPath = Path.Combine(episodeSpecificBaseDir, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                relativePathForAssetDB = Path.Combine("Resources", "Episodes", currentShowrunnerEpisodeId, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                string sanitizedActorName = SanitizeActorName(actorName);
                fileName = $"SpeakEvent_EpFallback_{sanitizedActorName}_{timestamp}.jpg";
            }
            // --- CASE 3: Auto or Manual screenshot, with an active ShowRunner episode ---
            else if (!isSpeakEventCall && !string.IsNullOrEmpty(currentShowrunnerEpisodeId) && currentShowrunnerEpisodeId != "UnknownEpisode" && currentShowrunnerEpisodeId != "NoEpisode")
            {
                Debug.Log($"Screenshot Case: Auto/Manual with ShowRunner episode context (Episode: {currentShowrunnerEpisodeId}).");
                string episodeSpecificBaseDir = Path.Combine(Application.dataPath, "Resources", "Episodes", currentShowrunnerEpisodeId);
                targetDirectoryPath = Path.Combine(episodeSpecificBaseDir, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                relativePathForAssetDB = Path.Combine("Resources", "Episodes", currentShowrunnerEpisodeId, EPISODE_SCREENSHOTS_SUBFOLDER_NAME);
                fileName = $"Screenshot_Ep_{timestamp}.jpg"; // Changed prefix to avoid confusion with precise speak event names
            }
            // --- CASE 4: Speak event, but NO episode context (neither from event nor ShowRunner) - Global Fallback ---
            else if (isSpeakEventCall)
            {
                Debug.Log("Screenshot Case: Speak Event - Global Fallback (no episode context).");
                targetDirectoryPath = speakEventScreenshotPath; // Uses Assets/Screenshots/SpeakEvents (or custom)
                relativePathForAssetDB = Path.Combine(screenshotFolderName, speakEventSubFolderName);
                string sanitizedActorName = SanitizeActorName(actorName);
                fileName = $"SpeakEvent_GlobalFallback_{sanitizedActorName}_{timestamp}.jpg";
            }
            // --- CASE 5: Auto or Manual screenshot, NO episode context - Global Fallback ---
            else // Not a speak event call, and no ShowRunner episode context
            {
                Debug.Log("Screenshot Case: Auto/Manual - Global Fallback (no episode context).");
                targetDirectoryPath = baseScreenshotPath; // Uses Assets/Screenshots (or custom)
                relativePathForAssetDB = screenshotFolderName;
                fileName = $"Screenshot_Global_{timestamp}.jpg";
            }

            // Ensure the target directory exists
            if (!Directory.Exists(targetDirectoryPath))
            {
                Directory.CreateDirectory(targetDirectoryPath);
                Debug.Log($"Screenshot directory created: {targetDirectoryPath}");
                #if UNITY_EDITOR
                // A single refresh for the *parent* of the newly created folder might be necessary
                // if the episode folder (e.g., S1E55) itself was also just created.
                // However, if 'targetDirectoryPath' is 'Assets/Resources/Episodes/S1E55/screenshots',
                // then 'Assets/Resources/Episodes/S1E55' must exist.
                // A general refresh after ImportAsset is often sufficient.
                UnityEditor.AssetDatabase.Refresh();
                #endif
            }
            
            string filePath = Path.Combine(targetDirectoryPath, fileName);

            File.WriteAllBytes(filePath, jpgBytes);
            Debug.Log($"Screenshot saved to: {filePath}");

            #if UNITY_EDITOR
            string relativeAssetPath = Path.Combine("Assets", relativePathForAssetDB, fileName);
            UnityEditor.AssetDatabase.ImportAsset(relativeAssetPath);
            // Potentially refresh again if there were issues with visibility, though ImportAsset usually handles it.
            // UnityEditor.AssetDatabase.Refresh(); 
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to capture or save screenshot: {e.Message}");
        }
    }

    /// <summary>
    /// Sanitizes an actor's name to be used in a filename.
    /// Removes or replaces characters that are invalid in filenames.
    /// </summary>
    /// <param name="actorName">The original actor name.</param>
    /// <returns>A sanitized string suitable for use in a filename.</returns>
    private string SanitizeActorName(string actorName)
    {
        if (string.IsNullOrEmpty(actorName))
            return "UnknownActor";

        // Remove invalid characters using regex. Keeps alphanumeric, underscores, hyphens.
        // You can adjust the regex pattern to allow more or fewer characters.
        string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        Regex r = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
        string sanitizedName = r.Replace(actorName, "");

        // Replace spaces with underscores
        sanitizedName = sanitizedName.Replace(" ", "_");

        // Ensure the name isn't empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitizedName))
            return "SanitizedActor";

        return sanitizedName;
    }

    // Optional: If you want to link a UI Toggle component directly
    // public void OnUIToggleChanged(bool isOn)
    // {
    //     ToggleAutoScreenshot(isOn);
    // }
} 