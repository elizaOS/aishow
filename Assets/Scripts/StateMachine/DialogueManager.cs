using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI dialogTextWindow; // Reference to the text component
    private TypewriterEffect typewriterEffect; // Reference to the TypewriterEffect component

    private void Awake()
    {
        if (dialogTextWindow == null)
        {
            Debug.LogError("Dialog Text Window is not assigned in the Inspector!");
            return;
        }

        // Get the TypewriterEffect component attached to the dialogTextWindow
        typewriterEffect = dialogTextWindow.GetComponent<TypewriterEffect>();
        if (typewriterEffect == null)
        {
            Debug.LogWarning("TypewriterEffect component not found on the Dialog Text Window! Adding it now.");
            typewriterEffect = dialogTextWindow.gameObject.AddComponent<TypewriterEffect>();
        }
    }

    /// <summary>
    /// Displays dialogue for the given actor with specific actions.
    /// </summary>
    /// <param name="actorObject">The actor speaking (optional, for additional functionality)</param>
    /// <param name="line">The dialogue line to display</param>
    /// <param name="action">The style or action for the dialogue</param>
    public void DisplayDialogue(GameObject actorObject, string line, string action)
    {
        if (dialogTextWindow == null)
        {
            Debug.LogError("Dialog Text Window is not assigned in the Inspector!");
            return;
        }

        // Apply styles based on the action
        ApplyDialogueStyle(action);

        // Display the dialogue using the TypewriterEffect if available
        if (typewriterEffect != null)
        {
            typewriterEffect.SetText(line); // Use typewriter effect to display text
        }
        else
        {
            dialogTextWindow.text = line; // Fallback: display text instantly
        }
    }

    /// <summary>
    /// Applies style to the dialog text based on the action type.
    /// </summary>
    /// <param name="action">The action type (e.g., "normal", "shout", "whisper")</param>
    private void ApplyDialogueStyle(string action)
    {
        switch (action.ToLower())
        {
            case "normal":
                dialogTextWindow.color = Color.white;
                dialogTextWindow.fontSize = 24;
                break;
            case "shout":
                dialogTextWindow.color = Color.red;
                dialogTextWindow.fontSize = 36;
                break;
            case "whisper":
                dialogTextWindow.color = Color.gray;
                dialogTextWindow.fontSize = 18;
                break;
            default:
                dialogTextWindow.color = Color.white;
                dialogTextWindow.fontSize = 24;
                //Debug.LogWarning($"Unhandled action type: {action}");
                break;
        }
    }
}
