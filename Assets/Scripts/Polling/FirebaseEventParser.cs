// FirebaseEventParser.cs
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using UnityEngine;

public class FirebaseEventParser
{
    public static ParsedEvents ParseEvents(string jsonResponse)
    {
        var parsedEvents = new ParsedEvents();
        
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            Debug.Log("JSON response is null or empty. No data to process.");
            return parsedEvents;
        }

        try
        {
            var jsonObject = JObject.Parse(jsonResponse);
            parsedEvents.Events = jsonObject
                .Properties()
                .Select(ParseEventProperty)
                .Where(e => e != null)
                .OrderBy(e => e.timestamp)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing Firebase events: {ex.Message}");
        }

        return parsedEvents;
    }

    private static EventData ParseEventProperty(JProperty item)
    {
        try
        {
            var timestampToken = item.Value["timestamp"];
            var typeToken = item.Value["type"];
            var dataToken = item.Value["data"];

            if (timestampToken == null || typeToken == null)
            {
                Debug.LogWarning($"Skipping event with missing required fields. Event key: {item.Name}");
                return null;
            }

            var eventData = new EventData
            {
                timestamp = Convert.ToInt64(timestampToken),
                type = typeToken.ToString()
            };

            ParseEventSpecificData(eventData, dataToken);
            return eventData;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error parsing event at key {item.Name}: {ex.Message}");
            return null;
        }
    }

    private static void ParseEventSpecificData(EventData eventData, JToken dataToken)
    {
        if (dataToken == null) return;

        switch (eventData.type)
        {
            case "prepareScene":
                eventData.location = dataToken["location"]?.ToString();
                break;

            case "speak":
                eventData.actor = dataToken["actor"]?.ToString();
                eventData.line = dataToken["line"]?.ToString();
                eventData.action = dataToken["action"]?.ToString();
                break;
        }
    }
}