using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// No longer requiring Camera component on this GameObject itself
// [RequireComponent(typeof(Camera))] 
public class HandheldCamera : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("The actual Unity Camera that this script will control (FOV, and its transform should follow this GameObject).")]
    public Camera actualUnityCamera; // Public reference to the Camera component

    [Header("Targeting")]
    public Transform[] targets; // List of targets (actors on stage)
    public float switchTargetTimeMin = 3f; // Minimum time before switching targets
    public float switchTargetTimeMax = 5f; // Maximum time before switching targets
    public float panSpeed = 2f; // Speed of panning/turning
    public float overshootAmount = 15f; // Maximum overshoot angle in degrees
    public float overshootDamping = 5f; // How quickly the overshoot corrects
    public float wobbleIntensity = 0.2f; // Intensity of the wobble
    public float wobbleFrequency = 1.5f; // Frequency of the wobble

    public float defaultFOV = 60f; // Default field of view
    public float zoomFOV = 40f; // Zoomed-in field of view
    public float zoomDuration = 2f; // Duration of the zoom effect
    public float zoomCooldownMin = 5f; // Minimum time between zooms
    public float zoomCooldownMax = 10f; // Maximum time between zooms

    private Transform currentTarget; // Current target being tracked
    private float timeToSwitchTarget; // Time left before switching targets
    private Rigidbody rb; // Rigidbody for smooth rotation
    private Quaternion desiredRotation; // Desired rotation to look at the target
    private float wobbleTimer; // Timer for calculating wobble

    private bool isZooming = false; // Flag to indicate if zoom is active
    private float zoomTimer = 0f; // Timer for managing zoom cooldown
    private float timeToNextZoom; // Time before the next zoom event

    // Flag to ensure one-time initialization for certain settings (like initial rotation snap)
    private bool m_IsFirstInitialization = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // cam = GetComponent<Camera>(); // Old way of getting camera
        
        // Validate the actualUnityCamera reference
        if (actualUnityCamera == null)
        {
            Debug.LogError("HandheldCamera: 'actualUnityCamera' is not assigned in the Inspector. This script requires a Camera to control.", this);
            enabled = false; // Disable script if no camera is assigned
            return;
        }

        // FOV initialization is now handled in InitializeCameraTarget to ensure correct behavior
        // on enable/disable cycles.
        // Initial FOV setting, if needed directly in Awake, would use actualUnityCamera.fieldOfView
    }

    private void Start()
    {
        // InitializeCameraTarget is called in OnEnable, which also runs on start.
        // Calling it here ensures initialization if the object starts active.
        // If it starts disabled, OnEnable will handle it when activated.
        // The logic within InitializeCameraTarget manages first-time vs. subsequent calls.
        InitializeCameraTarget();
    }

    private void OnEnable()
    {
        // Called when the component is enabled, including after being disabled or when the GameObject is activated.
        // Handles both initial setup (if Start hasn't run yet for a disabled-then-enabled object)
        // and re-initialization logic.
        InitializeCameraTarget();
    }

    private void InitializeCameraTarget()
    {
        if (targets == null || targets.Length == 0)
        {
            // No targets available.
            // Ensure camera is at default FOV and not in a zooming state.
            if (actualUnityCamera != null)
            {
                actualUnityCamera.fieldOfView = defaultFOV;
            }
            isZooming = false;
            // currentTarget will be null, Update/FixedUpdate will do nothing.
            // No rotation change is enforced here if no targets.

            // If this is the first time, mark it so full init doesn't happen again if targets appear later
            if (m_IsFirstInitialization)
            {
                m_IsFirstInitialization = false;
            }
            return;
        }

        // --- Target Selection ---
        // Preserve current target if it exists and is valid, otherwise pick a new one.
        // (Original logic for this was sound: if currentTarget is null, pick one)
        if (currentTarget == null)
        {
            currentTarget = targets[Random.Range(0, targets.Length)];
        }
        // If currentTarget is still null (e.g. targets array was filtered to empty by other logic not shown),
        // we might have an issue. Assuming targets array always has valid entries if length > 0.

        // --- Rotation ---
        if (currentTarget != null)
        {
            Vector3 directionToTarget = currentTarget.position - transform.position;
            desiredRotation = Quaternion.LookRotation(directionToTarget);

            if (m_IsFirstInitialization)
            {
                // Snap rotation only on the very first initialization.
                // This prevents rotational jumps when re-enabling the script.
                transform.rotation = desiredRotation;
            }
            // On subsequent enables, transform.rotation is not snapped here.
            // FixedUpdate will smoothly handle camera movement towards desiredRotation.
        }

        // --- FOV and Zoom State ---
        if (actualUnityCamera != null)
        {
            // On first initialization OR on any re-enable, reset to default FOV.
            // This addresses the "snap back" from a previous zoom state when re-enabled.
            actualUnityCamera.fieldOfView = defaultFOV;
        }
        // Reset zoom state variables consistently upon enable/re-enable.
        isZooming = false; // Ensures no zoom operation thinks it's active.
        zoomTimer = 0f;    // Reset zoom cooldown timer.
        timeToNextZoom = Random.Range(zoomCooldownMin, zoomCooldownMax); // Schedule next potential zoom.

        // --- Target Switching Timer ---
        // Reset the timer for switching targets.
        timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);

        // After the first full initialization sequence, set this flag to false.
        if (m_IsFirstInitialization)
        {
            m_IsFirstInitialization = false;
        }
    }

    private void Update()
    {
        if (targets.Length == 0) return; // No targets to focus on

        // Handle target switching
        timeToSwitchTarget -= Time.deltaTime;
        if (timeToSwitchTarget <= 0f)
        {
            SwitchTarget();
            timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        }

        // Calculate the desired rotation to look at the current target
        if (currentTarget != null)
        {
            Vector3 directionToTarget = currentTarget.position - transform.position;
            desiredRotation = Quaternion.LookRotation(directionToTarget);
        }

        // Handle zoom logic
        HandleZoom();
    }

    private void FixedUpdate()
    {
        if (currentTarget == null) return;

        // Apply a smooth rotation using physics (via Rigidbody)
        Quaternion currentRotation = rb.rotation;

        // Add an initial exaggerated overshoot
        Quaternion overshootRotation = Quaternion.Slerp(currentRotation, desiredRotation, 1f + (overshootAmount / 100f));

        // Smoothly pan toward the overshoot rotation
        Quaternion smoothRotation = Quaternion.Slerp(currentRotation, overshootRotation, panSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRotation);

        // Gradually correct back to the exact desired rotation
        rb.rotation = Quaternion.Slerp(rb.rotation, desiredRotation, overshootDamping * Time.fixedDeltaTime);

        // Add wobble for a more natural feel
        AddWobble();
    }

    private void SwitchTarget()
    {
        if (targets.Length > 1)
        {
            Transform newTarget;
            do
            {
                newTarget = targets[Random.Range(0, targets.Length)];
            } while (newTarget == currentTarget);

            currentTarget = newTarget;
        }
    }

    private void AddWobble()
    {
        // Increment wobble timer
        wobbleTimer += Time.deltaTime * wobbleFrequency;

        // Calculate small random rotations on each axis using Perlin noise
        float wobbleX = Mathf.PerlinNoise(wobbleTimer, 0f) * 2f - 1f;
        float wobbleY = Mathf.PerlinNoise(0f, wobbleTimer) * 2f - 1f;
        float wobbleZ = Mathf.PerlinNoise(wobbleTimer * 0.5f, wobbleTimer * 0.5f) * 2f - 1f;

        // Apply wobble to the current rotation
        Quaternion wobbleRotation = Quaternion.Euler(
            wobbleX * wobbleIntensity,
            wobbleY * wobbleIntensity,
            wobbleZ * wobbleIntensity
        );

        rb.MoveRotation(rb.rotation * wobbleRotation);
    }

    private void HandleZoom()
    {
        // Decrease the zoom timer
        zoomTimer -= Time.deltaTime;

        // Only trigger zoom if not already zooming and timer runs out
        if (!isZooming && zoomTimer <= 0f)
        {
            // Start zooming
            StartCoroutine(ZoomCoroutine());

            // Schedule the next zoom
            timeToNextZoom = Random.Range(zoomCooldownMin, zoomCooldownMax);
            zoomTimer = timeToNextZoom; // Reset the zoom timer
        }
    }


    private System.Collections.IEnumerator ZoomCoroutine()
{
    isZooming = true;
    //Debug.Log("Zoom started");

    // Zoom in
    float elapsedTime = 0f;
    while (elapsedTime < zoomDuration / 2f)
    {
        elapsedTime += Time.deltaTime;
        actualUnityCamera.fieldOfView = Mathf.Lerp(defaultFOV, zoomFOV, elapsedTime / (zoomDuration / 2f));
        yield return null;
    }

    // Hold at the zoomed-in state
    yield return new WaitForSeconds(1.0f); // Adjust this duration as needed

    // Zoom out
    elapsedTime = 0f;
    while (elapsedTime < zoomDuration / 2f)
    {
        elapsedTime += Time.deltaTime;
        actualUnityCamera.fieldOfView = Mathf.Lerp(zoomFOV, defaultFOV, elapsedTime / (zoomDuration / 2f));
        yield return null;
    }

    //Debug.Log("Zoom finished");
    isZooming = false;
}


}
