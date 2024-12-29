using UnityEngine;

public class BigHeadMode : MonoBehaviour
{
    [Header("Big Head Settings")]
    [SerializeField] private bool enableBigHead = false; // Toggle to enable/disable big head mode
    [Range(-10f, 10f)] [SerializeField] private float headScale = 2f; // Scale of the head, supports negative values
    private Transform headBone; // Reference to the head bone
    private Vector3 originalHeadScale; // Store the original scale of the head

    [Header("Other Bones Settings")]
    [SerializeField] private Transform bone1; // First additional bone
    [SerializeField] private Transform bone2; // Second additional bone
    [Range(-10f, 10f)] [SerializeField] private float otherBonesScale = 1f; // Scale of the additional bones, supports negative values
    private Vector3 originalBone1Scale; // Original scale of bone1
    private Vector3 originalBone2Scale; // Original scale of bone2

    private void Start()
    {
        // Find the head bone automatically
        headBone = FindHeadBone(transform);

        if (headBone == null)
        {
            Debug.LogError("Head bone not found! Please ensure the avatar's head bone has 'head' in its name.");
            enabled = false; // Disable the script if no head bone is found
            return;
        }

        // Save the original scale of the head bone
        originalHeadScale = headBone.localScale;

        // Save the original scales of the additional bones (if assigned)
        if (bone1 != null)
        {
            originalBone1Scale = bone1.localScale;
        }
        else
        {
            Debug.LogWarning("Bone1 is not assigned!");
        }

        if (bone2 != null)
        {
            originalBone2Scale = bone2.localScale;
        }
        else
        {
            Debug.LogWarning("Bone2 is not assigned!");
        }

        // Apply the initial states
        UpdateHeadScale();
        UpdateOtherBonesScale();
    }

    private void Update()
    {
        // Update the head scale dynamically if the values change
        UpdateHeadScale();

        // Update the additional bones scale dynamically if the values change
        UpdateOtherBonesScale();
    }

    /// <summary>
    /// Updates the scale of the head bone based on the toggle and slider value.
    /// </summary>
    private void UpdateHeadScale()
    {
        if (enableBigHead)
        {
            headBone.localScale = originalHeadScale * headScale;
        }
        else
        {
            headBone.localScale = originalHeadScale; // Reset to the original scale
        }
    }

    /// <summary>
    /// Updates the scale of the additional bones based on the slider value.
    /// </summary>
    private void UpdateOtherBonesScale()
    {
        if (bone1 != null)
        {
            bone1.localScale = originalBone1Scale * otherBonesScale;
        }

        if (bone2 != null)
        {
            bone2.localScale = originalBone2Scale * otherBonesScale;
        }
    }

    /// <summary>
    /// Finds the head bone recursively in the hierarchy.
    /// </summary>
    /// <param name="parent">The root transform to start searching from.</param>
    /// <returns>The Transform of the head bone if found; otherwise, null.</returns>
    private Transform FindHeadBone(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains("head")) // Look for "head" in the bone name
            {
                return child;
            }

            // Recursively search in the child objects
            Transform found = FindHeadBone(child);
            if (found != null)
                return found;
        }
        return null; // Return null if no head bone is found
    }

    /// <summary>
    /// Public method to toggle BigHeadMode externally.
    /// </summary>
    /// <param name="isEnabled">Enable or disable BigHeadMode.</param>
    public void SetBigHead(bool isEnabled)
    {
        enableBigHead = isEnabled;
    }

    /// <summary>
    /// Public method to set the scale value for the head externally.
    /// </summary>
    /// <param name="scale">The scale value for the head.</param>
    public void SetHeadScale(float scale)
    {
        headScale = scale;
    }

    /// <summary>
    /// Public method to set the scale value for the additional bones externally.
    /// </summary>
    /// <param name="scale">The scale value for the additional bones.</param>
    public void SetOtherBonesScale(float scale)
    {
        otherBonesScale = scale;
    }
}
