using UnityEngine;
using UnityEngine.Events;
// Add this to ensure SharedMusicSpectrum is recognized
// (If it's in a namespace, adjust accordingly)

public class MusicNodController : MonoBehaviour
{
    [Header("Audio Settings")]
    // Removed musicSource field, now uses SharedMusicSpectrum
    [SerializeField] private float[] frequencyBands = new float[8];
    [SerializeField] private float[] bandBuffer = new float[8];
    [SerializeField] private float[] bufferDecrease = new float[8];
    
    [Header("Beat Detection")]
    [SerializeField] private float beatThreshold = 1.5f;
    [SerializeField] private float beatCooldown = 0.2f;
    [SerializeField] private float[] bandWeights = new float[8] { 2f, 1.5f, 0.5f, 0.2f, 0.1f, 0.05f, 0.01f, 0.01f };
    [SerializeField] private bool useFocusBand = false;
    [SerializeField, Range(0,7)] private int focusBand = 0;
    [SerializeField] private bool autoCalibrateThreshold = true;
    [SerializeField] private float calibrationTime = 5f;
    [SerializeField] private float thresholdMultiplier = 1.2f; // Threshold will be average * this value
    
    [Header("Animation Settings")]
    [SerializeField] private float headNodAngle = 15f;
    [SerializeField] private float torsoNodAngle = 8f;
    [SerializeField] private float nodSpeed = 5f;
    [SerializeField, Range(0f, 1f)] private float intensityMultiplier = 1f;
    
    [Header("Nod Pattern Settings")]
    [SerializeField] private NodPattern currentPattern = NodPattern.Normal;
    [SerializeField] private float patternSpeedMultiplier = 1f;
    
    public enum NodPattern
    {
        Normal,
        Gentle,
        Enthusiastic,
        Headbanging,
        DoubleTime
    }
    
    [Header("Character References")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform torsoTransform;

    [Header("Control Settings")]
    [SerializeField] private bool isNoddingEnabled = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    
    // Events
    public UnityEvent onNoddingEnabled;
    public UnityEvent onNoddingDisabled;
    
    private bool isNodding = false;
    private float lastBeatTime;
    private float[] spectrumData = new float[1024]; // Not used, but kept for compatibility
    private float currentHeadAngle;
    private float currentTorsoAngle;
    private float targetHeadAngle;
    private float targetTorsoAngle;
    private float[] freqBandHighest = new float[8];
    private float lastDoubleTimeBeat;
    private bool isDoubleTimeBeat;
    private Quaternion initialHeadRotation;
    private Quaternion initialTorsoRotation;

    private float[] beatHistory = new float[100];
    private int beatHistoryIndex = 0;
    private float calibrationTimer = 0f;
    private bool isCalibrated = false;
    private float averageBeatValue = 0f;
    private float maxBeatValue = 0f;

    private void Start()
    {
        // Initialize all arrays
        frequencyBands = new float[8];
        bandBuffer = new float[8];
        bufferDecrease = new float[8];
        freqBandHighest = new float[8];
        
        // Initialize buffer decrease rates and highest values
        for (int i = 0; i < 8; i++)
        {
            bufferDecrease[i] = 0.005f * (i + 1);
            freqBandHighest[i] = 1f;
        }
        
        // Find character transforms if not assigned
        if (headTransform == null || torsoTransform == null)
        {
            FindCharacterTransforms();
        }

        // Store initial rotations
        if (headTransform != null) initialHeadRotation = headTransform.localRotation;
        if (torsoTransform != null) initialTorsoRotation = torsoTransform.localRotation;

        // Initialize nodding state
        isNodding = isNoddingEnabled;
        UpdatePatternSettings();

        // Initialize beat history array
        for (int i = 0; i < beatHistory.Length; i++)
        {
            beatHistory[i] = 0f;
        }
    }

    private void Update()
    {
        // Toggle nodding with specified key
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleNodding();
        }
        
        // Only analyze audio if SharedMusicSpectrum is available
        if (SharedMusicSpectrum.Instance != null && headTransform != null && torsoTransform != null)
        {
            AnalyzeAudio();
            DetectBeat();
            
            // Handle calibration
            if (autoCalibrateThreshold && !isCalibrated)
            {
                UpdateCalibration();
            }
        }
    }

