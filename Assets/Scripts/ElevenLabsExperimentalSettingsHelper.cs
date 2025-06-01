using UnityEngine;
using ShowGenerator; // For ShowGeneratorApiKeys

/// <summary>
/// EXPERIMENTAL helper component to provide an Inspector UI for generating single lines of audio via ElevenLabs,
/// INCLUDING voice characteristic settings (stability, style, etc.).
/// Attach this to a GameObject. The actual UI and logic are in ElevenLabsExperimentalSettingsHelperEditor.cs.
/// </summary>
public class ElevenLabsExperimentalSettingsHelper : MonoBehaviour
{
    [Header("API and Output Configuration")]
    [Tooltip("Assign your ShowGeneratorApiKeys ScriptableObject here.")]
    public ShowGeneratorApiKeys apiKeysConfig;

    [Tooltip("If true, uses the wrapper URLs defined in ApiKeysConfig. If false, uses direct API calls with API keys.")]
    public bool useWrapperEndpoints = false;

    [Tooltip("The text you want to convert to speech.")]
    public string textToSpeak = "Hello, this is an experimental test line with voice settings.";
    
    [Tooltip("Base name for the output audio file (e.g., my_line). Voice name and iteration will be handled by the editor script.")]
    public string outputFileName = "experimental_line";

    // Voice selection (managed by editor script)
    [HideInInspector] 
    public string selectedVoiceId = ""; 
    [HideInInspector]
    public int selectedVoiceIndex = 0;

    [Header("EXPERIMENTAL Voice Settings")]
    [Tooltip("Stability: Lower values (e.g., 0.2) = more varied emotion, higher (e.g., 0.9) = more monotonous. Default: 0.75")]
    [Range(0f, 1f)]
    public float stability = 0.75f;

    [Tooltip("Similarity Boost: How closely to match the original voice. Higher = more similar. Default: 0.75")]
    [Range(0f, 1f)]
    public float similarityBoost = 0.75f;

    [Tooltip("Style Exaggeration: Amplifies the voice's style. 0 = none. Can increase latency. Default: 0.0")]
    [Range(0f, 1f)]
    public float styleExaggeration = 0.0f;

    [Tooltip("Use Speaker Boost: Boosts similarity to original speaker. Can increase latency. Default: true")]
    public bool useSpeakerBoost = true;

    private void Awake()
    {
        // Initialize selectedVoiceId if it's empty and the map is available (copied from ElevenLabsOneLineHelper)
        if (string.IsNullOrEmpty(selectedVoiceId) && ShowrunnerManager.DefaultVoiceMap != null && ShowrunnerManager.DefaultVoiceMap.Count > 0)
        {
            bool firstVoiceSet = false;
            foreach (var voice in ShowrunnerManager.DefaultVoiceMap)
            {
                selectedVoiceId = voice.Value; 
                int currentIndex = 0;
                foreach(var key in ShowrunnerManager.DefaultVoiceMap.Keys)
                {
                    if (ShowrunnerManager.DefaultVoiceMap[key] == selectedVoiceId)
                    {
                        selectedVoiceIndex = currentIndex;
                        firstVoiceSet = true;
                        break;
                    }
                    currentIndex++;
                }
                if(firstVoiceSet) break;
            }
            if (!firstVoiceSet)
            {
                selectedVoiceId = "21m00Tcm4TlvDq8ikWAM"; 
                selectedVoiceIndex = 0; 
            }
        }
        else if (string.IsNullOrEmpty(selectedVoiceId))
        {
            selectedVoiceId = "21m00Tcm4TlvDq8ikWAM"; 
            selectedVoiceIndex = 0;
        }
    }
} 