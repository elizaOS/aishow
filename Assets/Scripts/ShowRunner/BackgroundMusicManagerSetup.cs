using UnityEngine;

namespace ShowRunner
{
    /// <summary>
    /// Sets up the BackgroundMusicManager by ensuring necessary components and references are available.
    /// </summary>
    [RequireComponent(typeof(BackgroundMusicManager))]
    public class BackgroundMusicManagerSetup : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("Optional: Assign the AudioSource for background music. If null, one will be added.")]
        [SerializeField] private AudioSource backgroundAudioSource;

        [Tooltip("Optional: Assign the ScenePreperationManager. If null, will search the scene.")]
        [SerializeField] private ScenePreperationManager scenePreparationManager;

        // [Tooltip("Optional: Assign the EpisodeCompletionNotifier. If null, will search the scene.")] // No longer needed by BackgroundMusicManager
        // [SerializeField] private EpisodeCompletionNotifier episodeCompletionNotifier;
        
        private BackgroundMusicManager backgroundMusicManager;

        private void Awake()
        {
            backgroundMusicManager = GetComponent<BackgroundMusicManager>();

            // Ensure AudioSource exists
            if (backgroundAudioSource == null)
            {
                backgroundAudioSource = GetComponent<AudioSource>();
                if (backgroundAudioSource == null)
                {
                    Debug.Log("BackgroundMusicManagerSetup: Adding AudioSource component for background music.", this);
                    backgroundAudioSource = gameObject.AddComponent<AudioSource>();
                    // Configure default AudioSource settings
                    backgroundAudioSource.playOnAwake = false;
                    backgroundAudioSource.loop = true; // Background music usually loops
                    backgroundAudioSource.volume = 0.3f; // Default volume, adjust as needed
                    // Consider adding to an Audio Mixer Group if using mixers
                }
            }
             else
            {
                 // Ensure assigned AudioSource is configured correctly
                 backgroundAudioSource.playOnAwake = false;
                 backgroundAudioSource.loop = true; 
            }

            // Find ScenePreperationManager if not assigned
            if (scenePreparationManager == null)
            {
                scenePreparationManager = FindObjectOfType<ScenePreperationManager>();
                if (scenePreparationManager == null)
                {
                     Debug.LogError("BackgroundMusicManagerSetup: ScenePreperationManager not found in scene!", this);
                }
            }

            // EpisodeCompletionNotifier is no longer a direct dependency for BackgroundMusicManager

            // Inject dependencies into BackgroundMusicManager
            // This uses reflection, alternatively use direct property access or an Initialize method.
            var scenePrepField = typeof(BackgroundMusicManager).GetField("scenePreparationManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scenePrepField != null && scenePreparationManager != null) scenePrepField.SetValue(backgroundMusicManager, scenePreparationManager);
            else if(scenePreparationManager == null) Debug.LogError("Failed to inject ScenePreperationManager dependency.", this);

            var audioSourceField = typeof(BackgroundMusicManager).GetField("backgroundAudioSource", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (audioSourceField != null && backgroundAudioSource != null) audioSourceField.SetValue(backgroundMusicManager, backgroundAudioSource);
            else if(backgroundAudioSource == null) Debug.LogError("Failed to inject backgroundAudioSource dependency.", this);

            Debug.Log("BackgroundMusicManagerSetup completed.", this);
        }
    }
} 