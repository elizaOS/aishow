using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO; // Required for Path and File operations
using System.Collections.Generic; // Required for List if ever needed for form parts

namespace ShowGenerator
{
    // Changed from ScriptableObject to MonoBehaviour
    public class EpisodeDubbingService : MonoBehaviour // This can now be attached to a GameObject
    {
        [Header("API Configuration")]
        [Tooltip("Assign your ShowGeneratorApiKeys asset here.")]
        public ShowGeneratorApiKeys apiKeys; // Assign this in the Inspector

        private const string ElevenLabsBaseUrl = "https://api.elevenlabs.io/v1/dubbing";

        // Results to be optionally displayed by a custom editor
        [HideInInspector] public string creationResult;
        [HideInInspector] public string statusResult;
        [HideInInspector] public string getAudioResult;
        [HideInInspector] public string getTranscriptResult;
        [HideInInspector] public string transcriptContent; 

        // Method to be called by the custom editor to start the coroutine
        public void CallCreateDubbingJob(string audioFilePath, string targetLangCode, 
                                         string projectName, string sourceLangCode, int numSpeakers, 
                                         bool watermark, int startTime, int endTime, bool highestResolution, 
                                         bool dropBackgroundAudio, bool useProfanityFilter, bool dubbingStudio, 
                                         bool disableVoiceCloning, string mode, 
                                         System.Action<string, double, string> onCompleted) // id, expectedDuration, error
        {
            StartCoroutine(CreateDubbingJob(audioFilePath, targetLangCode, projectName, sourceLangCode, numSpeakers,
                                            watermark, startTime, endTime, highestResolution, dropBackgroundAudio,
                                            useProfanityFilter, dubbingStudio, disableVoiceCloning, mode, onCompleted));
        }

        private IEnumerator CreateDubbingJob(string audioFilePath, string targetLangCode, 
                                             string projectName, string sourceLangCode, int numSpeakers, 
                                             bool watermark, int startTime, int endTime, bool highestResolution, 
                                             bool dropBackgroundAudio, bool useProfanityFilter, bool dubbingStudio, 
                                             bool disableVoiceCloning, string opMode, 
                                             System.Action<string, double, string> callback) // id, expectedDuration, error
        {
            creationResult = "Processing..."; // Set initial status
            if (apiKeys == null || string.IsNullOrEmpty(apiKeys.elevenLabsApiKey))
            {
                creationResult = "API Key not set in ShowGeneratorApiKeys.";
                Debug.LogError(creationResult);
                callback?.Invoke(null, -1, creationResult);
                yield break;
            }

            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                creationResult = "Audio/Video file not found at path: " + audioFilePath;
                Debug.LogError(creationResult);
                callback?.Invoke(null, -1, creationResult);
                yield break;
            }

            if (string.IsNullOrEmpty(targetLangCode))
            {
                creationResult = "Target language not specified.";
                Debug.LogError(creationResult);
                callback?.Invoke(null, -1, creationResult);
                yield break;
            }

            WWWForm form = new WWWForm();
            byte[] fileData = File.ReadAllBytes(audioFilePath);
            form.AddBinaryData("file", fileData, Path.GetFileName(audioFilePath), GetMimeType(audioFilePath));
            form.AddField("target_lang", targetLangCode);

            // Add optional parameters
            if (!string.IsNullOrEmpty(projectName)) form.AddField("name", projectName);
            if (!string.IsNullOrEmpty(sourceLangCode) && sourceLangCode.ToLower() != "auto") form.AddField("source_lang", sourceLangCode);
            // API defaults num_speakers to 0 for auto-detect, so only add if explicitly non-zero by user. Or always add it if UI guarantees a value.
            // For now, let's assume UI provides 0 for auto, and any other value is intentional.
            form.AddField("num_speakers", numSpeakers.ToString()); 
            if (watermark) form.AddField("watermark", "true");
            if (startTime > 0) form.AddField("start_time", startTime.ToString()); // API likely ignores 0 or negative
            if (endTime > 0) form.AddField("end_time", endTime.ToString());     // API likely ignores 0 or negative
            if (highestResolution) form.AddField("highest_resolution", "true");
            if (dropBackgroundAudio) form.AddField("drop_background_audio", "true");
            if (useProfanityFilter) form.AddField("use_profanity_filter", "true"); // Ensure this is a boolean string
            if (dubbingStudio) form.AddField("dubbing_studio", "true");
            if (disableVoiceCloning) form.AddField("disable_voice_cloning", "true");
            if (!string.IsNullOrEmpty(opMode) && opMode.ToLower() != "automatic") form.AddField("mode", opMode);

