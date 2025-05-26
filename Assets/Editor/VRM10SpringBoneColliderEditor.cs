using UnityEngine;
using UnityEditor;
using UniVRM10;

[CustomEditor(typeof(VRM10SpringBoneCollider))]
public class VRM10SpringBoneColliderEditor : Editor
{
    private void OnSceneGUI()
    {
        var collider = (VRM10SpringBoneCollider)target;
        
        // Draw radius handle
        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(
            Quaternion.identity,
            collider.transform.TransformPoint(collider.Offset),
            collider.Radius
        );
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(collider, "Change Collider Radius");
            collider.Radius = newRadius;
        }

        // Draw offset handle
        EditorGUI.BeginChangeCheck();
        Vector3 newOffset = Handles.PositionHandle(
            collider.transform.TransformPoint(collider.Offset),
            Quaternion.identity
        );
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(collider, "Change Collider Offset");
            collider.Offset = collider.transform.InverseTransformPoint(newOffset);
        }

        // Draw tail handle for capsule colliders
        if (collider.ColliderType == VRM10SpringBoneColliderTypes.Capsule || 
            collider.ColliderType == VRM10SpringBoneColliderTypes.CapsuleInside)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newTail = Handles.PositionHandle(
                collider.transform.TransformPoint(collider.Tail),
                Quaternion.identity
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(collider, "Change Collider Tail");
                collider.Tail = collider.transform.InverseTransformPoint(newTail);
            }
        }
    }
}

[CustomEditor(typeof(VRM10SpringBoneColliderGroup))]
public class VRM10SpringBoneColliderGroupEditor : Editor
{
    private void OnSceneGUI()
    {
        var group = (VRM10SpringBoneColliderGroup)target;
        
        // Draw gizmos for all colliders in the group
        foreach (var collider in group.Colliders)
        {
            if (collider != null)
            {
                collider.DrawGizmos();
            }
        }
    }
} 