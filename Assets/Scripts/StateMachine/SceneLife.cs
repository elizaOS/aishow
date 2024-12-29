using UnityEngine;

public class SceneLife : MonoBehaviour
{
    private void OnEnable()
    {
        //Debug.Log("Scene GameObject enabled. Reinitializing SpeakPayloadManager.");
        SpeakPayloadManager.Instance?.Reinitialize(); // Safely call reinitialization
    }

    private void OnDisable()
    {
        //Debug.Log("Scene GameObject disabled. Cleaning up SpeakPayloadManager.");
        SpeakPayloadManager.Instance?.ClearState(); // Optionally clean up
    }
}
