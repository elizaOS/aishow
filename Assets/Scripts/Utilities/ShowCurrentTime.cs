using UnityEngine;
using TMPro; // Import the TextMeshPro namespace

public class ShowCurrentTime : MonoBehaviour
{
    // Reference to the TextMeshPro UI element
    public TextMeshProUGUI timeText;

    // Time format (12-hour format with AM/PM, no seconds)
    public string timeFormat = "hh:mm tt";

    void Update()
    {
        if (timeText != null)
        {
            // Get the current time and format it
            string currentTime = System.DateTime.Now.ToString(timeFormat);
            
            // Update the TextMeshPro element
            timeText.text = currentTime;
        }
    }
}
