using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace ShowGenerator
{
    public static class ApiCaller
    {
        public static async Task<TResponse> PostJsonAsync<TResponse>(string url, object payload, Dictionary<string, string> headers = null)
        {
            string json = JsonConvert.SerializeObject(payload);
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        req.SetRequestHeader(kvp.Key, kvp.Value);
                    }
                }
                var op = req.SendWebRequest(); 
                while (!op.isDone) await Task.Yield();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string responseText = req.downloadHandler.text;
                    return JsonConvert.DeserializeObject<TResponse>(responseText);
                }
                else
                {
                    throw new Exception($"API call failed: {req.error}");
                }
            }
        }

        // New method for binary/audio response
        public static async Task<byte[]> PostJsonForBytesAsync(string url, object payload, Dictionary<string, string> headers = null)
        {
            string json = JsonConvert.SerializeObject(payload);
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        req.SetRequestHeader(kvp.Key, kvp.Value);
                    }
                }
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    return req.downloadHandler.data;
                }
                else
                {
                    throw new Exception($"API call failed: {req.error}");
                }
            }
        }
    }
} 