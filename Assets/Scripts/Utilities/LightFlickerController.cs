using UnityEngine;
using System.Collections.Generic;

public class LightFlickerController : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Range(0f, 1f)]
    public float flickerIntensity = 0.2f;  // Reduced for more subtle effect
    [Range(0f, 10f)]
    public float flickerSpeed = 4f;  // Increased for more rapid fluctuations
    [Range(0f, 1f)]
    public float randomness = 0.2f;  // Reduced for more consistent base effect
    
    [Header("Probability Settings")]
    [Range(0f, 1f)]
    public float flickerChance = 0.7f;  // Increased for more consistent flickering
    [Range(0f, 1f)]
    public float burnoutChance = 0.02f;  // Reduced for less frequent burnouts
    
    [Header("Timing Settings")]
    public float minFlickerDuration = 0.05f;  // Shorter minimum for more responsive flicker
    public float maxFlickerDuration = 0.2f;   // Shorter maximum for more natural effect
    public float burnoutDuration = 1.5f;      // Slightly reduced burnout time
    
    [Header("Synchronization")]
    public bool synchronizedFlicker = true;
    [Range(0f, 1f)]
    public float synchronizationStrength = 0.7f;  // How closely lights follow the main flicker pattern
    
    private Dictionary<Light, float> lightDefaults = new Dictionary<Light, float>();
    private Dictionary<Light, bool> isBurnedOut = new Dictionary<Light, bool>();
    private Dictionary<Light, float> nextFlickerTime = new Dictionary<Light, float>();
    private Dictionary<Light, float> flickerEndTime = new Dictionary<Light, float>();
    private float globalFlickerSeed;

    void Start()
    {
        InitializeLights();
        globalFlickerSeed = Random.Range(0f, 1000f);  // Random starting point for global flicker
    }

    void InitializeLights()
    {
        lightDefaults.Clear();
        isBurnedOut.Clear();
        nextFlickerTime.Clear();
        flickerEndTime.Clear();

        Light[] childLights = GetComponentsInChildren<Light>();

        foreach (Light light in childLights)
        {
            lightDefaults[light] = light.intensity;
            isBurnedOut[light] = false;
            nextFlickerTime[light] = Time.time + Random.Range(0f, 0.1f); // Closer initial timings
            flickerEndTime[light] = 0f;
        }
    }

    void Update()
    {
        float currentTime = Time.time;
        // Update global flicker pattern
        float globalFlicker = synchronizedFlicker ? 
            Mathf.PerlinNoise(currentTime * flickerSpeed, globalFlickerSeed) : 0f;

        foreach (KeyValuePair<Light, float> lightEntry in lightDefaults)
        {
            Light light = lightEntry.Key;
            float defaultIntensity = lightEntry.Value;

            if (isBurnedOut[light])
            {
                if (currentTime > flickerEndTime[light])
                {
                    RecoverLight(light);
                }
                continue;
            }

            if (currentTime >= nextFlickerTime[light])
            {
                if (Random.value < burnoutChance)
                {
                    BurnoutLight(light);
                }
                else if (Random.value < flickerChance)
                {
                    StartFlicker(light);
                }
                else
                {
                    light.intensity = defaultIntensity;
                }

                nextFlickerTime[light] = currentTime + Random.Range(minFlickerDuration, maxFlickerDuration);
            }

            if (currentTime <= flickerEndTime[light] && !isBurnedOut[light])
            {
                ApplyFlickerEffect(light, defaultIntensity, globalFlicker);
            }
        }
    }

    void ApplyFlickerEffect(Light light, float defaultIntensity, float globalFlicker)
    {
        // Individual noise pattern for this light
        float individualNoise = Mathf.PerlinNoise(Time.time * flickerSpeed, light.GetInstanceID());
        
        // Blend between global and individual patterns based on sync strength
        float blendedNoise = synchronizedFlicker ? 
            Mathf.Lerp(individualNoise, globalFlicker, synchronizationStrength) : 
            individualNoise;
        
        // Add subtle random variation
        float randomOffset = Random.Range(-randomness, randomness) * 0.5f;
        
        // Natural fluorescent light flicker curve
        float flickerCurve = Mathf.Pow(blendedNoise, 1.5f);  // Makes brighter states more stable
        
        // Calculate new intensity with more natural response curve
        float targetIntensity = defaultIntensity * (1f - (flickerIntensity * (1f - flickerCurve)) + randomOffset);
        
        // Add subtle high-frequency variation
        float microFlicker = Mathf.PerlinNoise(Time.time * 20f, light.GetInstanceID()) * 0.05f;
        targetIntensity *= (1f + microFlicker);
        
        // Ensure intensity stays within reasonable bounds
        light.intensity = Mathf.Clamp(targetIntensity, defaultIntensity * 0.5f, defaultIntensity * 1.1f);
    }

    void BurnoutLight(Light light)
    {
        isBurnedOut[light] = true;
        light.intensity = 0f;
        flickerEndTime[light] = Time.time + burnoutDuration;
    }

    void StartFlicker(Light light)
    {
        flickerEndTime[light] = Time.time + Random.Range(minFlickerDuration, maxFlickerDuration);
    }

    void RecoverLight(Light light)
    {
        isBurnedOut[light] = false;
        light.intensity = lightDefaults[light];
    }

    public void TriggerFlicker()
    {
        foreach (Light light in lightDefaults.Keys)
        {
            if (!isBurnedOut[light])
            {
                StartFlicker(light);
            }
        }
    }

    public void ResetLights()
    {
        foreach (KeyValuePair<Light, float> lightEntry in lightDefaults)
        {
            Light light = lightEntry.Key;
            light.intensity = lightEntry.Value;
            isBurnedOut[light] = false;
            nextFlickerTime[light] = Time.time;
            flickerEndTime[light] = 0f;
        }
    }
}