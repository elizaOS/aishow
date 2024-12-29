using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private float typingSpeed = 0.05f; // Speed per character
    [SerializeField] private bool isTypewriterEnabled = true; // Toggle for typewriter effect

    private Coroutine typingCoroutine;

    // The Text or TextMeshProUGUI component to update
    private TextMeshProUGUI textMeshPro;

    private void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// Public method to set and type out text.
    /// </summary>
    /// <param name="text">The text to type out.</param>
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            textMeshPro.text = ""; // Clear the text if empty
            return;
        }

        // Stop any ongoing typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Check if the typewriter effect is enabled
        if (isTypewriterEnabled)
        {
            typingCoroutine = StartCoroutine(TypeTextCoroutine(text)); // Run typewriter effect
        }
        else
        {
            DisplayFullText(text); // Display text instantly
        }
    }

    /// <summary>
    /// Coroutine for typing out text character by character.
    /// </summary>
    /// <param name="text">The full text to display.</param>
    private IEnumerator TypeTextCoroutine(string text)
    {
        textMeshPro.text = ""; // Clear the current text
        foreach (char c in text)
        {
            textMeshPro.text += c; // Add one character at a time
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    /// <summary>
    /// Instantly display the full text.
    /// </summary>
    public void DisplayFullText(string text)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        textMeshPro.text = text;
    }

    /// <summary>
    /// Toggle the typewriter effect on/off.
    /// </summary>
    /// <param name="isEnabled">True to enable, false to disable.</param>
    public void SetTypewriterEnabled(bool isEnabled)
    {
        isTypewriterEnabled = isEnabled;
    }
}
