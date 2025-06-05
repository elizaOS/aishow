#nullable enable
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System; // For Exception
using ShowGenerator; // For Claude API classes

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Required for JToken
#endif

public class JsonTranslationService : MonoBehaviour
{
    public enum ApiProvider
    {
        OpenRouter,
        AnthropicDirect,
        AnthropicWrapper
    }

    public ShowGenerator.ShowGeneratorApiKeys? apiKeysConfig;
    public ApiProvider currentApiProvider = ApiProvider.OpenRouter;
    public string openRouterModelName = "anthropic/claude-3-haiku-20240307"; // Default, user can change in inspector
    public bool prettyPrintOutput = true;

    // Store the results
    [HideInInspector] public string? rawJsonInput;
    [HideInInspector] public string? rawTranslatedJsonOutput;
    [HideInInspector] public string? displayableTranslatedJson;
    [HideInInspector] public string? translationError;
    [HideInInspector] public bool lastTranslationWasValidJson = false;
    [HideInInspector] public bool isProcessing = false;

    private const string OpenRouterApiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicApiVersion = "2023-06-01";

    // For OpenRouter
    [System.Serializable]
    private class OpenRouterMessage
    {
        public string role = "user";
        public string content = "";
    }

    [System.Serializable]
    private class OpenRouterChatRequest
    {
        public string model = "";
        public List<OpenRouterMessage> messages = new List<OpenRouterMessage>();
        // Potentially add other parameters like temperature, max_tokens, etc.
        // public Dictionary<string, string>? route_config; // For specifying models if using "models:[]" in OpenRouter
    }

    [System.Serializable]
    private class OpenRouterResponseChoice
    {
        public OpenRouterMessage? message;
        // public string? finish_reason; // if needed
    }

    [System.Serializable]
    private class OpenRouterChatResponse
    {
        public List<OpenRouterResponseChoice>? choices;
        public OpenRouterError? error; // For OpenRouter specific error structure
    }

    [System.Serializable]
    private class OpenRouterError // Simplified, adapt if OpenRouter provides a more complex error structure
    {
        public string? message;
        public string? type;
        public string? code;
    }


    public void CallTranslateJson(string jsonInput, string targetLanguage, string customInstructions, System.Action<string?, string?, bool> onCompleted)
    {
        rawJsonInput = jsonInput;
        StartCoroutine(TranslateJsonCoroutine(jsonInput, targetLanguage, customInstructions, onCompleted));
    }

