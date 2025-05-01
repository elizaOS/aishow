using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System;

namespace ShowRunner
{
    /// <summary>
    /// Helper component to automatically set up the required GameObjects and components 
    /// for the CommercialManager system in the editor or on Awake.
    /// </summary>
    [ExecuteInEditMode] // Allows running in the editor
    [RequireComponent(typeof(CommercialManager))] // Ensure CommercialManager is present
    public class CommercialManagerSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [Tooltip("If true, automatically creates missing Canvas, RawImage, and VideoPlayer GameObjects as children.")]
        [SerializeField] private bool createIfMissing = true;
        
        [Tooltip("If true, runs the setup logic automatically when the scene starts or the component is added in the editor.")]
        [SerializeField] private bool setupOnAwake = true;

        [Header("Optional References (Auto-assigned if Create If Missing)")]
        [Tooltip("The Canvas used for displaying commercials. Will be created if missing and 'Create If Missing' is true.")]
        [SerializeField] private Canvas commercialCanvas;
        
        [Tooltip("The RawImage used to display the video. Will be created if missing and 'Create If Missing' is true.")]
        [SerializeField] private RawImage videoDisplay;
        
        [Tooltip("The VideoPlayer component used to play commercials. Will be created if missing and 'Create If Missing' is true.")]
        [SerializeField] private VideoPlayer videoPlayer;

        private void Awake()
        {
            // Run setup on Awake if configured and in Play mode or if ExecuteInEditMode causes Awake in editor
            if (setupOnAwake && (Application.isPlaying || Application.isEditor))
            {
                 TrySetupCommercialSystem();
            }
        }

        private void Reset()
        {
             // Automatically run setup when component is added or reset in editor
            if (Application.isEditor)
            {
                 TrySetupCommercialSystem();
            }
        }

        // Context menu item to manually trigger setup
        [ContextMenu("Setup Commercial System Components")]
        private void SetupFromContextMenu()
        {
             TrySetupCommercialSystem();
        }

