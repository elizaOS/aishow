using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class SpeakPayloadManager : MonoBehaviour
{
    public static SpeakPayloadManager Instance { get; private set; } // Static instance for singleton pattern, making sure only one instance of this class exists.

    // Managers
    public ActorManager actorManager; // Reference to the ActorManager that handles actor-related operations.
    public DialogueManager dialogueManager; // Reference to the DialogueManager that handles displaying dialogue.
    public EventManager eventManager; // Reference to the EventManager that handles event-related logic.

    private float lastSpeakEventTime = -1f; // Timestamp of the last time a speak event occurred.
    private float clearDelay = 20f; // Delay in seconds before clearing the speaker and dialogue.
    private bool isClearing = false; // Flag indicating whether the speaker is currently being cleared.

    [Header("UI Components")]
    public GameObject uiContainer; // Reference to the UI container that displays dialogue

    private void Awake()
    {
        // Ensures only one instance of SpeakPayloadManager exists (singleton pattern).
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // Destroys this object if there is already an instance of SpeakPayloadManager.
            return;
        }
        Instance = this; // Assigns this instance to the singleton.
        actorManager = new ActorManager(); // Initializes the ActorManager.
        dialogueManager = FindObjectOfType<DialogueManager>(); // Finds the DialogueManager in the scene.
        eventManager = new EventManager(); // Initializes the EventManager.
        uiContainer?.SetActive(false); // Hides the UI container initially (using null-conditional operator).
    }

    public void Reinitialize() => actorManager = new ActorManager(); // Reinitializes the ActorManager.

    public void ClearState() => (actorManager, eventManager) = (null, null); // Clears the state by setting manager references to null.

    public void HandleSpeakPayload(string json)
    {
        if (string.IsNullOrEmpty(json)) { Debug.LogError("Received empty or null JSON string."); return; } // Checks if the received JSON string is empty or null.

        try
        {
            JObject payload = JObject.Parse(json); // Parses the JSON payload into a JObject for easier handling.

            actorManager.RegisterActorsFromCameras(); // Registers actors from camera data (assumed to be part of the ActorManager).

            // Extracts the actor's name, dialogue line, and action from the JSON payload.
            string actorName = payload["dialogue"]?["actor"]?.ToString();
            string dialogueLine = payload["dialogue"]?["line"]?.ToString();
            string action = payload["dialogue"]?["action"]?.ToString() ?? "normal"; // Defaults action to "normal" if not provided.

            if (string.IsNullOrEmpty(actorName) || string.IsNullOrEmpty(dialogueLine)) return; // If actor name or dialogue line is missing, return early.

            GameObject actorObject = GameObject.Find(actorName); // Finds the actor GameObject by its name in the scene.
            HandleSpeakerChange(actorObject); // Changes the current speaker if necessary.

            if (actorObject != null)
            {
                // Triggers the mouth movement and speaking lean animations on the actor.
                TriggerMouthMovement(actorObject);
                TriggerSpeakingLean(actorObject);

                uiContainer?.SetActive(true); // Makes the UI container active, displaying the dialogue UI.
                dialogueManager.DisplayDialogue(actorObject, dialogueLine, action);// Displays the dialogue using the DialogueManager.
                eventManager.InvokeSpeakerChange(actorObject.transform);// Invokes speaker change event to notify the system.
                lastSpeakEventTime = Time.time;// Sets the timestamp of the last speak event.

                AutoCam.Instance?.DeactivateAutoCam(); // Deactivates automatic camera control if AutoCam is active.
                if (!isClearing) StartCoroutine(ClearSpeakerWithDelay()); // Starts the coroutine to clear the speaker after a delay if not already clearing.
            }
            else Debug.LogWarning($"Actor {actorName} not found in the scene."); // Warns if the actor was not found.
        }
        catch (Exception ex) { Debug.LogError($"Error processing speak payload: {ex.Message}"); } // Catches any errors during the payload processing and logs the error message.
    }

    private void HandleSpeakerChange(GameObject actorObject)
    {
        var currentSpeaker = eventManager.GetCurrentSpeaker(); // Gets the current speaker from the event manager.
        if (currentSpeaker != null && currentSpeaker.gameObject != actorObject)
        {
            // Stops random mouth movement on the current speaker if the speaker has changed.
            currentSpeaker.GetComponentInChildren<RandomMouthMovement>()?.StopRandomMouthMovement();
        }
    }

    private void TriggerMouthMovement(GameObject actorObject)
    {
        // Triggers random mouth movement for the actor (e.g., talking animations).
        var mouthMovement = actorObject.GetComponentInChildren<RandomMouthMovement>();
        if (mouthMovement != null) mouthMovement.StartRandomMouthMovement(); // Starts random mouth movement if found.
        else Debug.LogWarning($"RandomMouthMovement not found on actor {actorObject.name} or its children."); // Warns if RandomMouthMovement is not found.
    }

    private void TriggerSpeakingLean(GameObject actorObject)
    {
        // Triggers a "speaking lean" animation or logic on the actor.
        var lookAtLogic = actorObject.GetComponent<LookAtLogic>();
        if (lookAtLogic != null)
        {
            // Randomly triggers a lean effect while the actor is speaking.
            lookAtLogic.TriggerSpeakingLean(UnityEngine.Random.Range(0.2f, 0.8f), UnityEngine.Random.Range(0.5f, 1.5f));
        }
    }

    private System.Collections.IEnumerator ClearSpeakerWithDelay()
    {
        isClearing = true; // Sets the clearing flag to true, indicating that we are in the process of clearing.
        // Waits for the specified delay time before clearing the speaker.
        while (Time.time - lastSpeakEventTime < clearDelay) yield return null;

        var currentSpeaker = eventManager.GetCurrentSpeaker(); // Gets the current speaker.
        // Stops the mouth movement on the current speaker.
        currentSpeaker?.GetComponentInChildren<RandomMouthMovement>()?.StopRandomMouthMovement();
        eventManager.InvokeClearSpeaker(); // Invokes an event to clear the current speaker.

        uiContainer?.SetActive(false); // Hides the UI container after the delay.

        AutoCam.Instance?.ActivateAutoCam(); // Reactivates automatic camera control if AutoCam is available.

        isClearing = false; // Resets the clearing flag to false.
    }
}
