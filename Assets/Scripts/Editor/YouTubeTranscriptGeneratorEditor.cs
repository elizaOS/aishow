using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(YouTubeTranscriptGenerator))]
public class YouTubeTranscriptGeneratorEditor : Editor
{
    // Hardcoded path and episode ID for the test button
    private const string TEST_JSON_PATH = "Assets/Resources/Episodes/aipodcast-archive-2025-05-01T09-30-54-169Z.json";
    private const string TEST_EPISODE_ID = "S1E1";

    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector(); 

        // Get the target script instance
        YouTubeTranscriptGenerator transcriptGenerator = (YouTubeTranscriptGenerator)target;

        // Add a horizontal space for better layout
        EditorGUILayout.Space();

        // Add the test button
        if (GUILayout.Button($"Generate Test Transcript ({TEST_EPISODE_ID})"))
        {
            // Check if the test JSON file exists before attempting generation
            string fullTestJsonPath = Path.Combine(Application.dataPath, "..", TEST_JSON_PATH);
            if (!File.Exists(fullTestJsonPath))
            {
                EditorUtility.DisplayDialog("Error", $"Test JSON file not found at:\n{TEST_JSON_PATH}\n\nPlease ensure the file exists.", "OK");
            }
            else
            {
                // Call the generation method with test parameters
                transcriptGenerator.GenerateTranscript(TEST_JSON_PATH, TEST_EPISODE_ID);
                // Optionally provide feedback to the user
                EditorUtility.DisplayDialog("Transcript Generation", $"Attempted to generate transcript for {TEST_EPISODE_ID}. Check console for details.", "OK");
            }
        }

        // Add helpful instructions
        EditorGUILayout.HelpBox("Click the button above to generate a test transcript for episode 'S1E1' using the hardcoded JSON file path. The final implementation should use events to trigger generation automatically after a show concludes.", MessageType.Info);
    }
} 