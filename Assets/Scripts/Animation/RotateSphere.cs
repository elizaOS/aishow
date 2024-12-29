using UnityEngine;

public class RotateSphere : MonoBehaviour
{
    // Speed of rotation around the Y-axis
    [SerializeField]
    private float rotationSpeed = 10f;

    void Update()
    {
        // Rotate the sphere around its Y-axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
