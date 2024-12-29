using System;
using UnityEngine;

public class EventManager
{
    public static event Action<Transform> OnSpeakerChange;
    public event Action<Transform> OnActorSpeak; // Triggered when an actor starts speaking

    public static event Action OnClearSpeaker;

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
}
