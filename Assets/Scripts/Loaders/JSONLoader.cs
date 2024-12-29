using UnityEngine;
using TMPro;
using System.IO;

public class JSONLoader : MonoBehaviour
{
    public ScenePayloadManager scenePayloadManager; // Reference to the PayloadManager
    public SpeakPayloadManager speakPayloadManager; // Reference to the PayloadManager
    public TextMeshProUGUI outputText;    // TMP UI Text to display debug info

    public void LoadScenePayload(string filePath)
    {
        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);

            // Display the loaded JSON in the output text box
            outputText.text = "Loaded JSON:\n" + jsonContent;

            // Call the PayloadManager to load the scene
            scenePayloadManager.LoadScene(jsonContent);
        }
        else
        {
            Debug.LogError($"File not found: {filePath}");
            outputText.text = $"Error: File not found at {filePath}";
        }
    }

    public void LoadSpeakPayload(string filePath)
    {
        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);

            // Display the loaded JSON in the output text box
            outputText.text = "Loaded JSON:\n" + jsonContent;

            // Call the PayloadManager to load the scene
            speakPayloadManager.HandleSpeakPayload(jsonContent);
        }
        else
        {
            Debug.LogError($"File not found: {filePath}");
            outputText.text = $"Error: File not found at {filePath}";
        }
    }
}
