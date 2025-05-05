using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;
using System;
using System.Linq; // For LINQ methods
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ShowRunner; // Explicitly add the namespace

namespace ShowRunner 
{
    // Ensure that a ScenePreparationManager is available on this GameObject
    [RequireComponent(typeof(ScenePreperationManager))]
    public class EventProcessor : MonoBehaviour
    {
        private ScenePreperationManager scenePreparationManager;
        
        private void Awake()
        {
            // Attempt to get the ScenePreparationManager on the same GameObject
            scenePreparationManager = GetComponent<ScenePreperationManager>();
            // Fallback to finding it in the scene if not attached
            if (scenePreparationManager == null)
            {
                scenePreparationManager = FindObjectOfType<ScenePreperationManager>();
                if (scenePreparationManager == null)
                {
                    Debug.LogError("ScenePreparationManager not found! The EventProcessor won't function properly.");
                }
            }
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
            // Previously just requested preparation but nobody was listening to the event
            // scenePreparationManager.RequestScenePreparation(eventData.location);
            
            // Directly call HandlePrepareScene to start the intro sequence and scene loading
            scenePreparationManager.HandlePrepareScene(eventData.location);
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
                return "normal"; // Return the original action if actor not found, or default
            }

            // Check for specific actions
            switch (action.ToLower()) // Use ToLower for case-insensitive matching
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
                    TriggerExcitedEffect(actorGameObject); // Trigger excited effect for the specific actor
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
                    TriggerLaughEffect(actorGameObject); // Trigger laugh effect for the specific actor
                    break;
                
                case "amused":
                    Debug.Log($"Actor '{actor}' is performing action 'amused'. Triggering laser effect.");
                    TriggerAmusedEffect(actorGameObject); // Trigger laser for the specific actor
                    break;

                // --- BigHeadEffect Triggers ---
                case "bighead_grow":
                    Debug.Log($"Actor '{actor}' performing action 'bighead_grow'.");
                    TriggerBigHeadEffect(actorGameObject, BigHeadEffect.EffectMode.Grow);
                    break;
                case "bighead_shrink":
                    Debug.Log($"Actor '{actor}' performing action 'bighead_shrink'.");
                    TriggerBigHeadEffect(actorGameObject, BigHeadEffect.EffectMode.Shrink);
                    break;
                case "bighead_random":
                    Debug.Log($"Actor '{actor}' performing action 'bighead_random'.");
                    TriggerBigHeadEffect(actorGameObject, BigHeadEffect.EffectMode.Random);
                    break;
                // --- End BigHeadEffect Triggers ---

                case "professional":
                    Debug.Log($"Actor '{actor}' is performing action 'professional'. ");
                    // No specific effect, relies on animation state
                    break;

                case "shouting":
                    Debug.Log($"Actor '{actor}' is performing action 'shouting'. ");
                    // No specific effect, relies on animation state
                    break;

                case "enthusiastic":
                    Debug.Log($"Actor '{actor}' is performing action 'enthusiastic'. ");
                    // No specific effect, relies on animation state
                    break;

                case "joking":
                    Debug.Log($"Actor '{actor}' is performing action 'joking'. ");
                    // No specific effect, relies on animation state
                    break;

                case "waving":
                    Debug.Log($"Actor '{actor}' is performing action 'waving'. ");
                    // No specific effect, relies on animation state
                    break;     

                case "concluding":
                    Debug.Log($"Actor '{actor}' is performing action 'concluding'. ");
                    // No specific effect, relies on animation state
                    break;                 

                case "cool":
                    Debug.Log($"Actor '{actor}' is performing action 'cool'. ");
                    // No specific effect, relies on animation state
                    break;

                case "smooth": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'smooth'. ");
                    // No specific effect, relies on animation state
                    break;

                case "confident": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'confident'. ");
                    // No specific effect, relies on animation state
                    break;

                case "optimistic": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'optimistic'. ");
                    // No specific effect, relies on animation state
                    break;

                case "observing": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'observing'. ");
                    // No specific effect, relies on animation state
                    break;

                case "pleased": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'pleased'. ");
                    // No specific effect, relies on animation state
                    break;

                case "technical": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'technical'. ");
                    // No specific effect, relies on animation state
                    break;

                case "analyzing": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'analyzing'. ");
                    // No specific effect, relies on animation state
                    break;

                case "agreeing": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'agreeing'. ");
                    // No specific effect, relies on animation state
                    break;

                case "explaining": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'explaining'. ");
                    // No specific effect, relies on animation state
                    break;

                case "recovered": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'recovered'. ");
                    // No specific effect, relies on animation state
                    break;

                case "passionate": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'passionate'. ");
                    // No specific effect, relies on animation state
                    break;

                case "chill": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'chill'. ");
                    // No specific effect, relies on animation state
                    break;

                case "transitioning": // Assuming similar to cool/professional
                    Debug.Log($"Actor '{actor}' is performing action 'transitioning'. ");
                    // No specific effect, relies on animation state
                    break;

                case "normal":
                    Debug.Log($"Actor '{actor}' is performing action 'normal'. Doing nothing special.");
                    break;

                default:
                    // Keep the original action string if it's not recognized as a special effect trigger
                    // This allows the action string to potentially be used for Animator states directly
                    Debug.LogWarning($"Unknown or unhandled action '{action}' received for actor '{actor}'. Passing action through.");
                    // action = "normal"; // Decided against forcing default
                    break;
            }

            // Return the original action string, as it might be used by animation controllers or other systems
            // even if it triggered a specific effect here.
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

        // New method to trigger the Amused Lazer Effect
        private void TriggerAmusedEffect(GameObject actorGameObject)
        {
            var amusedEffect = actorGameObject.GetComponent<AmusedLazerEffect>();
            if (amusedEffect != null)
            {
                amusedEffect.TriggerEffect(); // Trigger the laser effect
                Debug.Log($"Amused Lazer effect triggered successfully for actor '{actorGameObject.name}'.");
            }
            else
            {
                Debug.LogError($"AmusedLazerEffect not found on actor '{actorGameObject.name}'. Unable to trigger amused laser effect.");
            }
        }

        // --- Add method to trigger BigHeadEffect ---
        private void TriggerBigHeadEffect(GameObject actorGameObject, BigHeadEffect.EffectMode mode)
        {
            var bigHeadEffect = actorGameObject.GetComponent<BigHeadEffect>();
            if (bigHeadEffect != null)
            {
                bigHeadEffect.TriggerEffect(mode); // Trigger the specific BigHead effect mode
                Debug.Log($"BigHeadEffect ({mode}) triggered successfully for actor '{actorGameObject.name}'.");
            }
            else
            {
                Debug.LogError($"BigHeadEffect component not found on actor '{actorGameObject.name}'. Unable to trigger effect.");
            }
        }
        // --- End Add method ---
    }
}
