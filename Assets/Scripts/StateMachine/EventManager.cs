using System;
using UnityEngine;

// Data structure to pass event info
public struct EpisodeCompletionData
{
    public string JsonFilePath; // Relative path used to load the episode
    public string EpisodeId;    // The ID of the completed episode (e.g., "S1E1")
}

public class EventManager
{
    public static event Action<Transform> OnSpeakerChange;
    public event Action<Transform> OnActorSpeak; // Triggered when an actor starts speaking

    public static event Action OnClearSpeaker;

    /// <summary>
    /// Event fired when an episode has finished playing.
    /// Passes the path to the source JSON and the Episode ID.
    /// </summary>
    public static event Action<EpisodeCompletionData> OnEpisodeComplete;

    private Transform currentSpeaker;

    // Invoke the speaker change event and set the current speaker
    public void InvokeSpeakerChange(Transform speakerTransform)
    {
        currentSpeaker = speakerTransform;
        OnSpeakerChange?.Invoke(speakerTransform);
        OnActorSpeak?.Invoke(speakerTransform);
    }

    // Invoke the clear speaker event and reset the current speaker
    public void InvokeClearSpeaker()
    {
        currentSpeaker = null;
        OnClearSpeaker?.Invoke();
    }

    // Get the current speaker
    public Transform GetCurrentSpeaker()
    {
        return currentSpeaker;
    }

    public void SubscribeToEvents()
    {
        // Add subscriptions here if needed
    }

    public void UnsubscribeFromEvents()
    {
        // Remove subscriptions here to prevent memory leaks
    }

    /// <summary>
    /// Invokes the OnEpisodeComplete event.
    /// </summary>
    /// <param name="completionData">Data about the completed episode.</param>
    public static void InvokeEpisodeComplete(EpisodeCompletionData completionData)
    {
        Debug.Log($"Invoking OnEpisodeComplete for Episode: {completionData.EpisodeId} from path: {completionData.JsonFilePath}");
        OnEpisodeComplete?.Invoke(completionData);
    }
}
