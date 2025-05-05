using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System; // Add this for StringComparison

public class CameraStateMachine : MonoBehaviour
{
    // Singleton instance
    public static CameraStateMachine Instance { get; private set; }

    public Camera defaultCamera;  // Default fallback camera
    private Dictionary<string, Camera> actorCameras = new Dictionary<string, Camera>();

    [Range(0f, 1f)]
    public float chance = 0.3f; // Chance to select a B-shot (default: 30%)

    private void Awake()
    {
        // Check if there's already an instance
        if (Instance != null && Instance != this)
        {
            //Debug.LogError("Multiple CameraStateMachine instances detected. Destroying duplicate.");
            Destroy(this.gameObject);  // Destroy the duplicate instance
            return;
        }

        // Set the singleton instance
        Instance = this;

        // Make this object persist across scenes
        DontDestroyOnLoad(gameObject);

        // Initialize the default camera here
        InitializeDefaultCamera();
    }

   private void OnEnable()
    {
        // Subscribe to scene load events and custom event handlers
        SceneManager.sceneLoaded += OnSceneLoaded; 
        EventManager.OnSpeakerChange += HandleSpeakerChange;
        EventManager.OnClearSpeaker += HandleClearSpeaker;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventManager.OnSpeakerChange -= HandleSpeakerChange;
        EventManager.OnClearSpeaker -= HandleClearSpeaker;
    }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log($"Scene Loaded: {scene.name}");
        
        // Clear existing camera references
        ClearCameraReferences();

        // Reinitialize the default camera and register all actor cameras
        InitializeCameras();
        RegisterAllActorCameras();

        // Reset AutoCam if needed
        if (AutoCam.Instance != null)
        {
            AutoCam.Instance.DeactivateAutoCam();
        }
    }

    private void InitializeDefaultCamera()
    {
        if (defaultCamera == null)
        {
            ActorCamera[] actorCamerasArray = FindObjectsOfType<ActorCamera>();
            foreach (ActorCamera actorCamera in actorCamerasArray)
            {
                //Debug.Log("Checking actor camera: " + actorCamera.name); // Debug log
                if (actorCamera.actorName == "default")
                {
                    defaultCamera = actorCamera.GetComponent<Camera>();
                    //Debug.Log("Default camera set: " + defaultCamera.name);
                    break;
                }
            }

            if (defaultCamera == null)
            {
                //Debug.LogError("No default camera found in the scene.");
            }
            else
            {
                //Debug.Log("Default camera found and set: " + defaultCamera.name);
            }
        }
    }

    private void ClearCameraReferences()
    {
        actorCameras.Clear();  // Clear all existing camera references
        defaultCamera = null;  // Reset the default camera
    }

    private void InitializeCameras()
    {
        // Reinitialize the default camera
        InitializeDefaultCamera();
    }

    private void HandleSpeakerChange(Transform speakerTransform)
    {
        Camera matchingCamera = null;
        string matchedActorName = null;
        List<Camera> bShots = null;

        // Find the matching actor and their B-shots
        foreach (var actor in actorCameras)
        {
            if (string.Equals(speakerTransform.name, actor.Key, StringComparison.OrdinalIgnoreCase))
            {
                matchingCamera = actor.Value; // Actor's primary camera
                matchedActorName = actor.Key;

                ActorCamera actorCameraComponent = matchingCamera.GetComponent<ActorCamera>();
                if (actorCameraComponent != null)
                {
                    bShots = actorCameraComponent.bShots; // Get B-shots for this actor
                }
                break;
            }
        }

        // Attempt to switch to a B-shot
        if (bShots != null && bShots.Count > 0 && UnityEngine.Random.value < chance)
        {
            int randomIndex = UnityEngine.Random.Range(0, bShots.Count);
            Camera bShotCamera = bShots[randomIndex];

            if (bShotCamera != null)
            {
                //Debug.Log($"Switching to B-shot {bShotCamera.name} for actor {matchedActorName}.");
                SwitchToCamera(bShotCamera);
                return;
            }
        }

        // Fallback to the actor's primary camera if available
        if (matchingCamera != null)
        {
            //Debug.Log($"No B-shots available. Switching to primary camera {matchingCamera.name} for actor {matchedActorName}.");
            SwitchToCamera(matchingCamera);
        }
        else
        {
            // Final fallback to default camera
            Debug.LogWarning($"No matching actor camera found for {speakerTransform.name}. Switching to default camera.");
            SwitchToCamera(defaultCamera);
        }
    }



    private void HandleClearSpeaker()
    {
        // Deactivate AutoCam if it's active
        if (AutoCam.Instance != null && AutoCam.Instance.IsActive)
        {
            AutoCam.Instance.DeactivateAutoCam();
        }

        SwitchToCamera(defaultCamera);
    }

    private void SwitchToCamera(Camera camera)
    {
        if (camera == null)
        {
            Debug.LogError("Camera is null, cannot switch.");
            return;
        }

        //Debug.Log($"Switching to camera: {camera.name}");

        // If AutoCam is active and controlling a camera, deactivate it only if we're switching away
        if (AutoCam.Instance != null && AutoCam.Instance.IsActive)
        {
            Camera autoCamCamera = AutoCam.Instance.CurrentCamera; // Assuming AutoCam tracks its active camera
            if (autoCamCamera != camera)
            {
                AutoCam.Instance.DeactivateAutoCam();
            }
        }

        // Activate the target camera
        if (!camera.gameObject.activeSelf)
        {
            camera.gameObject.SetActive(true);
        }

        // Disable all other cameras except those tagged with "IgnoreCameras"
        foreach (var cam in actorCameras.Values)
        {
            if (cam != camera && cam.gameObject.activeSelf && !cam.CompareTag("IgnoreCameras"))
            {
                cam.gameObject.SetActive(false);
            }
        }
    }

    public void RegisterActorCamera(string actorName, Camera actorCamera)
    {
        if (!actorCameras.ContainsKey(actorName))
        {
            actorCameras.Add(actorName, actorCamera);
            //Debug.Log($"Registered camera for actor: {actorName}");
        }
        else
        {
            //Debug.Log($"Camera for actor {actorName} is already registered: {actorCameras[actorName].name}");
        }
    }

    public void RegisterAllActorCameras()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);

            foreach (Camera camera in allCameras)
            {
                if (camera != defaultCamera) // Skip the default camera
                {
                    ActorCamera actorCameraComponent = camera.GetComponent<ActorCamera>();
                    if (actorCameraComponent != null && !string.IsNullOrEmpty(actorCameraComponent.actorName))
                    {
                        RegisterActorCamera(actorCameraComponent.actorName, camera);

                        // Log B-shots for debugging
                        //if (actorCameraComponent.bShots != null && actorCameraComponent.bShots.Count > 0)
                        //{
                        //    Debug.Log($"Actor {actorCameraComponent.actorName} has {actorCameraComponent.bShots.Count} B-shots.");
                        //}
                    }
                }
            }

        // Activate the default camera
        if (defaultCamera != null)
        {
            defaultCamera.gameObject.SetActive(true);

            // Disable all other cameras
            foreach (var cam in actorCameras.Values)
            {
                if (cam != defaultCamera && cam.gameObject.activeSelf)
                {
                    cam.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("Default camera is null. Cannot activate default camera.");
        }
    }
}
