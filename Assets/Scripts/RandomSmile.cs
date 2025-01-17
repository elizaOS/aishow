using System.Collections;
using UnityEngine;

public class RandomSmile : MonoBehaviour
{
    [Header("Smile Settings")]
    public int smileBlendShapeIndex = 1;       // The index of the smile blend shape (adjust this to match your model's blend shape index)
    public float minSmileInterval = 5.0f;      // Minimum time before a random smile
    public float maxSmileInterval = 10.0f;     // Maximum time before a random smile
    public float smileDuration = 0.5f;         // Duration of the smile (how long the smile lasts)
    public float smileSpeed = 2.0f;            // Speed of the smile animation (how fast it transitions)
    public float maxSmileWeight = 70.0f;       // Maximum weight of the smile blend shape (dampen smile intensity)

    private SkinnedMeshRenderer skinnedMeshRenderer; // Reference to the SkinnedMeshRenderer component
    private float currentSmileTime = 0f;       // Timer for when to trigger the next smile
    private float currentSmileValue = 0f;      // Current value of the blend shape (used to animate smile)
    private bool isSmiling = false;            // Whether the character is currently smiling

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

        // Start the first smile cycle
        ScheduleNextSmile();
    }

    private void Update()
    {
        // If smiling, animate the smile blend shape
        if (isSmiling)
        {
            // Use SmoothStep for ease-in, ease-out animation
            currentSmileValue = Mathf.Lerp(currentSmileValue, maxSmileWeight, Time.deltaTime * smileSpeed);
            currentSmileValue = Mathf.SmoothStep(0f, maxSmileWeight, currentSmileValue / maxSmileWeight); // Apply the easing effect

            skinnedMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, currentSmileValue);

            if (currentSmileValue >= maxSmileWeight - 1f) // If it's almost fully smiled, start to release
            {
                isSmiling = false;
                StartCoroutine(ReleaseSmile());
            }
        }
        else
        {
            // If not smiling, wait for the next smile
            currentSmileTime += Time.deltaTime;
            if (currentSmileTime >= Random.Range(minSmileInterval, maxSmileInterval))
            {
                isSmiling = true;
                currentSmileValue = 0f; // Start with no smile
            }
        }
    }

    // Coroutine to release the smile after the duration
    private IEnumerator ReleaseSmile()
    {
        float timer = 0f;
        float initialSmileValue = currentSmileValue;

        while (timer < smileDuration)
        {
            timer += Time.deltaTime;
            // Apply easing for the release of the smile as well
            currentSmileValue = Mathf.Lerp(initialSmileValue, 0f, Mathf.SmoothStep(0f, 1f, timer / smileDuration));
            skinnedMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, currentSmileValue);
            yield return null;
        }

        currentSmileValue = 0f; // Ensure the blend shape is fully reset
        skinnedMeshRenderer.SetBlendShapeWeight(smileBlendShapeIndex, currentSmileValue);

        // Schedule the next smile after the specified interval
        ScheduleNextSmile();
    }

    // Schedule the next smile randomly
    private void ScheduleNextSmile()
    {
        currentSmileTime = 0f; // Reset the timer for the next smile
    }
}
