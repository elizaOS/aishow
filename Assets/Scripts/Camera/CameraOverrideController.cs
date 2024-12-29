using UnityEngine;

public class CameraOverrideController : MonoBehaviour
{
    [Header("General Settings")]
    public bool isCameraControlEnabled = true; // Toggle for enabling/disabling controls

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;         // Speed of zooming with keyboard
    public float mouseZoomSensitivity = 2f; // Multiplier for mouse scroll zoom sensitivity
    public float lerpSpeed = 5f;          // Speed of smoothing zoom transitions
    public float minFOV = 15f;            // Minimum field of view for perspective cameras
    public float maxFOV = 90f;            // Maximum field of view for perspective cameras
    public float minOrthoSize = 5f;       // Minimum orthographic size for orthographic cameras
    public float maxOrthoSize = 50f;      // Maximum orthographic size for orthographic cameras

    [Header("Pan Settings")]
    public float panSpeed = 0.5f;         // Speed of panning with middle mouse button

    private Camera activeCamera;
    private Transform activeCameraTransform; // Stores the transform of the active camera
    private float targetZoom;             // Target zoom value
    private float originalZoom;           // Original zoom value (FOV or OrthoSize)
    private bool isZooming;               // Tracks if user is currently zooming
    private Vector3 lastMousePosition;    // Last mouse position for panning

    void Start()
    {
        FindActiveCamera();
        if (activeCamera != null)
        {
            InitializeCameraSettings();
        }
    }

    void Update()
    {
        if (!isCameraControlEnabled) return; // Exit if controls are disabled

        // Ensure we have a valid active camera at all times
        if (activeCamera == null || !activeCamera.isActiveAndEnabled)
        {
            FindActiveCamera();
        }

        if (activeCamera != null)
        {
            HandleZoom();
            HandlePan();
            ApplyZoom();
        }
    }

    /// <summary>
    /// Finds the currently enabled and active camera in the scene.
    /// </summary>
    private void FindActiveCamera()
    {
        Camera[] cameras = Camera.allCameras; // Get all cameras in the scene
        foreach (Camera cam in cameras)
        {
            if (cam.isActiveAndEnabled) // Check if the camera is active and enabled
            {
                activeCamera = cam;
                activeCameraTransform = cam.transform; // Store the camera's transform

                // Initialize settings for the new active camera
                InitializeCameraSettings();
                break;
            }
        }
    }

    /// <summary>
    /// Initializes the target and original zoom values for the active camera.
    /// </summary>
    private void InitializeCameraSettings()
    {
        if (activeCamera.orthographic)
        {
            originalZoom = activeCamera.orthographicSize;
            targetZoom = originalZoom;
        }
        else
        {
            originalZoom = activeCamera.fieldOfView;
            targetZoom = originalZoom;
        }
    }

    /// <summary>
    /// Handles zooming logic for the currently active camera.
    /// </summary>
    private void HandleZoom()
    {
        // Get scroll input for zooming
        float scrollInput = Input.GetAxis("Mouse ScrollWheel") * mouseZoomSensitivity;

        // Get arrow key input for zooming
        float keyInput = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            keyInput = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            keyInput = -1f;
        }

        // Combine scroll and key input
        float zoomInput = scrollInput + keyInput * (zoomSpeed * 0.01f);

        if (zoomInput != 0f)
        {
            isZooming = true; // User is zooming

            if (activeCamera.orthographic)
            {
                // Update target zoom for orthographic cameras
                targetZoom -= zoomInput * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minOrthoSize, maxOrthoSize);
            }
            else
            {
                // Update target zoom for perspective cameras
                targetZoom -= zoomInput * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minFOV, maxFOV);
            }
        }
        else
        {
            isZooming = false; // User stopped zooming
        }
    }

    /// <summary>
    /// Handles panning logic for the currently active camera.
    /// </summary>
    private void HandlePan()
    {
        if (activeCameraTransform == null) return; // Exit if no active camera transform

        // Check if middle mouse button is held down
        if (Input.GetMouseButtonDown(2))
        {
            // Reset the last mouse position when the middle mouse button is first pressed
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            // Calculate mouse delta
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // Adjust camera position based on the delta
            Vector3 panTranslation = new Vector3(-mouseDelta.x * panSpeed * Time.deltaTime,
                                                 -mouseDelta.y * panSpeed * Time.deltaTime,
                                                 0);

            activeCameraTransform.Translate(panTranslation, Space.Self);

            // Update the last mouse position
            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// Smoothly applies the zoom effect by interpolating the camera's zoom value to the target zoom.
    /// </summary>
    private void ApplyZoom()
    {
        if (!isZooming)
        {
            // If the user is not zooming, smoothly return to the original zoom
            targetZoom = originalZoom;
        }

        if (activeCamera.orthographic)
        {
            // Smoothly interpolate the orthographic size to the target value
            activeCamera.orthographicSize = Mathf.Lerp(activeCamera.orthographicSize, targetZoom, Time.deltaTime * lerpSpeed);
        }
        else
        {
            // Smoothly interpolate the field of view to the target value
            activeCamera.fieldOfView = Mathf.Lerp(activeCamera.fieldOfView, targetZoom, Time.deltaTime * lerpSpeed);
        }
    }
}
