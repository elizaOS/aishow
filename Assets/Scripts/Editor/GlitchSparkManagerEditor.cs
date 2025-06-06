#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlitchSparkManager))]
public class GlitchSparkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector to show all serialized fields
        DrawDefaultInspector();

        // Get a reference to the GlitchSparkManager script
        GlitchSparkManager script = (GlitchSparkManager)target;

        EditorGUILayout.Space();

        // Add a button to trigger the glitch effect
        if (GUILayout.Button("Trigger Glitch Effect", GUILayout.Height(30)))
        {
            script.TriggerGlitch();
        }

        EditorGUILayout.Space();

        // Add a warning if the pool size is too small
        if (script.poolSize < 5)
        {
            EditorGUILayout.HelpBox("The pool size is quite small, consider increasing it for better results.", MessageType.Warning);
        }
    }
}
#endif
