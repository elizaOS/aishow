using UnityEngine;

public class ShowrunnerManager : MonoBehaviour
{
    [Header("Showrunner Workflow Settings")]
    [Tooltip("If enabled, audio will be generated automatically after each episode is generated.")]
    public bool autoGenerateAudioAfterEpisode = false;

    [Header("API Configuration")]
    public ShowGenerator.ShowGeneratorApiKeys apiKeysConfig;

    [Tooltip("If true, uses the wrapper URLs defined in ApiKeysConfig. If false, uses direct API calls with API keys.")]
    public bool useWrapperEndpoints = true;
} 