using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlitchOutEffect))]
public class GlitchOutEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GlitchOutEffect glitchOutEffect = (GlitchOutEffect)target;

        if (GUILayout.Button("Test Glitch Effect"))
        {
            glitchOutEffect.TriggerGlitchOut();
        }
    }
}
