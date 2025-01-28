using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RigWeightController))]
public class RigWeightControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RigWeightController controller = (RigWeightController)target;

        // Button to auto-populate the rigs array
        if (GUILayout.Button("Auto Populate Rigs"))
        {
            controller.AutoPopulateRigs();
            EditorUtility.SetDirty(controller); // Marks the object as dirty to save changes
        }

        // Button to unweight the rigs (set weights to 0)
        if (GUILayout.Button("Unweight Rigs (Set to 0)"))
        {
            controller.SetRigWeights(0f);
        }

        // Button to reweight the rigs (set weights to 1)
        if (GUILayout.Button("Reweight Rigs (Set to 1)"))
        {
            controller.SetRigWeights(1f);
        }
    }
}
