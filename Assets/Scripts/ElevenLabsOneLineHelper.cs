using UnityEngine;
using ShowGenerator; // For ShowGeneratorApiKeys

/// <summary>
/// Helper component to provide an Inspector UI for generating single lines of audio via ElevenLabs.
/// Attach this to a GameObject. The actual UI and logic are in ElevenLabsOneLineHelperEditor.cs.
/// </summary>
public class ElevenLabsOneLineHelper : MonoBehaviour
{
    [Tooltip("Assign your ShowGeneratorApiKeys ScriptableObject here.")]
    public ShowGeneratorApiKeys apiKeysConfig;

    [Tooltip("If true, uses the wrapper URLs defined in ApiKeysConfig. If false, uses direct API calls with API keys.")]
    public bool useWrapperEndpoints = false;

    [Tooltip("The text you want to convert to speech.")]
    public string textToSpeak = "Hello, this is a test line generated from the Inspector.";
    
    // This will be set by the editor script via the dropdown based on ShowrunnerManager.DefaultVoiceMap
    [HideInInspector] // Hide this as it's controlled by the dropdown
    public string selectedVoiceId = ""; 

    [Tooltip("The name of the output audio file (e.g., my_line.mp3).")]
    public string outputFileName = "inspector_generated_line.mp3";

    // Used by the editor script to manage the dropdown selection state
    [HideInInspector]
    public int selectedVoiceIndex = 0;

    // Optional: Add a default voice ID from the map if desired, e.g., the first one.
    private void Awake()
    {
        // Initialize selectedVoiceId if it's empty and the map is available
        if (string.IsNullOrEmpty(selectedVoiceId) && ShowrunnerManager.DefaultVoiceMap != null && ShowrunnerManager.DefaultVoiceMap.Count > 0)
        {
            bool firstVoiceSet = false;
            foreach (var voice in ShowrunnerManager.DefaultVoiceMap)
            {
                selectedVoiceId = voice.Value; // Set to the first voice ID
                // Try to find the index for this ID to set selectedVoiceIndex correctly
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
                 // Fallback if map is empty or something went wrong, though covered by count check
                selectedVoiceId = "21m00Tcm4TlvDq8ikWAM"; 
                selectedVoiceIndex = 0; // Assuming this ID might not be in map, so index 0 is a guess
            }
        }
        else if (string.IsNullOrEmpty(selectedVoiceId))
        {
            // Fallback if DefaultVoiceMap is not available or empty at Awake
            selectedVoiceId = "21m00Tcm4TlvDq8ikWAM"; // A generic default
            selectedVoiceIndex = 0;
        }
    }
} 