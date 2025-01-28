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

    public TextureLoader textureLoader; // Reference to the TextureLoader script
    public GameObject mediaTvReference; // Reference to the TV in the news room 



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
        //SetRenderersEnabled(mediaTvReference, false);  // To enable

        
    }

    public void Reinitialize() => actorManager = new ActorManager(); // Reinitializes the ActorManager.

    public void ClearState() => (actorManager, eventManager) = (null, null); // Clears the state by setting manager references to null.

    public void HandleSpeakPayload(string json)
    {
        if (string.IsNullOrEmpty(json)) 
        {
            Debug.LogError("Received empty or null JSON string.");
            return; 
        }

        try
        {
            JObject payload = JObject.Parse(json);

            actorManager.RegisterActorsFromCameras();

            string actorName = payload["dialogue"]?["actor"]?.ToString();
            string dialogueLine = payload["dialogue"]?["line"]?.ToString(); // For "tv", this is the texture URL
            string action = payload["dialogue"]?["action"]?.ToString() ?? "normal";

            if (string.IsNullOrEmpty(actorName)) return;

            // Special case for "tv"
            if (actorName == "tv")
            {
                if (string.IsNullOrEmpty(dialogueLine))
                {
                    SetRenderersEnabled(mediaTvReference, false); // To disable
                    return;
                }

                // Assuming a reference to the TextureLoader is already available
                if (textureLoader != null)
                {
                    SetRenderersEnabled(mediaTvReference, true);  // To enable
                    textureLoader.textureURL = dialogueLine; // Use dialogueLine as the texture URL
                    textureLoader.LoadEmissiveTexture();
                    Debug.Log($"Updated TV actor with texture URL: {dialogueLine}");
                }
                else
                {
                    Debug.LogError("TextureLoader reference is not assigned.");
                }

                return; // Exit early since this is a special event for 'tv'
            }

            // Normal processing for other actors
            if (string.IsNullOrEmpty(dialogueLine)) return;

            GameObject actorObject = GameObject.Find(actorName);
            HandleSpeakerChange(actorObject);

            if (actorObject != null)
            {
                TriggerMouthMovement(actorObject);
                TriggerSpeakingLean(actorObject);

                uiContainer?.SetActive(true);
                dialogueManager.DisplayDialogue(actorObject, dialogueLine, action);
                eventManager.InvokeSpeakerChange(actorObject.transform);
                lastSpeakEventTime = Time.time;

                AutoCam.Instance?.DeactivateAutoCam();
                if (!isClearing) StartCoroutine(ClearSpeakerWithDelay());
            }
            else 
            {
                Debug.LogWarning($"Actor {actorName} not found in the scene.");
            }
        }
        catch (Exception ex) 
        {
            Debug.LogError($"Error processing speak payload: {ex.Message}");
        }
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

    private void SetRenderersEnabled(GameObject target, bool isEnabled) // this is to turn on and off the media tv
    {
        MeshRenderer[] renderers = target?.GetComponentsInChildren<MeshRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = isEnabled;
            }
        }
        else
        {
            Debug.LogWarning($"No Renderers found on {target?.name} or its children!");
        }
    }
}
