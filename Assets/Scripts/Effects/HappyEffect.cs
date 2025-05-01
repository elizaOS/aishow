using UnityEngine;
using System.Collections;

namespace ShowRunner // Assuming HappyEffect should also be in this namespace
{
    public class HappyEffect : MonoBehaviour
    {
        [Header("Blend Shape Settings")]
        public SkinnedMeshRenderer mouthMeshRenderer;   // Reference to the SkinnedMeshRenderer for blendshapes
        public int smileBlendShapeIndex = 0;            // Index for the smile blendshape
        public int mouthOpenBlendShapeIndex = 1;        // Index for the mouth open blendshape
        public int browUpBlendShapeIndex = 2;           // Index for the eyebrow up blendshape
        public float blendShapeIntensity = 1.0f;        // Maximum intensity of the blendshapes
        public float animationSpeed = 0.5f;             // Speed at which blendshapes animate
        public float happyDuration = 3.0f;            // Renamed duration variable for clarity

        [Header("Animator Settings")]
        public Animator animator;                       // Reference to the Animator
        public int happyAnimationLayerIndex = 1;      // Renamed layer index variable
        public float maxAnimationLayerWeight = 1.0f;    // Maximum layer weight for the animation
        public float blendSpeed = 0.2f;                 // Speed of animation layer weight blending

        [Header("Particle Effect Settings")] // New section for particles
        [Tooltip("Optional particle system to play during the effect.")]
        public ParticleSystem happyParticles;           // Reference to the particle system

        private bool isAnimating = false;               // Tracks whether the animation is running
        private float initialSmileWeight;
        private float initialMouthOpenWeight;
        private float initialBrowUpWeight;

        private void Start()
        {
            // Validate if the required components are assigned
            if (mouthMeshRenderer == null)
            {
                Debug.LogError("Mouth Mesh Renderer is not assigned! Please assign a SkinnedMeshRenderer.", this);
                enabled = false;
                return;
            }
            if (animator == null)
            {
                Debug.LogError("Animator is not assigned!", this);
                enabled = false;
                return;
            }
            if (happyParticles == null)
            {
                 Debug.LogWarning("Happy Particles system is not assigned. No particles will play.", this);
            }
            else
            {
                // Ensure particles don't play automatically
                happyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
            }

            // Store initial blend shape values
            initialSmileWeight = mouthMeshRenderer.GetBlendShapeWeight(smileBlendShapeIndex);
            initialMouthOpenWeight = mouthMeshRenderer.GetBlendShapeWeight(mouthOpenBlendShapeIndex);
            initialBrowUpWeight = mouthMeshRenderer.GetBlendShapeWeight(browUpBlendShapeIndex);
        }

        public void TriggerHappyEffect()
        {
            if (!isAnimating) // Prevent overlapping animations
            {
                StartCoroutine(HappyRoutine());
            }
        }

