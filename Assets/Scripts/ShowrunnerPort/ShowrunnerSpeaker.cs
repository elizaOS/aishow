using UnityEngine;
using ShowGenerator;

public class ShowrunnerSpeaker : MonoBehaviour
{
    public void Speak(ShowDialogue dialogue)
    {
        Debug.Log($"[ShowrunnerSpeaker] {dialogue.actor}: {dialogue.line} (action: {dialogue.action})");
        // TODO: Integrate TTS/audio playback in the future
    }
} 