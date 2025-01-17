using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NewsAndJokesFetcher : MonoBehaviour
{
    // Reference to the InfiniteScrollText script
    public InfiniteScrollText infiniteScrollText;

    private List<string> metaverseJokes = new List<string>
    {
        "<i><color=#FF0000>BREAKING NEWS:</color></i> <color=#FFFFFF>Avatars are requesting snack breaks—virtual pizza only!</color>",
        "<i><color=#FF0000>DEV UPDATE:</color></i> <color=#FFFFFF>Engineers claim bugs are just 'features with potential.'</color>",
        "<i><color=#FF0000>NEWSFLASH:</color></i> <color=#FFFFFF>Metaverse now offers virtual coffee breaks!</color>",
        "<i><color=#FF0000>DEV ALERT:</color></i> <color=#FFFFFF>Better get an avatar for the digital you!</color>"
    };



    void Start()
    {
        // Start the joke display
        StartCoroutine(DisplayJokes());
    }

    IEnumerator DisplayJokes()
    {
        // Build the formatted text for the jokes
        string displayText = "";

        // Loop through all the jokes and add them to the displayText string
        foreach (var joke in metaverseJokes)
        {
            displayText += joke + "  "; // Space between jokes
        }

        // Update the ticker with the jokes (no need to wait for network request)
        infiniteScrollText.UpdateText(displayText);

        // Just display the jokes once for now, or you can add a delay if you want to loop them
        yield return null;
    }
}
