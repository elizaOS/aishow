using UnityEngine;
using System.Collections;

// Add this component to your avatar
public class AvatarImpactReaction : MonoBehaviour
{
    [Header("IK Settings")]
    public Animator animator;
    [Range(0, 1)]
    public float ikWeight = 1f;
    public Transform headBone;
    public Transform spineBone;
    public Transform lookAtTarget; // Reference point for look at IK
    public float recoveryTime = 1f;
    public float maxBendAngle = 45f;
    
    [Header("Physics Response")]
    public float impactMultiplier = 1f;
    public float minimumForceThreshold = 5f;
    public AnimationCurve recoveryCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private bool isReacting = false;
    private Vector3 currentImpactDirection;  
    private float currentImpactForce;
    private Quaternion originalHeadRotation;
    private Quaternion originalSpineRotation;
    private Vector3 lookAtPosition;
    private GameObject tempLookAtTarget;
    
    private void Start()
    {
        if (headBone) originalHeadRotation = headBone.localRotation;
        if (spineBone) originalSpineRotation = spineBone.localRotation;
        
        // Create a temporary object for look at target if none is assigned
        if (!lookAtTarget)
        {
            tempLookAtTarget = new GameObject("TempLookAtTarget");
            lookAtTarget = tempLookAtTarget.transform;
        }
        
        // Ensure animator has IK pass enabled
        if (animator)
        {
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        }
    }

    private void OnDestroy()
    {
        if (tempLookAtTarget)
        {
            Destroy(tempLookAtTarget);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!animator || !isReacting) return;

        // Set look at target
        animator.SetLookAtWeight(ikWeight);
        animator.SetLookAtPosition(lookAtTarget.position);

        // Set body position weight
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, ikWeight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, ikWeight);
    }

    public void ReactToImpact(Vector3 impactPoint, Vector3 impactVelocity)
    {
        if (!isReacting && impactVelocity.magnitude > minimumForceThreshold)
        {
            currentImpactDirection = impactVelocity.normalized;
            currentImpactForce = impactVelocity.magnitude;
            
            // Set look at target to opposite of impact direction
            Vector3 lookDirection = -currentImpactDirection * 2f;
            lookAtTarget.position = transform.position + lookDirection;
            
            StartCoroutine(ImpactReactionSequence());
        }
    }
    
    private IEnumerator ImpactReactionSequence()
    {
        isReacting = true;
        float elapsed = 0f;
        
        // Calculate impact angles
        float verticalAngle = Vector3.Angle(currentImpactDirection, Vector3.up);
        float horizontalAngle = Mathf.Atan2(currentImpactDirection.x, currentImpactDirection.z) * Mathf.Rad2Deg;
        
        // Clamp the bend angle based on force
        float bendAngle = Mathf.Min(maxBendAngle * (currentImpactForce / minimumForceThreshold), maxBendAngle);
        
        while (elapsed < recoveryTime)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / recoveryTime;
            float currentBend = bendAngle * (1 - recoveryCurve.Evaluate(normalizedTime));
            
            // Apply rotation to spine and head
            if (spineBone)
            {
                Quaternion spineRotation = Quaternion.Euler(
                    currentImpactDirection.y * currentBend,
                    horizontalAngle * 0.5f * currentBend / maxBendAngle,
                    -currentImpactDirection.x * currentBend
                );
                spineBone.localRotation = originalSpineRotation * spineRotation;
            }
            
            if (headBone)
            {
                Quaternion headRotation = Quaternion.Euler(
                    currentImpactDirection.y * currentBend * 0.5f,
                    horizontalAngle * 0.25f * currentBend / maxBendAngle,
                    -currentImpactDirection.x * currentBend * 0.5f
                );
                headBone.localRotation = originalHeadRotation * headRotation;
            }
            
            yield return null;
        }
        
        // Reset to original pose
        if (spineBone) spineBone.localRotation = originalSpineRotation;
        if (headBone) headBone.localRotation = originalHeadRotation;
        
        isReacting = false;
    }
}