#nullable enable
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ShowRunner;
using ShowGenerator;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using System.Linq;

public class SingleVideoProcessor : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private ShowGeneratorApiKeys apiKeysSO;
    [SerializeField] private ApiProcessingMode selectedApiMode = ApiProcessingMode.NewPublicAPI;

    [Header("Input Files")]
    [SerializeField] private string audioFilePath = "";
    [SerializeField] private string imageFilePath = "";
    [SerializeField] private string promptText = "";

    [Header("Output Settings")]
    [SerializeField] private string outputFolderName = "single_video_outputs";
    [SerializeField] private string outputFileName = "custom_video";

    [Header("Video Settings")]
    [SerializeField] private string aspectRatio = "16:9";
    [SerializeField] private string resolution = "720p";
    [SerializeField] private string aiModelId = "d1dd37a3-e39a-4854-a298-6510289f9cf2"; // Default model ID

    [Header("Polling Configuration")]
    [SerializeField] private float pollingIntervalSeconds = 10.0f;
    [SerializeField] private int maxPollingAttempts = 60;

    private bool isProcessing = false;
    private Coroutine? processingCoroutine = null;

    // Structure to hold episode path information
    private class EpisodePathInfo
    {
        public string episodeId = string.Empty;
        public string episodeRoot = string.Empty;
        public string hedraRecordingPath = string.Empty;
    }

    private EpisodePathInfo? GetEpisodeInfoFromAudioPath(string audioPath)
    {
        try
        {
            // Normalize path separators
            string normalizedPath = audioPath.Replace('\\', '/');
            
            // Split path into parts
            string[] pathParts = normalizedPath.Split('/');
            
            // Look for 'audio' directory and get episode ID from parent
            int audioIndex = -1;
            for (int i = 0; i < pathParts.Length; i++)
            {
                if (pathParts[i].ToLower() == "audio")
                {
                    audioIndex = i;
                    break;
                }
            }

            if (audioIndex > 0 && audioIndex < pathParts.Length - 1)
            {
                // Episode ID is the parent directory of 'audio'
                string episodeId = pathParts[audioIndex - 1];
                
                // Construct episode root path (up to episode ID directory)
                string episodeRoot = string.Join("/", pathParts.Take(audioIndex));
                
                // Construct hedra-recording path
                string hedraRecordingPath = Path.Combine(episodeRoot, "hedra-recording", "single").Replace('\\', '/');
                
                Debug.Log($"Found episode info from audio path:\nEpisode ID: {episodeId}\nEpisode Root: {episodeRoot}\nHedra Recording Path: {hedraRecordingPath}");
                
                return new EpisodePathInfo
                {
                    episodeId = episodeId,
                    episodeRoot = episodeRoot,
                    hedraRecordingPath = hedraRecordingPath
                };
            }
            else
            {
                Debug.LogError($"Could not find 'audio' directory in path: {normalizedPath}");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing episode info from audio path: {ex.Message}");
            return null;
        }
    }

    public void StartProcessing()
    {
        if (isProcessing)
        {
            Debug.LogWarning("Already processing a video. Please wait for it to complete.");
            return;
        }

        if (string.IsNullOrEmpty(audioFilePath) || string.IsNullOrEmpty(imageFilePath))
        {
            Debug.LogError("Please set both audio and image file paths.");
            return;
        }

        if (!File.Exists(audioFilePath) || !File.Exists(imageFilePath))
        {
            Debug.LogError("One or both input files do not exist.");
            return;
        }

        if (apiKeysSO == null)
        {
            Debug.LogError("API Keys SO reference is missing!");
            return;
        }

        // Get episode info from audio path
        var episodeInfo = GetEpisodeInfoFromAudioPath(audioFilePath);
        if (episodeInfo == null)
        {
            Debug.LogError("Could not determine episode information from audio path. Please ensure the audio file is in the correct directory structure.");
            return;
        }

        // Create the hedra-recording/single directory if it doesn't exist
        Directory.CreateDirectory(episodeInfo.hedraRecordingPath);

        isProcessing = true;
        if (selectedApiMode == ApiProcessingMode.NewPublicAPI)
        {
            processingCoroutine = StartCoroutine(ProcessVideo_NewAPI(episodeInfo));
        }
        else
        {
            processingCoroutine = StartCoroutine(ProcessVideo_LegacyAPI(episodeInfo));
        }
    }

    public void StopProcessing()
    {
        if (processingCoroutine != null)
        {
            StopCoroutine(processingCoroutine);
            processingCoroutine = null;
            isProcessing = false;
            Debug.Log("Processing stopped.");
        }
    }

    private string GetUniqueOutputPath(string baseOutputPath)
    {
        string directory = Path.GetDirectoryName(baseOutputPath) ?? "";
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseOutputPath);
        string extension = Path.GetExtension(baseOutputPath);
        string finalPath = baseOutputPath;
        int counter = 1;

        while (File.Exists(finalPath))
        {
            finalPath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}").Replace('\\', '/');
            counter++;
        }

        return finalPath;
    }

    private IEnumerator ProcessVideo_NewAPI(EpisodePathInfo episodeInfo)
    {
        Debug.Log($"Starting video processing with New API for episode {episodeInfo.episodeId}...");
        string baseUrl = apiKeysSO.hedraBaseUrlNewApi.TrimEnd('/');

        // Step 1: Upload Audio
        string? uploadedAudioId = null;
        CreateAssetRequest audioAssetRequest = new CreateAssetRequest 
        { 
            name = Path.GetFileName(audioFilePath), 
            type = "audio" 
        };

        yield return StartCoroutine(PostJsonApiCall<CreateAssetRequest, CreateAssetResponse>(
            $"{baseUrl}/public/assets", 
            audioAssetRequest,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to create audio asset: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedAudioId = res?.id;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(uploadedAudioId)) yield break;

        yield return StartCoroutine(PostFileApiCall<UploadedAssetConfirmation>(
            $"{baseUrl}/public/assets/{uploadedAudioId}/upload",
            audioFilePath,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to upload audio file: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedAudioId = res?.id ?? uploadedAudioId;
                }
            }
        ));

        if (!isProcessing) yield break;

        // Step 2: Upload Image
        string? uploadedImageId = null;
        CreateAssetRequest imageAssetRequest = new CreateAssetRequest 
        { 
            name = Path.GetFileName(imageFilePath), 
            type = "image" 
        };

        yield return StartCoroutine(PostJsonApiCall<CreateAssetRequest, CreateAssetResponse>(
            $"{baseUrl}/public/assets",
            imageAssetRequest,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to create image asset: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedImageId = res?.id;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(uploadedImageId)) yield break;

        yield return StartCoroutine(PostFileApiCall<UploadedAssetConfirmation>(
            $"{baseUrl}/public/assets/{uploadedImageId}/upload",
            imageFilePath,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to upload image file: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedImageId = res?.id ?? uploadedImageId;
                }
            }
        ));

        if (!isProcessing) yield break;

        // Step 3: Generate Video
        GenerateVideoRequest generationPayload = new GenerateVideoRequest
        {
            ai_model_id = aiModelId,
            start_keyframe_id = uploadedImageId,
            audio_id = uploadedAudioId,
            generated_video_inputs = new GenerateVideoRequestInput
            {
                text_prompt = promptText,
                aspect_ratio = aspectRatio,
                resolution = resolution
            }
        };

        string? generationId = null;
        yield return StartCoroutine(PostJsonApiCall<GenerateVideoRequest, GenerateVideoResponse>(
            $"{baseUrl}/public/generations",
            generationPayload,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to start video generation: {err}");
                    isProcessing = false;
                }
                else
                {
                    generationId = res?.id;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(generationId)) yield break;

        // Step 4: Poll for completion
        int attempts = 0;
        bool isComplete = false;

        while (attempts < maxPollingAttempts && !isComplete && isProcessing)
        {
            yield return StartCoroutine(GetJsonApiCall<GenerationStatusResponse>(
                $"{baseUrl}/public/generations/{generationId}/status",
                (res, err) => 
                {
                    if (err != null)
                    {
                        Debug.LogError($"Polling error: {err}");
                    }
                    else if (res != null)
                    {
                        string status = res.status?.ToLower() ?? "unknown";
                        Debug.Log($"Status: {status}, Progress: {res.progress * 100f:F1}%");

                        if (status == "complete" && !string.IsNullOrEmpty(res.url))
                        {
                            isComplete = true;
                            string outputDir = episodeInfo.hedraRecordingPath;
                            Directory.CreateDirectory(outputDir);
                            string outputFileName = $"{Path.GetFileNameWithoutExtension(audioFilePath)}_generated.mp4";
                            string baseOutputPath = Path.Combine(outputDir, outputFileName).Replace('\\', '/');
                            string finalOutputPath = GetUniqueOutputPath(baseOutputPath);
                            Debug.Log($"Will save video to: {finalOutputPath}");
                            StartCoroutine(DownloadFileCoroutine(res.url, finalOutputPath));
                        }
                        else if (status == "error")
                        {
                            Debug.LogError($"Generation failed: {res.error_message}");
                            isProcessing = false;
                        }
                    }
                }
            ));

            if (!isComplete)
            {
                attempts++;
                yield return new WaitForSeconds(pollingIntervalSeconds);
            }
        }

        isProcessing = false;
    }

    private IEnumerator ProcessVideo_LegacyAPI(EpisodePathInfo episodeInfo)
    {
        Debug.Log($"Starting video processing with Legacy API for episode {episodeInfo.episodeId}...");
        string baseUrl = apiKeysSO.hedraBaseUrl.TrimEnd('/');

        // Step 1: Upload Audio
        string? uploadedAudioUrl = null;
        yield return StartCoroutine(PostFileApiCall<Legacy_HedraUrlResponse>(
            $"{baseUrl}/v1/audio",
            audioFilePath,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to upload audio: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedAudioUrl = res?.url;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(uploadedAudioUrl)) yield break;

        // Step 2: Upload Image
        string? uploadedImageUrl = null;
        yield return StartCoroutine(PostFileApiCall<Legacy_HedraUrlResponse>(
            $"{baseUrl}/v1/portrait",
            imageFilePath,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to upload image: {err}");
                    isProcessing = false;
                }
                else
                {
                    uploadedImageUrl = res?.url;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(uploadedImageUrl)) yield break;

        // Step 3: Generate Character
        Legacy_HedraCharacterRequestPayload characterPayload = new Legacy_HedraCharacterRequestPayload
        {
            text = promptText,
            voiceUrl = uploadedAudioUrl,
            avatarImage = uploadedImageUrl,
            aspectRatio = aspectRatio,
            audioSource = "audio"
        };

        string? projectId = null;
        yield return StartCoroutine(PostJsonApiCall<Legacy_HedraCharacterRequestPayload, Legacy_HedraCharacterResponse>(
            $"{baseUrl}/v1/characters",
            characterPayload,
            (res, err) => 
            {
                if (err != null)
                {
                    Debug.LogError($"Failed to start character generation: {err}");
                    isProcessing = false;
                }
                else
                {
                    projectId = res?.projectId ?? res?.jobId;
                }
            }
        ));

        if (!isProcessing || string.IsNullOrEmpty(projectId)) yield break;

        // Step 4: Poll for completion
        int attempts = 0;
        bool isComplete = false;

        while (attempts < maxPollingAttempts && !isComplete && isProcessing)
        {
            yield return StartCoroutine(GetJsonApiCall<Legacy_HedraProjectStatusResponse>(
                $"{baseUrl}/v1/projects/{projectId}",
                (res, err) => 
                {
                    if (err != null)
                    {
                        Debug.LogError($"Polling error: {err}");
                    }
                    else if (res != null)
                    {
                        string status = res.status?.ToLower() ?? "unknown";
                        Debug.Log($"Status: {status}, Progress: {(res.progress.HasValue ? res.progress.Value * 100f : -1f):F1}%");

                        if ((status == "succeeded" || status == "completed") && !string.IsNullOrEmpty(res.videoUrl))
                        {
                            isComplete = true;
                            string outputDir = episodeInfo.hedraRecordingPath;
                            Directory.CreateDirectory(outputDir);
                            string outputFileName = $"{Path.GetFileNameWithoutExtension(audioFilePath)}_generated.mp4";
                            string baseOutputPath = Path.Combine(outputDir, outputFileName).Replace('\\', '/');
                            string finalOutputPath = GetUniqueOutputPath(baseOutputPath);
                            Debug.Log($"Will save video to: {finalOutputPath}");
                            StartCoroutine(DownloadFileCoroutine(res.videoUrl, finalOutputPath));
                        }
                        else if (status == "failed")
                        {
                            Debug.LogError($"Generation failed: {res.errorMessage}");
                            isProcessing = false;
                        }
                    }
                }
            ));

            if (!isComplete)
            {
                attempts++;
                yield return new WaitForSeconds(pollingIntervalSeconds);
            }
        }

        isProcessing = false;
    }

    private IEnumerator PostJsonApiCall<TRequest, TResponse>(string url, TRequest payload, System.Action<TResponse?, string?> callback)
        where TResponse : class
    {
        string? responseJson = null;
        string? error = null;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string? apiKey = selectedApiMode == ApiProcessingMode.NewPublicAPI ? 
                apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("X-API-KEY", apiKey);
            }
            else
            {
                error = "API Key not set";
            }

            if (error == null) yield return request.SendWebRequest();

            if (error == null)
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    responseJson = request.downloadHandler.text;
                }
                else
                {
                    error = $"[{request.responseCode}] {request.error}. Response: {request.downloadHandler.text}";
                }
            }
        }

        if (error == null && responseJson != null)
        {
            try { callback(JsonUtility.FromJson<TResponse>(responseJson), null); }
            catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}"); }
        }
        else { callback(null, error ?? "Unknown error"); }
    }

    private IEnumerator PostFileApiCall<TResponse>(string url, string filePath, System.Action<TResponse?, string?> callback)
        where TResponse : class
    {
        if (!File.Exists(filePath))
        {
            callback(null, $"File not found: {filePath}");
            yield break;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", fileData, Path.GetFileName(filePath), GetMimeType(Path.GetExtension(filePath))));

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            string? apiKey = selectedApiMode == ApiProcessingMode.NewPublicAPI ? 
                apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("X-API-KEY", apiKey);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try { callback(JsonUtility.FromJson<TResponse>(request.downloadHandler.text), null); }
                catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}"); }
            }
            else
            {
                callback(null, $"[{request.responseCode}] {request.error}");
            }
        }
    }

    private IEnumerator GetJsonApiCall<TResponse>(string url, System.Action<TResponse?, string?> callback)
        where TResponse : class
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            string? apiKey = selectedApiMode == ApiProcessingMode.NewPublicAPI ? 
                apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("X-API-KEY", apiKey);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try { callback(JsonUtility.FromJson<TResponse>(request.downloadHandler.text), null); }
                catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}"); }
            }
            else
            {
                callback(null, $"[{request.responseCode}] {request.error}");
            }
        }
    }

    private IEnumerator DownloadFileCoroutine(string url, string localSavePath)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string? directory = Path.GetDirectoryName(localSavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllBytes(localSavePath, request.downloadHandler.data);
                Debug.Log($"Video saved to: {localSavePath}");

                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                #endif
            }
            else
            {
                Debug.LogError($"Failed to download video: {request.error}");
            }
        }
    }

    private string GetMimeType(string extension)
    {
        if (extension == null) return "application/octet-stream";
        if (!extension.StartsWith(".")) extension = "." + extension;
        
        switch (extension.ToLower())
        {
            case ".mp3": return "audio/mpeg";
            case ".wav": return "audio/wav";
            case ".ogg": return "audio/ogg";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".png": return "image/png";
            default: return "application/octet-stream";
        }
    }

    // Helper classes from HedraEpisodeProcessor
    [System.Serializable] private class CreateAssetRequest { public string name = string.Empty; public string type = string.Empty; }
    [System.Serializable] private class CreateAssetResponse { public string? id; public string? name; public string? type; }
    [System.Serializable] private class UploadedAssetConfirmation { public string? id; }
    [System.Serializable] private class GenerateVideoRequestInput 
    {
        public string text_prompt = string.Empty;
        public string? resolution;
        public string? aspect_ratio;
    }
    [System.Serializable] private class GenerateVideoRequest 
    {
        public string type = "video";
        public string? ai_model_id;
        public string? start_keyframe_id;
        public string? audio_id;
        public GenerateVideoRequestInput generated_video_inputs = new GenerateVideoRequestInput();
    }
    [System.Serializable] private class GenerateVideoResponse 
    {
        public string? id;
        public string? asset_id;
        public string? type;
    }
    [System.Serializable] private class GenerationStatusResponse 
    {
        public string? id;
        public string? asset_id;
        public string? type;
        public string? status;
        public float progress;
        public string? error_message;
        public string? url;
    }
    [System.Serializable] private class Legacy_HedraUrlResponse { public string? url; }
    [System.Serializable] private class Legacy_HedraCharacterRequestPayload 
    {
        public string text = string.Empty;
        public string? voiceId;
        public string voiceUrl = string.Empty;
        public string avatarImage = string.Empty;
        public string aspectRatio = "1:1";
        public string audioSource = "audio";
    }
    [System.Serializable] private class Legacy_HedraCharacterResponse 
    {
        public string? jobId;
        public string? projectId;
        public string? id;
    }
    [System.Serializable] private class Legacy_HedraProjectStatusResponse 
    {
        public string? status;
        public string? videoUrl;
        public string? errorMessage;
        public float? progress;
        public string? stage;
    }
} 