using UnityEditor;
using UnityEngine;
using ShowRunner;

[CustomEditor(typeof(HappyEffect))]
public class HappyEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        HappyEffect happyEffect = (HappyEffect)target;

        if (GUILayout.Button("Test Happy Effect"))
        {
            happyEffect.TriggerHappyEffect();
        }
    }
}
