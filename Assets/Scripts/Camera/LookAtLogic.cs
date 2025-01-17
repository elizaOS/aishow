

 using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LookAtLogic : MonoBehaviour
{
    [Header("Target Settings")]
    public Camera assignedCamera;       // Manually assign the camera in the inspector
    public Transform defaultTarget;     // Default look-at target (e.g., camera or null)
    private Transform currentTarget;    // The current target to look at
    private bool isLocked;              // Whether the current target is locked

    private Animator animator;          // Reference to the Animator component

    [Header("LookAt Settings")]
    [Range(0f, 1f)] public float lookAtWeight = 1.0f;  // Weight of the LookAt behavior
    public float lookAtLerpSpeed = 5.0f;               // Speed for smooth target transition

    private Vector3 smoothLookAtPosition;              // Smoothly interpolated target position

    [Header("Natural Motion Settings")]
    [Range(0f, 1f)] public float naturalMotionWeight = 1.0f; // Weight for natural motion
    [Range(-1f, 1f)] public float leanForwardBackward = 0.0f; // Leaning forward/backward
    [Range(-1f, 1f)] public float swayLeftRight = 0.0f;       // Swaying left/right
    public float swaySpeed = 0.5f;                             // Sway speed
    public Transform hipBone;                                 // Reference to the hip bone

    [Header("Motion Multipliers")]
    public float leanMultiplier = 1.0f;   // Multiplier for leaning
    public float swayMultiplier = 1.0f;  // Multiplier for swaying

    private float swayTimer = 0f;         // Timer for natural swaying

    private void Start()
    {
        animator = FindAnimatorInHierarchy(transform);

        if (animator == null)
        {
            Debug.LogError("Animator component not found in the hierarchy!");
            enabled = false;
            return;
        }

        currentTarget = defaultTarget; // Initialize the current target
        smoothLookAtPosition = currentTarget != null ? currentTarget.position : transform.position;

        if (hipBone == null)
        {
            Debug.LogWarning("HipBone is not assigned. Natural motion won't be applied.");
        }
    }

    private void LateUpdate()
    {
        // If not locked, use the default target
        if (!isLocked && defaultTarget != null)
        {
            currentTarget = defaultTarget;
        }

        // Smoothly interpolate between the current LookAt position and the target position
        if (currentTarget != null)
        {
            smoothLookAtPosition = Vector3.Lerp(smoothLookAtPosition, currentTarget.position, lookAtLerpSpeed * Time.deltaTime);
        }

        // Update the sway timer for natural motion
        swayTimer += Time.deltaTime * swaySpeed;

        // Apply Natural Motion to the Hip Bone
        if (hipBone != null && naturalMotionWeight > 0f)
        {
            float leanAngle = Mathf.Lerp(-30f, 30f, (leanForwardBackward + 1f) / 2f) * leanMultiplier;
            float swayAngle = Mathf.Sin(swayTimer) * 10f * swayLeftRight * swayMultiplier;

            Quaternion naturalRotation = Quaternion.Euler(leanAngle, 0f, swayAngle);
            hipBone.localRotation = Quaternion.Lerp(hipBone.localRotation, naturalRotation, naturalMotionWeight);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator != null)
        {
            if (currentTarget != null)
            {
                animator.SetLookAtWeight(lookAtWeight);
                animator.SetLookAtPosition(smoothLookAtPosition); // Use the interpolated position
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }
    }

    public void SetTarget(Transform customTarget)
    {
        currentTarget = customTarget;
        isLocked = true;
    }

    public void ClearTarget()
    {
        currentTarget = defaultTarget;
        isLocked = false;
    }

    private Animator FindAnimatorInHierarchy(Transform currentTransform)
    {
        while (currentTransform != null)
        {
            Animator foundAnimator = currentTransform.GetComponent<Animator>();
            if (foundAnimator != null)
            {
                return foundAnimator;
            }
            currentTransform = currentTransform.parent;
        }
        return null;
    }

    public void TriggerSpeakingLean(float leanAmount, float duration)
    {
        StartCoroutine(ApplySpeakingLean(leanAmount, duration));
    }

    private System.Collections.IEnumerator ApplySpeakingLean(float leanAmount, float duration)
    {
        float timer = 0f;
        float initialLean = leanForwardBackward;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            leanForwardBackward = Mathf.Lerp(initialLean, leanAmount, timer / duration);
            yield return null;
        }

        // Gradually return to the original state
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            leanForwardBackward = Mathf.Lerp(leanAmount, initialLean, timer / duration);
            yield return null;
        }
    }

}
