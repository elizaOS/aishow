using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaughEffect))]
public class LaughEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Get the LaughEffect reference
        LaughEffect laughEffect = (LaughEffect)target;

        // Create a button to trigger the laugh effect
        if (GUILayout.Button("Test Laugh Effect"))
        {
            laughEffect.TriggerLaughEffect();
        }
    }
}
