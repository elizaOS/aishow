using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Camera))]
public class JibCameraOld : MonoBehaviour
{
    public Transform[] targets; // List of targets (actors on stage)
    public float switchTargetTimeMin = 3f; // Minimum time before switching targets
    public float switchTargetTimeMax = 5f; // Maximum time before switching targets
    public float panSpeed = 2f; // Speed of panning/turning
    public float overshootAmount = 15f; // Maximum overshoot angle in degrees
    public float overshootDamping = 5f; // How quickly the overshoot corrects
    public float defaultFOV = 60f; // Default field of view
    public float zoomFOV = 40f; // Zoomed-in field of view
    public float zoomDuration = 2f; // Duration of the zoom effect
    public float zoomCooldownMin = 5f; // Minimum time between zooms
    public float zoomCooldownMax = 10f; // Maximum time between zooms
    private Transform currentTarget; // Current target being tracked
    private float timeToSwitchTarget; // Time left before switching targets
    private Rigidbody rb; // Rigidbody for smooth rotation
    private Quaternion desiredRotation; // Desired rotation to look at the target
    private Camera cam; // Reference to the camera component
    private bool isZooming = false; // Flag to indicate if zoom is active
    private float zoomTimer = 0f; // Timer for managing zoom cooldown
    private float timeToNextZoom; // Time before the next zoom event
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = defaultFOV;
        }
        if (targets.Length > 0)
        {
            currentTarget = targets[Random.Range(0, targets.Length)];
        }
        timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        timeToNextZoom = Random.Range(zoomCooldownMin, zoomCooldownMax);
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
    private void LateUpdate()
    {
        if (currentTarget == null) return;
        // Calculate the desired rotation to look at the target
        Quaternion currentRotation = transform.rotation;
        // Add an initial exaggerated overshoot
        Quaternion overshootRotation = Quaternion.Slerp(currentRotation, desiredRotation, 1f + (overshootAmount / 100f));
        // Smoothly pan toward the overshoot rotation
        Quaternion smoothRotation = Quaternion.Slerp(currentRotation, overshootRotation, panSpeed * Time.deltaTime);
        // Apply the rotation
        transform.rotation = Quaternion.Slerp(smoothRotation, desiredRotation, overshootDamping * Time.deltaTime);
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
        Debug.Log("Zoom started");
        // Zoom in
        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(defaultFOV, zoomFOV, elapsedTime / (zoomDuration / 2f));
            yield return null;
        }
        // Hold at the zoomed-in state
        yield return new WaitForSeconds(1.0f); // Adjust this duration as needed
        // Zoom out
        elapsedTime = 0f;
        while (elapsedTime < zoomDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(zoomFOV, defaultFOV, elapsedTime / (zoomDuration / 2f));
            yield return null;
        }
        Debug.Log("Zoom finished");
        isZooming = false;
    }
}