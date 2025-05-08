using UnityEngine;
using System.Collections; // Required for IEnumerator

public class UnifiedJibController : MonoBehaviour
{
    [Header("Jib Structure References")]
    [Tooltip("The Transform that will be rotated for panning (Y-axis).")]
    public Transform jibBasePivot;
    [Tooltip("The Transform, child of Base Pivot, that will be rotated for pitching (typically X-axis).")]
    public Transform jibArmPivot;
    [Tooltip("The Transform of the actual camera object, child of Arm Pivot.")]
    public Transform cameraTransform;
    [Tooltip("The Camera component itself, usually on the cameraTransform object or its child.")]
    public Camera actualCamera;

    [Header("Panning (Base Y-Axis Rotation)")]
    public float panAngle = 180f;
    public float panSpeed = 2f; // Speed of the base panning motion
    private bool isPanningRight = true;
    private float currentPanTime = 0f;
    private float totalPanDuration; // Total time for one full pan sweep (half-angle)

    [Header("Pitching (Arm X-Axis Rotation)")]
    public float pitchMin = -10f;
    public float pitchMax = 10f;
    public float pitchCycleDuration = 5f; // Total time for one full pitch cycle (min to max and back to min)
    private bool isPitchingUp = true;
    private float currentPitchTime = 0f;

    [Header("Camera Targeting")]
    public Transform[] targets;
    public float switchTargetTimeMin = 3f;
    public float switchTargetTimeMax = 5f;
    [Tooltip("Speed at which the camera aims towards the target.")]
    public float cameraAimSpeed = 2f;
    [Tooltip("How much the camera overshoots the target. Set to 0 for no overshoot.")]
    public float overshootAmount = 0f;
    [Tooltip("How quickly the camera corrects from an overshoot.")]
    public float overshootDamping = 5f;
    private Transform currentTarget;
    private float timeToSwitchTarget;
    private Quaternion desiredCameraWorldRotation; // The ideal world rotation for the camera to look at the target

    [Header("Camera Zoom")]
    public float defaultFOV = 60f;
    public float zoomFOV = 40f;
    public float zoomInOutDuration = 1f; // Duration for zoom-in phase AND zoom-out phase each
    public float zoomHoldDuration = 1f; // How long to stay zoomed in
    public float zoomCooldownMin = 5f;
    public float zoomCooldownMax = 10f;
    private bool isZooming = false;
    private float zoomCooldownTimer = 0f;
    private float timeToNextZoomAttempt;


    void Awake()
    {
        // Automatically find camera component if not assigned and cameraTransform is.
        if (actualCamera == null && cameraTransform != null)
        {
            actualCamera = cameraTransform.GetComponentInChildren<Camera>();
        }

        // Validate critical references
        if (jibBasePivot == null) Debug.LogError("UnifiedJibController: Jib Base Pivot is not assigned!", this);
        if (jibArmPivot == null) Debug.LogError("UnifiedJibController: Jib Arm Pivot is not assigned!", this);
        if (cameraTransform == null) Debug.LogError("UnifiedJibController: Camera Transform is not assigned!", this);
        if (actualCamera == null) Debug.LogError("UnifiedJibController: Actual Camera component could not be found or assigned!", this);
    }

    void Start()
    {
        // Initialize Panning
        if (jibBasePivot != null && panSpeed > 0)
        {
            totalPanDuration = panAngle / panSpeed; // Time to pan half the total angle
            jibBasePivot.localRotation = Quaternion.Euler(0f, -panAngle / 2f, 0f);
        }
        else if (panSpeed <= 0 && panAngle > 0)
        {
            Debug.LogWarning("UnifiedJibController: Pan speed is zero or negative, panning will be disabled.", this);
        }

        // Initialize Pitching
        if (jibArmPivot != null && pitchCycleDuration > 0)
        {
            // Start pitch from min or current, let SmoothStep handle it.
            // jibArmPivot.localRotation = Quaternion.Euler(pitchMin, 0f, 0f); // Optional: start at min pitch
        }
        else if (pitchCycleDuration <= 0 && (pitchMin != 0 || pitchMax != 0))
        {
             Debug.LogWarning("UnifiedJibController: Pitch duration is zero or negative, pitching will be disabled.", this);
        }

        // Initialize Targeting & Zoom
        InitializeCameraTargetAndZoomState();
        if (actualCamera != null)
        {
            actualCamera.fieldOfView = defaultFOV;
        }
    }

