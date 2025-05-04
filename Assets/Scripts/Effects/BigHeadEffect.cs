using UnityEngine;
using System.Collections;

namespace ShowRunner
{
    /// <summary>
    /// Manages timed effects for scaling the character's head using BigHeadMode.
    /// Supports Grow, Shrink, and Random scaling modes over a defined duration.
    /// </summary>
    [RequireComponent(typeof(BigHeadMode))]
    public class BigHeadEffect : MonoBehaviour
    {
        /// <summary>
        /// Defines the type of scaling effect to apply.
        /// </summary>
        public enum EffectMode
        {
            Grow,
            Shrink,
            Random
        }

        [Header("Effect Settings")]
        [SerializeField] private float effectDuration = 2.0f; // Total duration of the effect
        [SerializeField] [Range(1f, 10f)] private float growScale = 3.0f; // Target scale multiplier for Grow mode
        [SerializeField] [Range(0.1f, 1f)] private float shrinkScale = 0.3f; // Target scale multiplier for Shrink mode
        [SerializeField] [Range(0.1f, 10f)] private float randomMinScale = 0.5f; // Minimum scale for Random mode
        [SerializeField] [Range(1f, 10f)] private float randomMaxScale = 4.0f; // Maximum scale for Random mode
        [SerializeField] private float randomChangeInterval = 0.1f; // How often the scale changes in Random mode

        private BigHeadMode bigHeadMode; // Reference to the core BigHeadMode script
        private bool isAnimating = false; // Prevents overlapping effects

        private void Awake()
        {
            // Get the required BigHeadMode component attached to the same GameObject
            bigHeadMode = GetComponent<BigHeadMode>();
            if (bigHeadMode == null)
            {
                Debug.LogError("BigHeadMode component not found! BigHeadEffect requires it.", this);
                enabled = false; // Disable this script if dependency is missing
            }
        }

        /// <summary>
        /// Triggers the head scaling effect with the specified mode.
        /// </summary>
        /// <param name="mode">The scaling mode to use (Grow, Shrink, or Random).</param>
        public void TriggerEffect(EffectMode mode)
        {
            // Prevent starting a new effect if one is already running
            if (isAnimating)
            {
                Debug.LogWarning("BigHeadEffect is already running.", this);
                return;
            }

            // Start the appropriate coroutine based on the selected mode
            switch (mode)
            {
                case EffectMode.Grow:
                    StartCoroutine(GrowRoutine());
                    break;
                case EffectMode.Shrink:
                    StartCoroutine(ShrinkRoutine());
                    break;
                case EffectMode.Random:
                    StartCoroutine(RandomRoutine());
                    break;
                default:
                    Debug.LogError($"Unsupported EffectMode: {mode}", this);
                    break;
            }
        }

        /// <summary>
        /// Coroutine for the Grow effect: Scales head up and then back to normal.
        /// </summary>
        private IEnumerator GrowRoutine()
        {
            yield return AnimateScaleRoutine(1f, growScale, effectDuration / 2f); // Scale up
            yield return AnimateScaleRoutine(growScale, 1f, effectDuration / 2f); // Scale down
        }

        /// <summary>
        /// Coroutine for the Shrink effect: Scales head down and then back to normal.
        /// </summary>
        private IEnumerator ShrinkRoutine()
        {
            yield return AnimateScaleRoutine(1f, shrinkScale, effectDuration / 2f); // Scale down
            yield return AnimateScaleRoutine(shrinkScale, 1f, effectDuration / 2f); // Scale up
        }

        /// <summary>
        /// Coroutine for the Random effect: Scales head randomly within limits before returning to normal.
        /// </summary>
        private IEnumerator RandomRoutine()
        {
            isAnimating = true;
            bigHeadMode.SetBigHead(true); // Enable BigHeadMode for the duration

            float startTime = Time.time;
            float timer = 0f;

            // Randomly change scale at intervals
            while (timer < effectDuration)
            {
                float randomScale = Random.Range(randomMinScale, randomMaxScale);
                bigHeadMode.SetHeadScale(randomScale); // Apply random scale

                yield return new WaitForSeconds(randomChangeInterval);
                timer += randomChangeInterval;
            }

            // Ensure scale returns to normal and BigHeadMode is disabled
            bigHeadMode.SetHeadScale(1f);
            bigHeadMode.SetBigHead(false);
            isAnimating = false;

            Debug.Log("BigHeadEffect (Random) finished.", this);
        }

        /// <summary>
        /// Helper coroutine to smoothly animate the head scale between two values over a duration.
        /// Handles enabling/disabling BigHeadMode and animation state.
        /// </summary>
        /// <param name="startScale">The starting scale multiplier.</param>
        /// <param name="endScale">The target scale multiplier.</param>
        /// <param name="duration">How long the animation should take.</param>
        private IEnumerator AnimateScaleRoutine(float startScale, float endScale, float duration)
        {
            isAnimating = true;
            bigHeadMode.SetBigHead(true); // Ensure BigHeadMode is active

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                // Consider adding easing here (e.g., Mathf.SmoothStep) for smoother animation
                float currentScale = Mathf.Lerp(startScale, endScale, progress);
                bigHeadMode.SetHeadScale(currentScale); // Apply the interpolated scale
                yield return null; // Wait for the next frame
            }

            bigHeadMode.SetHeadScale(endScale); // Ensure final scale is set accurately

            // Only disable BigHeadMode and reset animation state when returning to scale 1
            if (Mathf.Approximately(endScale, 1f))
            {
                bigHeadMode.SetBigHead(false); // Disable BigHeadMode to return to original scale
                isAnimating = false;
                Debug.Log($"BigHeadEffect ({startScale} -> {endScale}) finished.", this);
            }
        }

        // Optional: Add methods for editor testing if needed
        // [ContextMenu("Test Grow Effect")] // Removed in favor of custom editor button
        // private void TestGrow()
        // {
        //     if (Application.isPlaying) TriggerEffect(EffectMode.Grow);
        // }

        // [ContextMenu("Test Shrink Effect")] // Removed in favor of custom editor button
        // private void TestShrink()
        // {
        //     if (Application.isPlaying) TriggerEffect(EffectMode.Shrink);
        // }

        // [ContextMenu("Test Random Effect")] // Removed in favor of custom editor button
        // private void TestRandom()
        // {
        //     if (Application.isPlaying) TriggerEffect(EffectMode.Random);
        // }
    }
} 