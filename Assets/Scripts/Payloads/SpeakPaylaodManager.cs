using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class SpeakPayloadManager : MonoBehaviour
{
    public static SpeakPayloadManager Instance { get; private set; }

    // Managers
    public ActorManager actorManager;
    public DialogueManager dialogueManager;
    public EventManager eventManager;

    private float lastSpeakEventTime = -1f; // Track the last time a speak event was received
    private float clearDelay = 20f; // Delay before clearing the speaker
    private bool isClearing = false; // Prevent multiple coroutines from running simultaneously

    [Header("UI Components")]
    public GameObject uiContainer; // Assign your UI container GameObject in the Inspector

    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Destroy duplicate instance
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Initialize managers
        actorManager = new ActorManager();
        dialogueManager = FindObjectOfType<DialogueManager>();
        eventManager = new EventManager();

        // Ensure the UI container is initially hidden
        if (uiContainer != null)
        {
            uiContainer.SetActive(false); // Hide UI container at start
        }
    }

    public void Reinitialize()
    {
        actorManager = new ActorManager();
        eventManager = new EventManager();
    }

    public void ClearState()
    {
        actorManager = null;
        eventManager = null;
    }


public void HandleSpeakPayload(string json)
{
    try
    {
        //Debug.Log("Received speak payload: " + json);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("Received empty or null JSON string.");
            return;
        }

        JObject payload = JObject.Parse(json);

        // Register actors from ActorCamera components before processing (if needed)
        actorManager.RegisterActorsFromCameras();

        // Handle dialogue
        string actorName = payload["dialogue"]?["actor"]?.ToString();
        string dialogueLine = payload["dialogue"]?["line"]?.ToString();
        string action = payload["dialogue"]?["action"]?.ToString() ?? "normal";

        if (!string.IsNullOrEmpty(actorName) && !string.IsNullOrEmpty(dialogueLine))
        {
            // Find the actor by name
            GameObject actorObject = GameObject.Find(actorName);

            // Stop mouth movement for the current speaker before switching
            var currentSpeaker = eventManager.GetCurrentSpeaker();
            if (currentSpeaker != null && currentSpeaker.gameObject != actorObject)
            {
                var currentMouthMovement = currentSpeaker.GetComponentInChildren<RandomMouthMovement>();
                if (currentMouthMovement != null)
                {
                    currentMouthMovement.StopRandomMouthMovement();
                }
            }

            if (actorObject != null)
            {
                // Locate the RandomMouthMovement component in children
                var mouthMovement = actorObject.GetComponentInChildren<RandomMouthMovement>();
                if (mouthMovement != null)
                {
                    // Trigger the mouth movement event
                    mouthMovement.StartRandomMouthMovement();
                }
                else
                {
                    Debug.LogWarning($"RandomMouthMovement not found on actor {actorName} or its children.");
                }

                // Trigger the leaning motion with random values for lean amount and duration
                var lookAtLogic = actorObject.GetComponent<LookAtLogic>();
                if (lookAtLogic != null)
                {
                    float randomLeanAmount = UnityEngine.Random.Range(0.2f, 0.8f);   // Random lean amount between 0.2 and 0.8 (adjust as needed)
                    float randomDuration = UnityEngine.Random.Range(0.5f, 1.5f);      // Random duration between 0.5 and 1.5 seconds (adjust as needed)

                    lookAtLogic.TriggerSpeakingLean(randomLeanAmount, randomDuration);
                }

                // Show the UI container if it's not already active
                if (uiContainer != null && !uiContainer.activeSelf)
                {
                    uiContainer.SetActive(true);
                }

                // Display the dialogue
                dialogueManager.DisplayDialogue(actorObject, dialogueLine, action);

                // Trigger the speaker change event
                eventManager.InvokeSpeakerChange(actorObject.transform);

                // Reset the clear timer whenever a new speak event occurs
                lastSpeakEventTime = Time.time;

                // Deactivate AutoCam if active
                AutoCam.Instance?.DeactivateAutoCam();

                // Start the coroutine to clear the speaker after the delay
                if (!isClearing)
                {
                    StartCoroutine(ClearSpeakerWithDelay());
                }
            }
            else
            {
                Debug.LogWarning($"Actor {actorName} not found in the scene.");
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error processing speak payload: {ex.Message}");
    }
}

private System.Collections.IEnumerator ClearSpeakerWithDelay()
{
    isClearing = true;
    while (Time.time - lastSpeakEventTime < clearDelay)
    {
        yield return null;
    }

    // If no new speak events occurred during the delay, clear the speaker
    var currentSpeaker = eventManager.GetCurrentSpeaker();
    if (currentSpeaker != null)
    {
        var mouthMovement = currentSpeaker.GetComponentInChildren<RandomMouthMovement>();
        if (mouthMovement != null)
        {
            mouthMovement.StopRandomMouthMovement();
        }
    }

    eventManager.InvokeClearSpeaker();

    // Hide the UI container
    if (uiContainer != null && uiContainer.activeSelf)
    {
        uiContainer.SetActive(false);
    }

    // Activate AutoCam for fallback shots
    AutoCam.Instance?.ActivateAutoCam();

    isClearing = false;
}



}
