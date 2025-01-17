using UnityEngine;
using System.Collections.Generic;

public class WaypointManager : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public List<Transform> waypoints; // List of waypoints
    public float speed = 5f;          // Movement speed
    public float rotationSpeed = 5f; // Speed for orientation

    [Header("Modes")]
    public bool loop = true;          // Should the NPC loop the waypoints
    public bool pingPong = false;     // Should the NPC reverse direction
    public bool stopAtEnd = false;    // Should the NPC stop at the last waypoint

    private int currentWaypointIndex = 0;
    private bool isReversing = false; // Used for ping-pong mode

    void Update()
    {
        if (waypoints.Count == 0) return;

        // Move toward the current waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = targetWaypoint.position - transform.position;

        // Move and rotate toward the waypoint
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Check if we've reached the waypoint
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.1f)
        {
            GetNextWaypoint();
        }
    }

    private void GetNextWaypoint()
    {
        if (pingPong)
        {
            // Handle ping-pong behavior
            if (isReversing)
            {
                currentWaypointIndex--;
                if (currentWaypointIndex < 0)
                {
                    currentWaypointIndex = 1; // Avoid index out of bounds
                    isReversing = false;
                }
            }
            else
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Count)
                {
                    currentWaypointIndex = waypoints.Count - 2; // Avoid index out of bounds
                    isReversing = true;
                }
            }
        }
        else if (loop)
        {
            // Handle looping behavior
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
        else if (stopAtEnd)
        {
            // Handle stopping at the end
            if (currentWaypointIndex < waypoints.Count - 1)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            // Default to moving linearly through the list
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                currentWaypointIndex = waypoints.Count - 1; // Stay at the last waypoint
            }
        }
    }
}
