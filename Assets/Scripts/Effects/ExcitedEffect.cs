using UnityEngine;
using System.Collections;

public class ExcitedEffect : MonoBehaviour
{
    [Header("Blend Shape Settings")]
    public SkinnedMeshRenderer mouthMeshRenderer;   // Reference to the SkinnedMeshRenderer for blendshapes
    public int smileBlendShapeIndex = 0;            // Index for the smile blendshape
    public int mouthOpenBlendShapeIndex = 1;        // Index for the mouth open blendshape
    public int browUpBlendShapeIndex = 2;           // Index for the eyebrow up blendshape
    public float blendShapeIntensity = 1.0f;        // Maximum intensity of the blendshapes
    public float animationSpeed = 0.5f;             // Speed at which blendshapes animate
    public float excitedDuration = 3.0f;            // Duration of the excited effect

    [Header("Animator Settings")]
    public Animator animator;                       // Reference to the Animator
    public int excitedAnimationLayerIndex = 1;      // Index of the animation layer for the excited animation
    public float maxAnimationLayerWeight = 1.0f;    // Maximum layer weight for the animation
    public float blendSpeed = 0.2f;                 // Speed of animation layer weight blending

    private bool isAnimating = false;               // Tracks whether the animation is running
    private float initialSmileWeight;               // Initial smile blend shape weight
    private float initialMouthOpenWeight;           // Initial mouth open blend shape weight
    private float initialBrowUpWeight;              // Initial brow up blend shape weight

    private void Start()
    {
        // Validate if the required components are assigned
        if (mouthMeshRenderer == null)
        {
            Debug.LogError("Mouth Mesh Renderer is not assigned! Please assign a SkinnedMeshRenderer.");
            enabled = false;
            return;
        }

        // Store initial blend shape values
        initialSmileWeight = mouthMeshRenderer.GetBlendShapeWeight(smileBlendShapeIndex);
        initialMouthOpenWeight = mouthMeshRenderer.GetBlendShapeWeight(mouthOpenBlendShapeIndex);
        initialBrowUpWeight = mouthMeshRenderer.GetBlendShapeWeight(browUpBlendShapeIndex);
    }

    public void TriggerExcitedEffect()
    {
        if (!isAnimating) // Prevent overlapping animations
        {
            StartCoroutine(ExcitedRoutine());
        }
    }

    private IEnumerator ExcitedRoutine()
    {
        isAnimating = true;

        // Gradually increase the animation layer weight
        float layerWeightTime = 0f;
        float startTime = Time.time;
        while (layerWeightTime < excitedDuration / 2f)
        {
            layerWeightTime = Time.time - startTime;
            float layerWeight = Mathf.Lerp(0f, maxAnimationLayerWeight, layerWeightTime / (excitedDuration / 2f));
            animator.SetLayerWeight(excitedAnimationLayerIndex, layerWeight);
            UpdateBlendShapes(layerWeight);
            yield return null;
        }

        // Hold the excited state briefly
        yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));

        // Gradually decrease the animation layer weight
        float endTime = Time.time;
        while (Time.time - endTime < excitedDuration / 2f)
        {
            float layerWeight = Mathf.Lerp(maxAnimationLayerWeight, 0f, (Time.time - endTime) / (excitedDuration / 2f));
            animator.SetLayerWeight(excitedAnimationLayerIndex, layerWeight);
            UpdateBlendShapes(layerWeight);
            yield return null;
        }

        // Reset blend shapes to their initial values
        ResetBlendShapes();
        isAnimating = false;
    }

    private void UpdateBlendShapes(float intensity)
    {
        float weight = Mathf.Lerp(0f, blendShapeIntensity, Mathf.Sin(Time.time * animationSpeed) * 0.5f + 0.5f);
        mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, Mathf.Lerp(initialSmileWeight, weight, intensity));
        mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, Mathf.Lerp(initialMouthOpenWeight, weight, intensity));
        mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, Mathf.Lerp(initialBrowUpWeight, weight, intensity));
    }

    private void ResetBlendShapes()
    {
        mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, initialSmileWeight);
        mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, initialMouthOpenWeight);
        mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, initialBrowUpWeight);
    }
}
