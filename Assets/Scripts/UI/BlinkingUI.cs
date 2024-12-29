using UnityEngine;
using UnityEngine.UI;

public class BlinkingUI : MonoBehaviour
{
    [SerializeField] private float blinkInterval = 0.5f; // Interval in seconds between blinks
    [SerializeField] private bool isBlinking = true; // Toggle for enabling/disabling the blinking effect

    private RawImage rawImage; // The RawImage component attached to the GameObject
    private float timer; // Timer to track intervals

    private void Awake()
    {
        // Get the RawImage component from this GameObject
        rawImage = GetComponent<RawImage>();

        if (rawImage == null)
        {
            Debug.LogError("BlinkingRawImage script requires a RawImage component on the same GameObject.");
        }
    }

    private void Update()
    {
        // If blinking is disabled, ensure the RawImage remains active and exit
        if (!isBlinking)
        {
            SetRawImageActive(true);
            return;
        }

        // Timer logic to toggle the RawImage on and off
        timer += Time.deltaTime;
        if (timer >= blinkInterval)
        {
            timer = 0f; // Reset the timer
            ToggleRawImage();
        }
    }

    /// <summary>
    /// Toggles the RawImage's enabled state.
    /// </summary>
    private void ToggleRawImage()
    {
        if (rawImage != null)
        {
            rawImage.enabled = !rawImage.enabled; // Toggle the enabled state
        }
    }

    /// <summary>
    /// Sets the RawImage to a specific active state.
    /// </summary>
    /// <param name="isActive">True to enable, false to disable.</param>
    private void SetRawImageActive(bool isActive)
    {
        if (rawImage != null)
        {
            rawImage.enabled = isActive;
        }
    }

    /// <summary>
    /// Enables or disables the blinking effect.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    public void SetBlinking(bool enabled)
    {
        isBlinking = enabled;
        if (!isBlinking)
        {
            SetRawImageActive(true); // Ensure the RawImage is visible when blinking is stopped
        }
    }
}
