using UnityEngine;
using System.Collections;

public class RandomBlink : MonoBehaviour
{
    [Header("Blink Settings")]
    public int blinkBlendShapeIndex = 0;       // The index of the blink blend shape (adjust this to match your model's blend shape index)
    public float minBlinkInterval = 3.0f;      // Minimum time before a random blink
    public float maxBlinkInterval = 6.0f;      // Maximum time before a random blink
    public float blinkDuration = 0.1f;         // Duration of the blink (how long the blink lasts)
    public float blinkSpeed = 2.0f;            // Speed of the blink animation (how fast it opens and closes)

    private SkinnedMeshRenderer skinnedMeshRenderer; // Reference to the SkinnedMeshRenderer component
    private float currentBlinkTime = 0f;        // Timer for when to trigger the next blink
    private float currentBlinkValue = 0f;       // Current value of the blend shape (used to animate blink)
    private bool isBlinking = false;            // Whether the character is currently blinking

    private void Start()
    {
        // Get the SkinnedMeshRenderer component from the actor
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("No SkinnedMeshRenderer found. Make sure your actor has one.");
            enabled = false;
            return;
        }

        // Start the first blink cycle
        ScheduleNextBlink();
    }

    private void Update()
    {
        // If blinking, animate the blink blend shape
        if (isBlinking)
        {
            // Use SmoothStep for ease-in, ease-out animation
            currentBlinkValue = Mathf.Lerp(currentBlinkValue, 100f, Time.deltaTime * blinkSpeed); 
            currentBlinkValue = Mathf.SmoothStep(0f, 100f, currentBlinkValue / 100f); // Apply the easing effect

            skinnedMeshRenderer.SetBlendShapeWeight(blinkBlendShapeIndex, currentBlinkValue);

            if (currentBlinkValue >= 99f) // If it's almost fully closed, start to open
            {
                isBlinking = false;
                StartCoroutine(OpenEyes());
            }
        }
        else
        {
            // If not blinking, wait for the next blink
            currentBlinkTime += Time.deltaTime;
            if (currentBlinkTime >= Random.Range(minBlinkInterval, maxBlinkInterval))
            {
                isBlinking = true;
                currentBlinkValue = 0f; // Start with open eyes
            }
        }
    }

    // Coroutine to open the eyes after the blink
    private IEnumerator OpenEyes()
    {
        float timer = 0f;
        float initialBlinkValue = currentBlinkValue;

        while (timer < blinkDuration)
        {
            timer += Time.deltaTime;
            // Apply easing for the opening of the eyes as well
            currentBlinkValue = Mathf.Lerp(initialBlinkValue, 0f, Mathf.SmoothStep(0f, 1f, timer / blinkDuration));
            skinnedMeshRenderer.SetBlendShapeWeight(blinkBlendShapeIndex, currentBlinkValue);
            yield return null;
        }

        currentBlinkValue = 0f; // Ensure the blend shape is fully open
        skinnedMeshRenderer.SetBlendShapeWeight(blinkBlendShapeIndex, currentBlinkValue);

        // Schedule the next blink after the specified interval
        ScheduleNextBlink();
    }

    // Schedule the next blink randomly
    private void ScheduleNextBlink()
    {
        currentBlinkTime = 0f; // Reset the timer for the next blink
    }
}
