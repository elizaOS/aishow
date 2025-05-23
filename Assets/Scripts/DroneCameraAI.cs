using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class DroneCameraAI : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform[] actors;
    [SerializeField] private float minDistanceToActor = 3f;
    [SerializeField] private float maxDistanceToActor = 10f;
    // Removed: [SerializeField] private float targetHeight = 2f; // Now derived from targetPosition.y
    
    [Header("Movement Settings")]
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float stabilizationForce = 8f;
    [SerializeField] private float maxVelocity = 5f;
    [SerializeField] private float avoidanceDistance = 2f;
    [SerializeField] private float heightControl = 10f;
    [SerializeField] private float hoverForce = 9.81f;

    [Header("Orientation Settings")]
    [SerializeField] private float droneRotationSpeed = 5f; // Increased default
    [SerializeField] private float lookAheadDistance = 1f; 
    [SerializeField] private float angularDamping = 0.9f;

    [Header("Camera Settings")]
    [SerializeField] private Camera droneCamera;
    [SerializeField] private float fieldOfViewMin = 30f;
    [SerializeField] private float fieldOfViewMax = 60f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Shot Timing")]
    [SerializeField] private float minHoldDuration = 3f;
    [SerializeField] private float maxHoldDuration = 8f;
    [SerializeField] private float transitionThreshold = 0.5f;
    [SerializeField] private bool randomizeActorOrder = true;
    
    [Header("Boundary Settings")] // New Header
    [SerializeField] private BoxCollider boundaryBox; // Assign your trigger BoxCollider here
    [SerializeField] private float boundaryPushForce = 5f;
    [SerializeField] private float boundaryTargetPadding = 1f; // Keeps target slightly inside bounds
    
    private Rigidbody rb;
    private int currentActorIndex = 0;
    private Vector3 targetPosition;
    private bool isTransitioning;
    private float currentFOV;

    private Vector3 lastError;
    private Vector3 errorSum;
    private readonly float Kp = 2f;
    private readonly float Ki = 0.05f;
    private readonly float Kd = 1f; 
    
    private float shotTimer;
    private float currentHoldDuration;
    private List<int> actorSequence = new List<int>();
    private int sequenceIndex = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.drag = 2f;
        rb.angularDrag = 4f;
        
        if (droneCamera == null)
        {
            droneCamera = GetComponentInChildren<Camera>();
        }
        
        if (droneCamera != null && droneCamera.transform.parent == transform)
        {
            // Ensure camera is perfectly aligned forward and centered
            droneCamera.transform.localPosition = Vector3.zero;
            droneCamera.transform.localRotation = Quaternion.identity;
        }
        else if (droneCamera == null)
        {
            Debug.LogError("DroneCameraAI: No camera found or assigned. Visuals will be incorrect.");
        }
            
        currentFOV = (droneCamera != null) ? droneCamera.fieldOfView : fieldOfViewMax;
        InitializeActorSequence();
        
        if (actors != null && actors.Length > 0) {
            currentHoldDuration = Random.Range(minHoldDuration, maxHoldDuration);
            shotTimer = currentHoldDuration;
            FindNewTargetPosition();
        } else {
            Debug.LogWarning("DroneCameraAI: No actors assigned. Drone will be idle.");
            targetPosition = transform.position; // Default target to current position if no actors
        }
    }

    private void FixedUpdate()
    {
        if (boundaryBox != null) EnforceBoundaries(); // Enforce boundaries first

        if (actors == null || actors.Length == 0 || currentActorIndex >= actors.Length || actors[currentActorIndex] == null)
        {
            ApplySimpleHover(); // Maintain some height even if idle
            SimpleStabilization();
            return;
        }

        ApplyHoverForce();
        ApplyStabilization();
        HandleMovement();
        HandleObstacleAvoidance();
        UpdateDroneOrientation();
        UpdateCameraFOV();
        UpdateShotTimer();
    }

    private void EnforceBoundaries() // New Method
    {
        if (boundaryBox == null) return;

        Bounds bounds = boundaryBox.bounds;
        Vector3 dronePos = rb.position;

        // Check if the drone is outside the bounds
        if (!bounds.Contains(dronePos))
        {
            Vector3 closestPointInBounds = bounds.ClosestPoint(dronePos);
            Vector3 directionToSafety = (closestPointInBounds - dronePos).normalized;
            rb.AddForce(directionToSafety * boundaryPushForce);

            // Optional: Dampen velocity if hitting boundary hard
            // if (Vector3.Dot(rb.velocity, directionToSafety) < 0) // If moving away from safety
            // {
            //     rb.velocity *= 0.8f; // Dampen velocity
            // }
        }
    }

    private void ApplyHoverForce()
    {
        // Use the Y component of the overall targetPosition for hover height
        float heightError = targetPosition.y - transform.position.y;
        Vector3 hoverForceVector = Vector3.up * (hoverForce + heightError * heightControl);
        rb.AddForce(hoverForceVector, ForceMode.Acceleration);
    }

    private void ApplySimpleHover() // Used when no target, hovers at a default height (e.g., its current height or a fixed one)
    {
        // Maintain current height or a default idle height
        float idleTargetHeight = (boundaryBox != null) ? boundaryBox.bounds.center.y : transform.position.y;
        float heightError = idleTargetHeight - transform.position.y;
        // Weaken hover force if no specific target, just enough to counter gravity mostly
        Vector3 hoverForceVector = Vector3.up * (hoverForce + heightError * heightControl * 0.5f);
        rb.AddForce(hoverForceVector, ForceMode.Acceleration);
    }

    private void ApplyStabilization()
    {
        rb.angularVelocity *= angularDamping; 
        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if(rb.velocity.magnitude < maxVelocity * 0.5f && !isTransitioning){
            rb.AddForce(-horizontalVel * stabilizationForce * 0.1f);
        }
    }

    private void SimpleStabilization()
    {
        rb.angularVelocity *= angularDamping;
    }

    private void UpdateDroneOrientation()
    {
        if (actors[currentActorIndex] == null) return;

        Transform currentTargetActor = actors[currentActorIndex];
        Vector3 targetLookPosition = currentTargetActor.position;

        Rigidbody actorRb = currentTargetActor.GetComponent<Rigidbody>();
        if (actorRb != null)
        {
            targetLookPosition += actorRb.velocity * lookAheadDistance;
        }

        Vector3 directionToTarget = targetLookPosition - rb.position;

        // --- DEBUG SECTION START ---
        // Uncomment these lines in your Unity Editor to see debug rays in the Scene View
        // Debug.DrawRay(rb.position, directionToTarget, Color.red); // Shows desired look direction
        // Debug.DrawRay(rb.position, rb.transform.forward * directionToTarget.magnitude, Color.blue); // Shows current forward direction
        // --- DEBUG SECTION END ---

        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetBodyOrientation = Quaternion.LookRotation(directionToTarget);
            Quaternion newOrientation = Quaternion.Slerp(rb.rotation, targetBodyOrientation, droneRotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newOrientation);
        }
    }

    private void HandleMovement()
    {
        Vector3 error = targetPosition - transform.position;
        Vector3 errorDerivative = (error - lastError) / Time.fixedDeltaTime;
        errorSum += error * Time.fixedDeltaTime;
        errorSum = Vector3.ClampMagnitude(errorSum, 10f);
        Vector3 force = error * Kp + errorSum * Ki + errorDerivative * Kd;
        force = Vector3.ClampMagnitude(force, moveForce);

        // Horizontal force is still based on PID to reach the XZ of targetPosition
        Vector3 horizontalForce = new Vector3(force.x, 0, force.z);
        rb.AddForce(horizontalForce);

        // Vertical movement is now primarily handled by ApplyHoverForce aiming for targetPosition.y
        // We can remove or further reduce the PID's direct vertical force component if ApplyHoverForce is sufficient.
        // For now, let's keep a very small influence or remove it.
        // Vector3 verticalForce = new Vector3(0, force.y, 0);
        // rb.AddForce(verticalForce * 0.1f); // Significantly reduced or removed

        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 verticalVelocity = new Vector3(0, rb.velocity.y, 0);
        horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, maxVelocity);
        verticalVelocity = Vector3.ClampMagnitude(verticalVelocity, maxVelocity * 0.8f); // Allow a bit more vertical speed for hover adjustments
        rb.velocity = horizontalVelocity + verticalVelocity;
        lastError = error;
    }
    
    private void InitializeActorSequence()
    {
        if (actors == null || actors.Length == 0) return;
        actorSequence.Clear();
        for (int i = 0; i < actors.Length; i++)
        {
            actorSequence.Add(i);
        }
        if (randomizeActorOrder && actorSequence.Count > 1)
        {
            for (int i = actorSequence.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (actorSequence[i], actorSequence[j]) = (actorSequence[j], actorSequence[i]);
            }
        }
        sequenceIndex = -1;
        currentActorIndex = 0;
         if (actorSequence.Count > 0) currentActorIndex = actorSequence[0];
    }

    private void UpdateShotTimer()
    {
        if (actors == null || actors.Length == 0) return;
        shotTimer -= Time.deltaTime;
        bool shouldSwitch = shotTimer <= 0;
        if (currentActorIndex >= actors.Length || actors[currentActorIndex] == null)
        {
            shouldSwitch = true;
        }
        if (shouldSwitch && !isTransitioning)
        {
            FindNewTargetPosition();
            currentHoldDuration = Random.Range(minHoldDuration, maxHoldDuration);
            shotTimer = currentHoldDuration;
        }
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (isTransitioning && distanceToTarget < transitionThreshold)
        {
            isTransitioning = false;
            shotTimer = currentHoldDuration; 
        }
    }

    private void FindNewTargetPosition()
    {
        if (actors == null || actors.Length == 0) 
        {
            Debug.LogWarning("FindNewTargetPosition: No actors to target.");
            targetPosition = transform.position; // Stay put if no actors
            isTransitioning = false;
            return;
        }
        
        sequenceIndex++;
        if (sequenceIndex >= actorSequence.Count)
        {
            if (randomizeActorOrder && actorSequence.Count > 1)
            {
                for (int i = actorSequence.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (actorSequence[i], actorSequence[j]) = (actorSequence[j], actorSequence[i]);
                }
            }
            sequenceIndex = 0;
        }
        
        currentActorIndex = actorSequence[sequenceIndex];
        int initialIndex = sequenceIndex;
        bool foundValidActor = false;
        do {
            if (actors[currentActorIndex] != null) {
                foundValidActor = true;
                break;
            }
            sequenceIndex = (sequenceIndex + 1) % actorSequence.Count;
            currentActorIndex = actorSequence[sequenceIndex];
        } while (sequenceIndex != initialIndex);

        if (!foundValidActor) {
            Debug.LogWarning("No valid (non-null) actors found in sequence! Drone will hold current position or idle.");
            targetPosition = transform.position; // Hold current pos if no valid actor found
            isTransitioning = false;
            return;
        }

        Transform currentTargetTransform = actors[currentActorIndex];
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(minDistanceToActor, maxDistanceToActor);
        Vector3 randomOffset = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward * randomDistance;
        // targetPosition now directly uses actor's height + a random vertical offset for the drone's center to aim for.
        Vector3 calculatedTargetPosition = currentTargetTransform.position + randomOffset + Vector3.up * Random.Range(1f, 2f); // Reduced max Y offset a bit

        // Clamp target position to boundary box if it exists
        if (boundaryBox != null)
        {
            Bounds worldBounds = boundaryBox.bounds;
            calculatedTargetPosition.x = Mathf.Clamp(calculatedTargetPosition.x, worldBounds.min.x + boundaryTargetPadding, worldBounds.max.x - boundaryTargetPadding);
            calculatedTargetPosition.y = Mathf.Clamp(calculatedTargetPosition.y, worldBounds.min.y + boundaryTargetPadding, worldBounds.max.y - boundaryTargetPadding);
            calculatedTargetPosition.z = Mathf.Clamp(calculatedTargetPosition.z, worldBounds.min.z + boundaryTargetPadding, worldBounds.max.z - boundaryTargetPadding);
        }
        targetPosition = calculatedTargetPosition;
        isTransitioning = true;

        if (Physics.Raycast(targetPosition, (currentTargetTransform.position - targetPosition).normalized, 
            out RaycastHit hit, Vector3.Distance(targetPosition, currentTargetTransform.position) - 0.1f ))
        {
            if (hit.transform != currentTargetTransform && !hit.transform.IsChildOf(transform))
            {
                Debug.Log("Target position for drone movement might be occluded by " + hit.transform.name + ". Drone will attempt shot or pick new spot next cycle.");
            }
        }
    }

    private void HandleObstacleAvoidance()
    {
        Ray[] rays = new Ray[]
        {
            new Ray(transform.position, transform.forward * avoidanceDistance),
            new Ray(transform.position, transform.right * avoidanceDistance),
            new Ray(transform.position, -transform.right * avoidanceDistance),
            new Ray(transform.position, transform.up * avoidanceDistance * 0.5f),
            new Ray(transform.position, -transform.up * avoidanceDistance * 0.5f)
        };

        foreach (Ray ray in rays)
        {
            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, ray.direction.magnitude))
            {
                // Avoid hitting anything that isn't an actor AND isn't part of the boundary box itself (if applicable)
                bool isActor = actors.Contains(hit.transform);
                bool isBoundary = (boundaryBox != null && hit.transform == boundaryBox.transform);
                
                if (!isActor && !isBoundary)
                {
                    Vector3 avoidanceForceDir = transform.position - hit.point;
                    rb.AddForce(avoidanceForceDir.normalized * moveForce * 1.5f);
                }
            }
        }
    }

    private void UpdateCameraFOV()
    {
        if (actors == null || currentActorIndex >= actors.Length || actors[currentActorIndex] == null || droneCamera == null) return;
        float distanceToActor = Vector3.Distance(transform.position, actors[currentActorIndex].position);
        float targetFOV = Mathf.Lerp(fieldOfViewMax, fieldOfViewMin, 
            Mathf.InverseLerp(minDistanceToActor, maxDistanceToActor, distanceToActor));
        // Lerp from current camera FOV to target FOV
        droneCamera.fieldOfView = Mathf.Lerp(droneCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        // currentFOV variable is not used anymore, direct assignment to camera is better.
    }

    public void SetActors(Transform[] newActors)
    {
        actors = newActors;
        InitializeActorSequence();
        if (actors != null && actors.Length > 0)
        {
            FindNewTargetPosition();
            currentHoldDuration = Random.Range(minHoldDuration, maxHoldDuration);
            shotTimer = currentHoldDuration;
        }
    }
} 