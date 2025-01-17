using UnityEngine;
using System.Collections;

public class MoveUI : MonoBehaviour
{
    // The distance to move the UI element on the Y-axis
    public float moveDistance = 35f;
    // Duration for the movement to complete
    public float moveDuration = 1f;

    // Reference to the RectTransform of the UI element
    private RectTransform rectTransform;

    // Store the target position for smooth movement
    private Vector3 targetPosition;

    // Flag to prevent multiple movements at the same time
    private bool isMoving = false;

    private void Start()
    {
        // Get the RectTransform component attached to the same GameObject
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;  // Initial position
    }

    // Method to move the UI element up by 'moveDistance' on the Y-axis
    public void MoveUp()
    {
        if (!isMoving)
        {
            targetPosition = rectTransform.anchoredPosition + new Vector2(0, moveDistance);
            StartCoroutine(SmoothMove());
        }
    }

    // Method to move the UI element back down by 'moveDistance' on the Y-axis
    public void MoveDown()
    {
        if (!isMoving)
        {
            targetPosition = rectTransform.anchoredPosition - new Vector2(0, moveDistance);
            StartCoroutine(SmoothMove());
        }
    }

    // Coroutine to smoothly move the UI element
    private IEnumerator SmoothMove()
    {
        isMoving = true;
        Vector3 startPosition = rectTransform.anchoredPosition;
        float timeElapsed = 0f;

        // Smooth transition from start position to target position using Lerp
        while (timeElapsed < moveDuration)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, timeElapsed / moveDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is exactly the target
        rectTransform.anchoredPosition = targetPosition;
        isMoving = false;
    }
}
