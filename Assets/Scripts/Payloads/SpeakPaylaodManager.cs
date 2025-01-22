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

    private float lastSpeakEventTime = -1f;
    private float clearDelay = 20f;
    private bool isClearing = false;

    [Header("UI Components")]
    public GameObject uiContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        actorManager = new ActorManager();
        dialogueManager = FindObjectOfType<DialogueManager>();
        eventManager = new EventManager();
        uiContainer?.SetActive(false);
    }

    public void Reinitialize() => actorManager = new ActorManager();

    public void ClearState() => (actorManager, eventManager) = (null, null);

    public void HandleSpeakPayload(string json)
    {
        if (string.IsNullOrEmpty(json)) { Debug.LogError("Received empty or null JSON string."); return; }

        try
        {
            JObject payload = JObject.Parse(json);
            actorManager.RegisterActorsFromCameras();

            string actorName = payload["dialogue"]?["actor"]?.ToString();
            string dialogueLine = payload["dialogue"]?["line"]?.ToString();
            string action = payload["dialogue"]?["action"]?.ToString() ?? "normal";

            if (string.IsNullOrEmpty(actorName) || string.IsNullOrEmpty(dialogueLine)) return;

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
            else Debug.LogWarning($"Actor {actorName} not found in the scene.");
        }
        catch (Exception ex) { Debug.LogError($"Error processing speak payload: {ex.Message}"); }
    }

    private void HandleSpeakerChange(GameObject actorObject)
    {
        var currentSpeaker = eventManager.GetCurrentSpeaker();
        if (currentSpeaker != null && currentSpeaker.gameObject != actorObject)
        {
            currentSpeaker.GetComponentInChildren<RandomMouthMovement>()?.StopRandomMouthMovement();
        }
    }

    private void TriggerMouthMovement(GameObject actorObject)
    {
        var mouthMovement = actorObject.GetComponentInChildren<RandomMouthMovement>();
        if (mouthMovement != null) mouthMovement.StartRandomMouthMovement();
        else Debug.LogWarning($"RandomMouthMovement not found on actor {actorObject.name} or its children.");
    }

    private void TriggerSpeakingLean(GameObject actorObject)
    {
        var lookAtLogic = actorObject.GetComponent<LookAtLogic>();
        if (lookAtLogic != null)
        {
            lookAtLogic.TriggerSpeakingLean(UnityEngine.Random.Range(0.2f, 0.8f), UnityEngine.Random.Range(0.5f, 1.5f));
        }
    }

    private System.Collections.IEnumerator ClearSpeakerWithDelay()
    {
        isClearing = true;
        while (Time.time - lastSpeakEventTime < clearDelay) yield return null;

        var currentSpeaker = eventManager.GetCurrentSpeaker();
        currentSpeaker?.GetComponentInChildren<RandomMouthMovement>()?.StopRandomMouthMovement();
        eventManager.InvokeClearSpeaker();
        uiContainer?.SetActive(false);
        AutoCam.Instance?.ActivateAutoCam();
        isClearing = false;
    }
}