    void OnEnable()
    {
        // Re-initialize if the component is re-enabled
        InitializeCameraTargetAndZoomState();
         if (actualCamera != null)
        {
            actualCamera.fieldOfView = defaultFOV;
        }
    }
    
    void InitializeCameraTargetAndZoomState()
    {
        if (targets != null && targets.Length > 0)
        {
            // Select an initial target if none is set or current is invalid
            if (currentTarget == null || System.Array.IndexOf(targets, currentTarget) == -1)
            {
                 currentTarget = targets[Random.Range(0, targets.Length)];
            }
            
            // Calculate initial desired rotation (world space)
            if (currentTarget != null && cameraTransform != null)
            {
                Vector3 directionToTarget = currentTarget.position - cameraTransform.position;
                if (directionToTarget != Vector3.zero)
                {
                    desiredCameraWorldRotation = Quaternion.LookRotation(directionToTarget);
                }
                else // Camera is at target, look forward relative to parent (arm)
                {
                    desiredCameraWorldRotation = cameraTransform.parent != null ? cameraTransform.parent.rotation : Quaternion.identity;
                }
            }

            isZooming = false;
            zoomCooldownTimer = 0f; // Allow first zoom based on initial cooldown
            timeToNextZoomAttempt = Random.Range(zoomCooldownMin, zoomCooldownMax);

            timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        }
    }

    void Update()
    {
        HandleJibPanning();
        HandleJibPitching();

        if (targets == null || targets.Length == 0) return; // No targets, no need for camera logic

        HandleTargetSwitchingLogic();
        CalculateDesiredCameraWorldRotation();
        HandleCameraZoomLogic();
    }

    void LateUpdate()
    {
        if (targets == null || targets.Length == 0 || cameraTransform == null) return;
        
        ApplyCameraAimingRotation();
    }

    void HandleJibPanning()
    {
        if (jibBasePivot == null || panSpeed <= 0 || panAngle <= 0 || totalPanDuration <=0) return;

        currentPanTime += Time.deltaTime;
        float t = Mathf.Clamp01(currentPanTime / totalPanDuration);
        t = Mathf.SmoothStep(0f, 1f, t); // Ease in/out

        float currentAngle = Mathf.Lerp(
            isPanningRight ? -panAngle / 2f : panAngle / 2f,
            isPanningRight ? panAngle / 2f : -panAngle / 2f,
            t
        );
        jibBasePivot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

        if (t >= 1f)
        {
            isPanningRight = !isPanningRight;
            currentPanTime = 0f;
        }
    }

    void HandleJibPitching()
    {
        if (jibArmPivot == null || pitchCycleDuration <= 0) return;

        currentPitchTime += Time.deltaTime;
        // t goes 0->1 for up, then 0->1 for down, effectively making pitchCycleDuration the time for one direction
        float t = Mathf.Clamp01(currentPitchTime / (pitchCycleDuration / 2f) ); 
        t = Mathf.SmoothStep(0f, 1f, t);

        float pitch = Mathf.Lerp(
            isPitchingUp ? pitchMin : pitchMax,
            isPitchingUp ? pitchMax : pitchMin,
            t
        );
        // Pitching around the local X-axis of the arm pivot
        jibArmPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (t >= 1f)
        {
            isPitchingUp = !isPitchingUp;
            currentPitchTime = 0f;
        }
    }

    void HandleTargetSwitchingLogic()
    {
        timeToSwitchTarget -= Time.deltaTime;
        if (timeToSwitchTarget <= 0f)
        {
            SwitchToNewTarget();
            timeToSwitchTarget = Random.Range(switchTargetTimeMin, switchTargetTimeMax);
        }
    }

