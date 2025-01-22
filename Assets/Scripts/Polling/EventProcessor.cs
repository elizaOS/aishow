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
            // Process the action separately
            string processedAction = HandleSpeakAction(eventData.actor, eventData.action);

            var payload = new JObject
            {
                ["dialogue"] = new JObject
                {
                    ["actor"] = eventData.actor,
                    ["line"] = eventData.line,
                    ["action"] = processedAction
                }
            };

            SpeakPayloadManager.Instance.HandleSpeakPayload(payload.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling speak event: {ex.Message}");
        }
    }


   private string HandleSpeakAction(string actor, string action)
    {
        if (string.IsNullOrEmpty(action))
        {
            Debug.Log($"No action specified for speak event by {actor}. Defaulting to 'normal'.");
            return "normal";
        }

        // Locate the GameObject associated with the actor
        var actorGameObject = FindCharacterByName(actor);
        if (actorGameObject == null)
        {
            Debug.LogWarning($"Actor '{actor}' not found in the scene. Cannot trigger action '{action}'.");
            return "normal";
        }

        // Check for specific actions
        switch (action)
        {
            case "wave":
                Debug.Log($"Actor '{actor}' is performing action 'wave'.");
                // Optionally trigger animations or effects for 'wave'
                break;

            case "point":
                Debug.Log($"Actor '{actor}' is performing action 'point'.");
                // Optionally trigger animations or effects for 'point'
                break;
            
            case "excited":
                Debug.Log($"Actor '{actor}' is performing action 'excited'.");
                TriggerExcitedEffect(actorGameObject); // Trigger glitch for the specific actor
                break;
            
            case "happy":
                Debug.Log($"Actor '{actor}' is performing action 'happy'.");
                TriggerHappyEffect(actorGameObject); // Trigger happy for the specific actor
                break;

            case "concerned":
                Debug.Log($"Actor '{actor}' is performing action 'concerned'.");
                TriggerConcernedEffect(actorGameObject); // Trigger concerned for the specific actor
                break;

            case "spazz":
                Debug.Log($"Actor '{actor}' is performing action 'spazz'. Triggering glitch effect.");
                TriggerGlitchEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "laugh":
                Debug.Log($"Actor '{actor}' is performing action 'laugh'. Laughing.");
                TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;
            
            case "amused":
                Debug.Log($"Actor '{actor}' is performing action 'amused'.");
                ///TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "professional":
                Debug.Log($"Actor '{actor}' is performing action 'professional'. ");
                //TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "shouting":
                Debug.Log($"Actor '{actor}' is performing action 'professional'. ");
                //TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "cool":
                Debug.Log($"Actor '{actor}' is performing action 'cool'. ");
                //TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "smooth":
                Debug.Log($"Actor '{actor}' is performing action 'smooth'. ");
                //TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "confident":
                Debug.Log($"Actor '{actor}' is performing action 'smooth'. ");
                //TriggerLaughEffect(actorGameObject); // Trigger glitch for the specific actor
                break;

            case "normal":
                Debug.Log($"Actor '{actor}' is performing action 'normal'. Doing nothing.");
                break;

            default:
                Debug.LogWarning($"Unknown action '{action}' received for actor '{actor}'. Defaulting to 'normal'.");
                action = "normal";
                break;
        }

        return action;
    }

    private GameObject FindCharacterByName(string actorName)
    {
        // Attempt to find the GameObject by its name
        var actorGameObject = GameObject.Find(actorName);

        if (actorGameObject == null)
        {
            Debug.LogWarning($"Character GameObject with name '{actorName}' not found.");
        }

        return actorGameObject;
    }



    private void TriggerGlitchEffect(GameObject actorGameObject)
    {
        // Get the GlitchOutEffect component from the specified actor GameObject
        var glitchOutEffect = actorGameObject.GetComponent<GlitchOutEffect>();
        if (glitchOutEffect != null)
        {
            glitchOutEffect.TriggerGlitchOut(); // Trigger the glitch effect
            Debug.Log($"Glitch effect triggered successfully for actor '{actorGameObject.name}'.");
        }
        else
        {
            Debug.LogError($"GlitchOutEffect not found on actor '{actorGameObject.name}'. Unable to trigger glitch effect.");
        }
    }

    private void TriggerHappyEffect(GameObject actorGameObject)
    {
        var happyEffect = actorGameObject.GetComponent<HappyEffect>();
        if (happyEffect != null)
        {
            happyEffect.TriggerHappyEffect(); // Trigger the happyEffect 
            Debug.Log($"Happy triggered successfully for actor '{actorGameObject.name}'.");
        }
        else
        {
            Debug.LogError($"Happy not found on actor '{actorGameObject.name}'. Unable to trigger Happy effect.");
        }
        
    }

    private void TriggerConcernedEffect(GameObject actorGameObject)
    {
        var concernedEffect = actorGameObject.GetComponent<ConcernedEffect>();
        if (concernedEffect != null)
        {
            concernedEffect.TriggerConcernedEffect(); // Trigger the concernedEffect
            Debug.Log($"Concerned triggered successfully for actor '{actorGameObject.name}'.");
        }
        else
        {
            Debug.LogError($"concernedEffect not found on actor '{actorGameObject.name}'. Unable to trigger concernedEffect.");
        }
        
    }

    private void TriggerExcitedEffect(GameObject actorGameObject)
    {
        var excitedEffect = actorGameObject.GetComponent<ExcitedEffect>();
        if (excitedEffect != null)
        {
            excitedEffect.TriggerExcitedEffect(); // Trigger the glitch effect
            Debug.Log($"Excited triggered successfully for actor '{actorGameObject.name}'.");
        }
        else
        {
            Debug.LogError($"Excited not found on actor '{actorGameObject.name}'. Unable to trigger Excited effect.");
        }
        
    }

    private void TriggerLaughEffect(GameObject actorGameObject)
    {
        var laughEffect = actorGameObject.GetComponent<LaughEffect>();
        if (laughEffect != null)
        {
            laughEffect.TriggerLaughEffect(); // Trigger the glitch effect
            Debug.Log($"Laugh effect triggered successfully for actor '{actorGameObject.name}'.");
        }
        else
        {
            Debug.LogError($"LaughEffect not found on actor '{actorGameObject.name}'. Unable to trigger laugh effect.");
        }
    }




}
