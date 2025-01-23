using UnityEngine;

public class OrbitCam : MonoBehaviour
{
    public Transform orbitPivot;  // The pivot around which the camera will orbit
    public float orbitRange = 140f;  // Total range of the orbit (in degrees)
    public float orbitSpeed = 2f;  // Speed at which the camera orbits
    public float orbitHeight = 5f;  // The height of the camera relative to the pivot
    public bool enableOrbit = true;  // Toggle the orbit movement in the inspector
    public bool allowPitch = true;  // Toggle to enable or disable the pitch (vertical movement)

    private float currentAngle = 0f;  // Current angle around the orbit (from -70 to +70 for 140 degrees)
    private float targetAngle = 0f;  // Target angle to smoothly move towards
    private float angleOffset = 0f;  // The angle offset used to control the orbit range
    private float smoothTime = 0.3f;  // Time for smooth damping (ease-in and ease-out)
    private float velocity = 0f;  // Used by SmoothDamp for smooth easing
    private Vector3 offset;  // Offset from the orbit pivot to position the camera

    private void Start()
    {
        // Calculate the initial offset based on the orbit range and height
        offset = new Vector3(0f, orbitHeight, 0f);
        
        // Set the initial current angle to match the starting position.
        currentAngle = Mathf.PingPong(Time.time * orbitSpeed, orbitRange) - (orbitRange / 2);
        
        // Ensure the camera starts smoothly without a jump
        targetAngle = currentAngle;  // Set targetAngle to the starting position
    }

    private void Update()
    {
        if (enableOrbit)
        {
            HandleOrbit();
        }
    }

    private void HandleOrbit()
    {
        // Update the angleOffset with ping-pong effect to smoothly oscillate the angle between -orbitRange/2 and orbitRange/2
        angleOffset = Mathf.PingPong(Time.time * orbitSpeed, orbitRange) - (orbitRange / 2);

        // Smoothly interpolate the angle to create an easing effect using SmoothDamp
        currentAngle = Mathf.SmoothDamp(currentAngle, angleOffset, ref velocity, smoothTime);

        // Calculate the new camera position based on the angle
        float angleInRadians = currentAngle * Mathf.Deg2Rad;

        // If allowPitch is disabled, set the vertical offset (Y-axis) to zero
        float verticalOffset = allowPitch ? offset.y : 0f;

        // Update the camera's position relative to the orbit pivot (around the Y-axis)
        Vector3 position = new Vector3(Mathf.Sin(angleInRadians) * offset.magnitude, verticalOffset, Mathf.Cos(angleInRadians) * offset.magnitude);

        // Update the camera's position
        transform.position = orbitPivot.position + position;

        // Make the camera look at the orbit pivot
        transform.LookAt(orbitPivot.position);
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        if (orbitPivot != null)
        {
            // Draw the orbit range as a sphere around the pivot point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(orbitPivot.position, offset.magnitude);

            // Draw the start and end points of the orbit range
            Vector3 startPosition = orbitPivot.position + new Vector3(Mathf.Sin(-orbitRange / 2f * Mathf.Deg2Rad) * offset.magnitude, offset.y, Mathf.Cos(-orbitRange / 2f * Mathf.Deg2Rad) * offset.magnitude);
            Vector3 endPosition = orbitPivot.position + new Vector3(Mathf.Sin(orbitRange / 2f * Mathf.Deg2Rad) * offset.magnitude, offset.y, Mathf.Cos(orbitRange / 2f * Mathf.Deg2Rad) * offset.magnitude);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(orbitPivot.position, startPosition);
            Gizmos.DrawLine(orbitPivot.position, endPosition);

            // Draw a wireframe cone showing the range of the orbit
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
            Gizmos.DrawWireSphere(orbitPivot.position, offset.magnitude);
            Gizmos.DrawLine(orbitPivot.position, startPosition);
            Gizmos.DrawLine(orbitPivot.position, endPosition);
        }
    }
    #endregion
}