    private void LateUpdate()
    {
        if (isNodding && headTransform != null && torsoTransform != null)
        {
            UpdateNodAnimation();
        }
    }

    private void UpdateNodAnimation()
    {
        // Apply pattern speed multiplier
        float currentNodSpeed = nodSpeed * patternSpeedMultiplier;
        
        // Smoothly interpolate head and torso rotation
        currentHeadAngle = Mathf.Lerp(currentHeadAngle, targetHeadAngle, Time.deltaTime * currentNodSpeed);
        currentTorsoAngle = Mathf.Lerp(currentTorsoAngle, targetTorsoAngle, Time.deltaTime * currentNodSpeed);
        
        // Apply rotations to transforms while preserving their initial rotation
        if (headTransform != null)
        {
            headTransform.localRotation = initialHeadRotation * Quaternion.Euler(currentHeadAngle, 0f, 0f);
        }
        if (torsoTransform != null)
        {
            torsoTransform.localRotation = initialTorsoRotation * Quaternion.Euler(currentTorsoAngle, 0f, 0f);
        }
        
        // Reset target angles after reaching them
        if (Mathf.Approximately(currentHeadAngle, targetHeadAngle) && 
            Mathf.Approximately(currentTorsoAngle, targetTorsoAngle))
        {
            targetHeadAngle = 0f;
            targetTorsoAngle = 0f;
        }
    }

    public void EnableNodding()
    {
        isNoddingEnabled = true;
        isNodding = true;
        onNoddingEnabled?.Invoke();
    }

    public void DisableNodding()
    {
        isNoddingEnabled = false;
        isNodding = false;
        targetHeadAngle = 0f;
        targetTorsoAngle = 0f;
        onNoddingDisabled?.Invoke();
    }

    public void ToggleNodding()
    {
        if (isNoddingEnabled)
            DisableNodding();
        else
            EnableNodding();
    }

    public void SetNodPattern(NodPattern pattern)
    {
        currentPattern = pattern;
        UpdatePatternSettings();
    }

    private void UpdatePatternSettings()
    {
        switch (currentPattern)
        {
            case NodPattern.Gentle:
                patternSpeedMultiplier = 0.7f;
                headNodAngle = 10f;
                torsoNodAngle = 5f;
                break;
            case NodPattern.Enthusiastic:
                patternSpeedMultiplier = 1.2f;
                headNodAngle = 20f;
                torsoNodAngle = 12f;
                break;
            case NodPattern.Headbanging:
                patternSpeedMultiplier = 1.5f;
                headNodAngle = 30f;
                torsoNodAngle = 15f;
                break;
            case NodPattern.DoubleTime:
                patternSpeedMultiplier = 1f;
                headNodAngle = 15f;
                torsoNodAngle = 8f;
                break;
            default: // Normal
                patternSpeedMultiplier = 1f;
                headNodAngle = 15f;
                torsoNodAngle = 8f;
                break;
        }
    }

    private void FindCharacterTransforms()
    {
        // Try to find the character's head and torso transforms
        Transform characterRoot = transform.parent;
        if (characterRoot != null)
        {
            if (headTransform == null)
            {
                headTransform = characterRoot.Find("Head") ?? characterRoot.Find("head");
            }
            if (torsoTransform == null)
            {
                torsoTransform = characterRoot.Find("Torso") ?? characterRoot.Find("torso") ?? characterRoot.Find("Spine");
            }
        }
        
        // Log warning if transforms aren't found
        if (headTransform == null || torsoTransform == null)
        {
            Debug.LogWarning("MusicNodController: Could not find head or torso transforms. Please assign them manually in the inspector.");
        }
    }
    