    void SwitchToNewTarget()
    {
        if (targets.Length == 0) return;
        if (targets.Length == 1) {
            currentTarget = targets[0];
            return;
        }

        Transform newTarget;
        int attempts = 0; // Prevent infinite loop if all targets are somehow the same
        do
        {
            newTarget = targets[Random.Range(0, targets.Length)];
            attempts++;
        } while (newTarget == currentTarget && attempts < targets.Length * 2);
        currentTarget = newTarget;
    }

    void CalculateDesiredCameraWorldRotation()
    {
        if (currentTarget != null && cameraTransform != null)
        {
            Vector3 directionToTarget = currentTarget.position - cameraTransform.position;
            if (directionToTarget != Vector3.zero)
            {
                desiredCameraWorldRotation = Quaternion.LookRotation(directionToTarget);
            }
            // If direction is zero, keep the last desired rotation or aim forward from parent
            else if (cameraTransform.parent != null)
            {
                 desiredCameraWorldRotation = cameraTransform.parent.rotation * Quaternion.LookRotation(Vector3.forward);
            }
        }
    }

    void ApplyCameraAimingRotation()
    {
        if (currentTarget == null || cameraTransform == null) return;

        Quaternion currentActualCameraWorldRotation = cameraTransform.rotation;
        Quaternion targetRotation = desiredCameraWorldRotation; // This is the "perfect aim"

        if (overshootAmount > 0.01f) // Apply overshoot only if amount is significant
        {
            Quaternion overshotWorldRotation = Quaternion.SlerpUnclamped(targetRotation, currentActualCameraWorldRotation, -overshootAmount / 100f);
            targetRotation = Quaternion.Slerp(currentActualCameraWorldRotation, overshotWorldRotation, cameraAimSpeed * Time.deltaTime);
            targetRotation = Quaternion.Slerp(targetRotation, desiredCameraWorldRotation, overshootDamping * Time.deltaTime);
        }
        else // No overshoot, just smooth Slerp to target
        {
            targetRotation = Quaternion.Slerp(currentActualCameraWorldRotation, desiredCameraWorldRotation, cameraAimSpeed * Time.deltaTime);
        }
        
        // Convert the final target WORLD rotation into the LOCAL rotation for the cameraTransform,
        // relative to its parent (which should be jibArmPivot).
        if (cameraTransform.parent != null)
        {
            cameraTransform.localRotation = Quaternion.Inverse(cameraTransform.parent.rotation) * targetRotation;
        }
        else
        {
            // If cameraTransform has no parent, its local rotation is its world rotation.
            cameraTransform.rotation = targetRotation;
        }
    }

    void HandleCameraZoomLogic()
    {
        if (actualCamera == null) return;

        zoomCooldownTimer -= Time.deltaTime;

        if (!isZooming && zoomCooldownTimer <= 0f)
        {
            StartCoroutine(PerformZoomCoroutine());
            // Reset cooldown timer for the *next* zoom attempt
            timeToNextZoomAttempt = Random.Range(zoomCooldownMin, zoomCooldownMax);
            zoomCooldownTimer = timeToNextZoomAttempt; 
        }
    }

    IEnumerator PerformZoomCoroutine()
    {
        if (actualCamera == null) yield break;
        isZooming = true;

        // Zoom In
        float elapsedTime = 0f;
        float startFOV = actualCamera.fieldOfView; // Use current FOV in case it's not default
        while (elapsedTime < zoomInOutDuration)
        {
            elapsedTime += Time.deltaTime;
            actualCamera.fieldOfView = Mathf.Lerp(startFOV, zoomFOV, elapsedTime / zoomInOutDuration);
            yield return null;
        }
        actualCamera.fieldOfView = zoomFOV; // Ensure target FOV is reached

        // Hold Zoom
        yield return new WaitForSeconds(zoomHoldDuration);

        // Zoom Out
        elapsedTime = 0f;
        startFOV = actualCamera.fieldOfView; // Should be zoomFOV here
        while (elapsedTime < zoomInOutDuration)
        {
            elapsedTime += Time.deltaTime;
            actualCamera.fieldOfView = Mathf.Lerp(startFOV, defaultFOV, elapsedTime / zoomInOutDuration);
            yield return null;
        }
        actualCamera.fieldOfView = defaultFOV; // Ensure default FOV is reached

        isZooming = false;
    }
} 