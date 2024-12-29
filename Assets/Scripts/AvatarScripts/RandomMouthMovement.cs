using UnityEngine;
using UnityEngine.Events;

public class RandomMouthMovement : MonoBehaviour
{
    // Reference to the SkinnedMeshRenderer
    public SkinnedMeshRenderer faceMesh;

    // List of blend shape names (dynamically populated)
    public string[] availableBlendShapeNames;

    // List of blend shape indices that should be randomized
    private bool[] randomizeBlendShapes;

    // List of blend shape indices to animate (set before runtime)
    public int[] blendShapesToAnimate;

    // Range for random weights
    public float minWeight = 0f;
    public float maxWeight = 100f;

    // Speed of randomization
    public float changeInterval = 0.1f;

    // Timer to control when to change blend shapes
    private float timer;

    // Control the randomization (whether it's active or not)
    public bool isActive = false;

    // Event that triggers the random mouth movement (can be linked to events like "Start Talking")
    public UnityEvent onStartTalking;
    public UnityEvent onStopTalking;

    // Internal flag to check if randomization is in progress
    private bool isRandomizing;

    void Start()
    {
        if (faceMesh != null)
        {
            int blendShapeCount = faceMesh.sharedMesh.blendShapeCount;

            // Initialize blend shape arrays
            availableBlendShapeNames = new string[blendShapeCount];
            randomizeBlendShapes = new bool[blendShapeCount];

            for (int i = 0; i < blendShapeCount; i++)
            {
                availableBlendShapeNames[i] = faceMesh.sharedMesh.GetBlendShapeName(i);
                randomizeBlendShapes[i] = false; // Default: don't randomize
                //Debug.Log($"Blend Shape {i}: {availableBlendShapeNames[i]}");
            }

            // Configure blend shapes to animate based on `blendShapesToAnimate`
            MapBlendShapesToAnimate();
        }
        else
        {
            Debug.LogError("FaceMesh is not assigned. Please assign a SkinnedMeshRenderer.");
        }
    }

    void Update()
    {
        if (isActive && !isRandomizing)
        {
            timer += Time.deltaTime;

            if (timer >= changeInterval)
            {
                RandomizeMouthBlendShapes();
                timer = 0f; // Reset the timer after each change
            }
        }
    }

    // Configure `randomizeBlendShapes` based on `blendShapesToAnimate`
    private void MapBlendShapesToAnimate()
    {
        if (blendShapesToAnimate != null)
        {
            foreach (int index in blendShapesToAnimate)
            {
                if (index >= 0 && index < randomizeBlendShapes.Length)
                {
                    randomizeBlendShapes[index] = true; // Enable randomization for specified indices
                    //Debug.Log($"Mapped Blend Shape Index {index}: {availableBlendShapeNames[index]}");
                }
                else
                {
                    //Debug.LogWarning($"Blend Shape Index {index} is out of range and will be ignored.");
                }
            }
        }
    }

    // Starts the random mouth movement (triggered via event or directly)
    public void StartRandomMouthMovement()
    {
        if (!isActive)
        {
            isActive = true;
            //Debug.Log("Started Mouth Movement");
            onStartTalking?.Invoke(); // Trigger the start event if available
        }
    }

    // Stops the random mouth movement (triggered via event or directly)
    public void StopRandomMouthMovement()
    {
        if (isActive)
        {
            isActive = false;
            //Debug.Log("Stopped Mouth Movement");
            ResetBlendShapes(); // Reset all blend shape weights to zero
            onStopTalking?.Invoke(); // Trigger the stop event if available
        }
    }

    // Randomizes the weights of the selected blend shapes
    private void RandomizeMouthBlendShapes()
    {
        if (faceMesh != null && availableBlendShapeNames.Length > 0)
        {
            isRandomizing = true;

            for (int i = 0; i < availableBlendShapeNames.Length; i++)
            {
                // Apply randomization only to the selected blend shapes
                if (randomizeBlendShapes[i])
                {
                    float randomWeight = Random.Range(minWeight, maxWeight);
                    faceMesh.SetBlendShapeWeight(i, randomWeight);
                }
            }

            isRandomizing = false;
        }
    }

    // Resets all blend shape weights to zero
    private void ResetBlendShapes()
    {
        if (faceMesh != null)
        {
            for (int i = 0; i < availableBlendShapeNames.Length; i++)
            {
                faceMesh.SetBlendShapeWeight(i, 0f);
            }

            //Debug.Log("All blend shapes reset to zero.");
        }
    }

    // Example trigger from external systems (such as a dialogue manager)
    public void OnCharacterStartsTalking()
    {
        StartRandomMouthMovement();
    }

    public void OnCharacterStopsTalking()
    {
        StopRandomMouthMovement();
    }
}
