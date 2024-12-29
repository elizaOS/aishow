using System.Collections.Generic;
using UnityEngine;

public class AutoCam : MonoBehaviour
{
    public static AutoCam Instance { get; private set; }

    [Tooltip("Time (in seconds) to wait before switching to the next camera.")]
    public float switchInterval = 5f;

    [Tooltip("Cameras dedicated for fallback shots.")]
    public List<Camera> fallbackCameras = new List<Camera>();

    private int currentCameraIndex = 0;
    private bool isActive = false;
    public Camera CurrentCamera { get; private set; } // Exposes the currently active AutoCam camera

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple AutoCam instances detected. Destroying duplicate.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ensure all fallback cameras are initially disabled, but do not disable other cameras
        foreach (Camera cam in fallbackCameras)
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
        }
        CurrentCamera = null; // No active camera at the start
    }

    

    public void ActivateAutoCam()
    {
        if (fallbackCameras.Count == 0)
        {
            Debug.LogWarning("No fallback cameras set for AutoCam.");
            return;
        }

        if (!isActive)
        {
            isActive = true;
            currentCameraIndex = 0;
            SwitchToCamera(currentCameraIndex);
            InvokeRepeating(nameof(SwitchToNextCamera), switchInterval, switchInterval);
            Debug.Log("AutoCam activated.");
        }
    }

    public void DeactivateAutoCam()
    {
        if (isActive)
        {
            isActive = false;
            CancelInvoke(nameof(SwitchToNextCamera));
            DisableAllFallbackCameras(); // Disable only fallback cameras
            CurrentCamera = null; // Clear the current active camera
            Debug.Log("AutoCam deactivated.");
        }
    }

    private void SwitchToNextCamera()
    {
        if (fallbackCameras.Count == 0) return;

        currentCameraIndex = (currentCameraIndex + 1) % fallbackCameras.Count;
        SwitchToCamera(currentCameraIndex);
    }

    private void SwitchToCamera(int index)
    {
        DisableAllFallbackCameras(); // Disable fallback cameras before switching

        if (index >= 0 && index < fallbackCameras.Count)
        {
            Camera cam = fallbackCameras[index];
            if (cam != null)
            {
                cam.gameObject.SetActive(true);
                CurrentCamera = cam; // Track the active camera
                Debug.Log($"Switched to fallback camera: {cam.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid camera index: {index}");
        }
    }

    private void DisableAllFallbackCameras()
    {
        foreach (Camera cam in fallbackCameras)
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
        }
    }

    public bool IsActive => isActive; // Exposes the active state of AutoCam
}