    private void AnalyzeAudio()
    {
        if (SharedMusicSpectrum.Instance == null) return;
        for (int i = 0; i < 8; i++)
        {
            frequencyBands[i] = SharedMusicSpectrum.Instance.frequencyBands[i];

            // Update highest value for normalization
            if (frequencyBands[i] > freqBandHighest[i])
            {
                freqBandHighest[i] = frequencyBands[i];
            }

            // Apply buffer smoothing
            if (frequencyBands[i] > bandBuffer[i])
            {
                bandBuffer[i] = frequencyBands[i];
                bufferDecrease[i] = 0.005f;
            }
            else
            {
                bandBuffer[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }
        }
    }
    
    private void UpdateCalibration()
    {
        calibrationTimer += Time.deltaTime;
        
        // Add current beat value to history
        float currentBeatValue = CalculateBeatValue();
        beatHistory[beatHistoryIndex] = currentBeatValue;
        beatHistoryIndex = (beatHistoryIndex + 1) % beatHistory.Length;
        
        // Update max value
        maxBeatValue = Mathf.Max(maxBeatValue, currentBeatValue);
        
        // Calculate average
        float sum = 0f;
        int count = 0;
        for (int i = 0; i < beatHistory.Length; i++)
        {
            if (beatHistory[i] > 0f)
            {
                sum += beatHistory[i];
                count++;
            }
        }
        
        if (count > 0)
        {
            averageBeatValue = sum / count;
        }
        
        // Set threshold based on average
        if (calibrationTimer >= calibrationTime)
        {
            beatThreshold = averageBeatValue * thresholdMultiplier;
            isCalibrated = true;
            Debug.Log($"Beat detection calibrated. Average: {averageBeatValue:F2}, Threshold: {beatThreshold:F2}, Max: {maxBeatValue:F2}");
        }
    }

    private float CalculateBeatValue()
    {
        if (useFocusBand)
        {
            // Use only the selected band for beat detection
            return bandBuffer[focusBand] / freqBandHighest[focusBand];
        }
        else
        {
            float weightedSum = 0f;
            float totalWeight = 0f;
            // Calculate weighted sum of frequency bands
            for (int i = 0; i < 8; i++)
            {
                float normalizedValue = bandBuffer[i] / freqBandHighest[i];
                weightedSum += normalizedValue * bandWeights[i];
                totalWeight += bandWeights[i];
            }
            return weightedSum / totalWeight;
        }
    }

    private void DetectBeat()
    {
        float currentBeatValue = CalculateBeatValue();
        
        // Apply intensity multiplier
        float intensity = currentBeatValue * intensityMultiplier;
        
        // Check if we detected a beat
        if (currentBeatValue > beatThreshold && Time.time - lastBeatTime > beatCooldown)
        {
            lastBeatTime = Time.time;
            
            // Handle double time pattern
            if (currentPattern == NodPattern.DoubleTime)
            {
                isDoubleTimeBeat = !isDoubleTimeBeat;
                if (isDoubleTimeBeat)
                {
                    // First beat of the pair
                    targetHeadAngle = headNodAngle * intensity;
                    targetTorsoAngle = torsoNodAngle * intensity;
                }
                else
                {
                    // Second beat of the pair (slightly smaller)
                    targetHeadAngle = headNodAngle * 0.7f * intensity;
                    targetTorsoAngle = torsoNodAngle * 0.7f * intensity;
                }
            }
            else
            {
                // Normal beat detection for other patterns
                targetHeadAngle = headNodAngle * intensity;
                targetTorsoAngle = torsoNodAngle * intensity;
            }
        }
    }

    public float GetBandWeight(int index)
    {
        if (index >= 0 && index < bandWeights.Length)
            return bandWeights[index];
        return 0f;
    }

    // Add method to manually recalibrate
    public void Recalibrate()
    {
        isCalibrated = false;
        calibrationTimer = 0f;
        maxBeatValue = 0f;
        for (int i = 0; i < beatHistory.Length; i++)
        {
            beatHistory[i] = 0f;
        }
    }
} 