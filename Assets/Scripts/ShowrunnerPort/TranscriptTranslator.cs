#nullable enable
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System; // For Exception
using ShowGenerator; // For Claude API classes

//Removed Newtonsoft.Json related imports as they are not needed for plain text

public class TranscriptTranslator : MonoBehaviour
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
    // Removed prettyPrintOutput as it's not relevant for plain text

    // Store the results for text
    [HideInInspector] public string? rawTextInput;
    [HideInInspector] public string? rawTranslatedTextOutput;
    [HideInInspector] public string? displayableTranslatedText; // For UI display if needed, might be same as rawTranslatedTextOutput
    [HideInInspector] public string? translationError;
    // lastTranslationWasValidJson is not relevant for plain text
    [HideInInspector] public bool isProcessing = false;

    private const string OpenRouterApiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicApiVersion = "2023-06-01";

    // OpenRouter specific classes (can remain as they are, payload structure is similar)
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
    }

    [System.Serializable]
    private class OpenRouterResponseChoice
    {
        public OpenRouterMessage? message;
    }

    [System.Serializable]
    private class OpenRouterChatResponse
    {
        public List<OpenRouterResponseChoice>? choices;
        public OpenRouterError? error;
    }

    [System.Serializable]
    private class OpenRouterError
    {
        public string? message;
        public string? type;
        public string? code;
    }

    public void CallTranslateText(string textInput, string targetLanguage, string customInstructions, System.Action<string?, string?, bool> onCompleted)
    {
        rawTextInput = textInput;
        StartCoroutine(TranslateTextCoroutine(textInput, targetLanguage, customInstructions, onCompleted));
    }

    private IEnumerator TranslateTextCoroutine(string textInput, string targetLanguage, string customInstructions, System.Action<string?, string?, bool> onCompleted)
    {
        isProcessing = true;
        translationError = null;
        rawTranslatedTextOutput = null;
        displayableTranslatedText = null;
        // lastTranslationWasValidJson removed

        if (apiKeysConfig == null)
        {
            translationError = "API Keys Config is not set.";
            Debug.LogError(translationError);
            isProcessing = false;
            onCompleted?.Invoke(null, translationError, false);
            yield break;
        }

        // No JSON specific processing like removing 'config.prompts' is needed for plain text
        string processedTextInput = textInput;

        string apiKeyToUse = "";
        string effectiveModelName = openRouterModelName;

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
                effectiveModelName = !string.IsNullOrEmpty(apiKeysConfig.claudeModelName) ? apiKeysConfig.claudeModelName : "claude-3-opus-20240229";
                break;
        }

        if (!string.IsNullOrEmpty(translationError))
        {
            Debug.LogError(translationError);
            isProcessing = false;
            onCompleted?.Invoke(null, translationError, false);
            yield break;
        }

        StringBuilder systemPromptBuilder = new StringBuilder();
        systemPromptBuilder.AppendLine($"You are an expert translation assistant. Your task is to translate the provided text from its original language into {targetLanguage}.");
        systemPromptBuilder.AppendLine("Key requirements:");
        systemPromptBuilder.AppendLine("1. Translate the text accurately, maintaining the original meaning, tone, and style as much as possible.");
        systemPromptBuilder.AppendLine("2. Handle idiomatic expressions, slang, and cultural nuances appropriately for the target language.");
        systemPromptBuilder.AppendLine("3. If specific terminology or proper nouns are present, translate them correctly or keep them in the original language if that is the standard practice in the target language and context.");
        if (!string.IsNullOrEmpty(customInstructions))
        {
            systemPromptBuilder.AppendLine("4. Follow these additional user-provided instructions carefully:");
            systemPromptBuilder.AppendLine(customInstructions);
            systemPromptBuilder.AppendLine("5. IMPORTANT: Your response MUST be ONLY the translated text. Do not include any explanatory text, markdown, or any other characters before or after the translated text itself.");
        }
        else
        {
            systemPromptBuilder.AppendLine("4. IMPORTANT: Your response MUST be ONLY the translated text. Do not include any explanatory text, markdown, or any other characters before or after the translated text itself.");
        }

        // Use a verbatim string literal for multi-line content to avoid issues with newlines in constants.
        string fullUserContent = $@"{systemPromptBuilder.ToString()}

