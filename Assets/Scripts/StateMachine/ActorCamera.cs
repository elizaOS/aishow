using UnityEngine;
using System.Collections.Generic;
using System; // Add this for StringComparison

public class ActorCamera : MonoBehaviour
{
    // The name of the actor that this camera will follow
    [Tooltip("The name of the actor associated with this camera.")]
    public string actorName;  // Set this in the inspector or programmatically
    public List<Camera> bShots; // List of B-shots for this actor

    // Reference to the CameraStateMachine
    private CameraStateMachine cameraStateMachine;

    private void Start()
    {
        cameraStateMachine = FindObjectOfType<CameraStateMachine>();

        if (cameraStateMachine != null)
        {
            if (!string.IsNullOrEmpty(actorName))
            {
                cameraStateMachine.RegisterActorCamera(actorName, GetComponent<Camera>());
                //Debug.Log($"Camera registered for actor: {actorName}");
            }
            else
            {
                //Debug.LogError("ActorName is not set for the camera.");
            }
        }
        else
        {
            Debug.LogError("No CameraStateMachine found in the scene.");
        }
    }
}
