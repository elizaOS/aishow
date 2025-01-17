#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectPoolerAndMover))]
public class ObjectPoolerAndMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ObjectPoolerAndMover script = (ObjectPoolerAndMover)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Spawn Objects", GUILayout.Height(30)))
        {
            script.SpawnObjects();
        }
    }
}
#endif
