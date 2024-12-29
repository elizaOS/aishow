using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;
using System;
using System.Linq; // For LINQ methods
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class EventProcessor : MonoBehaviour
{
    private ScenePreparationManager scenePreparationManager;
    
    private void Awake()
    {
        scenePreparationManager = GetComponent<ScenePreparationManager>();
    }

    public void ProcessEvent(EventData eventData)
    {
        switch (eventData.type)
        {
            case "episodeGenerated":
            Debug.Log("Episode generated event received, waiting to prepare scene...");
            StartCoroutine(HandleEpisodeAndPrepareScene(eventData));
            HandlePrepareScene(eventData);
            break;

            case "prepareScene":
                HandlePrepareScene(eventData);
                break;

            case "speak":
                HandleSpeak(eventData);
                break;

            default:
                Debug.LogWarning($"Unknown event type: {eventData.type}");
                break;
        }
    }

    private IEnumerator HandleEpisodeAndPrepareScene(EventData eventData)
{
    // Simulate any processing needed for episodeGenerated
    yield return new WaitForSeconds(1f);
    Debug.Log("Episode generated processing complete. Now preparing scene.");
}

    private void HandlePrepareScene(EventData eventData)
    {
        Debug.Log($"Handling prepareScene event for location: {eventData.location}");
        scenePreparationManager.RequestScenePreparation(eventData.location);
    }

    private void HandleSpeak(EventData eventData)
    {
        try
        {
            var payload = new JObject
            {
                ["dialogue"] = new JObject
                {
                    ["actor"] = eventData.actor,
                    ["line"] = eventData.line,
                    ["action"] = eventData.action ?? "normal"
                }
            };

            SpeakPayloadManager.Instance.HandleSpeakPayload(payload.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling speak event: {ex.Message}");
        }
    }
}
