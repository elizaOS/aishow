using UnityEngine;

/// <summary>
/// Continuously rotates the GameObject around its Y axis at a configurable speed.
/// Attach this script to any GameObject you want to spin.
/// </summary>
public class RotateOnY : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 90f;

    void Update()
    {
        // Rotate around Y axis at the specified speed (degrees per second)
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
} 