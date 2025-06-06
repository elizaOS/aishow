using UnityEngine;

/// <summary>
/// Inversely toggles an array of GameObjects based on this object's active state.
/// When this object is enabled, the referenced objects are disabled.
/// When this object is disabled, the referenced objects are enabled.
/// </summary>
public class InvertOverride : MonoBehaviour
{
    [Tooltip("GameObjects that will be toggled inversely to this object's active state")]
    [SerializeField] private GameObject[] objectsToToggle;

    private void OnEnable()
    {
        ToggleObjects(false);
    }

    private void OnDisable()
    {
        ToggleObjects(true);
    }

    /// <summary>
    /// Sets the active state of all referenced GameObjects
    /// </summary>
    /// <param name="state">The active state to set for all objects</param>
    private void ToggleObjects(bool state)
    {
        if (objectsToToggle == null || objectsToToggle.Length == 0)
        {
            Debug.LogWarning("No GameObjects assigned to toggle in InvertOverride", this);
            return;
        }

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
            else
            {
                Debug.LogWarning("Null GameObject reference in InvertOverride's objectsToToggle array", this);
            }
        }
    }
} 