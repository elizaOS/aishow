using UnityEngine;
using System.Collections;

public class FirebaseHttpRequestWithKey : MonoBehaviour
{
    [HideInInspector]
    public string InputsUrl { get; set; }

    [HideInInspector]
    public string OutputsBaseUrl { get; set; }

    [Header("Polling Settings")]
    public float pollingInterval = 1.5f;

    private PollingManager pollingManager;
    private ScenePreparationManager scenePreparationManager;
    private EventProcessor eventProcessor;
    private long lastProcessedTimestamp = 0;

    private static FirebaseHttpRequestWithKey instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure that FirebaseConfig.Instance is not null before using it
        if (FirebaseConfig.Instance == null)
        {
            Debug.LogError("FirebaseConfig.Instance is not initialized. Please check your scene setup.");
            return;  // Stop further execution if FirebaseConfig is not initialized
        }

        InputsUrl = FirebaseConfig.Instance.InputsUrl;
        OutputsBaseUrl = FirebaseConfig.Instance.OutputsBaseUrl;

        // Check if URLs are null or empty
        if (string.IsNullOrEmpty(InputsUrl) || string.IsNullOrEmpty(OutputsBaseUrl))
        {
            Debug.LogError("InputsUrl or OutputsBaseUrl are not set properly in FirebaseConfig.");
            return; // Stop further execution if URLs are not set
        }

        InitializeComponents();
        SetupEventHandlers();
    }

    private void InitializeComponents()
    {
        pollingManager = gameObject.AddComponent<PollingManager>();
        scenePreparationManager = gameObject.AddComponent<ScenePreparationManager>();
        eventProcessor = gameObject.AddComponent<EventProcessor>();

        scenePreparationManager.SetOutputsBaseUrl(OutputsBaseUrl);
    }

    private void SetupEventHandlers()
    {
        if (scenePreparationManager != null)
        {
            scenePreparationManager.OnScenePrepareRequested += scenePreparationManager.HandlePrepareScene;
        }
    }

    private void OnDestroy()
    {
        if (scenePreparationManager != null)
        {
            scenePreparationManager.OnScenePrepareRequested -= scenePreparationManager.HandlePrepareScene;
        }
    }

    public void StartPollingInputs()
    {
        Debug.Log("Polling started...");
        pollingManager.StartPolling(pollingInterval, PollFirebase);
    }

    public void StopPollingInputs()
    {
        pollingManager.StopPolling();
    }

    private void PollFirebase()
    {
        StartCoroutine(FirebaseNetworkManager.GetFromFirebase(
            InputsUrl,
            HandleInputsEvent,
            error => Debug.LogError($"Error fetching inputs: {error}")
        ));
    }

    public void HandleInputsEvent(string jsonResponse)
    {
        var parsedEvents = FirebaseEventParser.ParseEvents(jsonResponse);

        foreach (var eventData in parsedEvents.Events)
        {
            if (eventData.timestamp <= lastProcessedTimestamp)
                continue;

            lastProcessedTimestamp = eventData.timestamp;
            eventProcessor.ProcessEvent(eventData);
        }
    }

    public bool IsPolling => pollingManager != null && pollingManager.IsPolling();
}
