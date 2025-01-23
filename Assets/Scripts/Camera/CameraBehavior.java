using UnityEngine;

public class CameraBehavior : MonoBehaviour
{
    public Transform[] targets; // List of targets (actors on stage) to track
    public float switchTargetTimeMin = 3f; // Minimum time before switching targets
    public float switchTargetTimeMax = 5f; // Maximum time before switching targets
    public float panSpeed = 1.5f; // Speed of camera panning
    public float zoomSpeed = 2f; // Speed of FOV zoom
    public float zoomInFOVPercentage = 0.2f; // Percentage of FOV reduction during zoom-in
    public float wobbleIntensity = 0.1f; // Intensity of wobble
    public float wobbleFrequency = 2f; // Frequency of wobble

    private Transform currentTarget; // Current target the camera is focusing on
    private float timeToSwitchTarget; // Time left before switching targets
    private Camera cam; // Reference to the camera component
    private float initialFOV; // Initial FOV to revert to after zooming
    private Vector3 targetPositionSmoothVelocity; // SmoothDamp velocity for position
    private Quaternion targetRotationSmoothVelocity; // SmoothDamp velocity for rotation
    private float wobbleTimer; // Tracks time for wobble calculations
 
    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("No Camera component found on this GameObject!");
            enabled = false;
            return;
        }

        // Initialize starting values
        initialFOV = cam.fieldOfView;
        timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        if (targets.Length > 0)
        {
            currentTarget = targets[Random.Range(0, targets.Length)];
        }
    }

    private void Update()
    {
        if (targets.Length == 0) return; // No targets to follow

        // Decrease the time to switch target
        timeToSwitchTarget -= Time.deltaTime;

        // If it's time to switch target, pick a new random target
        if (timeToSwitchTarget <= 0f)
        {
            SwitchTarget();
            timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        }

        // Perform smooth motion to the target
        TrackTarget();

        // Add wobble to simulate human imperfection
        AddWobble();
    }

    private void SwitchTarget()
    {
        if (targets.Length > 1)
        {
            // Choose a new random target that isn't the current target
            Transform newTarget;
            do
            {
                newTarget = targets[Random.Range(0, targets.Length)];
            } while (newTarget == currentTarget);

            currentTarget = newTarget;
        }
    }

    private void TrackTarget()
    {
        if (currentTarget == null) return;

        // Smoothly move the camera towards the target
        Vector3 targetPosition = currentTarget.position;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref targetPositionSmoothVelocity, panSpeed);

        // Smoothly rotate to look at the target
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * panSpeed);

        // Smooth zoom in and out
        float targetFOV = initialFOV * (1f - zoomInFOVPercentage);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);

        // Pull back the zoom after a moment (reset FOV)
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, initialFOV, Time.deltaTime * zoomSpeed * 0.5f);
    }

    private void AddWobble()
    {
        // Calculate wobble offset using Perlin noise
        wobbleTimer += Time.deltaTime * wobbleFrequency;
        float wobbleX = Mathf.PerlinNoise(wobbleTimer, 0f) * 2f - 1f;
        float wobbleY = Mathf.PerlinNoise(0f, wobbleTimer) * 2f - 1f;
        float wobbleZ = Mathf.PerlinNoise(wobbleTimer * 0.5f, wobbleTimer * 0.5f) * 2f - 1f;

        // Apply wobble as a subtle offset to rotation
        transform.rotation *= Quaternion.Euler(wobbleX * wobbleIntensity, wobbleY * wobbleIntensity, wobbleZ * wobbleIntensity);
    }
}
