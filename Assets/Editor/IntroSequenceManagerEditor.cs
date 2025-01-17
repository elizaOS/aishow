using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IntroSequenceManager))]
public class IntroSequenceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();

        // Add a button to test the intro sequence
        IntroSequenceManager manager = (IntroSequenceManager)target;
        if (GUILayout.Button("Test Intro Sequence"))
        {
            manager.StartTestIntroSequence();
        }
    }
}
