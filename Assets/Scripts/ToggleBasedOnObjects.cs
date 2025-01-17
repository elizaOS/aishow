using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleBasedOnObjects : MonoBehaviour
{
    [Tooltip("Array of GameObjects to monitor.")]
    public GameObject[] objectsToMonitor; // Array of objects to check

    [Tooltip("How often to check for active objects (in seconds).")]
    public float checkInterval = 0.5f; // Time interval to check the objects

    public GameObject controlledObject; // The object to control the audio volume

    private AudioSource audioSource; // AudioSource component on the controlledObject

    private void Start()
    {
        if (controlledObject != null)
        {
            audioSource = controlledObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("Controlled Object does not have an AudioSource component.");
            }
        }

        // Start the repeating check coroutine
        StartCoroutine(CheckObjects());
    }

    private IEnumerator CheckObjects()
    {
        while (true)
        {
            bool anyActive = false;

            foreach (GameObject obj in objectsToMonitor)
            {
                if (obj != null && obj.activeSelf)
                {
                    anyActive = true;
                    break;
                }
            }

            if (audioSource != null)
            {
                audioSource.volume = anyActive ? 0.4f : 0f;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}
