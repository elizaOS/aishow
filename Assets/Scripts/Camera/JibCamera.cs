using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JibCamera : MonoBehaviour
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

    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera; // Reference to the actual Camera component (likely on a child object)

    private bool isZooming = false; // Flag to indicate if zoom is active
    private float zoomTimer = 0f; // Timer for managing zoom cooldown
    private float timeToNextZoom; // Time before the next zoom event

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Ensure physics doesn't interfere with manual rotation
        }
    }

    private void Start()
    {
        InitializeCameraTarget();
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = defaultFOV;
        }
        else
        {
            Debug.LogError("Target Camera is not assigned in JibCamera!", this);
        }
    }

    private void OnEnable()
    {
        InitializeCameraTarget();
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = defaultFOV;
        }
    }

    private void InitializeCameraTarget()
    {
        if (targets.Length > 0)
        {
            // Select initial target if not already set
            if (currentTarget == null)
            {
                currentTarget = targets[Random.Range(0, targets.Length)];
            }
            
            // Calculate and set initial rotation
            if (currentTarget != null)
            {
                Vector3 directionToTarget = currentTarget.position - transform.position;
                desiredRotation = Quaternion.LookRotation(directionToTarget);
                
                // Ensure smooth initial rotation
                // transform.rotation = desiredRotation; // Commented out: Let LateUpdate handle the initial smoothing to prevent jitter
            }

            // Reset zoom-related variables
            isZooming = false;
            zoomTimer = 0f;
            timeToNextZoom = Random.Range(zoomCooldownMin, zoomCooldownMax);

            // Reset target switching and other timers
            timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
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

    private void LateUpdate()
    {
        if (currentTarget == null) return;

        // 'desiredRotation' is already calculated in Update() as the WORLD rotation
        // that the camera should have to look at the target.

        // Calculate the world rotation we actually want to aim for, including overshoot and smoothing
        Quaternion currentActualWorldRotation = transform.rotation; 
        Quaternion overshotWorldRotation = Quaternion.Slerp(currentActualWorldRotation, desiredRotation, 1f + (overshootAmount / 100f));
        Quaternion smoothTargetWorldRotation = Quaternion.Slerp(currentActualWorldRotation, overshotWorldRotation, panSpeed * Time.deltaTime);
        Quaternion finalTargetWorldRotation = Quaternion.Slerp(smoothTargetWorldRotation, desiredRotation, overshootDamping * Time.deltaTime);

        // Convert this final target WORLD rotation into the LOCAL rotation needed for this transform.
        // This ensures the camera aims correctly relative to its parent's orientation.
        if (transform.parent != null)
        {
            transform.localRotation = Quaternion.Inverse(transform.parent.rotation) * finalTargetWorldRotation;
        }
        else
        {
            // If there's no parent, localRotation is the same as worldRotation
            transform.rotation = finalTargetWorldRotation;
        }
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
        //Debug.Log("Zoom started");

        // Zoom in
        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = Mathf.Lerp(defaultFOV, zoomFOV, elapsedTime / (zoomDuration / 2f));
            }
            yield return null;
        }

        // Hold at the zoomed-in state
        yield return new WaitForSeconds(1.0f); // Adjust this duration as needed

        // Zoom out
        elapsedTime = 0f;
        while (elapsedTime < zoomDuration / 2f)
        {
            elapsedTime += Time.deltaTime;
            if (targetCamera != null)
            {
                targetCamera.fieldOfView = Mathf.Lerp(zoomFOV, defaultFOV, elapsedTime / (zoomDuration / 2f));
            }
            yield return null;
        }

        if (targetCamera != null)
        {
            targetCamera.fieldOfView = defaultFOV;
        }

        //Debug.Log("Zoom finished");
        isZooming = false;
    }
}