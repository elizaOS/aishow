using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AutoTypewriterEffect : MonoBehaviour
{
    [SerializeField] private float typingSpeed = 0.05f; // Speed per character
    [SerializeField] private float delayBetweenEntries = 2.0f; // Delay between entries
    [SerializeField] private List<string> textEntries; // List of text entries to display

    private TextMeshProUGUI textMeshPro;
    private int currentEntryIndex = 0;

    private void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();

        // Add random crypto-themed entries if not manually populated in the Inspector
        if (textEntries == null || textEntries.Count == 0)
        {
            textEntries = new List<string>
            {
                "My portfolio is 90% hope, 10% regret.",
                "I bought the dip, then it dipped harder.",
                "Who needs a 401(k) when you have 4.01 Dogecoins?",
                "HODL.",
                "Ethereum gas fees cost more than my rent.",
                "My dog barks every time I say 'Shiba Inu'.",
                "I just love memes man..",
                "Every time I check the market, Bitcoin drops $1,000.",
                "Decentralized finance? More like decentralized despair.",
                "NFTs: Never Financially Thriving."
            };
        }
    }

    private void Start()
    {
        StartCoroutine(CycleTextEntries());
    }

    /// <summary>
    /// Coroutine to cycle through text entries.
    /// </summary>
    private IEnumerator CycleTextEntries()
    {
        while (true)
        {
            yield return TypeTextCoroutine(textEntries[currentEntryIndex]);

            // Wait for a delay before moving to the next entry
            yield return new WaitForSeconds(delayBetweenEntries);

            // Move to the next entry (loop back to the first entry if at the end)
            currentEntryIndex = (currentEntryIndex + 1) % textEntries.Count;
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
}
