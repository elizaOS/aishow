using UnityEngine;
using System.Collections;

public class GlitchSoundEffect : MonoBehaviour
{
    [Header("Glitch Sound Settings")]
    public bool useGenerativeSounds = true;  // Toggle between generative sounds or predefined sounds
    public float minFrequency = 300f;        // Minimum frequency for the sound
    public float maxFrequency = 1500f;       // Maximum frequency for the sound
    public float minDuration = 0.1f;         // Minimum duration of a sound
    public float maxDuration = 0.3f;         // Maximum duration of a sound
    public float minVolume = 0.1f;           // Minimum volume for the sound
    public float maxVolume = 0.5f;           // Maximum volume for the sound
    public float glitchIntervalMin = 0.2f;   // Minimum time between sounds
    public float glitchIntervalMax = 0.5f;   // Maximum time between sounds
    public float fadeDuration = 0.01f;       // Duration of the fade-in and fade-out
    public AudioClip[] predefinedSounds;     // Array to store predefined glitch sounds

    // Pitch fluctuation range
    public float pitchMin = 0.75f;   // Minimum pitch (25% down)
    public float pitchMax = 1.25f;   // Maximum pitch (25% up)

    private AudioSource audioSource;
    private bool isGlitching = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource found. Please attach an AudioSource component.");
            enabled = false;
            return;
        }

        // If no predefined sounds are set, we default to using generative sounds
        if (predefinedSounds.Length == 0)
        {
            useGenerativeSounds = true;
            Debug.LogWarning("No predefined glitch sounds found. Switching to generative sounds.");
        }
    }

    public void StartGlitchSounds()
    {
        if (!isGlitching)
        {
            StartCoroutine(GenerateGlitchSounds());
        }
    }

    private IEnumerator GenerateGlitchSounds()
    {
        isGlitching = true;

        while (true) // Keep generating sounds during glitching
        {
            if (useGenerativeSounds)
            {
                // Generative sounds: Use random frequencies and durations
                float frequency = UnityEngine.Random.Range(minFrequency, maxFrequency);
                float duration = UnityEngine.Random.Range(minDuration, maxDuration);
                float volume = UnityEngine.Random.Range(minVolume, maxVolume);
                float pitch = UnityEngine.Random.Range(pitchMin, pitchMax);  // Random pitch fluctuation

                // Generate and play the sound with a fade effect and pitch variation
                StartCoroutine(PlaySoundWithFade(frequency, duration, volume, pitch));
            }
            else
            {
                // Predefined sounds: Pick a random sound from the array
                if (predefinedSounds.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, predefinedSounds.Length);
                    AudioClip randomClip = predefinedSounds[randomIndex];
                    float volume = UnityEngine.Random.Range(minVolume, maxVolume);
                    float pitch = UnityEngine.Random.Range(pitchMin, pitchMax);  // Random pitch fluctuation

                    // Play the predefined sound with a fade effect and pitch variation
                    StartCoroutine(PlayPredefinedSoundWithFade(randomClip, volume, pitch));
                }
            }

            // Wait for a random time before generating the next sound
            float interval = UnityEngine.Random.Range(glitchIntervalMin, glitchIntervalMax);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator PlaySoundWithFade(float frequency, float duration, float volume, float pitch)
    {
        int sampleRate = 44100; // Standard sample rate
        int sampleCount = Mathf.FloorToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        // Generate a sine wave for the sound
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * time); // Sine wave formula
        }

        // Create an AudioClip from the samples
        AudioClip clip = AudioClip.Create("GlitchSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        // Play the sound using AudioSource with fade-in and fade-out
        audioSource.clip = clip; // Assign the generated clip
        audioSource.pitch = pitch;  // Set initial pitch
        audioSource.Play();  // Start playing the sound at full volume

        // Fade in over the specified fade duration
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, volume, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = volume;  // Ensure full volume is reached after fade-in

        // Hold the sound for the remaining duration
        yield return new WaitForSeconds(duration - fadeDuration * 2); // Account for fade-out duration

        // Fade out over the specified fade duration
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(volume, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 0f;  // Ensure volume is 0 after fade-out
        audioSource.Stop();  // Stop the sound after fading out

        // Reset pitch after sound stops
        audioSource.pitch = 1.0f;  // Reset pitch to default (no glitch effect)
    }

    private IEnumerator PlayPredefinedSoundWithFade(AudioClip clip, float volume, float pitch)
    {
        // Play the predefined sound using AudioSource with fade-in and fade-out
        audioSource.clip = clip;
        audioSource.pitch = pitch;  // Set initial pitch
        audioSource.Play();  // Start playing the sound at full volume

        // Fade in over the specified fade duration
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, volume, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = volume;  // Ensure full volume is reached after fade-in

        // Hold the sound for the remaining duration
        yield return new WaitForSeconds(clip.length - fadeDuration * 2); // Account for fade-out duration

        // Fade out over the specified fade duration
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            audioSource.volume = Mathf.Lerp(volume, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 0f;  // Ensure volume is 0 after fade-out
        audioSource.Stop();  // Stop the sound after fading out

        // Reset pitch after sound stops
        audioSource.pitch = 1.0f;  // Reset pitch to default (no glitch effect)
    }

    public void StopGlitchSounds()
    {
        StopAllCoroutines();
        isGlitching = false;
        audioSource.volume = 0f;  // Ensure volume is reset after stopping sounds
        audioSource.Stop();  // Stop any ongoing sound

        // Reset pitch when stopping the glitch sounds
        audioSource.pitch = 1.0f;  // Ensure pitch is reset when glitching stops
    }
}
