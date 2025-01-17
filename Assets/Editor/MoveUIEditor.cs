using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(MoveUI))]
public class MoveUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Call the base class method to show default inspector properties
        DrawDefaultInspector();

        // Get reference to the script
        MoveUI moveUI = (MoveUI)target;

        // Create "Move Up" button in the Inspector
        if (GUILayout.Button("Move Up"))
        {
            moveUI.MoveUp();
        }

        // Create "Move Down" button in the Inspector
        if (GUILayout.Button("Move Down"))
        {
            moveUI.MoveDown();
        }
    }
}
