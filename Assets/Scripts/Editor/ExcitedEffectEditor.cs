using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExcitedEffect))]
public class ExcitedEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ExcitedEffect excitedEffect = (ExcitedEffect)target;

        if (GUILayout.Button("Test Excited Effect"))
        {
            excitedEffect.TriggerExcitedEffect();
        }
    }
}
