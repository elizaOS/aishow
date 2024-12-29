using System.Collections.Generic;
using UnityEngine;

public class LookatManager : MonoBehaviour
{
    public List<LookAtLogic> characters; // List of characters participating in the conversation

    public void OnCharacterSpeaking(Transform speaker)
    {
        Transform eyeBone = FindEyeBone(speaker); // Find the eye bone of the speaker
        if (eyeBone == null)
        {
            Debug.LogWarning("Eye bone not found for " + speaker.name + ". Using the speaker's main transform instead.");
            eyeBone = speaker; // Fallback to the speaker's main transform if no eye bone is found
        }

        foreach (LookAtLogic character in characters)
        {
            if (character.transform != speaker)
            {
                character.SetTarget(eyeBone); // Set the speaker's eye bone as the target
            }
        }
    }

    private Transform FindEyeBone(Transform root)
    {
        // Perform a search for a bone named "Eye", "eye", or similar
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLower().Contains("eye")) // Check for "eye" in the name
            {
                return child;
            }
        }
        return null; // Return null if no eye bone is found
    }
}
