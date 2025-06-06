#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization; // Required for CamelCasePropertyNamesContractResolver

public enum X23ApiRequestType
{
    SupportedProtocols,  // GET
    SupportedItemTypes,  // GET
    RecentFeed,          // POST
    TopScoredFeed,       // POST
    DigestFeed,          // POST (New)
    KeywordSearch,       // POST
    RagSearch,           // POST
    HybridSearch         // POST
}

/// <summary>
/// Represents the request payload for the x23.ai API's topScoredFeed endpoint.
/// Based on the specific documentation snippet for "Retrieve top scored items".
/// </summary>
public class TopScoredFeedRequest
{
    [JsonProperty("maxUnixTimestamp", NullValueHandling = NullValueHandling.Ignore)]
    public long? MaxUnixTimestamp { get; set; } // number <= 14 days in the past as a unix timestamp

    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; } // Default: []

    [JsonProperty("itemTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ItemTypes { get; set; } // Default: []

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public int? Limit { get; set; } // Default: 100

    /// <summary>
    /// Gets or sets the minimum score threshold for items to be included.
    /// Default is 3000, range 0 to ~5000 according to specific docs.
    /// </summary>
    [JsonProperty("scoreThreshold", NullValueHandling = NullValueHandling.Ignore)]
    public double? ScoreThreshold { get; set; } // Default: 3000

    /// <summary>
    /// Initializes a new instance of the <see cref="TopScoredFeedRequest"/> class.
    /// </summary>
    public TopScoredFeedRequest()
    {
        // Properties are initialized with JsonProperty(NullValueHandling.Ignore)
        // so they won't be serialized if null. Default values mentioned in API docs
        // will be handled by the API if these fields are omitted.
    }
}

/// <summary>
/// Request payload for retrieving recent feed items.
/// </summary>
public class RecentFeedRequest
{
    [JsonProperty("maxUnixTimestamp", NullValueHandling = NullValueHandling.Ignore)]
    public long? MaxUnixTimestamp { get; set; } // number <= 14 days in the past as a unix timestamp

    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; }

    [JsonProperty("itemTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ItemTypes { get; set; }

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public int? Limit { get; set; } // Default: 100
}

/// <summary>
/// Request payload for retrieving a digest feed.
/// </summary>
public class DigestFeedRequest
{
    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; } // Default: []

    [JsonProperty("timePeriod")] // Required, enum: "daily" "weekly" "monthly"
    public string TimePeriod { get; set; } = "daily"; // Default to daily, user must ensure valid string

    [JsonProperty("unixTimestamp", NullValueHandling = NullValueHandling.Ignore)]
    public long? UnixTimestamp { get; set; } // Default: null (current time on server)
}

/// <summary>
/// Request payload for performing a keyword search.
/// </summary>
public class KeywordSearchRequest
{
    [JsonProperty("query")] // Required
    public string Query { get; set; } = string.Empty;

    [JsonProperty("sortByRelevance", NullValueHandling = NullValueHandling.Ignore)]
    public bool? SortByRelevance { get; set; } // Default: true

    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; }

    [JsonProperty("itemTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ItemTypes { get; set; }

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public int? Limit { get; set; } // Default: 20 (as per new docs)

    [JsonProperty("exactMatch", NullValueHandling = NullValueHandling.Ignore)]
    public bool? ExactMatch { get; set; } // Default: false (this was from a previous understanding, might not be in this specific doc snippet but retaining for now)
}

/// <summary>
/// Request payload for performing a RAG/vector search.
/// </summary>
public class RagSearchRequest
{
    [JsonProperty("query")] // Required
    public string Query { get; set; } = string.Empty;

    [JsonProperty("similarityThreshold", NullValueHandling = NullValueHandling.Ignore)]
    public double? SimilarityThreshold { get; set; } // Default: 0.4, between 0 and 1

    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; }

    [JsonProperty("itemTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ItemTypes { get; set; }

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public int? Limit { get; set; } // Default: 5
}

/// <summary>
/// Request payload for performing a hybrid search.
/// </summary>
public class HybridSearchRequest
{
    [JsonProperty("query")] // Required
    public string Query { get; set; } = string.Empty;

    [JsonProperty("protocols", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? Protocols { get; set; }

    [JsonProperty("itemTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? ItemTypes { get; set; }

    [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
    public int? Limit { get; set; } // Default: 5

    [JsonProperty("similarityThreshold", NullValueHandling = NullValueHandling.Ignore)]
    public double? SimilarityThreshold { get; set; } // Default: 0.4, between 0 and 1
}

/// <summary>
/// Provides methods for interacting with the x23.ai API.
/// </summary>
public static class X23ApiService
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string BaseUrl = "https://api.x23.ai/v1/";
    private const string ApiKeyHeaderName = "x-api-key";

    private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore // Global setting for ignoring nulls
    };

    /// <summary>
    /// Retrieves the top scored feed items from the x23.ai API.
    /// </summary>
    /// <param name="apiKey">Your x23.ai API key.</param>
    /// <param name="requestPayload">The request payload containing filter criteria.</param>
    /// <returns>The API response as a JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if apiKey or requestPayload is null.</exception>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    /// <exception cref="Newtonsoft.Json.JsonException">Thrown if JSON serialization fails.</exception>
    public static async Task<string> GetTopScoredFeedAsync(string apiKey, TopScoredFeedRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
        }
        // Request payload can be null or empty for this specific call if we want to rely on defaults
        // but the parameter itself shouldn't be null for the method signature.
        // if (requestPayload == null) 
        // {
        //     throw new ArgumentNullException(nameof(requestPayload), "Request payload cannot be null.");
        // }

        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(BaseUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload ?? new TopScoredFeedRequest(), jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("topScoreFeed", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Retrieves the list of supported protocols from the x23.ai API.
    /// This is a GET request and does not require a request body.
    /// </summary>
    /// <param name="apiKey">Your x23.ai API key.</param>
    /// <returns>The API response as a JSON string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if apiKey is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    public static async Task<string> GetSupportedProtocolsAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
        }

        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(BaseUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        HttpResponseMessage response = await httpClient.GetAsync("supportedProtocols");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Retrieves the list of supported item types from the x23.ai API.
    /// </summary>
    public static async Task<string> GetSupportedItemTypesAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
        }
        // Similar setup as GetSupportedProtocolsAsync for headers and base URL
        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        HttpResponseMessage response = await httpClient.GetAsync("supportedItemTypes");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Retrieves recent feed items from the x23.ai API.
    /// </summary>
    public static async Task<string> PostRecentFeedAsync(string apiKey, RecentFeedRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (requestPayload == null) throw new ArgumentNullException(nameof(requestPayload));
        
        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload, jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("recentFeed", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Performs a keyword search using the x23.ai API.
    /// </summary>
    public static async Task<string> PostKeywordSearchAsync(string apiKey, KeywordSearchRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (requestPayload == null) throw new ArgumentNullException(nameof(requestPayload));
        if (string.IsNullOrEmpty(requestPayload.Query)) throw new ArgumentException("Query cannot be empty for KeywordSearch.", nameof(requestPayload.Query));

        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload, jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("keywordSearch", content); // Assuming endpoint is keywordSearch
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Performs a RAG/vector search using the x23.ai API.
    /// </summary>
    public static async Task<string> PostRagSearchAsync(string apiKey, RagSearchRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (requestPayload == null) throw new ArgumentNullException(nameof(requestPayload));
        if (string.IsNullOrEmpty(requestPayload.Query)) throw new ArgumentException("Query cannot be empty for RagSearch.", nameof(requestPayload.Query));
        
        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload, jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("ragSearch", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Performs a hybrid search using the x23.ai API.
    /// </summary>
    public static async Task<string> PostHybridSearchAsync(string apiKey, HybridSearchRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (requestPayload == null) throw new ArgumentNullException(nameof(requestPayload));
        if (string.IsNullOrEmpty(requestPayload.Query)) throw new ArgumentException("Query cannot be empty for HybridSearch.", nameof(requestPayload.Query));

        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload, jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("hybridSearch", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Retrieves a digest feed from the x23.ai API.
    /// </summary>
    public static async Task<string> PostDigestFeedAsync(string apiKey, DigestFeedRequest requestPayload)
    {
        if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
        if (requestPayload == null) throw new ArgumentNullException(nameof(requestPayload));
        if (string.IsNullOrEmpty(requestPayload.TimePeriod) || 
            !(requestPayload.TimePeriod == "daily" || requestPayload.TimePeriod == "weekly" || requestPayload.TimePeriod == "monthly"))
        {
            throw new ArgumentException("TimePeriod must be 'daily', 'weekly', or 'monthly'.", nameof(requestPayload.TimePeriod));
        }

        if (httpClient.BaseAddress == null) httpClient.BaseAddress = new Uri(BaseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Remove(ApiKeyHeaderName);
        httpClient.DefaultRequestHeaders.Add(ApiKeyHeaderName, apiKey);

        string jsonPayload = JsonConvert.SerializeObject(requestPayload, jsonSerializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.PostAsync("digestFeed", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Example usage of the GetTopScoredFeedAsync method.
    /// This method is for demonstration. In a Unity context, you would typically call
    /// GetTopScoredFeedAsync from a MonoBehaviour and update UI elements with the result.
    /// </summary>
    /// <param name="outputAction">Action to handle the output string (e.g., update a TextMeshProUGUI).</param>
    public static async Task ExampleUsageAsync(Action<string> outputAction)
    {
        string apiKey = "YOUR_API_KEY"; // Replace "YOUR_API_KEY" with your actual API key

        var request = new TopScoredFeedRequest
        {
            Protocols = new List<string> { "aave", "optimism" },
            Limit = 10,
            ScoreThreshold = 500,
            MaxUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 24 * 7 * 3600,
        };

        try
        {
            string requestPayloadString = JsonConvert.SerializeObject(request, Formatting.Indented, jsonSerializerSettings);
            outputAction?.Invoke($"Sending request to {BaseUrl}topScoredFeed...\nRequest Payload:\n{requestPayloadString}");
            
            string responseJson = await GetTopScoredFeedAsync(apiKey, request);
            outputAction?.Invoke("\nAPI Response:\n" + responseJson);
        }
        catch (ArgumentNullException ex)
        {
            outputAction?.Invoke($"Argument error: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            outputAction?.Invoke($"Request error: {ex.Message}");
        }
        catch (Newtonsoft.Json.JsonException ex) // Ensure this is Newtonsoft.Json.JsonException
        {
            outputAction?.Invoke($"JSON processing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            outputAction?.Invoke($"An unexpected error occurred: {ex.Message}");
        }
    }
}

// Example of how you might call this from a Unity MonoBehaviour:
// using UnityEngine;
// using TMPro; // If you're using TextMeshPro
// using System.Threading.Tasks; // Required for Task

// public class ApiCaller : MonoBehaviour
// {
//     public TextMeshProUGUI outputText;

//     void Start()
//     {
//         // It's generally better to not run async void directly in Start unless careful with context.
//         // Consider using a Unity-specific async/await library or managing the task.
//         CallApi();
//     }

//     private async void CallApi() // Or use Task and manage it
//     {
//         if (outputText == null) 
//         {
//             Debug.LogError("OutputText is not assigned in the inspector!");
//             return;
//         }
//         await X23ApiService.ExampleUsageAsync(message => {
//             // Ensure UI updates are on the main thread if called from a background thread
//             // (HttpClient operations can complete on a background thread).
//             // For TextMeshProUGUI, direct assignment is usually fine from async methods started on main thread.
//             outputText.text += message + "\n"; 
//         });
//     }
// } 