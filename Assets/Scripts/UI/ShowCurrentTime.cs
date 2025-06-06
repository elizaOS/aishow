using UnityEngine;
using TMPro; // Import the TextMeshPro namespace

public class ShowCurrentTime : MonoBehaviour
{
// Reference to the TextMeshPro UI element
    public TextMeshProUGUI dateTimeText;

    // Date and time format (e.g., "MM/dd/yyyy hh:mm tt")
    public string dateTimeFormat = "MM/dd/yyyy hh:mm tt";

    void Update()
    {
        if (dateTimeText != null)
        {
            // Get the current date and time and format it
            string currentDateTime = System.DateTime.Now.ToString(dateTimeFormat);
            
            // Update the TextMeshPro element
            dateTimeText.text = currentDateTime;
        }
    }
}