            using (UnityWebRequest www = UnityWebRequest.Post(ElevenLabsBaseUrl, form))
            {
                www.SetRequestHeader("xi-api-key", apiKeys.elevenLabsApiKey);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    try 
                    {
                        DubbingCreationResponse response = JsonUtility.FromJson<DubbingCreationResponse>(www.downloadHandler.text);
                        creationResult = $"Dubbing Job Created! ID: {response.dubbing_id}, Expected Duration: {response.expected_duration_sec}s";
                        Debug.Log(creationResult + " Raw Response: " + www.downloadHandler.text);
                        callback?.Invoke(response.dubbing_id, response.expected_duration_sec, null);
                    }
                    catch (System.Exception e)
                    {
                        creationResult = $"Failed to parse dubbing creation response: {e.Message} - Response: {www.downloadHandler.text}";
                        Debug.LogError(creationResult);
                        callback?.Invoke(null, -1, creationResult);
                    }
                }
                else
                {
                    creationResult = $"Error creating dubbing job ({www.responseCode}): {www.error} - {www.downloadHandler.text}";
                    Debug.LogError(creationResult);
                    callback?.Invoke(null, -1, creationResult);
                }
            }
        }

        private string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".mp3": return "audio/mpeg";
                case ".wav": return "audio/wav";
                case ".m4a": return "audio/mp4"; // m4a is often audio/mp4
                case ".mp4": return "video/mp4";
                // Add more video/audio types if needed
                default: return "application/octet-stream"; // Fallback
            }
        }

        public void CallGetDubbingStatus(string dubbingId, System.Action<string, string> onCompleted)
        {
            StartCoroutine(GetDubbingStatus(dubbingId, onCompleted));
        }

        private IEnumerator GetDubbingStatus(string dubbingId, System.Action<string, string> callback)
        {
            if (CheckApiKeyAndDubbingId(dubbingId, callback, (err) => statusResult = err) == false) yield break;
            statusResult = "Processing...";
            string url = $"{ElevenLabsBaseUrl}/{dubbingId}";
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("xi-api-key", apiKeys.elevenLabsApiKey);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        DubbingStatusResponse response = JsonUtility.FromJson<DubbingStatusResponse>(www.downloadHandler.text);
                        statusResult = $"Current Status: {response.status}";
                        if (!string.IsNullOrEmpty(response.error)) statusResult += $" - Error: {response.error}";
                        Debug.Log($"Dubbing status retrieved: {www.downloadHandler.text}");
                        callback?.Invoke(response.status, response.error);
                    }
                    catch (System.Exception e)
                    {
                        statusResult = $"Failed to parse dubbing status response: {e.Message} - Response: {www.downloadHandler.text}";
                        Debug.LogError(statusResult);
                        callback?.Invoke(null, statusResult);
                    }
                }
                else
                {
                    statusResult = $"Error getting dubbing status ({www.responseCode}): {www.error} - {www.downloadHandler.text}";
                    Debug.LogError(statusResult);
                    callback?.Invoke(null, statusResult);
                }
            }
        }

        public void CallGetDubbedAudioFile(string dubbingId, string languageCode, string filePathToSave, System.Action<bool, string> onCompleted)
        {
            StartCoroutine(GetDubbedAudioFile(dubbingId, languageCode, filePathToSave, onCompleted));
        }

        private IEnumerator GetDubbedAudioFile(string dubbingId, string languageCode, string filePathToSave, System.Action<bool, string> callback)
        {
            getAudioResult = "Processing...";
            if (CheckApiKeyAndDubbingId(dubbingId, (s, err) => { getAudioResult = err; callback?.Invoke(false, err); }, (err) => getAudioResult = err) == false) yield break;
            if (string.IsNullOrEmpty(languageCode))
            {
                getAudioResult = "Language code not specified.";
                Debug.LogError(getAudioResult);
                callback?.Invoke(false, getAudioResult);
                yield break;
            }
            if (string.IsNullOrEmpty(filePathToSave))
            {
                getAudioResult = "File path to save not specified.";
                Debug.LogError(getAudioResult);
                callback?.Invoke(false, getAudioResult);
                yield break;
            }

            string url = $"{ElevenLabsBaseUrl}/{dubbingId}/audio/{languageCode}";
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("xi-api-key", apiKeys.elevenLabsApiKey);
                try
                {
                    string directory = Path.GetDirectoryName(filePathToSave);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (System.Exception e)
                {
                    getAudioResult = $"Error creating directory for {filePathToSave}: {e.Message}";
                    Debug.LogError(getAudioResult);
                    callback?.Invoke(false, getAudioResult);
                    yield break;
                }
                www.downloadHandler = new DownloadHandlerFile(filePathToSave);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    getAudioResult = $"Dubbed audio file downloaded successfully to: {filePathToSave}";
                    Debug.Log(getAudioResult);
                    callback?.Invoke(true, filePathToSave);
                }
                else
                {
                    getAudioResult = $"Error downloading audio ({www.responseCode}): {www.error}.";
                     if (www.responseCode == 404) getAudioResult += $" Check dubbing ID/language. URL: {url}";
                    else if (!string.IsNullOrEmpty(www.downloadHandler.text)) getAudioResult += " Response: " + www.downloadHandler.text;
                    Debug.LogError(getAudioResult);
                    callback?.Invoke(false, getAudioResult);
                }
            }
        }

        public void CallGetDubbedTranscript(string dubbingId, string languageCode, string formatType, System.Action<string, string> onCompleted)
        {
            StartCoroutine(GetDubbedTranscript(dubbingId, languageCode, formatType, onCompleted));
        }

        private IEnumerator GetDubbedTranscript(string dubbingId, string languageCode, string formatType, System.Action<string, string> callback)
        {
            getTranscriptResult = "Processing...";
            transcriptContent = "";
            if (CheckApiKeyAndDubbingId(dubbingId, callback, (err) => getTranscriptResult = err) == false) yield break;
            if (string.IsNullOrEmpty(languageCode))
            {
                getTranscriptResult = "Language code not specified.";
                Debug.LogError(getTranscriptResult);
                callback?.Invoke(null, getTranscriptResult);
                yield break;
            }
            formatType = string.IsNullOrEmpty(formatType) ? "srt" : formatType;

            string url = $"{ElevenLabsBaseUrl}/{dubbingId}/transcript/{languageCode}?format_type={formatType}";
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetRequestHeader("xi-api-key", apiKeys.elevenLabsApiKey);
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    transcriptContent = www.downloadHandler.text;
                    getTranscriptResult = "Transcript retrieved successfully.";
                    Debug.Log(getTranscriptResult + (transcriptContent.Length > 100 ? " Transcript starts: " + transcriptContent.Substring(0,100) : " Transcript: " + transcriptContent));
                    callback?.Invoke(transcriptContent, null);
                }
                else
                {
                    getTranscriptResult = $"Error getting transcript ({www.responseCode}): {www.error}.";
                    if (www.responseCode == 404) getTranscriptResult += $" Check dubbing ID/language. URL: {url}";
                    else if (!string.IsNullOrEmpty(www.downloadHandler.text)) getTranscriptResult += " Response: " + www.downloadHandler.text;
                    Debug.LogError(getTranscriptResult);
                    callback?.Invoke(null, getTranscriptResult);
                }
            }
        }

        private bool CheckApiKeyAndDubbingId(string dubbingId, System.Action<string, string> errorCallbackForStringReturn, System.Action<string> storeErrorAction)
        {
            if (apiKeys == null || string.IsNullOrEmpty(apiKeys.elevenLabsApiKey))
            {
                string msg = "ElevenLabs API Key is not set in ShowGeneratorApiKeys.";
                Debug.LogError(msg);
                storeErrorAction?.Invoke(msg);
                errorCallbackForStringReturn?.Invoke(null, msg);
                return false;
            }
            if (string.IsNullOrEmpty(dubbingId))
            {
                string msg = "Dubbing ID not specified.";
                Debug.LogError(msg);
                storeErrorAction?.Invoke(msg);
                errorCallbackForStringReturn?.Invoke(null, msg);
                return false;
            }
            return true;
        }
        
        // Helper classes for JSON parsing (minimal implementation)
        [System.Serializable]
        private class DubbingCreationResponse
        {
            public string dubbing_id;
            public double expected_duration_sec; // Added this field
        }

        [System.Serializable]
        private class DubbingStatusResponse
        {
            public string dubbing_id;
            public string name;
            public string status;
            public string error;
        }
    }
} 