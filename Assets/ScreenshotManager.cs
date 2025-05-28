using UnityEngine;
using System.IO;
using System.Collections;
using System; // Required for DateTime

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

    [Header("Settings")]
    [Tooltip("Subfolder name within the Assets directory to save screenshots.")]
    public string screenshotFolderName = "Screenshots";
    
    [Tooltip("JPG quality for encoding (1-100).")]
    [Range(1, 100)]
    public int jpgQuality = 75;

    private string screenshotPath;
    private Coroutine autoScreenshotCoroutine;

    /// <summary>
    /// Initializes the ScreenshotManager.
    /// Creates the screenshot directory within Assets and starts the auto-screenshot coroutine if enabled.
    /// </summary>
    void Start()
    {
        // Define the full path for saving screenshots within the Assets folder
        // Application.dataPath points to the Assets folder of your project.
        screenshotPath = Path.Combine(Application.dataPath, screenshotFolderName);

        // Create the directory if it doesn't exist
        if (!Directory.Exists(screenshotPath))
        {
            Directory.CreateDirectory(screenshotPath);
            Debug.Log($"Screenshot directory created at: {screenshotPath}");
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
    /// Captures the screen, encodes it to JPG, and saves it to a file in the Assets folder.
    /// This runs at the end of the frame to ensure all rendering is complete.
    /// </summary>
    private IEnumerator CaptureAndSaveScreenshot()
    {
        // Wait for the end of the frame so all rendering is complete
        yield return new WaitForEndOfFrame();

        try
        {
            Texture2D screenTexture = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] jpgBytes = screenTexture.EncodeToJPG(jpgQuality);
            Destroy(screenTexture);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string fileName = $"Screenshot_{timestamp}.jpg";
            string filePath = Path.Combine(screenshotPath, fileName);

            File.WriteAllBytes(filePath, jpgBytes);
            Debug.Log($"Screenshot saved to: {filePath}");

            #if UNITY_EDITOR
            // Refresh the AssetDatabase to make the new screenshot visible in the Unity Editor immediately
            // This constructs the path relative to the Assets folder for ImportAsset
            string relativePath = Path.Combine("Assets", screenshotFolderName, fileName);
            UnityEditor.AssetDatabase.ImportAsset(relativePath);
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to capture or save screenshot: {e.Message}");
        }
    }

    // Optional: If you want to link a UI Toggle component directly
    // public void OnUIToggleChanged(bool isOn)
    // {
    //     ToggleAutoScreenshot(isOn);
    // }
} 