    private IEnumerator TranslateJsonCoroutine(string jsonInput, string targetLanguage, string customInstructions, System.Action<string?, string?, bool> onCompleted)
    {
        isProcessing = true;
        translationError = null;
        rawTranslatedJsonOutput = null;
        displayableTranslatedJson = null;
        lastTranslationWasValidJson = false;

        if (apiKeysConfig == null)
        {
            translationError = "API Keys Config is not set.";
            Debug.LogError(translationError);
            isProcessing = false;
            onCompleted?.Invoke(null, translationError, false);
            yield break;
        }

        string processedJsonInput = jsonInput;
#if NEWTONSOFT_JSON
        try
        {
            JObject jsonObject = JObject.Parse(jsonInput);
            if (jsonObject["config"] is JObject configObject && configObject["prompts"] != null)
            {
                configObject.Remove("prompts");
                processedJsonInput = jsonObject.ToString(Newtonsoft.Json.Formatting.None); // Keep it compact for sending
                Debug.Log("Removed 'config.prompts' from JSON input before sending for translation.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not remove 'prompts' from JSON input: {e.Message}. Proceeding with original input.");
            // processedJsonInput remains original jsonInput in case of error
        }
#else
        Debug.LogWarning("Newtonsoft.Json is not defined. Cannot remove 'prompts' key from JSON. Consider enabling it for optimal payload size.");
#endif

        string apiKeyToUse = "";
        string effectiveModelName = openRouterModelName; // Default to OpenRouter model

        switch (currentApiProvider)
        {
            case ApiProvider.OpenRouter:
                if (string.IsNullOrEmpty(apiKeysConfig.openRouterApiKey))
                {
                    translationError = "OpenRouter API Key is not set in API Keys Config.";
                }
                apiKeyToUse = apiKeysConfig.openRouterApiKey ?? "";
                effectiveModelName = openRouterModelName;
                break;
            case ApiProvider.AnthropicDirect:
                if (string.IsNullOrEmpty(apiKeysConfig.anthropicApiKey))
                {
                    translationError = "Anthropic API Key is not set in API Keys Config.";
                }
                apiKeyToUse = apiKeysConfig.anthropicApiKey ?? "";
                effectiveModelName = !string.IsNullOrEmpty(apiKeysConfig.claudeModelName) ? apiKeysConfig.claudeModelName : "claude-3-opus-20240229";
                break;
            case ApiProvider.AnthropicWrapper:
                if (string.IsNullOrEmpty(apiKeysConfig.claudeWrapperUrl))
                {
                    translationError = "Anthropic Wrapper URL is not set in API Keys Config.";
                }
                // No specific API key needed for the wrapper itself, it handles auth to Claude
                effectiveModelName = !string.IsNullOrEmpty(apiKeysConfig.claudeModelName) ? apiKeysConfig.claudeModelName : "claude-3-opus-20240229"; // Wrapper might still use a model name in payload
                break;
        }

        if (!string.IsNullOrEmpty(translationError))
        {
            Debug.LogError(translationError);
            isProcessing = false;
            onCompleted?.Invoke(null, translationError, false);
            yield break;
        }

        // Construct the system prompt elements
        StringBuilder systemPromptBuilder = new StringBuilder();
        systemPromptBuilder.AppendLine($"You are an expert JSON translation assistant. Your task is to translate the textual content of the provided JSON structure from its original language into {targetLanguage}.");
        systemPromptBuilder.AppendLine("Key requirements:");
        systemPromptBuilder.AppendLine("1. Preserve the exact JSON structure, including all keys, arrays, and nested objects.");
        systemPromptBuilder.AppendLine("2. Only translate the string values associated with keys. Do not translate keys themselves, booleans, numbers, or null values.");
        systemPromptBuilder.AppendLine("3. Be context-aware: If the JSON represents a script (e.g., with scenes, dialogue, actions), ensure translations maintain the original intent, tone, and character voices.");
        systemPromptBuilder.AppendLine("4. Handle technical terms, proper nouns, and culturally specific references appropriately for the target language, either by translating them or keeping them in the original language if that's standard practice.");
        if (!string.IsNullOrEmpty(customInstructions))
        {
            systemPromptBuilder.AppendLine("5. Follow these additional user-provided instructions carefully:");
            systemPromptBuilder.AppendLine(customInstructions);
            systemPromptBuilder.AppendLine("6. IMPORTANT: Your response MUST be ONLY the translated JSON object/string, starting with `{` and ending with `}` (or `[` and `]` if it's a JSON array). Do not include any explanatory text, markdown, or any other characters before or after the JSON string itself.");
        }
        else
        {
            systemPromptBuilder.AppendLine("5. IMPORTANT: Your response MUST be ONLY the translated JSON object/string, starting with `{` and ending with `}` (or `[` and `]` if it's a JSON array). Do not include any explanatory text, markdown, or any other characters before or after the JSON string itself.");
        }

        string fullUserContent = $"{systemPromptBuilder.ToString()}\n\nORIGINAL JSON TO TRANSLATE:\n{processedJsonInput}";

        UnityWebRequest req;
        string requestBodyJson = "";

        switch (currentApiProvider)
        {
            case ApiProvider.OpenRouter:
                var openRouterRequest = new OpenRouterChatRequest
                {
                    model = effectiveModelName,
                    messages = new List<OpenRouterMessage> { new OpenRouterMessage { role = "user", content = fullUserContent } }
                };
                requestBodyJson = JsonUtility.ToJson(openRouterRequest);
                req = new UnityWebRequest(OpenRouterApiUrl, "POST");
                req.SetRequestHeader("Authorization", $"Bearer {apiKeyToUse}");
                break;

            case ApiProvider.AnthropicDirect:
            case ApiProvider.AnthropicWrapper: // Assuming wrapper takes a similar payload to direct Anthropic
                var anthropicMessages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { role = "user", content = fullUserContent }
                };
                var anthropicPayload = new ClaudeApiRequestPayload // Using class from ShowGenerator namespace
                {
                    model = effectiveModelName,
                    messages = anthropicMessages,
                    max_tokens = apiKeysConfig != null && apiKeysConfig.claudeMaxTokens > 0 ? apiKeysConfig.claudeMaxTokens : 4096,
                    // system prompt is prepended to user message, so no system field here
                };

                // For Anthropic, we explicitly construct the JSON to omit the 'system' field if it's not used,
                // similar to ShowrunnerGeneratorLLM
                var payloadDict = new Dictionary<string, object>
                {
                    { "model", anthropicPayload.model },
                    { "messages", anthropicPayload.messages },
                    { "max_tokens", anthropicPayload.max_tokens }
                };
// Log the anthropic payload before serialization
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                try 
                {
                    string tempSerializedPayload = "";
#if NEWTONSOFT_JSON
                    tempSerializedPayload = JsonConvert.SerializeObject(payloadDict, Formatting.Indented);
#else
                    // Basic manual serialization for logging if Newtonsoft is not available
                    StringBuilder sb = new StringBuilder("{\n");
                    sb.AppendLine($"  \"model\": \"{payloadDict["model"]}\",");
                    sb.AppendLine($"  \"max_tokens\": {payloadDict["max_tokens"]},");
                    sb.AppendLine("  \"messages\": [");
                    var msgs = payloadDict["messages"] as List<ClaudeMessage>;
                    if (msgs != null) {
                        for(int i=0; i < msgs.Count; i++) {
                            sb.Append($"    {{ \"role\": \"{msgs[i].role}\", \"content\": \"...omitted for brevity...\" }}"); // Simplified content for logging
                            if (i < msgs.Count -1) sb.AppendLine(","); else sb.AppendLine();
                        }
                    }
                    sb.AppendLine("  ]");
                    sb.AppendLine("}");
                    tempSerializedPayload = sb.ToString();
#endif
                    Debug.Log($"Anthropic Payload ({currentApiProvider}):\n{tempSerializedPayload}");
                } 
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not serialize Anthropic payload for logging: {e.Message}");
                }
#endif


#if NEWTONSOFT_JSON
                requestBodyJson = JsonConvert.SerializeObject(payloadDict);
#else
                // Manual serialization if Newtonsoft.Json is not available
                // This is a simplified version and might not cover all edge cases for message content.
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append($"\"model\":\"{anthropicPayload.model}\",");
                sb.Append("\"messages\":[");
                for (int i = 0; i < anthropicPayload.messages.Count; i++)
                {
                    sb.Append($"{{\"role\":\"{anthropicPayload.messages[i].role}\",\"content\":\"{EscapeJsonString(anthropicPayload.messages[i].content)}\"}}");
                    if (i < anthropicPayload.messages.Count - 1)
                        sb.Append(",");
                }
                sb.Append("],");
                sb.Append($"\"max_tokens\":{anthropicPayload.max_tokens}");
                sb.Append("}");
                requestBodyJson = sb.ToString();
#endif
                string targetUrl = currentApiProvider == ApiProvider.AnthropicDirect ? AnthropicApiUrl : apiKeysConfig!.claudeWrapperUrl!;
                req = new UnityWebRequest(targetUrl, "POST");

                if (currentApiProvider == ApiProvider.AnthropicDirect)
                {
                    req.SetRequestHeader("x-api-key", apiKeyToUse);
                    req.SetRequestHeader("anthropic-version", AnthropicApiVersion);
                }
                // Wrapper might have its own auth or no auth from client side
                break;

            default: // Should not happen
                translationError = "Invalid API Provider selected.";
                Debug.LogError(translationError);
                isProcessing = false;
                onCompleted?.Invoke(null, translationError, false);
                yield break;
        }

        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBodyJson);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 300; // Increased timeout for potentially long translations

        string urlForLogging = req.url;
        if (currentApiProvider == ApiProvider.AnthropicWrapper)
        {
            urlForLogging = "Anthropic Wrapper Endpoint";
        }
        Debug.Log($"Sending translation request to {urlForLogging} using {currentApiProvider}. Payload: {requestBodyJson}");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string responseText = req.downloadHandler.text;
            Debug.Log($"Raw response from {currentApiProvider}: {responseText}");

            string? extractedJson = null;
            bool isValidJsonResponse = false;

            try
            {
                switch (currentApiProvider)
                {
                    case ApiProvider.OpenRouter:
                        OpenRouterChatResponse? openRouterResponse = JsonUtility.FromJson<OpenRouterChatResponse>(responseText);
                        if (openRouterResponse?.choices != null && openRouterResponse.choices.Count > 0 && openRouterResponse.choices[0]?.message != null)
                        {
                            extractedJson = openRouterResponse.choices[0].message!.content;
                        }
                        else if (openRouterResponse?.error != null) {
                            translationError = $"OpenRouter API Error: ({openRouterResponse.error.type} / {openRouterResponse.error.code}) {openRouterResponse.error.message}";
                            Debug.LogError(translationError);
                        }
                        else
                        {
                            translationError = "Failed to parse OpenRouter response or response was empty.";
                            Debug.LogWarning($"{translationError} Raw Response: {responseText}");
                        }
                        break;

                    case ApiProvider.AnthropicDirect:
                    case ApiProvider.AnthropicWrapper: // Assuming wrapper returns Claude-like response
#if NEWTONSOFT_JSON
                        // Use Newtonsoft.Json for more robust parsing of potentially complex Claude responses
                        ClaudeApiResponse? anthropicResponse = JsonConvert.DeserializeObject<ClaudeApiResponse>(responseText);
#else
                        // Basic JsonUtility parsing if Newtonsoft is not available
                        ClaudeApiResponse? anthropicResponse = JsonUtility.FromJson<ClaudeApiResponse>(responseText);
#endif
                        if (anthropicResponse?.content != null && anthropicResponse.content.Count > 0 && anthropicResponse.content[0] != null)
                        {
                            extractedJson = anthropicResponse.content[0].text;
                        }
                        else
                        {
                            // Try to parse a potential error structure from Anthropic or Wrapper
                            // Anthropic error structure: {"type": "error", "error": {"type": "invalid_request_error", "message": "..."}}
                            try
                            {
#if NEWTONSOFT_JSON
                                JObject errorObj = JObject.Parse(responseText);
                                if (errorObj["error"]?["message"] != null)
                                {
                                    translationError = $"Anthropic API Error: ({errorObj["error"]?["type"]}) {errorObj["error"]?["message"]}";
                                }
                                else if (errorObj["detail"] != null) // Some wrappers might use "detail"
                                {
                                     translationError = $"API Error: {errorObj["detail"]}";
                                }
                                else
                                {
                                    translationError = "Failed to parse Anthropic/Wrapper response or content was empty.";
                                }
#else
                                // Simplified error parsing without Newtonsoft
                                if (responseText.Contains("\"error\"") && responseText.Contains("\"message\""))
                                {
                                   // Basic extraction, not robust
                                   translationError = "Anthropic API Error (details in log)";
                                } else {
                                   translationError = "Failed to parse Anthropic/Wrapper response or content was empty.";
                                }
#endif
                                Debug.LogError($"{translationError} Raw Response: {responseText}");
                            }
                            catch (Exception parseEx)
                            {
                                translationError = "Failed to parse Anthropic/Wrapper response (and failed to parse error structure).";
                                Debug.LogError($"{translationError} ParseEx: {parseEx.Message} Raw Response: {responseText}");
                            }
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(extractedJson))
                {
                    // The LLM might sometimes wrap the JSON in markdown ```json ... ```
                    extractedJson = extractedJson.Trim();
                    if (extractedJson.StartsWith("```json"))
                    {
                        extractedJson = extractedJson.Substring(7);
                        if (extractedJson.EndsWith("```"))
                        {
                            extractedJson = extractedJson.Substring(0, extractedJson.Length - 3);
                        }
                    }
                    extractedJson = extractedJson.Trim(); // Trim again after potential markdown removal

                    rawTranslatedJsonOutput = extractedJson;

#if NEWTONSOFT_JSON
                    try
                    {
                        // Validate if the result is valid JSON
                        JToken.Parse(rawTranslatedJsonOutput); // Throws error if invalid
                        isValidJsonResponse = true;
                        lastTranslationWasValidJson = true;
                        if (prettyPrintOutput)
                        {
                            displayableTranslatedJson = JToken.Parse(rawTranslatedJsonOutput).ToString(Formatting.Indented);
                        }
                        else
                        {
                            displayableTranslatedJson = rawTranslatedJsonOutput;
                        }
                    }
                    catch (JsonReaderException jsonEx)
                    {
                        translationError = $"LLM returned invalid JSON: {jsonEx.Message}. Raw output: {rawTranslatedJsonOutput}";
                        Debug.LogError(translationError);
                        displayableTranslatedJson = rawTranslatedJsonOutput; // Show raw output even if invalid
                        isValidJsonResponse = false;
                        lastTranslationWasValidJson = false;
                    }
#else
                    // Without Newtonsoft, we can't easily validate or pretty print.
                    // We'll assume it's valid if we got here and the LLM was asked for JSON.
                    displayableTranslatedJson = rawTranslatedJsonOutput;
                    isValidJsonResponse = true; // Best effort assumption
                    lastTranslationWasValidJson = true; // Best effort assumption
                    if (prettyPrintOutput) {
                         Debug.LogWarning("Pretty printing requested but Newtonsoft.Json is not enabled. Output will be raw.");
                    }
#endif
                }
                // If extractedJson is null/empty, translationError should have been set by the parsing logic.
            }
            catch (Exception ex)
            {
                translationError = $"Error processing LLM response: {ex.Message}. Raw response: {responseText}";
                Debug.LogError(translationError);
                displayableTranslatedJson = responseText; // Show raw response on error
                isValidJsonResponse = false;
            }

            if (!string.IsNullOrEmpty(translationError)) // If an error occurred during parsing or API returned error message
            {
                 onCompleted?.Invoke(null, translationError, false);
            }
            else if (isValidJsonResponse)
            {
                onCompleted?.Invoke(rawTranslatedJsonOutput, null, true);
            }
            else // Extracted JSON was null or empty, but no specific error message was set (should be rare)
            {
                translationError = "Translation attempt resulted in empty or unparseable content, but no specific API error was reported.";
                Debug.LogWarning(translationError + " Raw Response: " + responseText);
                onCompleted?.Invoke(null, translationError, false);
            }
        }
        else
        {
            translationError = $"API call failed: {req.responseCode} - {req.error}";
            string responseBody = req.downloadHandler?.text ?? "N/A";
            Debug.LogError($"{translationError}. Response Body: {responseBody}");
            displayableTranslatedJson = responseBody; // Show raw error response
            onCompleted?.Invoke(null, translationError, false);
        }
        isProcessing = false;
    }

#if !NEWTONSOFT_JSON
    // Basic JSON string escaper for manual JSON construction
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            switch (c)
            {
                case '\"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        // Basic Unicode escape for control characters
                        sb.AppendFormat("\\u{0:x4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
#endif
}
