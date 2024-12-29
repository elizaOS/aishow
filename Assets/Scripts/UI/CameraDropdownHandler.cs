using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure you include this for TextMeshPro components
using System.Collections.Generic;

public class CameraDropdownHandler : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown; // Reference to the existing TMP Dropdown in the scene
    public ScenePayloadManager payloadManager; // Reference to PayloadManager (if needed later)

    private List<Camera> cameras = new List<Camera>(); // List to hold cameras

    void Start()
    {
        // Find all cameras in the scene, regardless of their active state
        cameras.AddRange(FindObjectsOfType<Camera>(true)); // true ensures we get both active and inactive cameras

        // Populate the dropdown with camera names
        PopulateCameraDropdown();

        /* // Set the default camera as selected (optional)
        if (cameraDropdown.options.Count > 0)
        {
            cameraDropdown.value = 0; // Set to first camera by default
            SwitchToCamera(cameras[0]); // Automatically switch to the first camera
        } */

        // Add listener to handle dropdown changes 
        cameraDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    // Method to populate the dropdown with camera names
    void PopulateCameraDropdown()
    {
        cameraDropdown.options.Clear(); // Clear any existing options
        foreach (Camera cam in cameras)
        {
            cameraDropdown.options.Add(new TMP_Dropdown.OptionData(cam.name));
        }
    }

    // Method to switch to the selected camera
    public void SwitchToCamera(Camera cam)
    {
        // Deactivate all cameras
        foreach (Camera camera in cameras)
        {
            camera.gameObject.SetActive(false); // Disable all cameras
        }

        // Activate the selected camera
        cam.gameObject.SetActive(true);
        Debug.Log($"Switched to camera: {cam.name}");
    }

    // This is called when the dropdown value changes
    void OnDropdownValueChanged(int value)
    {
        // Get the camera corresponding to the selected dropdown value
        Camera selectedCamera = cameras[value];
        
        // Switch to the selected camera
        SwitchToCamera(selectedCamera);
    }
}
