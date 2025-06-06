using UnityEditor;
using UnityEngine;
using ShowRunner; // Required to access BigHeadEffect

/// <summary>
/// Custom editor for the BigHeadEffect component.
/// Adds buttons to the Inspector for testing the different effect modes during runtime.
/// </summary>
[CustomEditor(typeof(BigHeadEffect))]
public class BigHeadEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields provided by Unity
        base.OnInspectorGUI();

        // Get a reference to the BigHeadEffect script instance being inspected
        BigHeadEffect bigHeadEffect = (BigHeadEffect)target;

        // Add a horizontal layout group for the test buttons
        EditorGUILayout.LabelField("Runtime Testing", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // Add a button to test the Grow effect
        if (GUILayout.Button("Test Grow"))
        {
            // Ensure the button only works in Play mode to avoid editor errors
            if (Application.isPlaying)
            {
                bigHeadEffect.TriggerEffect(BigHeadEffect.EffectMode.Grow);
            }
            else
            {
                Debug.LogWarning("Test buttons only work in Play Mode.");
            }
        }

        // Add a button to test the Shrink effect
        if (GUILayout.Button("Test Shrink"))
        {
            if (Application.isPlaying)
            {
                bigHeadEffect.TriggerEffect(BigHeadEffect.EffectMode.Shrink);
            }
            else
            {
                Debug.LogWarning("Test buttons only work in Play Mode.");
            }
        }

        // Add a button to test the Random effect
        if (GUILayout.Button("Test Random"))
        {
            if (Application.isPlaying)
            {
                bigHeadEffect.TriggerEffect(BigHeadEffect.EffectMode.Random);
            }
            else
            {
                Debug.LogWarning("Test buttons only work in Play Mode.");
            }
        }

        // End the horizontal layout group
        EditorGUILayout.EndHorizontal();
    }
} 