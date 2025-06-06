using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor; // Required for EditorApplication.delayCall
using UnityEngine; // Added for Debug.Log
#endif

namespace ShowGenerator
{
    public static class ApiCaller
    {
#if UNITY_EDITOR
        // Helper to execute a function on the main thread and return its Task
        private static Task<T> ExecuteOnMainThreadAsync<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            EditorApplication.delayCall += async () =>
            {
                try
                {
                    var result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ApiCaller.ExecuteOnMainThreadAsync.delayCall] Exception: {e.Message}\n{e.StackTrace}");
                    tcs.SetException(e);
                }
            };
            return tcs.Task;
        }

        // Overload for Action
        private static Task ExecuteOnMainThreadAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object>(); // Using object as a placeholder
            EditorApplication.delayCall += async () =>
            {
                try
                {
                    await action();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ApiCaller.ExecuteOnMainThreadAsync Action.delayCall] Exception: {e.Message}\n{e.StackTrace}");
                    tcs.SetException(e);
                }
            };
            return tcs.Task;
        }
#endif

        private static async Task<(UnityWebRequest.Result result, byte[] data, string text, string error)> SendUnityWebRequestAsync(string url, string method, byte[] bodyRaw, Dictionary<string, string> headers)
        {
            // Debug.Log($"[ApiCaller.SendUnityWebRequestAsync] Starting. URL: {url}, Method: {method}");
            using (UnityWebRequest req = new UnityWebRequest(url, method))
            {
                if (bodyRaw != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    // Debug.Log($"[ApiCaller.SendUnityWebRequestAsync] Attached bodyRaw, length: {bodyRaw.Length}");
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json"); // Default, can be overridden by headers
                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        req.SetRequestHeader(kvp.Key, kvp.Value);
                        // Debug.Log($"[ApiCaller.SendUnityWebRequestAsync] Set header: {kvp.Key}: {kvp.Value}");
                    }
                }
                req.timeout = 60; // Added a 60-second timeout
                // Debug.Log($"[ApiCaller.SendUnityWebRequestAsync] Sending UnityWebRequest to {url}. Timeout set to {req.timeout}s.");
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield(); // Yield to allow other operations
                }
                // Debug.Log($"[ApiCaller.SendUnityWebRequestAsync] UnityWebRequest completed. URL: {url}, Result: {req.result}, Error: {req.error}, HTTP Status: {req.responseCode}");
                if (req.result != UnityWebRequest.Result.Success)
                {
                     Debug.LogError($"[ApiCaller.SendUnityWebRequestAsync] DownloadHandler text (on error): {req.downloadHandler.text}");
                }
                return (req.result, req.downloadHandler.data, req.downloadHandler.text, req.error);
            }
        }

        public static async Task<TResponse> PostJsonAsync<TResponse>(string url, object payload, Dictionary<string, string> headers = null)
        {
            // Debug.Log($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] Preparing for URL: {url}");
            string json = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            // Debug.Log($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] Payload serialized. JSON length: {json.Length}");
            
            Func<Task<(UnityWebRequest.Result, byte[], string, string)>> sendRequestFunc = 
                () => SendUnityWebRequestAsync(url, "POST", bodyRaw, headers);

            // Debug.Log($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] Awaiting ExecuteOnMainThreadAsync.");
#if UNITY_EDITOR
            var (result, _, responseText, error) = await ExecuteOnMainThreadAsync(sendRequestFunc);
#else
            // For runtime, assume we are already on the main thread or Unity's WebRequest handles it.
            // If issues arise at runtime, a runtime-specific main thread dispatcher will be needed here.
            var (result, _, responseText, error) = await sendRequestFunc();
#endif
            // Debug.Log($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] ExecuteOnMainThreadAsync completed. Result: {result}");

            if (result == UnityWebRequest.Result.Success)
            {
                // Debug.Log($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] Success. Deserializing response.");
                return JsonConvert.DeserializeObject<TResponse>(responseText);
            }
            else
            {
                Debug.LogError($"[ApiCaller.PostJsonAsync<{typeof(TResponse).Name}>] API call failed with error: {error}. Response text: {responseText}");
                throw new Exception($"API call failed: {error}");
            }
        }

        // New method for binary/audio response
        public static async Task<byte[]> PostJsonForBytesAsync(string url, object payload, Dictionary<string, string> headers = null)
        {
            // Debug.Log($"[ApiCaller.PostJsonForBytesAsync] Preparing for URL: {url}");
            string json = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            // Debug.Log($"[ApiCaller.PostJsonForBytesAsync] Payload serialized. JSON length: {json.Length}, Raw body length: {bodyRaw.Length}");

            Func<Task<(UnityWebRequest.Result, byte[], string, string)>> sendRequestFunc =
                () => SendUnityWebRequestAsync(url, "POST", bodyRaw, headers);
            
            // Debug.Log("[ApiCaller.PostJsonForBytesAsync] Awaiting ExecuteOnMainThreadAsync.");
#if UNITY_EDITOR
            var (result, responseData, responseText, error) = await ExecuteOnMainThreadAsync(sendRequestFunc);
#else
            // For runtime, assume we are already on the main thread or Unity's WebRequest handles it.
            // If issues arise at runtime, a runtime-specific main thread dispatcher will be needed here.
            var (result, responseData, responseText, error) = await sendRequestFunc();
#endif
            // Debug.Log($"[ApiCaller.PostJsonForBytesAsync] ExecuteOnMainThreadAsync completed. Result: {result}");

            if (result == UnityWebRequest.Result.Success)
            {
                // Debug.Log($"[ApiCaller.PostJsonForBytesAsync] Success. Returning {responseData?.Length ?? 0} bytes.");
                return responseData;
            }
            else
            {
                Debug.LogError($"[ApiCaller.PostJsonForBytesAsync] API call failed with error: {error}. Response text from downloadhandler: {responseText}");
                throw new Exception($"API call failed: {error}");
            }
        }
    }
} 