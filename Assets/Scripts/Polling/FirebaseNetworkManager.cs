using System; // For Task and Func
using System.Collections; // For IEnumerator and Coroutines
using System.Collections.Generic; // For Dictionary and other collections

using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class FirebaseNetworkManager : MonoBehaviour
{
    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 1f;

    // Fetch data from Firebase with retry logic
    public static IEnumerator GetFromFirebase(string url, Action<string> onSuccess, Action<string> onError)
    {
        int retryCount = 0;
        while (retryCount < MAX_RETRIES)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"Firebase GET request failed (Attempt {retryCount + 1}): {request.error}");
                    retryCount++;

                    if (retryCount < MAX_RETRIES)
                    {
                        yield return new WaitForSeconds(RETRY_DELAY);
                    }
                    else
                    {
                        onError?.Invoke(request.error);
                    }
                }
            }
        }
    }

    // Send data to Firebase with retry logic
    public static IEnumerator SendToFirebase(string url, string jsonData, Action<string> onSuccess, Action<string> onError)
    {
        int retryCount = 0;
        while (retryCount < MAX_RETRIES)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(jsonData);
                    Debug.LogWarning(jsonData);
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"Firebase PUT request failed (Attempt {retryCount + 1}): {request.error}");
                    retryCount++;

                    if (retryCount < MAX_RETRIES)
                    {
                        yield return new WaitForSeconds(RETRY_DELAY);
                    }
                    else
                    {
                        onError?.Invoke(request.error);
                    }
                }
            }
        }
    }
}