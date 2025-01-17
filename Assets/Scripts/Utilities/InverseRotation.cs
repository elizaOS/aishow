using UnityEngine;

public class InverseRotation : MonoBehaviour
{
    public Transform sourceObject;  // The object that will drive the rotation

    void Update()
    {
        if (sourceObject != null)
        {
            // Apply the inverse of the source object's rotation to this object's rotation
            transform.rotation = Quaternion.Inverse(sourceObject.rotation);
        }
    }
}
