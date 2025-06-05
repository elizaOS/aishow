using System;
using UnityEngine;
using ShowRunner;

// Define the data structure for the original transcript generated event
// This is placed outside the EventManager class to be globally accessible
// or can be placed inside if preferred and accessed via EventManager.OriginalTranscriptData
public struct OriginalTranscriptData
{
    public string EpisodeId;
    public string OriginalTranscriptFilePath; // Full path to the base transcript (e.g., _youtubetranscript.txt)
    public string EpisodeJsonFilePath;      // Full path to the episode JSON file that was used to generate the transcript
}

// New: Define the data structure for the translated transcript generated event
public struct TranslatedTranscriptData
{
    public string EpisodeId;
    public string TranslatedTranscriptFilePath;
    public string EpisodeJsonFilePath;
    public string LanguageName; // e.g., "Chinese (Simplified)"
    public string LanguageCode; // e.g., "ch"
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
    public static event Action<ShowRunner.EpisodeCompletionData> OnEpisodeComplete;

    // New Event: Fired when the original (e.g., English) transcript is generated and saved
    public static event System.Action<OriginalTranscriptData> OnOriginalTranscriptGenerated;

    // New Event: Fired when a translated transcript is generated and saved
    public static event System.Action<TranslatedTranscriptData> OnTranslatedTranscriptGenerated;

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
    public static void InvokeEpisodeComplete(ShowRunner.EpisodeCompletionData completionData)
    {
        Debug.Log($"[EventManager] Invoking OnEpisodeComplete for Episode: {completionData.EpisodeId} from path: {completionData.JsonFilePath}");
        OnEpisodeComplete?.Invoke(completionData);
    }

    /// <summary>
    /// Invokes the OnOriginalTranscriptGenerated event.
    /// </summary>
    /// <param name="data">Data about the generated original transcript.</param>
    public static void RaiseOriginalTranscriptGenerated(OriginalTranscriptData data)
    {
        Debug.Log($"[EventManager] Invoking OnOriginalTranscriptGenerated for Episode: {data.EpisodeId}, TranscriptPath: {data.OriginalTranscriptFilePath}");
        OnOriginalTranscriptGenerated?.Invoke(data);
    }

    // New: Invoker for the translated transcript event
    public static void RaiseTranslatedTranscriptGenerated(TranslatedTranscriptData data)
    {
        Debug.Log($"[EventManager] Invoking OnTranslatedTranscriptGenerated for Episode: {data.EpisodeId}, Lang: {data.LanguageCode}, TranscriptPath: {data.TranslatedTranscriptFilePath}");
        OnTranslatedTranscriptGenerated?.Invoke(data);
    }
}
