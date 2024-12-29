using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CameraStateMachine : MonoBehaviour
{
    // Singleton instance
    public static CameraStateMachine Instance { get; private set; }

    public Camera defaultCamera;  // Default fallback camera
    private Dictionary<string, Camera> actorCameras = new Dictionary<string, Camera>();

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
        // Log all registered actor cameras for debugging
        //foreach (var actor in actorCameras)
        //{
            //Debug.Log($"Registered Actor: {actor.Key}, Camera: {actor.Value.name}");
        //}

        // Try to find the matching actor camera by checking all registered cameras
        Camera matchingCamera = null;
        string matchedActorName = null;

        foreach (var actor in actorCameras)
        {
            if (speakerTransform.name.Contains(actor.Key))
            {
                matchingCamera = actor.Value;
                matchedActorName = actor.Key;
                break;
            }
        }

        if (matchingCamera != null)
        {
            //Debug.Log($"Matched actor camera for {speakerTransform.name}: {matchedActorName}");
            SwitchToCamera(matchingCamera);
        }
        else
        {
            //Debug.LogWarning($"No matching actor camera found for {speakerTransform.name}. Switching to default camera.");
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

        // Disable all other cameras
        foreach (var cam in actorCameras.Values)
        {
            if (cam != camera && cam.gameObject.activeSelf)
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
                }
                else
                {
                    //Debug.LogWarning($"No ActorCamera component or actorName found on camera {camera.name}");
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
