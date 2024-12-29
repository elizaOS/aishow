// FirebaseQueueManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class FirebaseQueueManager : MonoBehaviour
{
    private Queue<(string url, string jsonData)> eventQueue = new Queue<(string url, string jsonData)>();
    private bool isProcessingQueue = false;

    public void EnqueueEvent(string url, string jsonData)
    {
        eventQueue.Enqueue((url, jsonData));
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (isProcessingQueue || eventQueue.Count == 0)
            return;

        isProcessingQueue = true;
        var (url, jsonData) = eventQueue.Dequeue();

        StartCoroutine(FirebaseNetworkManager.SendToFirebase(
            url,
            jsonData,
            onSuccess: HandleSuccess,
            onError: (error) => HandleError(error, url, jsonData)
        ));
    }

    private void HandleSuccess(string response)
    {
        Debug.Log($"Successfully wrote to Firebase: {response}");
        isProcessingQueue = false;
        ProcessQueue();
    }

    private void HandleError(string error, string url, string jsonData)
    {
        Debug.LogError($"Error writing to Firebase: {error}");
        eventQueue.Enqueue((url, jsonData));
        isProcessingQueue = false;
        ProcessQueue();
    }
}