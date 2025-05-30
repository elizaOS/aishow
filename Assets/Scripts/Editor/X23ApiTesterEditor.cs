#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for the X23ApiTester class to add a button to the Inspector.
/// </summary>
[CustomEditor(typeof(X23ApiTester))]
public class X23ApiTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get a reference to the X23ApiTester script instance
        X23ApiTester apiTester = (X23ApiTester)target;

        // Add a space for better layout
        EditorGUILayout.Space();

        // Add a button to trigger the API call
        if (GUILayout.Button("Test API Call Now"))
        {
            // Ensure we are in play mode or the call is editor-safe
            // HttpClient calls might not work ideally outside play mode without specific setup.
            // For this test, it's generally expected to be used in Play Mode.
            if (Application.isPlaying)
            {
                apiTester.TestApiCall();
            }
            else
            {
                Debug.LogWarning("X23ApiTesterEditor: Please enter Play Mode to test the API call with HttpClient.");
            }
        }
    }
}
#endif 