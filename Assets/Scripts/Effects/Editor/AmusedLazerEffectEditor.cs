using UnityEngine;
using UnityEditor;
using ShowRunner; // Include the namespace where AmusedLazerEffect resides

/// <summary>
/// Custom editor for the AmusedLazerEffect script.
/// Adds a button to the Inspector to test the effect during Play mode.
/// </summary>
[CustomEditor(typeof(AmusedLazerEffect))]
public class AmusedLazerEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (laserObject, laserAudioSource, etc.)
        DrawDefaultInspector();

        // Get a reference to the script being inspected
        AmusedLazerEffect effectScript = (AmusedLazerEffect)target;

        // Add some space before the button
        EditorGUILayout.Space();

        // Add a help box explaining the button only works in Play mode
        EditorGUILayout.HelpBox("Test button only functions during Play mode.", MessageType.Info);

        // Disable the button if the application is not playing
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        // Add the button
        if (GUILayout.Button("Test Amused Laser Effect"))
        {
            // Check again if playing, just to be safe
            if (Application.isPlaying)
            {
                // Call the public method on the target script
                effectScript.TriggerEffect();
                Debug.Log("Test button clicked: Triggering Amused Lazer Effect.");
            }
            else
            {
                 Debug.LogWarning("Test button clicked, but application is not playing.");
            }
        }

        // End the disabled group
        EditorGUI.EndDisabledGroup();
    }
} 