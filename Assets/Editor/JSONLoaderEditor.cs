using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JSONLoader))]
public class JSONLoaderEditor : Editor
{
    private string selectedSceneFilePath = ""; // Store the scene file path
    private string selectedSpeakFilePath = ""; // Store the speak file path

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw default inspector

        JSONLoader jsonLoader = (JSONLoader)target;

        GUILayout.Space(10);

        // Section for loading Scene Payload
        GUILayout.Label("Select a JSON File for Scene Payload", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Scene Payload"))
        {
            selectedSceneFilePath = EditorUtility.OpenFilePanel("Select JSON File to Load Scene", "", "json");

            if (!string.IsNullOrEmpty(selectedSceneFilePath))
            {
                Debug.Log($"Selected Scene file: {selectedSceneFilePath}");

                // Call LoadScenePayload method with the selected file
                jsonLoader.LoadScenePayload(selectedSceneFilePath);
            }
        }

        // Display the currently selected scene file path
        if (!string.IsNullOrEmpty(selectedSceneFilePath))
        {
            GUILayout.Label($"Selected Scene File: {selectedSceneFilePath}");
        }

        GUILayout.Space(10);

        // Section for loading Speak Payload
        GUILayout.Label("Select a JSON File for Speak Payload", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Speak Payload"))
        {
            selectedSpeakFilePath = EditorUtility.OpenFilePanel("Select JSON File to Load Speak", "", "json");

            if (!string.IsNullOrEmpty(selectedSpeakFilePath))
            {
                Debug.Log($"Selected Speak file: {selectedSpeakFilePath}");

                // Call LoadSpeakPayload method with the selected file
                jsonLoader.LoadSpeakPayload(selectedSpeakFilePath);
            }
        }

        // Display the currently selected speak file path
        if (!string.IsNullOrEmpty(selectedSpeakFilePath))
        {
            GUILayout.Label($"Selected Speak File: {selectedSpeakFilePath}");
        }
    }
}
