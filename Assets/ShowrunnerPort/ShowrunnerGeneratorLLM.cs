#nullable enable
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using ShowGenerator;
using System.Collections.Generic;
using System.IO; // Added for File and Directory operations
using System; // Added for Exception
using System.Text.RegularExpressions;
using System.Linq; // Added for ParseStringToList helper

namespace ShowGenerator
{
    // Helper classes for Claude API structure
    [System.Serializable]
    public class ClaudeMessage
    {
        public string role = string.Empty;
        public string content = string.Empty;
    }

    [System.Serializable]
    public class ClaudeApiRequestPayload
    {
        public string model = "claude-3-opus-20240229"; // Default, can be overridden by ShowrunnerManager settings
        public List<ClaudeMessage> messages = new List<ClaudeMessage>();
        public int max_tokens = 4096; // Increased from 2048 to allow for longer episode JSON
        public string system = string.Empty; // Field for the system prompt
        // Add other parameters like temperature if needed
    }

    [System.Serializable]
    public class ClaudeApiResponseContent
    {
        public string type = string.Empty;
        public string text = string.Empty; // This is where the generated episode JSON string will be
    }

    [System.Serializable]
    public class ClaudeApiResponse
    {
        public string id = string.Empty;
        public string type = string.Empty;
        public string role = string.Empty;
        public string model = string.Empty;
        public List<ClaudeApiResponseContent> content = new List<ClaudeApiResponseContent>();
        // Add usage, stop_reason, stop_sequence if needed for more detailed handling
    }

    public class ShowrunnerGeneratorLLM : MonoBehaviour
    {
        private const string DirectClaudeApiUrl = "https://api.anthropic.com/v1/messages";
        private const string ClaudeApiVersion = "2023-06-01";

