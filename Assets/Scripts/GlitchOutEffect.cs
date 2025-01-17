using UnityEngine;
using System.Collections;

public class GlitchOutEffect : MonoBehaviour
{
    [Header("Glitch Settings")]
    public float glitchSwayMultiplier = 5.0f;         // How exaggerated the sway becomes during glitching
    public float glitchSwayLeftMultiplier = 4.0f;     // How exaggerated the sway to the left becomes
    public float glitchSwayRightMultiplier = 6.0f;    // How exaggerated the sway to the right becomes
    public float glitchLeanMultiplier = 2.0f;         // How exaggerated the leaning becomes during glitching
    public float glitchSwaySpeed = 5.0f;              // How fast the swaying becomes during glitching
    public float glitchDuration = 2.0f;               // How long the glitch lasts
    public float burstInterval = 0.5f;                // Interval between each burst
    public int totalBursts = 5;                       // Total number of bursts to emit during glitch

    private LookAtLogic lookAtLogic;                  // Reference to the LookAtLogic script
    private bool isGlitching = false;                 // Whether the glitch effect is active

    private float originalSwayMultiplier;
    private float originalLeanMultiplier;
    private float originalSwaySpeed;
    private float originalSwayLeftRight;

    public GlitchSoundEffect glitchSoundEffect;       // Reference to the GlitchSoundEffect component
    public ParticleSystemController particleController; // Reference to the ParticleSystemController

    private int currentBurstCount = 0;  // Track the number of bursts emitted so far

    private void Start()
    {
        // Get the GlitchSoundEffect component (assuming it's on the same GameObject)
        glitchSoundEffect = GetComponent<GlitchSoundEffect>();

        // Get the LookAtLogic component on the same GameObject
        lookAtLogic = GetComponent<LookAtLogic>();

        if (lookAtLogic == null)
        {
            Debug.LogError("LookAtLogic component not found! GlitchOutEffect requires LookAtLogic.");
            enabled = false;
            return;
        }

        // Store the original values from LookAtLogic
        originalSwayMultiplier = lookAtLogic.swayMultiplier;
        originalLeanMultiplier = lookAtLogic.leanMultiplier;
        originalSwaySpeed = lookAtLogic.swaySpeed;
        originalSwayLeftRight = lookAtLogic.swayLeftRight; // Save the original swayLeftRight

        particleController = FindObjectOfType<ParticleSystemController>();
        
        // Ensure that the ParticleSystemController is set (can be attached via inspector)
        if (particleController == null)
        {
            Debug.LogWarning("ParticleSystemController is not assigned. Particle bursts will not trigger.");
        }
    }

    public void TriggerGlitchOut()
    {
        if (!isGlitching) // Prevent overlapping glitches
        {
            StartCoroutine(GlitchOutRoutine());
        }
    }

    private IEnumerator GlitchOutRoutine()
    {
        isGlitching = true;

        // Save the initial values for smooth transition back
        float initialSwayMultiplier = lookAtLogic.swayMultiplier;
        float initialLeanMultiplier = lookAtLogic.leanMultiplier;
        float initialSwaySpeed = lookAtLogic.swaySpeed;
        float initialSwayLeftRight = lookAtLogic.swayLeftRight;

        // Variables to apply random glitch effects
        float timer = 0f;
        float glitchTime = glitchDuration / 2f;

        // Start glitch sound effects
        glitchSoundEffect.StartGlitchSounds();

        // Trigger particle emission stream
        if (particleController != null)
        {
            particleController.ToggleParticles(true); // Start particle emission
        }

        // Apply random glitch effects on sway and lean
        while (timer < glitchTime) // First half of the glitch (glitch ramp-up)
        {
            timer += Time.deltaTime;
            float lerpFactor = timer / glitchTime;

            // Randomize sway direction and multiplier (left/right)
            float randomSwayMultiplier = Random.Range(glitchSwayLeftMultiplier, glitchSwayRightMultiplier);
            lookAtLogic.swayMultiplier = Mathf.Lerp(initialSwayMultiplier, randomSwayMultiplier, lerpFactor);

            // Randomize sway direction (left/right)
            float randomSwayLeftRight = Random.Range(-1f, 1f); // Randomize sway direction between -1 and 1
            lookAtLogic.swayLeftRight = Mathf.Lerp(initialSwayLeftRight, randomSwayLeftRight, lerpFactor);

            // Randomize lean multiplier
            lookAtLogic.leanMultiplier = Mathf.Lerp(initialLeanMultiplier, glitchLeanMultiplier, lerpFactor);

            // Apply random sway speed fluctuation
            lookAtLogic.swaySpeed = Mathf.Lerp(initialSwaySpeed, glitchSwaySpeed, lerpFactor);

            // Emit bursts of particles at intervals
            if (particleController != null && Mathf.Floor(timer / burstInterval) > currentBurstCount)
            {
                currentBurstCount++;
                particleController.TriggerBurst(10); // Emit burst of 50 particles at each interval
            }

            yield return null;
        }

        // Stay in the glitch state briefly
        yield return new WaitForSeconds(0.5f);

        // Lerp back to original values (glitch ramp-down)
        timer = 0f;
        while (timer < glitchTime) // Second half of the glitch (glitch ramp-down)
        {
            timer += Time.deltaTime;
            float lerpFactor = timer / glitchTime;

            // Randomize sway direction and multiplier again during ramp-down
            float randomSwayMultiplier = Random.Range(glitchSwayLeftMultiplier, glitchSwayRightMultiplier);
            lookAtLogic.swayMultiplier = Mathf.Lerp(lookAtLogic.swayMultiplier, originalSwayMultiplier, lerpFactor);

            // Randomize sway direction (left/right) again during ramp-down
            float randomSwayLeftRight = Random.Range(-1f, 1f); // Randomize sway direction back
            lookAtLogic.swayLeftRight = Mathf.Lerp(lookAtLogic.swayLeftRight, originalSwayLeftRight, lerpFactor);

            // Randomize lean multiplier back to original
            lookAtLogic.leanMultiplier = Mathf.Lerp(lookAtLogic.leanMultiplier, originalLeanMultiplier, lerpFactor);

            // Randomize sway speed back to original
            lookAtLogic.swaySpeed = Mathf.Lerp(lookAtLogic.swaySpeed, originalSwaySpeed, lerpFactor);

            // Emit bursts of particles during the ramp-down phase as well
            if (particleController != null && Mathf.Floor(timer / burstInterval) > currentBurstCount)
            {
                currentBurstCount++;
                particleController.TriggerBurst(50); // Emit particles at the end of the glitch (burst at ramp-down)
            }

            yield return null;
        }

        // Stop emitting particles after the glitch ends
        if (particleController != null)
        {
            particleController.ToggleParticles(false); // Stop particle emission
        }

        // Restore the original values to ensure consistency after glitch
        lookAtLogic.swayMultiplier = originalSwayMultiplier;
        lookAtLogic.leanMultiplier = originalLeanMultiplier;
        lookAtLogic.swaySpeed = originalSwaySpeed;
        lookAtLogic.swayLeftRight = originalSwayLeftRight;

        // Stop glitch sound effects
        glitchSoundEffect.StopGlitchSounds();

        isGlitching = false;
        currentBurstCount = 0;  // Reset the burst counter for next glitch
    }
}
