using UnityEngine;

public class CameraAutoZoom : MonoBehaviour
{
    [Header("Auto Zoom Settings")]
    public bool isZooming = true; // Toggle for the zoom effect
    public float zoomSpeed = 0.1f; // Speed of zoom effect (adjust as necessary)
    public float zoomAmount = 0.05f; // Amount to zoom in and out (5% = 0.05)

    private Camera cameraComponent; // The camera component
    private float initialFieldOfView; // The starting FOV value

    private void Start()
    {
        // Get the camera component
        cameraComponent = GetComponent<Camera>();

        // Store the initial field of view
        if (cameraComponent != null)
        {
            initialFieldOfView = cameraComponent.fieldOfView;
        }
        else
        {
            Debug.LogError("No Camera component found on this GameObject.");
        }
    }

    private void Update()
    {
        if (cameraComponent == null || !isZooming) return;

        // Slowly zoom in and out by 5% over time
        float targetFov = initialFieldOfView + Mathf.Sin(Time.time * zoomSpeed) * zoomAmount * initialFieldOfView;

        // Apply the zoom effect smoothly
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, Time.deltaTime);
    }

    // Method to toggle the zoom effect manually (if needed)
    public void ToggleZoom(bool enableZoom)
    {
        isZooming = enableZoom;
    }
}