ORIGINAL TEXT TO TRANSLATE:
{processedTextInput}";

        UnityWebRequest req;
        string requestBodyJson = ""; // Request body is still JSON for the API, content is text

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
                
                // Debug log for OpenRouter API Key
                string keyForLog = "null_or_empty";
                if (!string.IsNullOrEmpty(apiKeyToUse))
                {
                    keyForLog = apiKeyToUse.Length > 7 ? apiKeyToUse.Substring(0, 4) + "..." + apiKeyToUse.Substring(apiKeyToUse.Length - 3) : "key_too_short_to_mask";
                }
                Debug.Log($"TranscriptTranslator: Using OpenRouter. API Key for log: {keyForLog}");

                req.SetRequestHeader("Authorization", $"Bearer {apiKeyToUse}");
                break;

            case ApiProvider.AnthropicDirect:
            case ApiProvider.AnthropicWrapper:
                var anthropicMessages = new List<ClaudeMessage> // Assuming ClaudeMessage structure exists
                {
                    new ClaudeMessage { role = "user", content = fullUserContent }
                };
                var anthropicPayload = new ClaudeApiRequestPayload
                {
                    model = effectiveModelName,
                    messages = anthropicMessages,
                    max_tokens = apiKeysConfig != null && apiKeysConfig.claudeMaxTokens > 0 ? apiKeysConfig.claudeMaxTokens : 4096,
                };
                
                // Simplified payload construction for Anthropic as we don't need Newtonsoft specific features here.
                // JsonUtility should suffice for this structure.
                requestBodyJson = JsonUtility.ToJson(anthropicPayload);
                // Logging the payload for debugging
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // Use a verbatim string for the log message to handle potential newlines in requestBodyJson correctly for logging.
                Debug.Log($@"Anthropic Payload ({currentApiProvider}):
{requestBodyJson}");
#endif

                string targetUrl = currentApiProvider == ApiProvider.AnthropicDirect ? AnthropicApiUrl : apiKeysConfig!.claudeWrapperUrl!;
                req = new UnityWebRequest(targetUrl, "POST");

                if (currentApiProvider == ApiProvider.AnthropicDirect)
                {
                    req.SetRequestHeader("x-api-key", apiKeyToUse);
                    req.SetRequestHeader("anthropic-version", AnthropicApiVersion);
                }
                break;

            default:
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
        req.timeout = 300;

        string urlForLogging = req.url;
        if (currentApiProvider == ApiProvider.AnthropicWrapper)
        {
            urlForLogging = "Anthropic Wrapper Endpoint";
        }
        Debug.Log($"Sending translation request to {urlForLogging} using {currentApiProvider}.");
        // Removed payload logging from here as it can be very large for text. Logged above for Anthropic.

        yield return req.SendWebRequest();

        bool translationSucceeded = false; // Used instead of isValidJsonResponse

        if (req.result == UnityWebRequest.Result.Success)
        {
            string responseText = req.downloadHandler.text;
            Debug.Log($"Raw response from {currentApiProvider}: {responseText}");

            string? extractedText = null;

            try
            {
                switch (currentApiProvider)
                {
                    case ApiProvider.OpenRouter:
                        OpenRouterChatResponse? openRouterResponse = JsonUtility.FromJson<OpenRouterChatResponse>(responseText);
                        if (openRouterResponse?.choices != null && openRouterResponse.choices.Count > 0 && openRouterResponse.choices[0]?.message != null)
                        {
                            extractedText = openRouterResponse.choices[0].message!.content;
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
                    case ApiProvider.AnthropicWrapper:
                        // Anthropic response parsing needs to be adapted as it might not use Newtonsoft.Json here.
                        // Assuming ClaudeApiResponse has a structure like { "content": [{"type": "text", "text": "..."}] }
                        // We need to define or ensure ClaudeApiResponse and its nested types are available and parsable by JsonUtility.
                        // For now, using a simplified approach based on common patterns.
                        // This part might need ShowGenerator.ClaudeApiResponse to be JsonUtility-friendly or use manual parsing if complex.
                        
                        // Attempting to parse with JsonUtility assuming a compatible ClaudeApiResponse structure.
                        // If ShowGenerator.ClaudeApiResponse is not JsonUtility compatible, this will need adjustment.
                        ClaudeApiResponse? anthropicResponse = null;
                        try 
                        {
                            anthropicResponse = JsonUtility.FromJson<ClaudeApiResponse>(responseText);
                        }
                        catch (Exception ex)
                        {
                             Debug.LogError($"Failed to parse Anthropic response with JsonUtility: {ex.Message}. Raw: {responseText}");
                             // Fallback: try to extract text if it's a very simple JSON string output from the LLM itself.
                             // This is a Hail Mary, as LLMs usually wrap in their standard API structure.
                             if (responseText.StartsWith("\"") && responseText.EndsWith("\"")) {
                                 // Corrected Substring and Replace logic
                                 // Ensure to handle escaped characters if necessary, but for a simple string, this might be okay.
                                 // Consider just taking the substring without complex replacements if issues persist.
                                 extractedText = responseText.Substring(1, responseText.Length -2);
                                 extractedText = extractedText.Replace("\\n", "\n").Replace("\\\"", "\""); // Purpose: unescape newline and quote characters.
                             }
                        }

                        if (anthropicResponse?.content != null && anthropicResponse.content.Count > 0 && anthropicResponse.content[0] != null && anthropicResponse.content[0].type == "text")
                        {
                            extractedText = anthropicResponse.content[0].text;
                        }
                        else if (anthropicResponse != null && string.IsNullOrEmpty(anthropicResponse.type) && string.IsNullOrEmpty(anthropicResponse.id)) // check if it's not an error object
                        {
                             // If content is missing but it's not an error object, it could be an issue with the response structure or an empty translation.
                             // Check for common error patterns if direct parsing fails or content is missing.
                             // This assumes ClaudeErrorResponse structure for parsing errors.
                             try
                             {
                                 var errorDetails = JsonUtility.FromJson<ClaudeErrorResponse>(responseText); // Assumes ClaudeErrorResponse has 'error.message'
                                 if (errorDetails != null && errorDetails.error != null && !string.IsNullOrEmpty(errorDetails.error.message))
                                 {
                                     translationError = $"Anthropic API Error: ({errorDetails.error.type}) {errorDetails.error.message}";
                                 }
                                 else if (!string.IsNullOrEmpty(responseText)) // If it's not an error, but no text, log it.
                                 {
                                     translationError = "Anthropic response parsed, but no translatable text content found or content was empty.";
                                 } else {
                                     translationError = "Anthropic response was empty or unparseable.";
                                 }
                             } catch (Exception parseExInner) {
                                 translationError = "Failed to parse Anthropic response, and also failed to parse it as a known error structure.";
                                 Debug.LogWarning($"{translationError} ParseExInner: {parseExInner.Message}. Raw Response: {responseText}");
                             }
                             if(!string.IsNullOrEmpty(translationError)) Debug.LogError(translationError + " Raw Response: " + responseText);
                        } else if (anthropicResponse == null && extractedText == null && string.IsNullOrEmpty(translationError)) {
                            // If parsing with JsonUtility failed and no specific error was set, then set a generic one.
                            translationError = "Failed to parse Anthropic response or content was empty.";
                            Debug.LogError(translationError + " Raw Response: " + responseText);
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(extractedText))
                {
                    // The LLM might sometimes wrap the text in markdown ``` ```
                    rawTranslatedTextOutput = extractedText.Trim();
                    if (rawTranslatedTextOutput.StartsWith("```") && rawTranslatedTextOutput.EndsWith("```"))
                    {
                        rawTranslatedTextOutput = rawTranslatedTextOutput.Substring(3, rawTranslatedTextOutput.Length - 6).Trim();
                    }
                    // Also check for markdown code blocks that specify a language.
                    // Corrected logic for stripping markdown with language specifier
                    if (rawTranslatedTextOutput.StartsWith("```")) 
                    {
                        int firstNewline = rawTranslatedTextOutput.IndexOf('\n'); // Use char literal for single newline
                        if (firstNewline > 0 && firstNewline < rawTranslatedTextOutput.Length -1) 
                        { 
                            // Check if the line before newline is just ```<lang>
                            string firstLine = rawTranslatedTextOutput.Substring(0, firstNewline).Trim();
                            // A simple check: if the first line starts with ``` and doesn't contain spaces (common for ```lang format)
                            if (firstLine.StartsWith("```") && !firstLine.Substring(3).Contains(" ")) 
                            {
                                rawTranslatedTextOutput = rawTranslatedTextOutput.Substring(firstNewline + 1);
                                if (rawTranslatedTextOutput.EndsWith("```")) 
                                {
                                    rawTranslatedTextOutput = rawTranslatedTextOutput.Substring(0, rawTranslatedTextOutput.Length - 3).Trim();
                                }
                            }
                        }
                    }


                    displayableTranslatedText = rawTranslatedTextOutput;
                    translationSucceeded = true;
                    // No JSON validation or pretty printing needed
                }
                // If extractedText is null/empty, translationError should have been set by the parsing logic.
            }
            catch (Exception ex)
            {
                translationError = $"Error processing LLM response: {ex.Message}. Raw response: {responseText}";
                Debug.LogError(translationError);
                displayableTranslatedText = responseText; // Show raw response on error
                translationSucceeded = false;
            }

            if (!string.IsNullOrEmpty(translationError))
            {
                 onCompleted?.Invoke(null, translationError, false);
            }
            else if (translationSucceeded)
            {
                onCompleted?.Invoke(rawTranslatedTextOutput, null, true);
            }
            else
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
            displayableTranslatedText = responseBody;
            onCompleted?.Invoke(null, translationError, false);
        }
        isProcessing = false;
    }

    // EscapeJsonString is not needed if not manually constructing JSON with text content that needs escaping for a JSON field value.
    // The API client libraries (JsonUtility or Newtonsoft) handle string escaping when serializing objects.
    // If we were manually building a JSON string that included user-provided text within it, then we'd need it.
    // For now, assuming the fullUserContent is correctly handled by JsonUtility.ToJson when placed in OpenRouterMessage or ClaudeMessage.
}

// Helper class for parsing Anthropic error responses with JsonUtility, ensure this matches actual error structure
// Example: {"type": "error", "error": {"type": "invalid_request_error", "message": "..."}}
[System.Serializable]
public class ClaudeErrorResponse
{
    public string? type;
    public ClaudeErrorDetails? error;
}

[System.Serializable]
public class ClaudeErrorDetails
{
    public string? type;
    public string? message;
} 