        private async Task<string?> FetchExternalDataAsync(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                    webRequest.result == UnityWebRequest.Result.ProtocolError ||
                    webRequest.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogError("Error fetching external data");
                    return null;
                }
                else
                {
                    Debug.Log("Successfully fetched external data");
                    return webRequest.downloadHandler.text;
                }
            }
        }

        // Simplified ProcessShortcodesAsync
        private async Task<string?> ProcessShortcodesAsync(string prompt, bool extractOnly = false)
        {
            if (extractOnly)
            {
                var match = Regex.Match(prompt, @"\[externalData src=['\""]([^'\""]+)['\""]\]");
                if (match.Success)
                {
                    string targetUrl = match.Groups[1].Value;
                    if (targetUrl.Contains("elizaos.github.io"))
                    {
                        Debug.Log("Fetching external data");
                        string? externalDataJson = await FetchExternalDataAsync(targetUrl);
                        if (!string.IsNullOrEmpty(externalDataJson))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(externalDataJson);
                            sb.AppendLine();
                            return sb.ToString();
                        }
                        else
                        {
                            Debug.LogWarning("Failed to fetch external data");
                            return string.Empty; // Return empty string instead of null if fetch failed but was expected
                        }
                    }
                    else
                    {
                        Debug.LogWarning("External data URL not recognized");
                        return string.Empty; // Return empty string if URL not recognized
                    }
                }
                else
                {
                    Debug.Log("No external data tags found");
                    return string.Empty; // Return empty string if no tags found
                }
            }
            else
            {
                // In-place replacement logic is not the current strategy.
                // For now, just return the prompt as is if this mode is ever called.
                // A more robust implementation would parse and replace if needed.
                Debug.LogWarning("ProcessShortcodesAsync called with extractOnly=false");
                return prompt;
            }
        }

        // Helper to parse comma-separated strings into a list for X23 API requests
        private List<string>? ParseStringToList(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null; 
            return s.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        public async Task<ShowEpisode?> GenerateEpisode(ShowConfig config, ShowGenerator.ShowGeneratorApiKeys apiKeys, bool useWrapper, bool useCustomAffixes = false, string customPrefix = "", string customSuffix = "", ShowrunnerManager? manager = null)
        {
            if (config == null)
            {
                Debug.LogError("ShowConfig is null");
                return null;
            }
            if (apiKeys == null)
            {
                Debug.LogError("API Keys are not provided");
                return null;
            }

            string? apiUrl = useWrapper ? apiKeys.claudeWrapperUrl : DirectClaudeApiUrl;
            if (string.IsNullOrEmpty(apiUrl))
            {
                Debug.LogError("API URL is not configured");
                return null;
            }

            // Build the single prompt string as in the web app
            StringBuilder promptBuilder = new StringBuilder();

            // 0. Custom Prefix (if enabled and provided)
            if (useCustomAffixes && !string.IsNullOrEmpty(customPrefix))
            {
                promptBuilder.AppendLine(customPrefix);
                promptBuilder.AppendLine(); // Add a newline for separation
            }

            // 1. Main prompt/instructions
            string rawConfigEpisodePrompt;
            if (config.prompts != null && config.prompts.TryGetValue("episode", out string? episodePromptValueFromDict) && !string.IsNullOrEmpty(episodePromptValueFromDict))
            {
                rawConfigEpisodePrompt = episodePromptValueFromDict;
            }
            else
            {
                rawConfigEpisodePrompt = "Generate a new episode."; // Default value
                if (config.prompts == null)
                {
                    Debug.LogWarning("Config.prompts is null. Using default episode prompt.");
                }
                else if (!config.prompts.ContainsKey("episode"))
                {
                     Debug.LogWarning("Episode prompt key 'episode' not found in config.prompts. Using default episode prompt.");
                }
                else if (string.IsNullOrEmpty(config.prompts["episode"])) // Safe to access if Key is present
                {
                    Debug.LogWarning("Episode prompt is present but empty in config.prompts. Using default episode prompt.");
                }
            }
            promptBuilder.AppendLine(rawConfigEpisodePrompt);


            // 2. External data (if present from [externalData] shortcode)
            bool hasShortcodes = rawConfigEpisodePrompt.Contains("[externalData src='") || rawConfigEpisodePrompt.Contains("[externalData src=\"");
            if (hasShortcodes)
            {
                string? extractedExternalData = await ProcessShortcodesAsync(rawConfigEpisodePrompt, true);
                if (!string.IsNullOrEmpty(extractedExternalData))
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine($"EXTERNAL_DATA_CONTEXT:\n{extractedExternalData}");
                }
            }

            // 2.5. X23.ai API Data Injection (if manager and its settings are provided)
            if (manager != null && manager.useX23ApiData && manager.apiKeysConfig != null && !string.IsNullOrEmpty(manager.apiKeysConfig.x23ApiKey))
            {
                Debug.Log("[ShowrunnerGeneratorLLM] X23.ai data injection is enabled. Fetching data...");
                string x23DataJson = string.Empty;
                try
                {
                    List<string>? protocols = ParseStringToList(manager.x23ProtocolsToFilter);
                    List<string>? itemTypes = ParseStringToList(manager.x23ItemTypesToFilter);
                    long? x23Timestamp = manager.x23UnixTimestamp == 0 ? (long?)null : manager.x23UnixTimestamp;

                    switch (manager.x23ApiRequestType)
                    {
                        case X23ApiRequestType.SupportedProtocols:
                            x23DataJson = await X23ApiService.GetSupportedProtocolsAsync(manager.apiKeysConfig.x23ApiKey);
                            break;
                        case X23ApiRequestType.SupportedItemTypes:
                            x23DataJson = await X23ApiService.GetSupportedItemTypesAsync(manager.apiKeysConfig.x23ApiKey);
                            break;
                        case X23ApiRequestType.RecentFeed:
                            var recentReq = new RecentFeedRequest { Limit = manager.x23Limit, Protocols = protocols, ItemTypes = itemTypes, MaxUnixTimestamp = x23Timestamp };
                            x23DataJson = await X23ApiService.PostRecentFeedAsync(manager.apiKeysConfig.x23ApiKey, recentReq);
                            break;
                        case X23ApiRequestType.TopScoredFeed:
                            var topScoredReq = new TopScoredFeedRequest { Limit = manager.x23Limit, Protocols = protocols, ItemTypes = itemTypes, ScoreThreshold = manager.x23ScoreThresholdForTopScored, MaxUnixTimestamp = x23Timestamp };
                            x23DataJson = await X23ApiService.GetTopScoredFeedAsync(manager.apiKeysConfig.x23ApiKey, topScoredReq);
                            break;
                        case X23ApiRequestType.DigestFeed:
                            var digestReq = new DigestFeedRequest { Protocols = protocols, TimePeriod = manager.x23TimePeriodForDigest, UnixTimestamp = x23Timestamp }; // Limit is not applicable here by default
                            x23DataJson = await X23ApiService.PostDigestFeedAsync(manager.apiKeysConfig.x23ApiKey, digestReq);
                            break;
                        case X23ApiRequestType.KeywordSearch:
                            var keywordReq = new KeywordSearchRequest { Query = manager.x23SearchQuery, Limit = manager.x23Limit, Protocols = protocols, ItemTypes = itemTypes, ExactMatch = manager.x23ExactMatchForKeyword, SortByRelevance = manager.x23SortByRelevanceForKeyword };
                            x23DataJson = await X23ApiService.PostKeywordSearchAsync(manager.apiKeysConfig.x23ApiKey, keywordReq);
                            break;
                        case X23ApiRequestType.RagSearch:
                            var ragReq = new RagSearchRequest { Query = manager.x23SearchQuery, Limit = manager.x23Limit, Protocols = protocols, ItemTypes = itemTypes, SimilarityThreshold = manager.x23SimilarityThreshold };
                            x23DataJson = await X23ApiService.PostRagSearchAsync(manager.apiKeysConfig.x23ApiKey, ragReq);
                            break;
                        case X23ApiRequestType.HybridSearch:
                            var hybridReq = new HybridSearchRequest { Query = manager.x23SearchQuery, Limit = manager.x23Limit, Protocols = protocols, ItemTypes = itemTypes, SimilarityThreshold = manager.x23SimilarityThreshold };
                            x23DataJson = await X23ApiService.PostHybridSearchAsync(manager.apiKeysConfig.x23ApiKey, hybridReq);
                            break;
                        default:
                            Debug.LogWarning($"[ShowrunnerGeneratorLLM] X23.ai API request type '{manager.x23ApiRequestType}' is not handled.");
                            break;
                    }

                    if (!string.IsNullOrEmpty(x23DataJson))
                    {
                        promptBuilder.AppendLine();
                        promptBuilder.AppendLine($"X23_API_DATA_CONTEXT:\n{x23DataJson}");
                        Debug.Log("[ShowrunnerGeneratorLLM] Successfully fetched and appended X23.ai data to prompt.");
                    }
                    else
                    {
                        Debug.LogWarning("[ShowrunnerGeneratorLLM] X23.ai data was empty or not fetched for the selected type.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ShowrunnerGeneratorLLM] Error fetching or processing X23.ai data: {e.Message}");
                    // Decide if you want to stop or continue without this data. For now, we continue.
                }
            }

            // 3. Pilot episode JSON (if present)
            if (config.pilot != null)
            {
                try
                {
                    ShowEpisode? pilotCopy = JsonConvert.DeserializeObject<ShowEpisode>(JsonConvert.SerializeObject(config.pilot));
                    if (pilotCopy != null && pilotCopy.scenes != null) // Added null check for pilotCopy
                    {
                        foreach (var scene in pilotCopy.scenes)
                        {
                            scene.description = "<example scene description>";
                            if (scene.dialogue != null)
                            {
                                foreach (var dialogueLine in scene.dialogue)
                                {
                                    if (dialogueLine != null)
                                    {
                                        dialogueLine.line = "<example dialogue line content>";
                                    }
                                }
                            }
                        }
                    }
                    string pilotJsonString = JsonConvert.SerializeObject(pilotCopy, Formatting.Indented);
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine($"PILOT EPISODE JSON (for structural reference ONLY, actual dialogue content AND DESCRIPTIONS replaced):\n{pilotJsonString}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error adding modified pilot episode: {e.Message}");
                }
            }

            // 4. Config JSON (without pilot/prompts)
            ShowConfig? configForPayload = JsonConvert.DeserializeObject<ShowConfig>(JsonConvert.SerializeObject(config));
            if (configForPayload != null)
            {
                configForPayload.pilot = null;
                configForPayload.prompts = null;
                string configJsonString = JsonConvert.SerializeObject(configForPayload, Formatting.Indented);
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"CONFIG:\n{configJsonString}");
            }


            // 5. Final instruction
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Now, generate the episode. Remember, I need ONLY the JSON for the new ShowEpisode object. This JSON should represent a single episode and must not contain a list of other episodes within its own structure. Ensure it adheres to the ShowEpisode class definition and all instructions provided in the preceding messages.");

            // 6. Custom Suffix (if enabled and provided)
            if (useCustomAffixes && !string.IsNullOrEmpty(customSuffix))
            {
                promptBuilder.AppendLine(); // Add a newline for separation
                promptBuilder.AppendLine(customSuffix);
            }

            // Build the messages array with a single message
            var messages = new List<ClaudeMessage>
            {
                new ClaudeMessage { role = "user", content = promptBuilder.ToString() }
            };

            var payload = new ClaudeApiRequestPayload
            {
                model = !string.IsNullOrEmpty(apiKeys.claudeModelName) ? apiKeys.claudeModelName : "claude-3-opus-20240229",
                messages = messages,
                max_tokens = apiKeys.claudeMaxTokens > 0 ? apiKeys.claudeMaxTokens : 4096
                // system = null // REMOVE system field
            };

            // Remove the system field by not setting it at all
            // Serialize payload without the system field
            var payloadDict = new Dictionary<string, object>
            {
                { "model", payload.model },
                { "messages", payload.messages },
                { "max_tokens", payload.max_tokens }
            };
            string payloadJson = JsonConvert.SerializeObject(payloadDict);

            // Save payload for debugging
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string directoryPath = System.IO.Path.Combine(Application.dataPath, "Resources", "LLMRequests");
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }
            string filePath = System.IO.Path.Combine(directoryPath, $"LLMPayload_{timestamp}.json");
            System.IO.File.WriteAllText(filePath, payloadJson);

            using (UnityWebRequest req = new UnityWebRequest(apiUrl, "POST"))
            {
                req.timeout = 600;
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payloadJson);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                if (!useWrapper)
                {
                    if (string.IsNullOrEmpty(apiKeys.llmApiKey))
                    {
                        Debug.LogError("LLM API Key is missing");
                        return null;
                    }
                    req.SetRequestHeader("x-api-key", apiKeys.llmApiKey);
                    req.SetRequestHeader("anthropic-version", ClaudeApiVersion);
                }

                Debug.Log("Sending request to LLM API");
                var operation = req.SendWebRequest();
                float startTime = Time.realtimeSinceStartup;
                int iterationCount = 0;
                while (!operation.isDone)
                {
                    iterationCount++;
                    if (iterationCount % 300 == 0)
                    {
                        Debug.Log($"Waiting for response... Elapsed: {(Time.realtimeSinceStartup - startTime).ToString("F2")}s");
                    }
                    await Task.Yield();
                }

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string responseText = req.downloadHandler.text;
                    try
                    {
                        // Parse the LLM response as a ClaudeApiResponse
                        var apiResponse = JsonConvert.DeserializeObject<ClaudeApiResponse>(responseText);
                        if (apiResponse?.content != null && apiResponse.content.Count > 0 && apiResponse.content[0] != null)
                        {
                            string episodeJson = apiResponse.content[0].text;
                            var episode = JsonConvert.DeserializeObject<ShowEpisode>(episodeJson);
                            return episode;
                        }
                        else
                        {
                            Debug.LogError("API response content is null, empty, or first element is null");
                            return null;
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Debug.LogError($"JSON parsing error: {jsonEx.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing response: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"API call failed: {req.responseCode}");
                    return null;
                }
            }
        }

        // New method to test the Claude endpoint with a simple "Hi" message
        public async Task<string> TestClaudeEndpointAsync(string wrapperUrlFromManager, string llmApiKey, bool useWrapper, ShowGenerator.ShowGeneratorApiKeys apiKeys)
        {
            string? effectiveApiUrl;
            if (useWrapper)
            {
                effectiveApiUrl = wrapperUrlFromManager;
                if (string.IsNullOrEmpty(effectiveApiUrl))
                {
                    Debug.LogError("Wrapper URL is not configured");
                    return "Error: Wrapper URL not configured";
                }
            }
            else
            {
                effectiveApiUrl = DirectClaudeApiUrl; // Use internal constant for direct calls
            }
            
            var messages = new List<ClaudeMessage> { new ClaudeMessage { role = "user", content = "Hi" } };
            // For testing, always use a small max_tokens, but respect model choice from config if available
            var requestPayload = new ClaudeApiRequestPayload { messages = messages, max_tokens = 25 }; // Minimal tokens for a test response
            
            // Override model from ApiKeysConfig if provided for the test
            if (apiKeys != null && !string.IsNullOrEmpty(apiKeys.claudeModelName))
            {
                requestPayload.model = apiKeys.claudeModelName;
            }
            // max_tokens is kept low for testing regardless of config for this specific test method.

            string payloadJson = JsonConvert.SerializeObject(requestPayload);

            Debug.Log($"[LLM Test] Sending test request to: {effectiveApiUrl} using model {requestPayload.model} with payload: {payloadJson}");

            using (UnityWebRequest req = new UnityWebRequest(effectiveApiUrl, "POST"))
            {
                req.timeout = 60; // 60 seconds timeout for the test request
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payloadJson);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                if (!useWrapper)
                {
                    if (string.IsNullOrEmpty(llmApiKey))
                    {
                        Debug.LogError("LLM API Key is missing");
                        return "Error: LLM API Key missing";
                    }
                    req.SetRequestHeader("x-api-key", llmApiKey);
                    req.SetRequestHeader("anthropic-version", ClaudeApiVersion);
                }

                var operation = req.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string responseText = req.downloadHandler.text;
                    Debug.Log($"[LLM Test] Success! Response: {responseText}");
                    return responseText;
                }
                else
                {
                    Debug.LogError($"[LLM Test] Error: {req.error}");
                    Debug.LogError($"[LLM Test] Status Code: {req.responseCode}");
                    string responseBody = req.downloadHandler?.text ?? "N/A";
                    Debug.LogError($"[LLM Test] Body: {responseBody}");
                    return $"Error: {req.responseCode} - {req.error}. Body: {responseBody}";
                }
            }
        }
    }

    // Helper class for parsing potential error responses from the wrapper
    [System.Serializable]
    public class ErrorResponseWrapper
    {
        public string error = string.Empty;
        public string type = string.Empty;
    }
} 