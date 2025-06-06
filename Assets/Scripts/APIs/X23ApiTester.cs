#nullable enable
using UnityEngine;
using TMPro; // Make sure TextMeshPro is imported and used in your project
using System.Threading.Tasks; // Required for Task
using System.Collections.Generic; // Required for List<string> in request
using ShowGenerator; // Required for ShowGeneratorApiKeys
using System.Linq; // Required for parsing comma-separated strings

/// <summary>
/// A MonoBehaviour class to test the X23ApiService from the Unity Editor.
/// Attach this to a GameObject in your scene.
/// Assign the ShowGeneratorApiKeys ScriptableObject.
/// Link a UI Button to the TestApiCall method and a TextMeshProUGUI to the outputText field.
/// You can also use the button in the Inspector if the X23ApiTesterEditor script is present.
/// </summary>
public class X23ApiTester : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Assign your ShowGeneratorApiKeys ScriptableObject here.")]
    public ShowGeneratorApiKeys? apiKeysSO; // Assign your ScriptableObject asset here

    [Tooltip("Select the type of API request to make.")]
    public X23ApiRequestType apiRequestType = X23ApiRequestType.SupportedProtocols;

    [Header("General Parameters (Fill as needed for selected request type)")]
    [Tooltip("Query string for search requests.")]
    public string searchQuery = "optimism grants";
    
    [Tooltip("Maximum number of items to fetch. Default varies by endpoint.")]
    public int limit = 10;

    [Tooltip("Comma-separated list of protocols to filter by (e.g., aave,optimism). Leave empty for all.")]
    public string protocolsToFilter = "";

    [Tooltip("Comma-separated list of item types to filter by (e.g., discussion,snapshot). Leave empty for all.")]
    public string itemTypesToFilter = "";

    [Header("Search Specific Parameters")]
    [Tooltip("Similarity threshold for RAG/Hybrid search (0.0 to 1.0). Default: 0.4")]
    [Range(0f, 1f)]
    public float similarityThreshold = 0.4f;

    [Tooltip("For KeywordSearch: Exact match? Default: false")]
    public bool exactMatchForKeyword = false;

    [Tooltip("For KeywordSearch: Sort by relevance? Default: true")]
    public bool sortByRelevanceForKeyword = true;

    [Header("Feed Specific Parameters")]
    [Tooltip("For RecentFeed/TopScoredFeed/DigestFeed: Unix Timestamp (seconds). 0 for default/no limit/current time if API supports.")]
    public long unixTimestamp = 0; // Shared by RecentFeed, TopScoredFeed, DigestFeed

    [Tooltip("For TopScoredFeed: Minimum score threshold. Default: 3000")]
    public double scoreThresholdForTopScored = 3000;

    [Tooltip("For DigestFeed: Time period ('daily', 'weekly', 'monthly'). Default: 'daily'")]
    public string timePeriodForDigest = "daily";

    [Header("UI References")]
    [Tooltip("Assign a TextMeshProUGUI component here to display the API response.")]
    public TextMeshProUGUI? outputText;

    private List<string>? ParseStringToList(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null; // Return null to be ignored by NullValueHandling.Ignore
        return s.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
    }

    /// <summary>
    /// Public method to be called by a UI Button's OnClick event or an Inspector button.
    /// Initiates the API call test.
    /// </summary>
    public async void TestApiCall()
    {
        if (apiKeysSO == null)
        {
            Debug.LogError("ShowGeneratorApiKeys ScriptableObject is not assigned in the Inspector on X23ApiTester.", this);
            if(outputText != null) outputText.text = "Error: ShowGeneratorApiKeys SO not assigned.";
            return;
        }

        string currentApiKey = apiKeysSO.x23ApiKey;
        if (string.IsNullOrEmpty(currentApiKey))
        {
            Debug.LogError("x23ApiKey is not set in the assigned ShowGeneratorApiKeys ScriptableObject.", this);
            if(outputText != null) outputText.text = "Error: x23ApiKey not set in API Keys SO.";
            return;
        }

        if (outputText == null)
        {
            Debug.LogWarning("OutputText field is not assigned in the Inspector. API results will only be logged to the console.", this);
        }

        if (outputText != null) 
        {
            outputText.text = $"Attempting API call: {apiRequestType}...\n"; // Clear previous output
        }
        else
        {
            Debug.Log($"Attempting API call: {apiRequestType} (output to console only)...");
        }

        string responseJson = string.Empty;
        object? requestPayloadObject = null; // To hold the DTO for logging

        try
        {
            List<string>? protocols = ParseStringToList(protocolsToFilter);
            List<string>? itemTypes = ParseStringToList(itemTypesToFilter);
            long? currentUnixTimestamp = this.unixTimestamp == 0 ? (long?)null : this.unixTimestamp;

            switch (apiRequestType)
            {
                case X23ApiRequestType.SupportedProtocols:
                    LogMessage("Sending GET request for SupportedProtocols...");
                    responseJson = await X23ApiService.GetSupportedProtocolsAsync(currentApiKey);
                    break;
                case X23ApiRequestType.SupportedItemTypes:
                    LogMessage("Sending GET request for SupportedItemTypes...");
                    responseJson = await X23ApiService.GetSupportedItemTypesAsync(currentApiKey);
                    break;
                case X23ApiRequestType.RecentFeed:
                    var recentFeedReq = new RecentFeedRequest 
                    {
                        Limit = this.limit,
                        Protocols = protocols,
                        ItemTypes = itemTypes,
                        MaxUnixTimestamp = currentUnixTimestamp
                    };
                    requestPayloadObject = recentFeedReq;
                    LogMessage("Sending POST request for RecentFeed...");
                    responseJson = await X23ApiService.PostRecentFeedAsync(currentApiKey, recentFeedReq);
                    break;
                case X23ApiRequestType.TopScoredFeed:
                    var topScoredReq = new TopScoredFeedRequest 
                    {
                        Limit = this.limit,
                        Protocols = protocols,
                        ItemTypes = itemTypes,
                        ScoreThreshold = this.scoreThresholdForTopScored,
                        MaxUnixTimestamp = currentUnixTimestamp
                    };
                    requestPayloadObject = topScoredReq;
                    LogMessage("Sending POST request for TopScoredFeed (using scoreThreshold)...");
                    responseJson = await X23ApiService.GetTopScoredFeedAsync(currentApiKey, topScoredReq);
                    break;
                case X23ApiRequestType.DigestFeed:
                    var digestReq = new DigestFeedRequest
                    {
                        Protocols = protocols,
                        TimePeriod = this.timePeriodForDigest,
                        UnixTimestamp = currentUnixTimestamp
                    };
                    requestPayloadObject = digestReq;
                    LogMessage("Sending POST request for DigestFeed...");
                    responseJson = await X23ApiService.PostDigestFeedAsync(currentApiKey, digestReq);
                    break;
                case X23ApiRequestType.KeywordSearch:
                    if (string.IsNullOrEmpty(searchQuery)) { LogMessage("Error: Search Query is required for Keyword Search."); return; }
                    var keywordReq = new KeywordSearchRequest 
                    {
                        Query = this.searchQuery,
                        Limit = this.limit,
                        Protocols = protocols,
                        ItemTypes = itemTypes,
                        ExactMatch = this.exactMatchForKeyword,
                        SortByRelevance = this.sortByRelevanceForKeyword
                    };
                    requestPayloadObject = keywordReq;
                    LogMessage("Sending POST request for KeywordSearch...");
                    responseJson = await X23ApiService.PostKeywordSearchAsync(currentApiKey, keywordReq);
                    break;
                case X23ApiRequestType.RagSearch:
                    if (string.IsNullOrEmpty(searchQuery)) { LogMessage("Error: Search Query is required for RAG Search."); return; }
                    var ragReq = new RagSearchRequest 
                    {
                        Query = this.searchQuery, 
                        Limit = this.limit, 
                        Protocols = protocols, 
                        ItemTypes = itemTypes,
                        SimilarityThreshold = this.similarityThreshold
                    };
                    requestPayloadObject = ragReq;
                    LogMessage("Sending POST request for RagSearch...");
                    responseJson = await X23ApiService.PostRagSearchAsync(currentApiKey, ragReq);
                    break;
                case X23ApiRequestType.HybridSearch:
                    if (string.IsNullOrEmpty(searchQuery)) { LogMessage("Error: Search Query is required for Hybrid Search."); return; }
                    var hybridReq = new HybridSearchRequest 
                    {
                        Query = this.searchQuery, 
                        Limit = this.limit, 
                        Protocols = protocols, 
                        ItemTypes = itemTypes, 
                        SimilarityThreshold = this.similarityThreshold
                    };
                    requestPayloadObject = hybridReq;
                    LogMessage("Sending POST request for HybridSearch...");
                    responseJson = await X23ApiService.PostHybridSearchAsync(currentApiKey, hybridReq);
                    break;
                default:
                    LogMessage($"Error: API Request Type {apiRequestType} not implemented in tester.");
                    return;
            }

            if (requestPayloadObject != null)
            {
                string requestPayloadString = Newtonsoft.Json.JsonConvert.SerializeObject(requestPayloadObject, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                });
                LogMessage($"Request Payload:\n{requestPayloadString}");
            }

            LogMessage($"\nAPI Response Success ({apiRequestType}):\n" + responseJson);
        }
        catch (System.ArgumentException ex) // Catch argument exceptions specifically for better feedback
        {
             LogMessage($"Argument error for {apiRequestType}: {ex.ParamName} - {ex.Message}");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            LogMessage($"Request error for {apiRequestType}: {ex.Message} (Possibly check endpoint spelling or API tier access)");
        }
        catch (Newtonsoft.Json.JsonException ex)
        {
            LogMessage($"JSON processing error for {apiRequestType}: {ex.Message}");
        }
        catch (System.Exception ex)
        { 
            LogMessage($"An unexpected error occurred with {apiRequestType}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        if (outputText != null)
        {
            outputText.text += message + "\n";
        }
        Debug.Log(message); // Always log to console
    }
} 