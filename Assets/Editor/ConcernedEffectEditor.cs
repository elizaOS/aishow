using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConcernedEffect))]
public class ConcernedEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ConcernedEffect concernedEffect = (ConcernedEffect)target;

        if (GUILayout.Button("Test Concerned Effect"))
        {
            concernedEffect.TriggerConcernedEffect();
        }
    }
}
