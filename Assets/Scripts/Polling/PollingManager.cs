using System; // For Task and Func
using System.Collections; // For IEnumerator and Coroutines
using System.Collections.Generic; // For Dictionary and other collections

using UnityEngine;

public class PollingManager : MonoBehaviour
{
    private Coroutine pollingCoroutine;
    private bool isPolling = false;

    private void Awake()
    {
        // This will keep the PollingManager persistent between scene loads
        DontDestroyOnLoad(gameObject);
    }

    // Public getter for polling status
    public bool IsPolling()
    {
        return isPolling;
    }

    public void StartPolling(float pollingInterval, Action onFetch)
    {
        if (isPolling)
        {
            Debug.LogWarning("Polling is already running.");
            return;
        }

        isPolling = true;
        pollingCoroutine = StartCoroutine(Poll(pollingInterval, onFetch));
    }

    public void StopPolling()
    {
        if (!isPolling)
        {
            Debug.LogWarning("Polling is not running.");
            return;
        }

        isPolling = false;
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }

        Debug.Log("Polling has been stopped.");
    }

    private IEnumerator Poll(float interval, Action onFetch)
    {
        while (isPolling)
        {
            onFetch?.Invoke();
            yield return new WaitForSeconds(interval);
        }
    }
}

