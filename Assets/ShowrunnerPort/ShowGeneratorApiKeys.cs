using UnityEngine;

namespace ShowGenerator
{
    [CreateAssetMenu(fileName = "ShowGeneratorApiKeys", menuName = "ShowGenerator/API Keys")]
    public class ShowGeneratorApiKeys : ScriptableObject
    {
        private const string DefaultClaudeWrapperUrl = "";
        private const string DefaultElevenLabsWrapperUrl = "";

        [Header("Direct API Keys")]
        [Tooltip("API Key for direct LLM access (e.g., Claude, OpenAI). Used if 'Use Wrapper Endpoints' is false.")]
        public string llmApiKey = "";

        [Tooltip("API Key for direct ElevenLabs TTS access. Used if 'Use Wrapper Endpoints' is false.")]
        public string elevenLabsApiKey = "";

        [Tooltip("API Key for Hedra video generation services.")]
        public string hedraApiKey = "";

        [Tooltip("API Key for the NEW Hedra video generation services (if different from the legacy key).")]
        public string hedraApiKeyNewApi = "";

        [Tooltip("Base URL for the Hedra API (e.g., https://api.hedra.com). Do not include trailing slash. This is typically for the LEGACY /v1 API.")]
        public string hedraBaseUrl = "https://api.hedra.com";

        [Tooltip("Base URL for the NEW Hedra API (e.g., https://mercury.dev.dream-ai.com/web-app or /api/web-app). Do not include trailing slash.")]
        public string hedraBaseUrlNewApi = "";

        [Tooltip("API Key for x23.ai access.")]
        public string x23ApiKey = "";

        [Header("Wrapper URLs")]
        [Tooltip("The URL for the Claude wrapper endpoint. Used if 'Use Wrapper Endpoints' is true.")]
        public string claudeWrapperUrl = "";
        
        [Tooltip("The URL for the ElevenLabs wrapper endpoint. Used if 'Use Wrapper Endpoints' is true.")]
        public string elevenLabsWrapperUrl = ""; 

        [Header("Claude Specific Settings")]
        [Tooltip("The model name to use for Claude API calls (e.g., claude-3-opus-20240229, claude-3-sonnet-20240229). Refer to Anthropic documentation for available models.")]
        public string claudeModelName = "claude-3-opus-20240229";
        [Tooltip("Maximum number of tokens the model should generate in the response.")]
        public int claudeMaxTokens = 4096;

        // Reset is called when the ScriptableObject is first created or when the user selects "Reset" from the context menu.
        private void Reset()
        {
            llmApiKey = ""; // Default to empty for keys
            elevenLabsApiKey = ""; // Default to empty for keys
            hedraApiKey = ""; // Default to empty for Hedra key
            hedraApiKeyNewApi = ""; // Initialize new API key field
            hedraBaseUrl = "https://api.hedra.com"; // Initialize in Reset
            hedraBaseUrlNewApi = ""; // Initialize new field in Reset
            x23ApiKey = ""; // Default to empty for x23.ai key
            claudeWrapperUrl = DefaultClaudeWrapperUrl;
            elevenLabsWrapperUrl = DefaultElevenLabsWrapperUrl;
        }
    }
}  