using UnityEngine;
using System.Collections;

public class LaughEffect : MonoBehaviour
{
    [Header("Laugh Settings")]
    public SkinnedMeshRenderer mouthMeshRenderer;   // Reference to the SkinnedMeshRenderer for blendshapes (mouth)
    public int smileBlendShapeIndex = 0;            // Index for the smile blendshape
    public int mouthOpenBlendShapeIndex = 1;        // Index for the mouth open blendshape
    public int browUpBlendShapeIndex = 2;           // Index for the eyebrow up blendshape
    public int browDownBlendShapeIndex = 3;         // Index for the eyebrow down blendshape
    public float laughBlendShapeIntensity = 1.0f;   // Maximum intensity of the blendshapes for laughter
    public float laughSpeed = 0.5f;                 // Speed at which the facial blendshapes change
    public float torsoLeanMultiplier = 2.0f;        // How exaggerated the torso lean becomes
    public float torsoLeanSpeed = 1.0f;             // Speed of leaning back and forth during the laugh
    public float laughDuration = 3.0f;              // Duration for how long the laugh lasts

    [Header("Torso Settings")]
    public Transform torsoBone;                     // Reference to the torso bone for rotation (assign in Inspector)
    public Vector3 initialTorsoRotation;            // Initial torso rotation for smooth return

    [Header("Animator Settings")]
    public Animator animator;                       // Reference to the Animator for blending the laugh layer weight
    public int laughAnimationLayerIndex = 1;        // Index of the animation layer for the laugh animation
    public float maxLaughLayerWeight = 1.0f;        // Maximum weight to blend the laugh animation layer
    public float blendSpeed = 0.2f;                 // Speed at which the blend layer weight adjusts (during the laugh)

    private bool isLaughing = false;                // Whether the laugh effect is active
    private Vector3 initialTorsoPosition;           // Initial torso position

    private float initialSmileWeight;               // Initial smile blendshape weight
    private float initialMouthOpenWeight;           // Initial mouth open blendshape weight
    private float initialBrowUpWeight;              // Initial eyebrow up blendshape weight
    private float initialBrowDownWeight;            // Initial eyebrow down blendshape weight

    private void Start()
    {
        // Validate if the required components are assigned
        if (mouthMeshRenderer == null)
        {
            Debug.LogError("Mouth Mesh Renderer is not assigned! Please assign a SkinnedMeshRenderer.");
            enabled = false;
            return;
        }

        if (torsoBone == null)
        {
            Debug.LogError("Torso bone is not assigned! Please assign a torso bone for rotation.");
            enabled = false;
            return;
        }

        // Store the initial torso position and rotation
        initialTorsoPosition = torsoBone.localPosition;
        initialTorsoRotation = torsoBone.localRotation.eulerAngles;

        // Store the initial blendshape values
        initialSmileWeight = mouthMeshRenderer.GetBlendShapeWeight(smileBlendShapeIndex);
        initialMouthOpenWeight = mouthMeshRenderer.GetBlendShapeWeight(mouthOpenBlendShapeIndex);
        initialBrowUpWeight = mouthMeshRenderer.GetBlendShapeWeight(browUpBlendShapeIndex);
        initialBrowDownWeight = mouthMeshRenderer.GetBlendShapeWeight(browDownBlendShapeIndex);
    }

    public void TriggerLaughEffect()
    {
        if (!isLaughing) // Prevent overlapping laugh animations
        {
            StartCoroutine(LaughRoutine());
        }
    }

    private IEnumerator LaughRoutine()
    {
        isLaughing = true;

        // Gradually increase the laugh animation layer weight from 0 to max during the first half of the laugh duration
        float layerWeightTime = 0f;
        float startTime = Time.time;
        while (layerWeightTime < (laughDuration / 2f))
        {
            layerWeightTime = Time.time - startTime;
            float layerWeight = Mathf.Lerp(0f, maxLaughLayerWeight, layerWeightTime / (laughDuration / 2f)); // Lerp for smooth weight increase
            animator.SetLayerWeight(laughAnimationLayerIndex, layerWeight);
            yield return null;
        }

        // Animate the blendshapes and torso lean over the laughDuration (first half)
        float timer = 0f;
        while (timer < (laughDuration / 2f))
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Briefly hold the laugh state (random variation)
        yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));

        // Gradually decrease the laugh animation layer weight back to 0 during the second half of the laugh duration
        float endTime = Time.time;
        while (Time.time - endTime < (laughDuration / 2f))
        {
            float layerWeight = Mathf.Lerp(maxLaughLayerWeight, 0f, (Time.time - endTime) / (laughDuration / 2f)); // Lerp for smooth weight decrease
            animator.SetLayerWeight(laughAnimationLayerIndex, layerWeight);
            yield return null;
        }

        // Reset everything back to the initial state
        mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, initialSmileWeight);
        mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, initialMouthOpenWeight);
        mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, initialBrowUpWeight);
        mouthMeshRenderer.SetBlendShapeWeight(browDownBlendShapeIndex, initialBrowDownWeight);
        torsoBone.localPosition = initialTorsoPosition;
        torsoBone.localRotation = Quaternion.Euler(initialTorsoRotation);

        isLaughing = false;
    }

    private void LateUpdate()
    {
        if (isLaughing)
        {
            // Apply torso lean after IK calculations but respect existing IK solutions
            ApplyTorsoLeanPostIK();
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (isLaughing)
        {
            // Smoothly update blendshapes during laughing
            UpdateLaughBlendShapes();
        }
    }

    private void ApplyTorsoLeanPostIK()
    {
        // Perform torso movements respecting IK adjustments (additive changes)
        float torsoLean = Mathf.Sin(Time.time * torsoLeanSpeed) * torsoLeanMultiplier;
        torsoBone.localPosition += new Vector3(0, 0, torsoLean); // Add lean without overriding IK

        float torsoRotation = Mathf.Sin(Time.time * torsoLeanSpeed) * torsoLeanMultiplier * 15f;
        torsoBone.localRotation *= Quaternion.Euler(torsoRotation, 0, 0); // Apply rotation additively
    }

    private void UpdateLaughBlendShapes()
    {
        float smileWeight = Mathf.Lerp(initialSmileWeight, laughBlendShapeIntensity, Mathf.Sin(Time.time * laughSpeed) * 0.5f + 0.5f);
        mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, smileWeight);

        float mouthOpenWeight = Mathf.Lerp(initialMouthOpenWeight, laughBlendShapeIntensity, Mathf.Sin(Time.time * laughSpeed) * 0.5f + 0.5f);
        mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, mouthOpenWeight);

        // Update eyebrow blendshapes
        float browUpWeight = Mathf.Lerp(initialBrowUpWeight, laughBlendShapeIntensity, Mathf.Sin(Time.time * laughSpeed) * 0.5f + 0.5f);
        mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, browUpWeight);

        float browDownWeight = Mathf.Lerp(initialBrowDownWeight, laughBlendShapeIntensity, Mathf.Sin(Time.time * laughSpeed) * 0.5f + 0.5f);
        mouthMeshRenderer.SetBlendShapeWeight(browDownBlendShapeIndex, browDownWeight);
    }
}
