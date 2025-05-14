using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class HeadlineLoader : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headlinesText;

    [Header("Configuration")]
    [SerializeField] private string newsUrl = "https://elizaos.github.io/knowledge/ai-news/elizaos/json/daily.json";
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private string separator = "     "; // Extra spacing between headlines

    private void Start()
    {
        if (headlinesText == null)
        {
            Debug.LogError("TextMeshProUGUI component not assigned! Please assign it in the inspector.");
            return;
        }
        if (autoLoadOnStart)
        {
            LoadHeadlines();
        }
    }

    public void LoadHeadlines()
    {
        StartCoroutine(FetchAndParseHeadlines());
    }

    private IEnumerator FetchAndParseHeadlines()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(newsUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonContent = request.downloadHandler.text;
                ParseAndDisplayHeadlines(jsonContent);
            }
            else
            {
                Debug.LogError($"Error fetching headlines: {request.error}");
                if (headlinesText != null)
                {
                    headlinesText.text = "Error loading headlines. Please try again later.";
                }
            }
        }
    }

    private void ParseAndDisplayHeadlines(string jsonContent)
    {
        try
        {
            JObject jsonObject = JObject.Parse(jsonContent);
            JArray categories = (JArray)jsonObject["categories"];
            if (categories == null)
            {
                Debug.LogError("No categories found in JSON");
                return;
            }

            List<string> headlines = new List<string>();
            foreach (JObject category in categories)
            {
                JArray content = (JArray)category["content"];
                if (content != null)
                {
                    foreach (JObject item in content)
                    {
                        string text = (string)item["text"];
                        if (!string.IsNullOrEmpty(text))
                        {
                            // Remove all line breaks and HTML breaks, then trim
                            text = text.Replace("\n", " ").Replace("\r", " ").Replace("<br>", " ").Replace("<br/>", " ");
                            text = text.Trim();
                            headlines.Add(text);
                        }
                    }
                }
            }

            // Join all headlines with the separator, remove any line breaks from the final string
            string allHeadlines = string.Join(separator, headlines);
            allHeadlines = allHeadlines.Replace("\n", " ").Replace("\r", " ").Trim();
            allHeadlines += separator + separator;
            if (headlinesText != null)
            {
                headlinesText.text = allHeadlines;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing headlines: {e.Message}");
            if (headlinesText != null)
            {
                headlinesText.text = "Error parsing headlines. Please try again later.";
            }
        }
    }
} 