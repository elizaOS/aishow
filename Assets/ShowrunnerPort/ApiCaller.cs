using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor; // Required for EditorApplication.delayCall
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
                    tcs.SetException(e);
                }
            };
            return tcs.Task;
        }
#endif

        private static async Task<(UnityWebRequest.Result result, byte[] data, string text, string error)> SendUnityWebRequestAsync(string url, string method, byte[] bodyRaw, Dictionary<string, string> headers)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, method))
            {
                if (bodyRaw != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json"); // Default, can be overridden by headers
                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        req.SetRequestHeader(kvp.Key, kvp.Value);
                    }
                }

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield(); // Yield to allow other operations
                }
                return (req.result, req.downloadHandler.data, req.downloadHandler.text, req.error);
            }
        }

        public static async Task<TResponse> PostJsonAsync<TResponse>(string url, object payload, Dictionary<string, string> headers = null)
        {
            string json = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            
            Func<Task<(UnityWebRequest.Result, byte[], string, string)>> sendRequestFunc = 
                () => SendUnityWebRequestAsync(url, "POST", bodyRaw, headers);

#if UNITY_EDITOR
            var (result, _, responseText, error) = await ExecuteOnMainThreadAsync(sendRequestFunc);
#else
            // For runtime, assume we are already on the main thread or Unity's WebRequest handles it.
            // If issues arise at runtime, a runtime-specific main thread dispatcher will be needed here.
            var (result, _, responseText, error) = await sendRequestFunc();
#endif

            if (result == UnityWebRequest.Result.Success)
            {
                return JsonConvert.DeserializeObject<TResponse>(responseText);
            }
            else
            {
                throw new Exception($"API call failed: {error}");
            }
        }

        // New method for binary/audio response
        public static async Task<byte[]> PostJsonForBytesAsync(string url, object payload, Dictionary<string, string> headers = null)
        {
            string json = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            Func<Task<(UnityWebRequest.Result, byte[], string, string)>> sendRequestFunc =
                () => SendUnityWebRequestAsync(url, "POST", bodyRaw, headers);
            
#if UNITY_EDITOR
            var (result, responseData, _, error) = await ExecuteOnMainThreadAsync(sendRequestFunc);
#else
            // For runtime, assume we are already on the main thread or Unity's WebRequest handles it.
            // If issues arise at runtime, a runtime-specific main thread dispatcher will be needed here.
            var (result, responseData, _, error) = await sendRequestFunc();
#endif

            if (result == UnityWebRequest.Result.Success)
            {
                return responseData;
            }
            else
            {
                throw new Exception($"API call failed: {error}");
            }
        }
    }
} 