        /// <summary>
        /// Attempts to find or create necessary components and assign them to the CommercialManager.
        /// </summary>
        private void TrySetupCommercialSystem()
        {
            CommercialManager manager = GetComponent<CommercialManager>();
            if (manager == null) 
            {   // Should not happen due to [RequireComponent]
                Debug.LogError("CommercialManagerSetup: CommercialManager component not found! Setup aborted.", this);
                return; 
            }

            bool changed = false; // Track if any changes were made for logging

            // --- Find or Create Canvas ---            
            if (commercialCanvas == null)
            {
                 // Try finding an existing child Canvas first
                 commercialCanvas = GetComponentInChildren<Canvas>(true); // Include inactive
                 
                 if (commercialCanvas == null && createIfMissing)
                 {
                     Debug.Log("CommercialManagerSetup: Creating Commercial Canvas.", this);
                     GameObject canvasGO = new GameObject("Commercial Canvas");
                     canvasGO.transform.SetParent(transform, false); // Set parent
                     commercialCanvas = canvasGO.AddComponent<Canvas>();
                     commercialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                     commercialCanvas.sortingOrder = 100; // High sorting order
                     
                     CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                     scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                     scaler.referenceResolution = new Vector2(1920, 1080);
                     
                     canvasGO.AddComponent<GraphicRaycaster>();
                     canvasGO.SetActive(false); // Start disabled
                     changed = true;
                 }
            }

            // --- Find or Create Video Display (RawImage) --- 
            if (videoDisplay == null && commercialCanvas != null)
            {
                 // Try finding an existing RawImage under the canvas
                 videoDisplay = commercialCanvas.GetComponentInChildren<RawImage>(true);
                 
                 if (videoDisplay == null && createIfMissing)
                 {
                     Debug.Log("CommercialManagerSetup: Creating Video Display RawImage.", this);
                     GameObject displayGO = new GameObject("Video Display");
                     displayGO.transform.SetParent(commercialCanvas.transform, false);
                     videoDisplay = displayGO.AddComponent<RawImage>();
                     videoDisplay.color = Color.black; // Start black
                     
                     // Stretch to fill canvas
                     RectTransform rectTransform = videoDisplay.GetComponent<RectTransform>();
                     rectTransform.anchorMin = Vector2.zero;
                     rectTransform.anchorMax = Vector2.one;
                     rectTransform.offsetMin = Vector2.zero;
                     rectTransform.offsetMax = Vector2.zero;
                     changed = true;
                 }
            }

            // --- Find or Create Video Player --- 
            if (videoPlayer == null)
            {
                 // Try finding an existing child VideoPlayer
                 videoPlayer = GetComponentInChildren<VideoPlayer>(true);
                 
                 if (videoPlayer == null && createIfMissing)
                 {
                     Debug.Log("CommercialManagerSetup: Creating Video Player GameObject.", this);
                     GameObject videoPlayerGO = new GameObject("Commercial Video Player");
                     videoPlayerGO.transform.SetParent(transform, false);
                     videoPlayer = videoPlayerGO.AddComponent<VideoPlayer>();
                     changed = true;
                 }
                 
                 // Configure Video Player if found or created
                 if (videoPlayer != null)
                 {
                    videoPlayer.playOnAwake = false;
                    videoPlayer.waitForFirstFrame = true;
                    videoPlayer.skipOnDrop = true;
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
                    // Create Render Texture if needed
                    if (videoPlayer.targetTexture == null)
                    {
                         Debug.Log("CommercialManagerSetup: Creating RenderTexture for VideoPlayer.", this);
                         videoPlayer.targetTexture = new RenderTexture(1920, 1080, 24);
                         changed = true;
                    }
                 }
            }
            
            // --- Assign References to CommercialManager --- 
            // Use reflection or direct assignment if CommercialManager fields were public/internal
            // Since they are private [SerializeField], we use reflection for editor setup.
            // This part *requires* UnityEditor namespace if we were to use SerializedObject.
            // Let's try direct access (requires making fields accessible or using a public setup method)
            // **Alternative: Add a public method in CommercialManager to set these.**
            // For simplicity in this example, we'll assume we can access them for setup purpose
            // A better approach is a public Initialize method in CommercialManager.
            try 
            {
                // Attempt to set private fields via reflection (Editor only utility)
                #if UNITY_EDITOR
                var fieldInfoCanvas = typeof(CommercialManager).GetField("commercialCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fieldInfoDisplay = typeof(CommercialManager).GetField("videoDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fieldInfoPlayer = typeof(CommercialManager).GetField("videoPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (fieldInfoCanvas != null && fieldInfoCanvas.GetValue(manager) == null && commercialCanvas != null) { fieldInfoCanvas.SetValue(manager, commercialCanvas); changed = true; }
                if (fieldInfoDisplay != null && fieldInfoDisplay.GetValue(manager) == null && videoDisplay != null) { fieldInfoDisplay.SetValue(manager, videoDisplay); changed = true; }
                if (fieldInfoPlayer != null && fieldInfoPlayer.GetValue(manager) == null && videoPlayer != null) { fieldInfoPlayer.SetValue(manager, videoPlayer); changed = true; }
                
                // Assign texture last, after ensuring player and display exist
                if (videoPlayer != null && videoDisplay != null && videoDisplay.texture == null) 
                { 
                    videoDisplay.texture = videoPlayer.targetTexture;
                    UnityEditor.EditorUtility.SetDirty(videoDisplay); // Mark display as dirty
                    changed = true; 
                }
                
                if(changed)
                {
                    UnityEditor.EditorUtility.SetDirty(manager); // Mark manager as dirty if changed
                    Debug.Log("CommercialManagerSetup: References assigned to CommercialManager.", this);
                }
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"CommercialManagerSetup: Error assigning references via reflection: {ex.Message}", this);
            }

            if (changed)
            {            
                Debug.Log("CommercialManagerSetup: Setup complete.", this);
            }
            else
            {
                 Debug.Log("CommercialManagerSetup: No changes needed.", this);
            }
        }
    }
} 