using UnityEngine;
using System.Collections;

namespace ShowRunner
{
    /// <summary>
    /// Manages a simple laser effect triggered by the 'amused' action.
    /// Activates a GameObject, plays a sound, waits, then deactivates.
    /// </summary>
    public class AmusedLazerEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("The GameObject representing the laser visual effect.")]
        public GameObject laserObject;

        [Tooltip("The AudioSource to play the laser sound.")]
        public AudioSource laserAudioSource;

        [Tooltip("The sound effect for the laser activation.")]
        public AudioClip laserSound;

        [Tooltip("How long the laser stays active in seconds.")]
        public float onDuration = 1.0f;

        private Coroutine effectCoroutine = null; // To ensure only one instance runs

        private void Start()
        {
            // Initial validation
            if (laserObject == null)
            {
                Debug.LogError($"'{gameObject.name}': Laser Object is not assigned in AmusedLazerEffect.", this);
                enabled = false; // Disable script if not configured
                return;
            }
            if (laserAudioSource == null)
            {
                Debug.LogWarning($"'{gameObject.name}': Laser Audio Source is not assigned in AmusedLazerEffect. Sound will not play.", this);
                // Don't disable, maybe visual only is intended
            }
            if (laserSound == null && laserAudioSource != null)
            {
                Debug.LogWarning($"'{gameObject.name}': Laser Sound is not assigned in AmusedLazerEffect. Audio source assigned but no clip to play.", this);
            }

            // Ensure the laser object is initially inactive
            laserObject.SetActive(false);
        }

        /// <summary>
        /// Triggers the laser effect sequence.
        /// </summary>
        public void TriggerEffect()
        {
            // Prevent overlapping effects if already running
            if (effectCoroutine == null)
            {
                effectCoroutine = StartCoroutine(AmusedLazerRoutine());
            }
            else
            {
                Debug.LogWarning($"'{gameObject.name}': AmusedLazerEffect already running. Ignoring new trigger.", this);
            }
        }

        private IEnumerator AmusedLazerRoutine()
        {
            Debug.Log($"'{gameObject.name}': Starting Amused Lazer Effect.", this);

            // Activate visual
            if (laserObject != null)
            {
                laserObject.SetActive(true);
            }

            // Play sound
            if (laserAudioSource != null && laserSound != null)
            {
                laserAudioSource.Stop(); // Stop previous sound just in case
                laserAudioSource.clip = laserSound;
                laserAudioSource.Play();
                Debug.Log($"'{gameObject.name}': Playing laser sound.", this);
            }

            // Wait for the specified duration
            yield return new WaitForSeconds(onDuration);

            Debug.Log($"'{gameObject.name}': Ending Amused Lazer Effect.", this);

            // Stop sound
            if (laserAudioSource != null && laserAudioSource.isPlaying)
            {
                 // Optional: Add a small fade out here if desired
                laserAudioSource.Stop();
                 Debug.Log($"'{gameObject.name}': Stopping laser sound.", this);
            }

            // Deactivate visual
            if (laserObject != null)
            {
                laserObject.SetActive(false);
            }

            // Allow the effect to be triggered again
            effectCoroutine = null;
        }

        // Optional: Stop the effect prematurely if needed
        public void StopEffect()
        {
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);

                 // Ensure cleanup happens
                 if (laserAudioSource != null && laserAudioSource.isPlaying)
                 {
                     laserAudioSource.Stop();
                 }
                 if (laserObject != null)
                {
                    laserObject.SetActive(false);
                }

                effectCoroutine = null;
                Debug.Log($"'{gameObject.name}': Amused Lazer Effect stopped prematurely.", this);
            }
        }

         private void OnDisable()
         {
             // Stop the effect if the component or GameObject is disabled
             StopEffect();
         }
    }
} 