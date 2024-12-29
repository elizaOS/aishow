using UnityEngine;

public class CamMovement : MonoBehaviour
{
    public bool enableMovement = false; // Toggle movement in the inspector

    // Movement limits and time
    public float moveRangeX = 0f; // Range of movement on the X axis (default set to 0)
    public float moveRangeY = 0f; // Range of movement on the Y axis (default set to 0)
    public float moveRangeZ = 0f; // Range of movement on the Z axis (default set to 0)

    public float moveTimeX = 2f; // Time to move across X axis
    public float moveTimeY = 2f; // Time to move across Y axis
    public float moveTimeZ = 2f; // Time to move across Z axis

    private Vector3 initialPosition;
    private float timeElapsedX = 0f;
    private float timeElapsedY = 0f;
    private float timeElapsedZ = 0f;

    private void Start()
    {
        initialPosition = transform.position;

      
    }

    private void Update()
    {
        if (enableMovement)
        {
            HandlePingPongMovement();
        }
    }

    private void HandlePingPongMovement()
    {
        // Update time for each axis
        timeElapsedX += Time.deltaTime;
        timeElapsedY += Time.deltaTime;
        timeElapsedZ += Time.deltaTime;

        // Calculate the ping-pong effect for each axis with ease-in and ease-out
        float pingPongX = Mathf.SmoothStep(0f, moveRangeX, Mathf.PingPong(timeElapsedX / moveTimeX, 1f));
        float pingPongY = Mathf.SmoothStep(0f, moveRangeY, Mathf.PingPong(timeElapsedY / moveTimeY, 1f));
        float pingPongZ = Mathf.SmoothStep(0f, moveRangeZ, Mathf.PingPong(timeElapsedZ / moveTimeZ, 1f));

        // Update the camera's position with easing on all axes
        transform.position = initialPosition + new Vector3(pingPongX, pingPongY, pingPongZ);
    }

    #region Gizmos
    private void OnDrawGizmos()
    {
        // Draw the movement ranges for X, Y, Z axes
        Gizmos.color = Color.red;
        Gizmos.DrawLine(initialPosition, initialPosition + new Vector3(moveRangeX, 0, 0)); // X axis range

        Gizmos.color = Color.green;
        Gizmos.DrawLine(initialPosition, initialPosition + new Vector3(0, moveRangeY, 0)); // Y axis range

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(initialPosition, initialPosition + new Vector3(0, 0, moveRangeZ)); // Z axis range

        // Optional: Draw a box showing the full movement range area
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f); // Semi-transparent
        Gizmos.DrawWireCube(initialPosition + new Vector3(moveRangeX / 2, moveRangeY / 2, moveRangeZ / 2), new Vector3(moveRangeX, moveRangeY, moveRangeZ));
    }
    #endregion
}
