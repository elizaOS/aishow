using UnityEngine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using ShowGenerator;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ShowGenerator
{
    public class ShowrunnerSpeakerElevenLabs : MonoBehaviour
    {
        // Audio is always saved to Resources/Episodes/<episodeID>/audio for each episode.
        // The outputPath field is no longer used and has been removed for clarity.
        private const string DirectElevenLabsApiEndpointBase = "https://api.elevenlabs.io/v1/text-to-speech/";
        // Default outputPath, can be configured in Inspector or changed programmatically
        public string outputPath = "Assets/AudioOutput/ElevenLabsAudio";

        // Method to be called by ShowrunnerManager
        public async Task GenerateAndSaveAudioForEpisode(ShowEpisode episode, Dictionary<string, string> actorVoiceMap, ShowGenerator.ShowGeneratorApiKeys apiKeys, bool useWrapper, System.Threading.CancellationToken cancellationToken)
        {
            Debug.Log("Starting audio generation for episode");
            try
            {
                if (episode == null || episode.scenes == null)
                {
                    Debug.LogError("Episode or scenes list is null. Cannot generate audio.");
                    return;
                }
                if (actorVoiceMap == null)
                {
                    Debug.LogError("Actor voice map is null. Cannot generate audio.");
                    return;
                }
                if (apiKeys == null)
                {
                    Debug.LogError("API Keys are not provided. Cannot generate audio.");
                    return;
                }

                if (useWrapper)
                {
                    if (string.IsNullOrEmpty(apiKeys.elevenLabsWrapperUrl))
                    {
                        Debug.LogWarning("Using wrapper, but ElevenLabs Wrapper URL is not configured in API Keys. Skipping audio generation.");
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(apiKeys.elevenLabsApiKey))
                    {
                        Debug.LogWarning("Not using wrapper, and ElevenLabs API Key is not configured. Skipping audio generation.");
                        return;
                    }
                }

                string episodePath = Path.Combine(Application.dataPath, "Resources", "Episodes", episode.id, "audio");
                Directory.CreateDirectory(episodePath);

                int sceneIndex = 0;
                foreach (var scene in episode.scenes)
                {
                    if (cancellationToken.IsCancellationRequested) { Debug.Log("Audio generation cancelled (scene loop)"); return; }
                    if (scene.dialogue == null) { Debug.LogWarning($"Scene '{scene?.location}' has null dialogue"); sceneIndex++; continue; }
                    int dialogueIndex = 0;
                    foreach (var dialogue in scene.dialogue)
                    {
                        if (cancellationToken.IsCancellationRequested) { Debug.Log("Audio generation cancelled (dialogue loop)"); return; }
                        string fileName = Path.Combine(episodePath, $"{episode.id}_{sceneIndex + 1}_{dialogueIndex + 1}.mp3");
                        Debug.Log($"Processing audio for actor: {dialogue.actor}, line: {dialogue.line}");
                        await GenerateAndSaveAudio(dialogue, voiceId: actorVoiceMap.TryGetValue(dialogue.actor, out var v) ? v : null, fileName, apiKeys, useWrapper, cancellationToken);
                        if (cancellationToken.IsCancellationRequested) { Debug.Log("Audio generation cancelled (after line)"); return; }
                        dialogueIndex++;
                    }
                    sceneIndex++;
                }
                Debug.Log($"Finished processing audio for episode: {episode.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in GenerateAndSaveAudioForEpisode: {ex.Message}");
            }
        }

        // Helper to run code on the main thread and await its completion (Editor only)
        private static Task RunOnMainThread(Action action)
        {
#if UNITY_EDITOR
            var tcs = new TaskCompletionSource<bool>();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                try { action(); tcs.SetResult(true); }
                catch (Exception ex) { tcs.SetException(ex); }
            };
            return tcs.Task;
#else
            action();
            return Task.CompletedTask;
#endif
        }

        // Generic version for returning a value
        private static Task<T> RunOnMainThread<T>(Func<T> func)
        {
#if UNITY_EDITOR
            var tcs = new TaskCompletionSource<T>();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            };
            return tcs.Task;
#else
            return Task.FromResult(func());
#endif
        }

        // Serializable request class for ElevenLabs wrapper
        [Serializable]
        private class ElevenLabsRequest
        {
            public string text;
            public string voice_id;
        }

        private async Task GenerateAndSaveAudio(ShowDialogue dialogue, string voiceId, string filePath, ShowGenerator.ShowGeneratorApiKeys apiKeys, bool useWrapper, System.Threading.CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) { Debug.Log("Audio generation cancelled (before line)"); return; }
            const int MAX_RETRIES = 5;
            const int BASE_DELAY_MS = 2000;
            int attempt = 0;

            while (attempt < MAX_RETRIES)
            {
                if (cancellationToken.IsCancellationRequested) { Debug.Log("Audio generation cancelled (in retry loop)"); return; }
                attempt++;
                try
                {
                    var result = await RunOnMainThread(() =>
                    {
                        var res = new {
                            Success = false,
                            Error = "",
                            ResponseCode = 0
                        };
                        string endpointUrl = string.Empty;
                        string payloadJson = string.Empty;
                        UnityWebRequest www = null;
                        try
                        {
                            if (useWrapper)
                            {
                                endpointUrl = apiKeys.elevenLabsWrapperUrl;
                                var payload = new ElevenLabsRequest { text = dialogue.line, voice_id = voiceId };
                                payloadJson = JsonUtility.ToJson(payload);
                                www = new UnityWebRequest(endpointUrl, "POST");
                                byte[] bodyRaw = Encoding.UTF8.GetBytes(payloadJson);
                                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                                www.downloadHandler = new DownloadHandlerBuffer();
                                www.SetRequestHeader("Content-Type", "application/json");
                            }
                            else
                            {
                                endpointUrl = DirectElevenLabsApiEndpointBase + voiceId;
                                var payload = new { text = dialogue.line };
                                payloadJson = JsonUtility.ToJson(payload);
                                www = new UnityWebRequest(endpointUrl, "POST");
                                byte[] bodyRaw = Encoding.UTF8.GetBytes(payloadJson);
                                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                                www.downloadHandler = new DownloadHandlerBuffer();
                                www.SetRequestHeader("Content-Type", "application/json");
                                www.SetRequestHeader("xi-api-key", apiKeys.elevenLabsApiKey);
                            }
                            www.timeout = 30;
                            Debug.Log($"Attempt {attempt}/{MAX_RETRIES} for actor '{dialogue.actor}'");
                            var operation = www.SendWebRequest();
                            while (!operation.isDone) { }
                            Debug.Log($"Network call completed. Result: {www.result}");
                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                File.WriteAllBytes(filePath, www.downloadHandler.data);
                                Debug.Log($"Audio saved: {filePath}");
                                return new { Success = true, Error = "", ResponseCode = (int)www.responseCode };
                            }
                            else
                            {
                                Debug.LogWarning($"Attempt {attempt} failed: {www.responseCode} - {www.error}");
                                return new { Success = false, Error = www.error, ResponseCode = (int)www.responseCode };
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Exception on attempt {attempt}: {ex.Message}");
                            return new { Success = false, Error = ex.Message, ResponseCode = 0 };
                        }
                    });

                    if (result.Success)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Exception on attempt {attempt}: {ex.Message}");
                }

                if (attempt < MAX_RETRIES)
                {
                    int delay = BASE_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                    Debug.Log($"Retrying in {delay / 1000.0f} seconds...");
                    await Task.Delay(delay, cancellationToken);
                }
            }

            Debug.LogError($"Failed to generate audio for actor '{dialogue.actor}' after {MAX_RETRIES} attempts");
        }
    }
} 