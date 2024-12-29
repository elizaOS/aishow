using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class ScenePreparationManager : MonoBehaviour
{
    private string outputsBaseUrl;
    public IntroSequenceManager introSequenceManagerRef;

    // Event to signal when a scene should be prepared
    public event Action<string> OnScenePrepareRequested;

    // Queue for Firebase events to ensure reliability
    private Queue<(string url, string jsonData)> firebaseEventQueue = new Queue<(string url, string jsonData)>();
    private bool isProcessingQueue = false;

    private void Awake()
    {
        // Ensure that introSequenceManagerRef is assigned at runtime if it's not already set in the inspector
        if (introSequenceManagerRef == null)
        {
            introSequenceManagerRef = FindObjectOfType<IntroSequenceManager>();
            if (introSequenceManagerRef == null)
            {
                Debug.LogError("IntroSequenceManager not found in the scene!");
            }
        }
    }

    // Method to set the OutputsBaseUrl (called from FirebaseHttpRequestWithKey)
    public void SetOutputsBaseUrl(string url)
    {
        outputsBaseUrl = url;
    }

    // Method to trigger the scene preparation (this can be called by other scripts)
    public void RequestScenePreparation(string sceneName)
    {
        Debug.Log($"Scene preparation requested for: {sceneName}");
        OnScenePrepareRequested?.Invoke(sceneName);
    }

    public void HandlePrepareScene(string sceneName)
    {
        //Debug.Log($"Preparing scene: {sceneName}");

        // Attempt to load the scene by its name
        StartCoroutine(PrepareAndSendEvent(sceneName));
    }

    private IEnumerator PrepareAndSendEvent(string sceneName)
    {
        
        yield return StartCoroutine(introSequenceManagerRef.StartIntroSequence());
        // Wait for the scene to load
        yield return StartCoroutine(LoadSceneAsync(sceneName));

        // Once the scene is loaded, send the event
        WritePrepareSceneCompleteEvent(sceneName);
    }

    // Method to load a scene asynchronously and check when it is finished
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Check if the scene is valid and included in the build settings
        if (SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0)
        {
            //Debug.Log($"Loading scene: {sceneName}");

            // Asynchronously load the scene
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;

            // Wait until the scene is fully loaded
            while (!asyncOperation.isDone)
            {
                //Debug.Log($"Loading scene: {sceneName} ({asyncOperation.progress * 100}%)");

                // Check if the scene loading is almost complete (90% progress)
                if (asyncOperation.progress >= 0.9f)
                {
                    //Debug.Log("Scene loading is almost complete (90%)...");

                    // Activate the scene when it's ready
                    asyncOperation.allowSceneActivation = true;
                }

                // Wait until the next frame
                yield return null;
            }

        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' not found in the build settings.");
        }
    }

    // Improved Method to send the "prepareSceneComplete" event to Firebase
    public void WritePrepareSceneCompleteEvent(string sceneName)
    {
        // Validate OutputsBaseUrl
        if (string.IsNullOrEmpty(outputsBaseUrl))
        {
            Debug.LogError("OutputsBaseUrl is not set. Cannot send event to Firebase.");
            return;
        }

        string outputEventKey = $"event{Guid.NewGuid()}"; // Generate a unique event key
        string fullUrl = $"{outputsBaseUrl}{outputEventKey}.json";  // Use the passed OutputsBaseUrl
        string jsonData = $"{{\"type\": \"prepareSceneComplete\", \"scene\": \"{sceneName}\"}}";

        // Enqueue the event and process the queue
        firebaseEventQueue.Enqueue((fullUrl, jsonData));
        ProcessFirebaseQueue();
    }

    // Queue Processor to ensure reliable Firebase communication
    private void ProcessFirebaseQueue()
    {
        if (isProcessingQueue || firebaseEventQueue.Count == 0)
        {
            return;
        }

        isProcessingQueue = true;
        var (url, jsonData) = firebaseEventQueue.Dequeue();

        StartCoroutine(FirebaseNetworkManager.SendToFirebase(
            url,
            jsonData,
            onSuccess: (response) =>
            {
                Debug.Log($"Successfully wrote to Firebase: {jsonData}");
                isProcessingQueue = false;
                ProcessFirebaseQueue(); // Process the next event in the queue
            },
            onError: (error) =>
            {
                Debug.LogError($"Error writing to Firebase: {error}");
                firebaseEventQueue.Enqueue((url, jsonData)); // Requeue the event for retry
                isProcessingQueue = false;
                ProcessFirebaseQueue(); // Retry processing
            }
        ));
    }
}