        private IEnumerator HappyRoutine()
        {
            isAnimating = true;
            Debug.Log("Starting Happy Effect Routine", this);

            // Start Particles (if assigned)
            if (happyParticles != null)
            {
                happyParticles.Play();
                Debug.Log("Playing happy particles.", this);
            }

            // Gradually increase the animation layer weight
            float layerWeightTime = 0f;
            float startTime = Time.time;
            while (layerWeightTime < happyDuration / 2f)
            {
                layerWeightTime = Time.time - startTime;
                // Use smooth step for a nicer blend?
                float t = Mathf.Clamp01(layerWeightTime / (happyDuration / 2f));
                float layerWeight = Mathf.SmoothStep(0f, maxAnimationLayerWeight, t);
                // float layerWeight = Mathf.Lerp(0f, maxAnimationLayerWeight, t); // Original Lerp
                animator.SetLayerWeight(happyAnimationLayerIndex, layerWeight);
                UpdateBlendShapes(layerWeight); // Update blend shapes based on layer weight
                yield return null;
            }
            // Ensure weight is exactly max at the end of the ramp up
            animator.SetLayerWeight(happyAnimationLayerIndex, maxAnimationLayerWeight);
            UpdateBlendShapes(maxAnimationLayerWeight);

            // Hold the Happy state briefly
            float holdDuration = Mathf.Clamp(Random.Range(0.2f, 0.5f), 0, happyDuration); // Ensure hold isn't longer than total duration
             Debug.Log($"Holding happy state for {holdDuration}s", this);
            yield return new WaitForSeconds(holdDuration);

            // Gradually decrease the animation layer weight
            float rampDownDuration = happyDuration - (happyDuration / 2f) - holdDuration;
             if(rampDownDuration < 0) rampDownDuration = 0; // Prevent negative duration
            float endTime = Time.time;
             Debug.Log($"Ramping down happy state over {rampDownDuration}s", this);
            while (Time.time - endTime < rampDownDuration)
            {
                float t = Mathf.Clamp01((Time.time - endTime) / rampDownDuration);
                float layerWeight = Mathf.SmoothStep(maxAnimationLayerWeight, 0f, t);
                // float layerWeight = Mathf.Lerp(maxAnimationLayerWeight, 0f, t); // Original Lerp
                animator.SetLayerWeight(happyAnimationLayerIndex, layerWeight);
                UpdateBlendShapes(layerWeight);
                yield return null;
            }
             // Ensure weight is exactly zero at the end
            animator.SetLayerWeight(happyAnimationLayerIndex, 0f);

            // Stop Particles (if assigned) - allow existing particles to finish
            if (happyParticles != null)
            {
                happyParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                 Debug.Log("Stopping happy particles emission.", this);
            }

            // Reset blend shapes to their initial values explicitly
            ResetBlendShapes();
            Debug.Log("Finished Happy Effect Routine", this);
            isAnimating = false;
        }

        private void UpdateBlendShapes(float intensity) // Intensity linked to layer weight
        {
            // Optional: Could make blend shape intensity also ramp with the layer weight
            // float weight = Mathf.Lerp(0f, blendShapeIntensity, intensity); // Direct link

             // Use a sine wave modulation for a bit of life, scaled by intensity
            float dynamicIntensity = Mathf.Sin(Time.time * animationSpeed) * 0.5f + 0.5f; // Varies between 0 and 1
            float modulatedWeight = Mathf.Lerp(0f, blendShapeIntensity, dynamicIntensity); // Apply base intensity

            float finalSmileWeight = Mathf.Lerp(initialSmileWeight, modulatedWeight, intensity);
            float finalMouthOpenWeight = Mathf.Lerp(initialMouthOpenWeight, modulatedWeight, intensity);
            float finalBrowUpWeight = Mathf.Lerp(initialBrowUpWeight, modulatedWeight, intensity);

            // Ensure indices are valid before setting weights
            if (mouthMeshRenderer != null)
            {
                 if(smileBlendShapeIndex >= 0 && smileBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, finalSmileWeight);
                 if(mouthOpenBlendShapeIndex >= 0 && mouthOpenBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, finalMouthOpenWeight);
                 if(browUpBlendShapeIndex >= 0 && browUpBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, finalBrowUpWeight);
            }
        }

        private void ResetBlendShapes()
        {
             Debug.Log("Resetting blend shapes.", this);
            if (mouthMeshRenderer != null)
            {
                // Ensure indices are valid before setting weights
                if(smileBlendShapeIndex >= 0 && smileBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, initialSmileWeight);
                if(mouthOpenBlendShapeIndex >= 0 && mouthOpenBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(mouthOpenBlendShapeIndex, initialMouthOpenWeight);
                if(browUpBlendShapeIndex >= 0 && browUpBlendShapeIndex < mouthMeshRenderer.sharedMesh.blendShapeCount)
                    mouthMeshRenderer.SetBlendShapeWeight(browUpBlendShapeIndex, initialBrowUpWeight);
            }
        }

         private void OnDisable()
        {
            // Stop effects and coroutine if the component/GameObject gets disabled
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
                 if (happyParticles != null)
                 {
                    happyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                 }
                 if(animator != null)
                 {
                     animator.SetLayerWeight(happyAnimationLayerIndex, 0f);
                 }
                 ResetBlendShapes();
                 Debug.Log("HappyEffect disabled mid-routine, stopping effects.", this);
            }
        }

    } // End of class
} // End of namespace
