using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomMouthMovement))]
public class RandomMouthMovementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();  // Draw the default Inspector

        // Get a reference to the RandomMouthMovement script
        RandomMouthMovement script = (RandomMouthMovement)target;

        // Add a button to start the random mouth movement
        if (GUILayout.Button("Start Mouth Movement"))
        {
            script.StartRandomMouthMovement();
        }

        // Add a button to stop the random mouth movement
        if (GUILayout.Button("Stop Mouth Movement"))
        {
            script.StopRandomMouthMovement();
        }
    }
} 
