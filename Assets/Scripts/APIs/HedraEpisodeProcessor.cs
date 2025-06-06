#nullable enable
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Added for potential future LINQ use, not strictly necessary for current version
using ShowGenerator; // For ShowGeneratorApiKeys
using UnityEngine.Networking;
using System.Text;

// Enum to select which API version to use
public enum ApiProcessingMode 
{
    NewPublicAPI, 
    LegacyV1API 
}

// --- New C# Classes to match AI_Podcast_S1E56.json structure ---
[System.Serializable]
public class JsonDialogueEntry // Was OriginalJsonLine
{
    public string actor = string.Empty; // Initialize to prevent CS8618
    public string line = string.Empty;  // Initialize to prevent CS8618
    public string? action; // Action might be optional
    // Add other fields if needed, e.g., characterDescription, voiceId, if they come from here
}

[System.Serializable]
public class JsonScene
{
    public string location = string.Empty; // Initialize
    public string description = string.Empty; // Initialize
    // public Dictionary<string, string> cast; // JsonUtility doesn't support dictionaries directly
    public List<JsonDialogueEntry> dialogue = new List<JsonDialogueEntry>(); // Initialize
}

[System.Serializable]
public class JsonEpisode
{
    public string id = string.Empty; // Initialize
    public string name = string.Empty; // Initialize
    public string premise = string.Empty; // Initialize
    public string summary = string.Empty; // Initialize
    public List<JsonScene> scenes = new List<JsonScene>(); // Initialize
}

// This will be the root object we deserialize from the JSON file.
// Assuming the root of your JSON file has an "episodes" array.
[System.Serializable]
public class JsonRootForEpisodeList // Was OriginalJsonRoot
{
    // This assumes your JSON file, when loaded, has a top-level key "episodes" 
    // which is an array of JsonEpisode objects.
    // If the file *itself* is just an array of episodes `[ {episode1}, {episode2} ]`,
    // then JsonUtility parsing needs to be handled by wrapping it as shown in the parsing logic.
    public List<JsonEpisode> episodes = new List<JsonEpisode>(); 
    public ConfigBlock? config; // Config might be optional
}

// Added to represent the "config" block if we ever need data from it
[System.Serializable]
public class ConfigBlock 
{
    public string id = string.Empty; // Initialize
    public string name = string.Empty; // Initialize
    // public List<JsonEpisode> episodes; // This is the nested one we'll likely ignore if a root one exists
    // Add other config fields if necessary
}

// --- End of new JSON structure classes ---

// Define the structure for each segment in the Hedra processing manifest
[System.Serializable]
public class HedraSegmentConfig
{
    [Tooltip("The dialogue text to be spoken. This will be sent as the 'text' field to the Hedra API if the /v1/characters endpoint supports it.")]
    public string text = string.Empty; // Initialize

    [Tooltip("Initially, the local path to the audio file. The batch processor will upload this file and use the returned Hedra URL for 'voiceUrl' in the character generation API call.")]
    public string voiceUrl = string.Empty; // Initialize

    [Tooltip("Initially, the local path to the avatar image. The batch processor will upload this file and use the returned Hedra URL for 'avatarImage' in the character generation API call.")]
    public string avatarImage = string.Empty; // Initialize

    [Tooltip("Desired aspect ratio (e.g., 16:9). Maps to Hedra API 'aspectRatio' if supported by /v1/characters.")]
    public string aspectRatio = "16:9"; // Initialize with a common default

    [Tooltip("Specifies the source of the audio. Defaults to 'audio' for pre-recorded files. Maps to Hedra API 'audioSource'.")]
    public string audioSource = "audio"; // Already has default

    [Tooltip("Optional. Hedra Voice ID to use. Maps to Hedra API 'voiceId' if supported by /v1/characters.")]
    public string? voiceId; // Optional field

    // Nested class for avatarImageInput
    [System.Serializable]
    public class AvatarImageInputPayload
    {
        [Tooltip("Optional. A general description of the character, emotion, or scene. Maps to Hedra API 'avatarImageInput.prompt' if supported by /v1/characters.")]
        public string? prompt; // Optional field
        [Tooltip("Optional. Seed for avatar image generation. Maps to Hedra API 'avatarImageInput.seed' if supported.")]
        public int? seed; // Optional field, make it nullable int
    }
    [Tooltip("Payload for avatar image input, including prompt. Maps to Hedra API 'avatarImageInput'.")]
    public AvatarImageInputPayload avatarImageInput = new AvatarImageInputPayload();

    // --- Fields NOT directly sent to Hedra 'Initialize character generation' API in *this* form ---
    // --- These are used by the Unity processor to find local files and prepare for API calls ---
    // [Tooltip("Local path to the screenshot image for this segment, relative to the project root. Used to upload and get the 'avatarImage' URL for the API call.")]
    // public string localImagePath; 
    // [Tooltip("Local path to the audio file for this segment, relative to the project root. Used to upload and get the 'voiceUrl' for the API call.")]
    // public string localAudioPath; 
    
    [Tooltip("Informational: Target resolution (e.g., 720p). May be for image upload step or other pre-processing.")]
    public string resolution = "720p"; // Initialize with a common default

    [Tooltip("Optional: AI Model ID for video generation. Maps to Hedra API 'ai_model_id' if supported by /v1/characters.")]
    public string? ai_model_id_override; // Optional: if we want to specify a model per segment for video generation
}

// Define the root structure for the Hedra manifest JSON file
// This structure is used by both API modes for loading the manifest.
[System.Serializable]
public class HedraManifestRoot
{
    public List<HedraSegmentConfig> segmentsToProcess;
    public HedraManifestRoot()
    {
        segmentsToProcess = new List<HedraSegmentConfig>();
    }
}

// Changed from ScriptableObject to MonoBehaviour
public class HedraEpisodeProcessor : MonoBehaviour // Base class changed
{
    [Header("API Configuration")]
    public ShowGeneratorApiKeys? apiKeysSO;
    // Make selectedApiMode public for the editor script to access and save/load
    [Header("API Mode Selection")] // New Header for API mode
    public ApiProcessingMode selectedApiMode = ApiProcessingMode.NewPublicAPI;

    [Header("Input Configuration")]
    public string sourceEpisodeJsonPath = "Assets/StreamingAssets/AI_Podcast_S1E56.json"; // Example Path
    public string inputEpisodeId = "AI_Podcast_S1E56"; // Example Episode ID

    [Header("Manifest File (Explicit Path)")] // New Header
    [Tooltip("Path to the hedra_manifest.json file. Generate one or select an existing one.")]
    public string explicitManifestPath = ""; // User will set this via Browse button or after generation

    [Header("Output Configuration")]
    [Tooltip("Name of the subfolder (relative to the input JSON\'s directory) to save Hedra manifests/outputs.")]
    public string outputSubfolderName = "hedra_processing";

    [Header("Hedra Output Defaults (for each segment)")]
    public string defaultAspectRatio = "16:9"; 
    public string defaultResolution = "720p";
    public string screenshotExtension = ".jpg";
    public string audioExtension = ".wav";   
    public string defaultAudioSource = "audio";
    [Header("Segment Content Defaults (Optional)")]
    public string defaultVoiceId = ""; // This can be used to initialize HedraSegmentConfig.voiceId if needed

    [Header("Single Segment Processing")] // Added for clarity in Inspector
    [Tooltip("Index of the segment in the manifest to process when 'Process Single Segment' is clicked.")]
    public int segmentIndexToProcess = 0;
    private bool stopProcessing = false; // Added this flag back

    [Header("Loaded Manifest Preview (Read-Only)")] // New Header for preview
    [TextArea(10, 30)] // Makes it a larger text area in the inspector
    public string loadedManifestContent = ""; // To display manifest content

    [Header("Batch Processing Options")] // New Header
    [Tooltip("If true, the processor will wait for each segment\'s video generation to COMPLETE with Hedra before starting the next segment\'s processing. Polling for this segment still runs, and downloads (if successful) will occur. If false, all generation jobs are submitted with a short delay between them, and all polling happens concurrently.")]
    public bool processBatchStrictlySequentially = false;

    [Header("Polling Configuration")]
    [Tooltip("How many seconds to wait between polling for job status.")]
    public float pollingIntervalSeconds = 10.0f;
    [Tooltip("Maximum number of polling attempts before timing out.")]
    public int maxPollingAttempts = 60; // e.g., 60 attempts * 10s = 10 minutes timeout per segment

    [Header("Batch Processing Delays")]
    [Tooltip("Delay in seconds between submitting new generation jobs when in CONCURRENT batch mode. Helps prevent immediate API overload.")]
    public float delayBetweenConcurrentSubmissions = 1.0f;
    [Tooltip("Delay in seconds between segment processing when in STRICTLY SEQUENTIAL batch mode (after previous segment's polling completes, before next segment's asset uploads start). Use 0 for no delay.")]
    public float delayBetweenStrictSequentialSubmissions = 0.5f;

    [Header("New API Defaults")] // Added this header for clarity
    [Tooltip("Default AI Model ID for video generation (New API). Can be overridden in HedraSegmentConfig if needed.")]
    public string defaultNewApiAiModelId = "d1dd37a3-e39a-4854-a298-6510289f9cf2"; // From spec example

    private Coroutine? batchProcessCoroutine = null; // Made nullable
    private bool isProcessingBatch = false; // Shared flag for both API modes

    // Helper classes for JSON deserialization/serialization
    [System.Serializable] private class HedraUrlResponse { public string? url; /* add other fields if any */ }
    [System.Serializable] 
    private class HedraCharacterRequestPayload { 
        public string text = string.Empty;
        public string? voiceId; // Can be null
        public string voiceUrl = string.Empty; 
        public string avatarImage = string.Empty; 
        public string aspectRatio = "1:1";
        public string audioSource = "audio";
        public HedraSegmentConfig.AvatarImageInputPayload? avatarImageInput; // Can be null or an object
    }
    [System.Serializable] 
    private class HedraCharacterResponse { 
        public string? jobId; // Prioritized based on new schema
        public string? projectId;  
        public string? id; 
    }
    [System.Serializable] 
    private class HedraProjectStatusResponse { 
        public string? status; // Matches AvatarProjectItem.status (which is AvatarProjectStatus enum)
        public string? videoUrl; // Matches AvatarProjectItem.videoUrl (was previously 'url')
        public string? errorMessage; // Matches AvatarProjectItem.errorMessage (was previously 'error')
        public float? progress; // Optional: Matches AvatarProjectItem.progress
        public string? stage; // Optional: Matches AvatarProjectItem.stage
    }

    // --- NEW Hedra API v_Public (FastAPI spec) Helper Classes ---
    [System.Serializable] public class CreateAssetRequest { public string name = string.Empty; public string type = string.Empty; /* image or audio */ }
    [System.Serializable] public class CreateAssetResponse { public string? id; public string? name; public string? type; }
    // For POST /public/assets/{id}/upload, the response is a full Asset object.
    // We can define a simplified version or use a generic dictionary if fields vary wildly.
    // For now, let's assume we just need to confirm success or get an updated ID if it changes.
    [System.Serializable] public class UploadedAssetConfirmation { public string? id; /* could have more fields from Asset schema */ }

    [System.Serializable] 
    public class GenerateVideoRequestInput 
    {
        public string text_prompt = string.Empty; // Main dialogue text
        public string? resolution; // e.g., "720p"
        public string? aspect_ratio; // e.g., "16:9"
        // duration_ms is in the spec but we might not need to set it if Hedra derives from audio
    }

    [System.Serializable] 
    public class GenerateVideoRequest 
    {
        public string type = "video";
        public string? ai_model_id; // e.g., "d1dd37a3-e39a-4854-a298-6510289f9cf2"
        public string? start_keyframe_id; // Asset ID of uploaded image
        public string? audio_id; // Asset ID of uploaded audio
        public GenerateVideoRequestInput generated_video_inputs = new GenerateVideoRequestInput();
    }

    [System.Serializable] 
    public class GenerateVideoResponse 
    {
        public string? id; // This is the generation_id for polling
        public string? asset_id; // Asset ID of the resulting video
        public string? type;
        // other fields from spec if needed e.g. ai_model_id, inputs etc.
    }

    [System.Serializable] 
    public class GenerationStatusResponse 
    {
        public string? id; // generation_id
        public string? asset_id;
        public string? type; // AssetType
        public string? status; // "complete", "error", "processing", "finalizing", "queued", "pending"
        public float progress; // 0-1
        public string? error_message;
        public string? url; // URL of the generated asset if status is "complete"
    }
    // --- End of NEW Hedra API v_Public Helper Classes ---

    // --- LEGACY Hedra API v1 Helper Classes (for /v1/... endpoints) ---
    // These are brought in from the HedraEpisodeProcessor_LegacyAPI script
    [System.Serializable] 
    private class Legacy_HedraUrlResponse { public string? url; }

    [System.Serializable]
    private class Legacy_HedraCharacterRequestPayload 
    {
        public string text = string.Empty;
        public string? voiceId;
        public string voiceUrl = string.Empty;
        public string avatarImage = string.Empty;
        public string aspectRatio = "1:1";
        public string audioSource = "audio";
        public HedraSegmentConfig.AvatarImageInputPayload? avatarImageInput; // Re-using the nested class from HedraSegmentConfig
    }

    [System.Serializable]
    private class Legacy_HedraCharacterResponse 
    {
        public string? jobId;
        public string? projectId; // This was the one typically used for polling /v1/projects/{projectId}
        public string? id;
    }

    [System.Serializable]
    private class Legacy_HedraProjectStatusResponse 
    {
        public string? status; // e.g., "succeeded", "failed", "processing"
        public string? videoUrl; // URL of the final video
        public string? errorMessage;
        public float? progress;
        public string? stage;
    }
    // --- End of LEGACY Hedra API v1 Helper Classes ---

    /// <summary>
    /// Generates a Hedra processing manifest from the specified source JSON file.
    /// This manifest can then be used by a batch processor to make API calls.
    /// </summary>
    public void GenerateHedraManifest()
    {
        if (apiKeysSO == null) { Debug.LogError("API Keys SO not assigned.", this); return; }
        if (string.IsNullOrEmpty(sourceEpisodeJsonPath)) { Debug.LogError("Source JSON Path empty.", this); return; }
        if (!File.Exists(sourceEpisodeJsonPath)) { Debug.LogError($"Source JSON not found: {sourceEpisodeJsonPath}", this); return; }

        string? sourceJsonDirectory = Path.GetDirectoryName(sourceEpisodeJsonPath);
        if(string.IsNullOrEmpty(sourceJsonDirectory)) { Debug.LogError($"Could not get directory for {sourceEpisodeJsonPath}", this); return; }

        JsonRootForEpisodeList? parsedJsonRoot = null;
        JsonEpisode? targetEpisode = null;

        try
        {
            string jsonContent = File.ReadAllText(sourceEpisodeJsonPath);
            parsedJsonRoot = JsonUtility.FromJson<JsonRootForEpisodeList>(jsonContent);

            if (parsedJsonRoot != null && parsedJsonRoot.episodes != null && parsedJsonRoot.episodes.Count > 0)
            {
                targetEpisode = parsedJsonRoot.episodes[0]; 
                Debug.Log($"Using first episode (ID: {targetEpisode.id}) from root 'episodes' array.", this);
            }
            else if (parsedJsonRoot != null && parsedJsonRoot.config != null)
            {
                Debug.LogWarning("Root 'episodes' array is missing or empty. If episode data is under 'config.episodes', this needs specific handling not yet fully implemented for that path.", this);
            }

            if (targetEpisode == null)
            {
                 Debug.LogError($"HedraEpisodeProcessor: Could not identify target episode from '{sourceEpisodeJsonPath}'. Manifest generation aborted.", this);
                 return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"HedraEpisodeProcessor: Error parsing JSON from '{sourceEpisodeJsonPath}': {ex.Message}", this);
            return;
        }

        HedraManifestRoot hedraOutput = new HedraManifestRoot();
        string baseAssetPathForMedia = sourceJsonDirectory.Replace("\\", "/");
        string episodeId = targetEpisode.id ?? "UnknownEpisode"; // Null check
        int segmentsCreated = 0;

        if (targetEpisode.scenes != null)
        {
            for (int sceneIndex = 0; sceneIndex < targetEpisode.scenes.Count; sceneIndex++)
            {
                JsonScene currentScene = targetEpisode.scenes[sceneIndex];
                if (currentScene.dialogue != null)
                {
                    for (int dialogueIndexInScene = 0; dialogueIndexInScene < currentScene.dialogue.Count; dialogueIndexInScene++)
                    {
                        JsonDialogueEntry currentDialogue = currentScene.dialogue[dialogueIndexInScene];

                        // Skip segments for the 'tv' actor or if the dialogue line is null/empty/whitespace
                        if (currentDialogue != null && !string.IsNullOrWhiteSpace(currentDialogue.actor) && currentDialogue.actor.Trim().ToLower() == "tv")
                        {
                            Debug.Log($"Skipping segment for 'tv' actor: Episode '{episodeId}', Scene {sceneIndex + 1}, Line {dialogueIndexInScene + 1} (Original line: '{currentDialogue.line}')", this);
                            continue; // Skip to the next dialogue entry
                        }
                        if (currentDialogue == null || string.IsNullOrWhiteSpace(currentDialogue.line))
                        {
                             Debug.LogWarning($"Skipping segment due to null dialogue object or empty/whitespace line: Episode '{episodeId}', Scene {sceneIndex + 1}, Line {dialogueIndexInScene + 1} (Actor: {currentDialogue?.actor})". Trim(), this);
                            continue; // Skip to the next dialogue entry
                        }

                        HedraSegmentConfig segment = new HedraSegmentConfig();
                        segment.text = currentDialogue.line;
                        
                        // Populate avatarImageInput.prompt with dialogue text for clarity, though generated_video_inputs.text_prompt will be primary
                        segment.avatarImageInput.prompt = currentDialogue.line; 
                        // segment.avatarImageInput.seed can be set if desired, from inspector or other logic

                        segment.audioSource = "audio"; // This might become less relevant
                        segment.voiceId = string.IsNullOrEmpty(defaultVoiceId) ? null : defaultVoiceId; 
                        segment.aspectRatio = defaultAspectRatio;
                        segment.resolution = defaultResolution;
                        segment.ai_model_id_override = defaultNewApiAiModelId; // Assign default model ID

                        string fileNameWithoutExtension = $"{episodeId}_{sceneIndex + 1}_{dialogueIndexInScene + 1}";

                        string relativeScreenshotDir = Path.Combine(baseAssetPathForMedia, "screenshots").Replace("\\", "/");
                        string relativeAudioDir = Path.Combine(baseAssetPathForMedia, "audio").Replace("\\", "/");

                        segment.avatarImage = Path.Combine(relativeScreenshotDir, $"{fileNameWithoutExtension}{screenshotExtension}").Replace("\\", "/");
                        segment.voiceUrl = Path.Combine(relativeAudioDir, $"{fileNameWithoutExtension}{audioExtension}").Replace("\\", "/");
                        
                        if (!File.Exists(segment.avatarImage)) { Debug.LogWarning($"Screenshot not found: '{segment.avatarImage}'", this); }
                        if (!File.Exists(segment.voiceUrl)) { Debug.LogWarning($"Audio file not found: '{segment.voiceUrl}'", this); }

                        hedraOutput.segmentsToProcess.Add(segment);
                        segmentsCreated++;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"Target episode '{episodeId}' has no scenes defined. Manifest will be empty.", this);
        }

        if (segmentsCreated == 0 && targetEpisode.scenes != null && targetEpisode.scenes.Any(s => s.dialogue != null && s.dialogue.Count > 0)) 
        {
             Debug.LogWarning("No segments added to manifest, but dialogue lines seemed to be present. Check loop logic or conditions.", this);
        }
        else if (segmentsCreated == 0)
        {
             Debug.LogWarning($"No dialogue lines found in '{Path.GetFileName(sourceEpisodeJsonPath)}' for episode '{episodeId}'. Manifest will be empty.", this);
        }

        string outputDir = Path.Combine(sourceJsonDirectory, outputSubfolderName).Replace("\\", "/");
        Directory.CreateDirectory(outputDir); 
        string outputManifestSystemPath = Path.Combine(outputDir, "hedra_manifest.json").Replace("\\", "/"); // New line uses fixed name

        try
        {
            string outputJsonString = JsonUtility.ToJson(hedraOutput, true);
            File.WriteAllText(outputManifestSystemPath, outputJsonString);
            Debug.Log($"Successfully generated Hedra manifest for episode '{episodeId}' ('{Path.GetFileName(sourceEpisodeJsonPath)}') at: '{outputManifestSystemPath}'. Segments: {segmentsCreated}", this);
            
            #if UNITY_EDITOR
            string assetDbPath = outputManifestSystemPath;
            if (assetDbPath.StartsWith(Application.dataPath, System.StringComparison.OrdinalIgnoreCase))
            {
                assetDbPath = "Assets" + assetDbPath.Substring(Application.dataPath.Length);
                UnityEditor.AssetDatabase.ImportAsset(assetDbPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                Object? obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetDbPath); 
                if (obj != null) { UnityEditor.EditorGUIUtility.PingObject(obj); }

                this.explicitManifestPath = outputManifestSystemPath;
                LoadAndDisplayManifestContent(); 
                Debug.Log($"Explicit manifest path set to: {this.explicitManifestPath}", this);
            }
            else { Debug.Log("Manifest saved outside Assets folder, not pinging: " + assetDbPath, this); }
            #endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error writing Hedra manifest to '{outputManifestSystemPath}': {ex.Message}", this);
        }
    }

    public void StartBatchProcessing()
    {
        if (batchProcessCoroutine != null)
        {
            Debug.LogWarning("Batch processing is already running.", this);
            return;
        }
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKey))
        {
            Debug.LogError("Hedra API Key missing from ShowGeneratorApiKeys asset!", this);
            return;
        }
        if (string.IsNullOrEmpty(apiKeysSO.hedraBaseUrl)) 
        {
            Debug.LogError("Hedra Base URL missing from ShowGeneratorApiKeys asset! Please set it.", this);
            return;
        }

        if (string.IsNullOrEmpty(explicitManifestPath) || !File.Exists(explicitManifestPath))
        {
            Debug.LogError($"Explicit Manifest Path is not set or file not found: '{explicitManifestPath}'. Please generate a manifest or browse to select an existing one.", this);
            return;
        }
        string manifestPath = explicitManifestPath; 

        HedraManifestRoot? manifestRoot = null;
        try
        {
            string manifestJson = File.ReadAllText(manifestPath);
            manifestRoot = JsonUtility.FromJson<HedraManifestRoot>(manifestJson);
        }
        catch (System.Exception ex) { Debug.LogError($"Error reading manifest '{manifestPath}': {ex.Message}", this); return; }

        if (manifestRoot == null || manifestRoot.segmentsToProcess == null || manifestRoot.segmentsToProcess.Count == 0)
        { Debug.LogError("Manifest empty or invalid after attempting to load.", this); return; }

        Debug.Log($"Starting batch processing for {manifestRoot.segmentsToProcess.Count} segments from '{manifestPath}' using API Mode: {selectedApiMode}...", this);
        
        isProcessingBatch = true; // Set processing flag
        if (selectedApiMode == ApiProcessingMode.NewPublicAPI)
        {
            batchProcessCoroutine = StartCoroutine(ProcessSegmentsCoroutine_NewAPI(manifestRoot.segmentsToProcess, isSingleSegmentRun: false));
        }
        else // LegacyV1API
        {
            batchProcessCoroutine = StartCoroutine(ProcessSegmentsCoroutine_LegacyAPI(manifestRoot.segmentsToProcess, isSingleSegmentRun: false));
        }
    }

    public void ProcessSingleSegmentWrapper() 
    {
        if (batchProcessCoroutine != null)
        {
            Debug.LogWarning("Cannot start single segment processing while batch processing is already running.", this);
            return;
        }
        StartSingleSegmentProcessing(segmentIndexToProcess);
    }

    public void StartSingleSegmentProcessing(int indexToProcess)
    {
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKey))
        {
            Debug.LogError("Hedra API Key missing from ShowGeneratorApiKeys asset!", this);
            return;
        }
        if (string.IsNullOrEmpty(apiKeysSO.hedraBaseUrl))
        {
            Debug.LogError("Hedra Base URL missing from ShowGeneratorApiKeys asset!", this);
            return;
        }

        if (string.IsNullOrEmpty(explicitManifestPath) || !File.Exists(explicitManifestPath))
        {
            Debug.LogError($"Explicit Manifest Path is not set or file not found: '{explicitManifestPath}'. Please generate a manifest or browse to select an existing one.", this);
            return;
        }
        string manifestPath = explicitManifestPath; 

        HedraManifestRoot? manifestRoot = null;
        try
        {
            string manifestJson = File.ReadAllText(manifestPath);
            manifestRoot = JsonUtility.FromJson<HedraManifestRoot>(manifestJson);
        }
        catch (System.Exception ex) { Debug.LogError($"Error reading manifest '{manifestPath}': {ex.Message}", this); return; }

        if (manifestRoot == null || manifestRoot.segmentsToProcess == null || manifestRoot.segmentsToProcess.Count == 0)
        { Debug.LogError("Manifest empty or invalid after attempting to load.", this); return; }

        if (indexToProcess < 0 || indexToProcess >= manifestRoot.segmentsToProcess.Count)
        {
            Debug.LogError($"Selected segment index {indexToProcess} is out of range. Manifest contains {manifestRoot.segmentsToProcess.Count} segments (0 to {manifestRoot.segmentsToProcess.Count - 1}).", this);
            return;
        }

        HedraSegmentConfig selectedSegment = manifestRoot.segmentsToProcess[indexToProcess];
        List<HedraSegmentConfig> singleSegmentList = new List<HedraSegmentConfig> { selectedSegment };

        Debug.Log($"Starting single segment processing for index {indexToProcess} (Text: '{selectedSegment.text.Substring(0, Mathf.Min(selectedSegment.text.Length, 50))}') from '{manifestPath}' using API Mode: {selectedApiMode}...", this);
        
        isProcessingBatch = true; // Set processing flag, even for single segment
        if (selectedApiMode == ApiProcessingMode.NewPublicAPI)
        {
            batchProcessCoroutine = StartCoroutine(ProcessSegmentsCoroutine_NewAPI(singleSegmentList, isSingleSegmentRun: true));
        }
        else // LegacyV1API
        {
            batchProcessCoroutine = StartCoroutine(ProcessSegmentsCoroutine_LegacyAPI(singleSegmentList, isSingleSegmentRun: true));
        }
    }

    public void StopBatchProcessing()
    {
        if (batchProcessCoroutine != null)
        {
            Debug.Log($"Attempting to stop batch processing (Mode: {selectedApiMode})...", this);
            StopCoroutine(batchProcessCoroutine);
            batchProcessCoroutine = null;
            isProcessingBatch = false; 
        }
        else { Debug.LogWarning("Batch processing not running or already stopped.", this); }
    }

    public bool IsProcessingBatch() 
    {
        return isProcessingBatch;
    }

    private System.Collections.IEnumerator PostJsonApiCall<TRequest, TResponse>(string url, TRequest payload, System.Action<TResponse?, string?> callback)
        where TResponse : class // Ensure TResponse is a class type for default(TResponse) to be null
    {
        string? responseJson = null;
        string? error = null;
        long responseCode = -1;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            string jsonPayload = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string? apiKeyToUse = null;
            if (apiKeysSO != null)
            {
                apiKeyToUse = selectedApiMode == ApiProcessingMode.NewPublicAPI ? apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;
            }

            if (!string.IsNullOrEmpty(apiKeyToUse)) 
            { request.SetRequestHeader("X-API-KEY", apiKeyToUse); }
            else { error = $"API Key not set for {selectedApiMode} mode or APIKeysSO is null."; }

            if (error == null) yield return request.SendWebRequest();

            if (error == null)
            {
                responseCode = request.responseCode;
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
            catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}. Response: {responseJson}"); }
        }
        else { callback(null, error ?? "Unknown error in PostJsonApiCall"); }
    }

    private System.Collections.IEnumerator PostFileApiCall<TResponse>(string url, string filePath, System.Action<TResponse?, string?> callback)
        where TResponse : class
    {
        string? responseJson = null;
        string? error = null;
        long responseCode = -1;

        if (!File.Exists(filePath)) { callback(null, $"File not found: {filePath}"); yield break; }

        byte[] fileData = File.ReadAllBytes(filePath);
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", fileData, Path.GetFileName(filePath), MimeTypeMap.GetMimeType(Path.GetExtension(filePath))));

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            string? apiKeyToUse = null;
            if (apiKeysSO != null)
            {
                apiKeyToUse = selectedApiMode == ApiProcessingMode.NewPublicAPI ? apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;
            }
            
            if (!string.IsNullOrEmpty(apiKeyToUse))
            { request.SetRequestHeader("X-API-KEY", apiKeyToUse); }
            else { error = $"API Key not set for {selectedApiMode} mode or APIKeysSO is null."; }

            if (error == null) yield return request.SendWebRequest();

            if (error == null)
            {
                responseCode = request.responseCode;
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
            catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}. Response: {responseJson}"); }
        }
        else { callback(null, error ?? "Unknown error in PostFileApiCall"); }
    }

    private System.Collections.IEnumerator GetJsonApiCall<TResponse>(string url, System.Action<TResponse?, string?> callback)
       where TResponse : class
    {
        string? responseJson = null;
        string? error = null;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            string? apiKeyToUse = null;
            if (apiKeysSO != null)
            {
                apiKeyToUse = selectedApiMode == ApiProcessingMode.NewPublicAPI ? apiKeysSO.hedraApiKeyNewApi : apiKeysSO.hedraApiKey;
            }

            if (!string.IsNullOrEmpty(apiKeyToUse))
            { request.SetRequestHeader("X-API-KEY", apiKeyToUse); }
            else { error = $"API Key not set for {selectedApiMode} mode or APIKeysSO is null."; }

            if (error == null) yield return request.SendWebRequest();

            if (error == null)
            {
                if (request.result == UnityWebRequest.Result.Success)
                { responseJson = request.downloadHandler.text; }
                else
                { error = $"[{request.responseCode}] {request.error}. Response: {request.downloadHandler.text}"; }
            }
        }

        if (error == null && responseJson != null)
        { 
            try { callback(JsonUtility.FromJson<TResponse>(responseJson), null); } 
            catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}. Response: {responseJson}"); }
        }
        else { callback(null, error ?? "Unknown error in GetJsonApiCall"); }
    }

    private System.Collections.IEnumerator ProcessSegmentsCoroutine_NewAPI(List<HedraSegmentConfig> segments, bool isSingleSegmentRun = false)
    {
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKeyNewApi) || string.IsNullOrEmpty(apiKeysSO.hedraBaseUrlNewApi))
        {
            Debug.LogError("[ProcessSegments_NewAPI] APIKeysSO reference, Hedra New API Key, or New API Base URL are not set. Aborting processing.", this);
            if (!isSingleSegmentRun) 
            {
                isProcessingBatch = false; batchProcessCoroutine = null;
            }
            yield break;
        }
        // isProcessingBatch is set by the caller (StartBatchProcessing or StartSingleSegmentProcessing)
        // For single segment runs, isSingleSegmentRun will be true.
        // For batch runs, isSingleSegmentRun will be false.
        
        string baseUrl = apiKeysSO.hedraBaseUrlNewApi.TrimEnd('/'); 
        Debug.Log($"[ProcessSegments_NewAPI] Using NEW API FLOW. Base URL: {baseUrl}. Strict Sequential: {processBatchStrictlySequentially}. Single Run: {isSingleSegmentRun}", this);

        int segmentIdx = 0; 
        foreach (HedraSegmentConfig segment in segments)
        {
            if (stopProcessing && !isSingleSegmentRun) { Debug.Log("[ProcessSegments_NewAPI] Batch processing stopped by user.", this); break; }
            
            // Use segment.text for logging, as HedraSegmentConfig has 'text', not 'text_prompt' directly.
            string currentSegmentIdentifier = $"Segment {segmentIdx + 1}/{segments.Count} (New API)";
            Debug.Log($"[ProcessSegments_NewAPI] Processing {currentSegmentIdentifier}: {segment.text?.Substring(0, Mathf.Min(segment.text?.Length ?? 0, 50))}", this);

            string? uploadedAudioId = null;
            string? uploadedImageId = null;
            string? generationIdForPolling = null;

            // Variables for error tracking through the multi-step audio/image processing
            string? audioProcessingError = null; // Consolidated error for audio steps
            string? imageProcessingError = null; // Consolidated error for image steps

            // Step 1: Create Audio Asset & Upload
            if (string.IsNullOrEmpty(segment.voiceUrl))
            {
                Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Audio file path (segment.voiceUrl) is null or empty. Skipping audio processing for this segment.", this);
                audioProcessingError = "Audio file path is null or empty.";
            }
            else
            {
                string? audioAssetIdToUpload = null; // ID for the initially created asset slot
                // 1a. Create Audio Asset
                CreateAssetRequest audioAssetRequest = new CreateAssetRequest { name = Path.GetFileName(segment.voiceUrl), type = "audio" };
                Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 1a. Creating audio asset for: {segment.voiceUrl}", this);
                yield return StartCoroutine(PostJsonApiCall<CreateAssetRequest, CreateAssetResponse>(
                    $"{baseUrl}/public/assets", audioAssetRequest,
                    (res, err) => { 
                        audioAssetIdToUpload = res?.id; 
                        if (err != null) audioProcessingError = $"Failed to create audio asset: {err}";
                        else if (string.IsNullOrEmpty(audioAssetIdToUpload)) audioProcessingError = "Audio asset ID was null after creation.";
                    }
                ));

                // 1b. Upload Audio File (if asset creation succeeded)
                if (audioProcessingError == null && !string.IsNullOrEmpty(audioAssetIdToUpload))
                {
                    Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 1b. Audio asset created (ID: {audioAssetIdToUpload}). Uploading audio file {segment.voiceUrl}", this);
                    yield return StartCoroutine(PostFileApiCall<UploadedAssetConfirmation>(
                        $"{baseUrl}/public/assets/{audioAssetIdToUpload}/upload", segment.voiceUrl,
                        (res, err) => { 
                            // Use the ID from the confirmation if available, otherwise the original asset ID is assumed to be the final one.
                            uploadedAudioId = res?.id ?? audioAssetIdToUpload; 
                            if (err != null) audioProcessingError = $"Failed to upload audio: {err}";
                            else if (string.IsNullOrEmpty(uploadedAudioId)) audioProcessingError = "Uploaded audio ID was null after upload.";
                        }
                    ));
                } else if (audioProcessingError == null) { // Asset ID was null but no specific error from PostJsonApiCall
                     audioProcessingError = "Audio asset ID was unexpectedly null before upload attempt.";
                }
            }

            if (audioProcessingError != null)
            { Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Audio processing failed: {audioProcessingError}. Skipping segment.", this); segmentIdx++; continue; }
            Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Audio processing successful. Final Audio Asset ID: {uploadedAudioId}", this);

            // Step 2: Create Image Asset & Upload
            if (string.IsNullOrEmpty(segment.avatarImage))
            {
                Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Image file path (segment.avatarImage) is null or empty. Skipping image processing for this segment.", this);
                imageProcessingError = "Image file path is null or empty.";
            }
            else
            {
                string? imageAssetIdToUpload = null; // ID for the initially created asset slot
                // 2a. Create Image Asset
                CreateAssetRequest imageAssetRequest = new CreateAssetRequest { name = Path.GetFileName(segment.avatarImage), type = "image" };
                Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 2a. Creating image asset for: {segment.avatarImage}", this);
                yield return StartCoroutine(PostJsonApiCall<CreateAssetRequest, CreateAssetResponse>(
                    $"{baseUrl}/public/assets", imageAssetRequest,
                    (res, err) => { 
                        imageAssetIdToUpload = res?.id; 
                        if (err != null) imageProcessingError = $"Failed to create image asset: {err}";
                        else if (string.IsNullOrEmpty(imageAssetIdToUpload)) imageProcessingError = "Image asset ID was null after creation.";
                    }
                ));

                // 2b. Upload Image File (if asset creation succeeded)
                if (imageProcessingError == null && !string.IsNullOrEmpty(imageAssetIdToUpload))
                {
                    Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 2b. Image asset created (ID: {imageAssetIdToUpload}). Uploading image file {segment.avatarImage}", this);
                    yield return StartCoroutine(PostFileApiCall<UploadedAssetConfirmation>(
                        $"{baseUrl}/public/assets/{imageAssetIdToUpload}/upload", segment.avatarImage,
                        (res, err) => {
                            uploadedImageId = res?.id ?? imageAssetIdToUpload;
                            if (err != null) imageProcessingError = $"Failed to upload image: {err}";
                            else if (string.IsNullOrEmpty(uploadedImageId)) imageProcessingError = "Uploaded image ID was null after upload.";
                        }
                    ));
                } else if (imageProcessingError == null) { // Asset ID was null but no specific error from PostJsonApiCall
                    imageProcessingError = "Image asset ID was unexpectedly null before upload attempt.";
                }
            }
            
            if (imageProcessingError != null)
            { Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Image processing failed: {imageProcessingError}. Skipping segment.", this); segmentIdx++; continue; }
            Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Image processing successful. Final Image Asset ID: {uploadedImageId}", this);

            // Step 3: Call /public/generations
            GenerateVideoRequest generationPayload = new GenerateVideoRequest
            {
                ai_model_id = string.IsNullOrEmpty(segment.ai_model_id_override) ? defaultNewApiAiModelId : segment.ai_model_id_override,
                start_keyframe_id = uploadedImageId,
                audio_id = uploadedAudioId,
                generated_video_inputs = new GenerateVideoRequestInput
                {
                    text_prompt = segment.text, // Corrected: Use segment.text for the payload
                    aspect_ratio = segment.aspectRatio,
                    resolution = segment.resolution
                }
            };

            GenerateVideoResponse? generationResponse = null; string? generationError = null;
            string generationUrl = $"{baseUrl}/public/generations";
            Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 3a. Generating video with payload: {JsonUtility.ToJson(generationPayload, false)} to {generationUrl}", this);
            yield return StartCoroutine(PostJsonApiCall<GenerateVideoRequest, GenerateVideoResponse>(
                generationUrl, generationPayload,
                (res, err) => { generationResponse = res; generationError = err; }
            ));
            if (!string.IsNullOrEmpty(generationError) || string.IsNullOrEmpty(generationResponse?.id))
            { Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Failed to initiate video generation: {generationError ?? "Null ID"}. Skipping.", this); segmentIdx++; continue; }
            
            generationIdForPolling = generationResponse.id;
            Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 3b. Video generation initiated. Generation ID: {generationIdForPolling}", this);

            // Step 4: Start Polling
            string baseSegmentFileName = string.Empty; // Initialize to satisfy compiler
            try 
            {
                string? tempName = Path.GetFileNameWithoutExtension(segment.voiceUrl ?? segment.avatarImage ?? $"segment_{segmentIdx}");
                baseSegmentFileName = tempName ?? $"unknown_segment_{segmentIdx}"; // Ensure non-null
            }
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"Could not determine base filename for {currentSegmentIdentifier} due to: {ex.Message}. Using default ({segmentIdx}).", this);
                baseSegmentFileName = $"unknown_segment_{segmentIdx}"; // Fallback
            }
            // Now baseSegmentFileName is guaranteed to be non-null by prior assignments
            baseSegmentFileName = SanitizeFileName(baseSegmentFileName);
            
            string pollingSegmentIdentifier = $"{currentSegmentIdentifier} GenID: {generationIdForPolling}";
            if (!string.IsNullOrEmpty(generationIdForPolling))
            {
                Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] 4. Starting polling for Gen ID: {generationIdForPolling}. BaseFN: {baseSegmentFileName}. Single: {isSingleSegmentRun}. StrictSeq: {processBatchStrictlySequentially}.", this);
                Coroutine pollingCoroutine = StartCoroutine(PollProjectStatusCoroutine_NewAPI(generationIdForPolling, pollingSegmentIdentifier, baseSegmentFileName, isSingleSegmentRun));
                
                if (processBatchStrictlySequentially && !isSingleSegmentRun)
                {
                    Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Strict mode: Waiting for polling Gen ID {generationIdForPolling} to complete.", this);
                    yield return pollingCoroutine; 
                    Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Polling for Gen ID {generationIdForPolling} finished. Continuing in strict mode.", this);
                }
            }
            else { Debug.LogError($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] No GenerationID, cannot poll.", this); }
            
            if (!isSingleSegmentRun && (!processBatchStrictlySequentially || (processBatchStrictlySequentially && delayBetweenStrictSequentialSubmissions > 0)))
            {
                float delayToApply = processBatchStrictlySequentially ? delayBetweenStrictSequentialSubmissions : delayBetweenConcurrentSubmissions;
                if(delayToApply > 0)
                { Debug.Log($"[ProcessSegments_NewAPI - {currentSegmentIdentifier}] Delaying {delayToApply}s before next submission.", this); yield return new WaitForSeconds(delayToApply); }
            }
            segmentIdx++;
        }

        Debug.Log($"[ProcessSegments_NewAPI] Finished iterating {segments.Count} segments.", this);
        if (!isSingleSegmentRun) 
        { isProcessingBatch = false; batchProcessCoroutine = null; Debug.Log("[ProcessSegments_NewAPI] Batch processing finished & flags reset.", this); }
        else { Debug.Log("[ProcessSegments_NewAPI] Single segment iteration finished.", this); }
    }

    private System.Collections.IEnumerator PollProjectStatusCoroutine_NewAPI(string generationId, string segmentIdentifier, string baseSegmentFileName, bool isSingleSegmentRun = false)
    {
        Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Coroutine STARTING for Gen ID: {generationId}. baseFileName: {baseSegmentFileName}, sourceEpisodeJsonPath: {this.sourceEpisodeJsonPath ?? "NULL"}", this);
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKeyNewApi) || string.IsNullOrEmpty(apiKeysSO.hedraBaseUrlNewApi))
        {
            Debug.LogError($"[Polling_NewAPI - {segmentIdentifier}] APIKeysSO reference, Hedra New API Key, or New API Base URL are not set. Aborting polling for generation {generationId}.", this);
            if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
            yield break;
        }
        string baseUrl = apiKeysSO.hedraBaseUrlNewApi.TrimEnd('/');
        // Construct the specific path for /public/generations/{generationId}/status
        string pollingUrl = $"{baseUrl}/public/generations/{generationId}/status";
        Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Effective Polling URL: {pollingUrl}", this);

        int attempts = 0;
        // Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Initializing polling. URL: {pollingUrl}. Max attempts: {maxPollingAttempts}", this); // Redundant with above

        while (attempts < maxPollingAttempts)
        {
            if (!isProcessingBatch) { Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Batch stopped. Halting polling for {generationId}.", this); yield break; }
            attempts++;
            GenerationStatusResponse? statusResponse = null; string? pollError = null;
            yield return StartCoroutine(GetJsonApiCall<GenerationStatusResponse>(
                pollingUrl, 
                (res, err) => { statusResponse = res; pollError = err; }
            ));

            if (pollError != null)
            {
                Debug.LogError($"[Polling_NewAPI - {segmentIdentifier}] Error fetching status for {generationId} (Attempt {attempts}/{maxPollingAttempts}): {pollError}", this);
            }
            else if (statusResponse != null)
            {
                string currentStatus = statusResponse.status?.ToLower() ?? "unknown";
                Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Attempt {attempts}/{maxPollingAttempts}. GenID {generationId} Status: '{currentStatus}', Progress: {statusResponse.progress * 100f:F1}%. AssetID: {statusResponse.asset_id}", this);

                if (currentStatus == "complete")
                {
                    if (!string.IsNullOrEmpty(statusResponse.url))
                    { 
                        Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] SUCCESS! Generation {generationId} complete. Video URL: {statusResponse.url}", this);
                        string? sourceJsonDir = Path.GetDirectoryName(this.sourceEpisodeJsonPath);
                        Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] Values for download path: sourceEpisodeJsonPath='{this.sourceEpisodeJsonPath ?? "NULL"}', sourceJsonDir='{sourceJsonDir ?? "NULL"}', baseSegmentFileName='{baseSegmentFileName ?? "NULL"}'", this);

                        if (!string.IsNullOrEmpty(sourceJsonDir) && !string.IsNullOrEmpty(baseSegmentFileName))
                        {
                            string videoSaveDir = Path.Combine(sourceJsonDir, outputSubfolderName, "final_videos");
                            string videoSavePath = Path.Combine(videoSaveDir, $"{baseSegmentFileName}_new.mp4").Replace("\\", "/");
                            Debug.Log($"[Polling_NewAPI - {segmentIdentifier}] About to start DownloadFileCoroutine for URL: {statusResponse.url} to Path: {videoSavePath}", this);
                            StartCoroutine(DownloadFileCoroutine(statusResponse.url, videoSavePath));
                        }
                        else
                        {
                            Debug.LogError($"[Polling_NewAPI - {segmentIdentifier}] Could not determine source JSON directory or base filename ('{baseSegmentFileName}') to save video.", this);
                        }
                    }
                    else
                    { Debug.LogError($"[Polling_NewAPI - {segmentIdentifier}] Gen {generationId} status is '{currentStatus}' but no URL found.", this); }
                    if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
                    yield break; 
                }
                else if (currentStatus == "error")
                {
                    Debug.LogError($"[Polling_NewAPI - {segmentIdentifier}] FAILED! Generation {generationId} status: '{currentStatus}'. Error: {statusResponse.error_message ?? "N/A"}", this);
                    if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
                    yield break; 
                }
                // For "processing", "finalizing", "queued", "pending" -> continue polling
            }
            else { Debug.LogWarning($"[Polling_NewAPI - {segmentIdentifier}] Status response was null for {generationId} (Attempt {attempts}).", this); }

            if (attempts >= maxPollingAttempts)
            { Debug.LogWarning($"[Polling_NewAPI - {segmentIdentifier}] TIMEOUT. Generation {generationId} did not complete after {maxPollingAttempts} attempts.", this); if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; } yield break; }
            
            yield return new WaitForSeconds(pollingIntervalSeconds);
        }
        // Debug.Log("[ProcessSegments_NewAPI] All segments processed. Polling will continue for active jobs.", this); // Commenting out, seems misplaced
        // isProcessingBatch = false; // This should not be here, polling completion for one segment doesn't mean batch is done.
        // batchProcessCoroutine = null; // This should not be here either.
    }

    // --- LEGACY API Processing Coroutines ---
    private System.Collections.IEnumerator ProcessSegmentsCoroutine_LegacyAPI(List<HedraSegmentConfig> segments, bool isSingleSegmentRun = false)
    {
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKey) || string.IsNullOrEmpty(apiKeysSO.hedraBaseUrl))
        {
            Debug.LogError("[ProcessSegments_LegacyAPI] API Keys or Base URL are not set. Aborting processing.", this);
            if (!isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
            yield break;
        }
        // isProcessingBatch is already true, set by the caller
        string baseUrl = apiKeysSO.hedraBaseUrl.TrimEnd('/');
        Debug.Log($"[ProcessSegments_LegacyAPI] Using LEGACY API. Base URL: {baseUrl}. Strict Seq: {processBatchStrictlySequentially}. Single Run: {isSingleSegmentRun}", this);

        int segmentIdx = 0;
        foreach (HedraSegmentConfig segment in segments) 
        {
            if (stopProcessing && !isSingleSegmentRun) { Debug.Log("[ProcessSegments_LegacyAPI] Batch processing stopped by user.", this); break; }

            string currentSegmentIdentifier = $"Segment {segmentIdx + 1}/{segments.Count} (Legacy API)";
            Debug.Log($"[ProcessSegments_LegacyAPI] Processing {currentSegmentIdentifier}: {segment.text?.Substring(0, Mathf.Min(segment.text?.Length ?? 0, 50))}", this);

            string? uploadedAudioUrl = null;
            string? uploadedImageUrl = null;
            string? projectIdForPolling = null;

            // Step 1: Upload Audio
            Legacy_HedraUrlResponse? audioUrlResponse = null; 
            string? uploadAudioError = null;
            if (string.IsNullOrEmpty(segment.voiceUrl))
            {
                Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Audio file path (segment.voiceUrl) is null or empty. Cannot upload audio.", this);
                uploadAudioError = "Audio file path is null or empty.";
            }
            else
            {
                Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] 1. Uploading audio from {segment.voiceUrl}", this);
                yield return StartCoroutine(PostFileApiCall_Legacy<Legacy_HedraUrlResponse>(
                    $"{baseUrl}/v1/audio", segment.voiceUrl, "file", 
                    (res, err) => { 
                        audioUrlResponse = res; 
                        uploadAudioError = err; 
                    }
                ));
            }

            if (uploadAudioError != null || audioUrlResponse?.url == null)
            { Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Failed to upload audio: {uploadAudioError ?? "Audio URL response or URL itself was null"}. Skipping.", this); segmentIdx++; continue; }
            uploadedAudioUrl = audioUrlResponse.url; // Now we know uploadedAudioUrl is not null
            Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Audio uploaded. URL: {uploadedAudioUrl}", this);

            // Step 2: Upload Image
            Legacy_HedraUrlResponse? imageUrlResponse = null; 
            string? uploadImageError = null;
            if (string.IsNullOrEmpty(segment.avatarImage))
            {
                Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Image file path (segment.avatarImage) is null or empty. Cannot upload image.", this);
                uploadImageError = "Image file path is null or empty.";
            }
            else
            {
                Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] 2. Uploading image from {segment.avatarImage}", this);
                yield return StartCoroutine(PostFileApiCall_Legacy<Legacy_HedraUrlResponse>(
                    $"{baseUrl}/v1/portrait", segment.avatarImage, "file", 
                    (res, err) => { 
                        imageUrlResponse = res; 
                        uploadImageError = err; 
                    }
                ));
            }

            if (uploadImageError != null || imageUrlResponse?.url == null)
            { Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Failed to upload image: {uploadImageError ?? "Image URL response or URL itself was null"}. Skipping.", this); segmentIdx++; continue; }
            
            // Ensure non-null before assignment to payload
            string finalUploadedAudioUrl = uploadedAudioUrl!;
            string finalUploadedImageUrl = imageUrlResponse.url; // imageUrlResponse.url is confirmed non-null by the check above

            Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Image uploaded. URL: {finalUploadedImageUrl}", this);

            // Step 3: Call /v1/characters
            Legacy_HedraCharacterRequestPayload characterPayload = new Legacy_HedraCharacterRequestPayload
            {
                text = segment.text,
                voiceUrl = finalUploadedAudioUrl, 
                avatarImage = finalUploadedImageUrl, 
                aspectRatio = segment.aspectRatio, 
                audioSource = segment.audioSource,
                voiceId = string.IsNullOrEmpty(segment.voiceId) ? null : segment.voiceId,
                avatarImageInput = (segment.avatarImageInput?.prompt != null || segment.avatarImageInput?.seed != null) ? segment.avatarImageInput : null
            };
            Legacy_HedraCharacterResponse? characterResponse = null; string? characterError = null;
            Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] 3. Generating character with payload: {JsonUtility.ToJson(characterPayload, false)}", this);
            yield return StartCoroutine(PostJsonApiCall<Legacy_HedraCharacterRequestPayload, Legacy_HedraCharacterResponse>(
                $"{baseUrl}/v1/characters", characterPayload,
                (res, err) => { characterResponse = res; characterError = err; }
            ));
            if (characterError != null || (characterResponse?.projectId == null && characterResponse?.jobId == null))
            { Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Failed character gen: {characterError ?? "Null ID"}. Skipping.", this); segmentIdx++; continue; }
            
            projectIdForPolling = characterResponse.projectId ?? characterResponse.jobId;
            Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Character gen initiated. Proj/Job ID: {projectIdForPolling}", this);

            // Step 4: Start Polling
            string baseSegmentFileName = string.Empty; // Initialize to satisfy compiler
            try 
            {
                string? tempName = Path.GetFileNameWithoutExtension(segment.voiceUrl ?? segment.avatarImage ?? $"segment_{segmentIdx}");
                baseSegmentFileName = tempName ?? $"unknown_segment_{segmentIdx}"; // Ensure non-null
            }
            catch (System.Exception ex) 
            {
                Debug.LogWarning($"Could not determine base filename for {currentSegmentIdentifier} due to: {ex.Message}. Using default ({segmentIdx}).", this);
                baseSegmentFileName = $"unknown_segment_{segmentIdx}"; // Fallback
            }
            // Now baseSegmentFileName is guaranteed to be non-null by prior assignments
            baseSegmentFileName = SanitizeFileName(baseSegmentFileName);
            
            string pollingSegmentIdentifier = $"{currentSegmentIdentifier} ProjID: {projectIdForPolling}";
            if (!string.IsNullOrEmpty(projectIdForPolling))
            {
                Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] 4. Starting polling for Proj ID: {projectIdForPolling}. BaseFN: {baseSegmentFileName}. Single: {isSingleSegmentRun}. StrictSeq: {processBatchStrictlySequentially}.", this);
                Coroutine pollingCoroutine = StartCoroutine(PollProjectStatusCoroutine_LegacyAPI(projectIdForPolling, pollingSegmentIdentifier, baseSegmentFileName, isSingleSegmentRun));
                
                if (processBatchStrictlySequentially && !isSingleSegmentRun)
                {
                    Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Strict mode: Waiting for polling Proj ID {projectIdForPolling} to complete.", this);
                    yield return pollingCoroutine; 
                    Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Polling for Proj ID {projectIdForPolling} finished. Continuing in strict mode.", this);
                }
            }
            else { Debug.LogError($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] No ProjectID/JobID, cannot poll.", this); }
            
            if (!isSingleSegmentRun && (!processBatchStrictlySequentially || (processBatchStrictlySequentially && delayBetweenStrictSequentialSubmissions > 0)))
            {
                float delayToApply = processBatchStrictlySequentially ? delayBetweenStrictSequentialSubmissions : delayBetweenConcurrentSubmissions;
                if(delayToApply > 0)
                { Debug.Log($"[ProcessSegments_LegacyAPI - {currentSegmentIdentifier}] Delaying {delayToApply}s before next submission.", this); yield return new WaitForSeconds(delayToApply); }
            }
            segmentIdx++;
        }

        Debug.Log($"[ProcessSegments_LegacyAPI] Finished iterating {segments.Count} segments.", this);
        if (!isSingleSegmentRun)
        { isProcessingBatch = false; batchProcessCoroutine = null; Debug.Log("[ProcessSegments_LegacyAPI] Batch processing finished & flags reset.", this); }
        else { Debug.Log("[ProcessSegments_LegacyAPI] Single segment iteration finished.", this); }
    }

    private System.Collections.IEnumerator PollProjectStatusCoroutine_LegacyAPI(string projectId, string segmentIdentifier, string baseSegmentFileName, bool isSingleSegmentRun = false)
    {
        Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Coroutine STARTING for Project ID: {projectId}. isProcessingBatch: {isProcessingBatch}, isSingleSegmentRun: {isSingleSegmentRun}, baseFileName: {baseSegmentFileName}, sourceEpisodeJsonPath: {this.sourceEpisodeJsonPath ?? "NULL"}", this);
        if (apiKeysSO == null || string.IsNullOrEmpty(apiKeysSO.hedraApiKey) || string.IsNullOrEmpty(apiKeysSO.hedraBaseUrl))
        {
            Debug.LogError($"[Polling_LegacyAPI - {segmentIdentifier}] API Keys or Base URL are not set. Aborting polling for project {projectId}.", this);
            if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
            yield break;
        }
        string baseUrl = apiKeysSO.hedraBaseUrl.TrimEnd('/');
        string pollingUrl = $"{baseUrl}/v1/projects/{projectId}"; 
        int attempts = 0;
        Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Initializing polling. URL: {pollingUrl}. Max attempts: {maxPollingAttempts}", this);

        while (attempts < maxPollingAttempts)
        {
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Loop attempt {attempts + 1}. isProcessingBatch: {isProcessingBatch}", this);
            if (!isProcessingBatch) 
            {
                 Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Batch stopped (isProcessingBatch is false). Halting polling for {projectId}.", this);
                 yield break; 
            }
            attempts++;
            Legacy_HedraProjectStatusResponse? statusResponse = null; string? pollError = null;
            
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: About to call GetJsonApiCall.", this);
            yield return StartCoroutine(GetJsonApiCall<Legacy_HedraProjectStatusResponse>(
                pollingUrl,
                (res, err) => { 
                    statusResponse = res; 
                    pollError = err; 
                    Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: GetJsonApiCall callback executed. Error: {err ?? "null"}", this);
                }
            ));
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: Returned from GetJsonApiCall coroutine. pollError: {pollError ?? "null"}", this);

            if (pollError != null)
            {
                Debug.LogError($"[Polling_LegacyAPI - {segmentIdentifier}] Error fetching status for {projectId} (Attempt {attempts}/{maxPollingAttempts}): {pollError}", this);
                // No yield break here, will let it try again unless max attempts are hit, or we could break if it's a critical error.
            }
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: After pollError check.", this);
            
            if (statusResponse != null)
            {
                string currentStatus = statusResponse.status?.ToLower() ?? "unknown";
                Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}/{maxPollingAttempts}. ProjectID {projectId} Status: '{currentStatus}', Progress: {(statusResponse.progress.HasValue ? statusResponse.progress.Value * 100f : -1f):F1}%. Stage: {statusResponse.stage ?? "N/A"}", this);

                if (currentStatus == "succeeded" || currentStatus == "completed") 
                {
                    if (!string.IsNullOrEmpty(statusResponse.videoUrl))
                    { 
                        Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] SUCCESS (Status: {currentStatus})! Project {projectId} complete. Video URL: {statusResponse.videoUrl}", this);
                        string? sourceJsonDir = Path.GetDirectoryName(this.sourceEpisodeJsonPath);
                        Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Values for download path: sourceEpisodeJsonPath='{this.sourceEpisodeJsonPath ?? "NULL"}', sourceJsonDir='{sourceJsonDir ?? "NULL"}', baseSegmentFileName='{baseSegmentFileName ?? "NULL"}'", this);

                        if (!string.IsNullOrEmpty(sourceJsonDir) && !string.IsNullOrEmpty(baseSegmentFileName))
                        {
                            string videoSaveDir = Path.Combine(sourceJsonDir, outputSubfolderName, "final_videos");
                            string videoSavePath = Path.Combine(videoSaveDir, $"{baseSegmentFileName}_legacy.mp4").Replace("\\", "/");
                            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] About to start DownloadFileCoroutine for URL: {statusResponse.videoUrl} to Path: {videoSavePath}", this);
                            StartCoroutine(DownloadFileCoroutine(statusResponse.videoUrl, videoSavePath));
                        }
                        else
                        {
                            Debug.LogError($"[Polling_LegacyAPI - {segmentIdentifier}] Could not determine source JSON directory or base filename ('{baseSegmentFileName}') to save video.", this);
                        }
                    }
                    else
                    { Debug.LogWarning($"[Polling_LegacyAPI - {segmentIdentifier}] Project {projectId} status is '{currentStatus}' but no videoUrl found. Assuming success but check output.", this); }
                    Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Exiting polling due to {currentStatus.ToUpper()}. Current Time.time: {Time.time}", this);
                    if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
                    yield break; 
                }
                else if (currentStatus == "failed")
                {
                    Debug.LogError($"[Polling_LegacyAPI - {segmentIdentifier}] FAILED! Project {projectId} status: '{currentStatus}'. Error: {statusResponse.errorMessage ?? "N/A"}", this);
                    Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Exiting polling due to FAILURE. Current Time.time: {Time.time}", this);
                    if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
                    yield break;
                }
            }
            else 
            {
                 Debug.LogWarning($"[Polling_LegacyAPI - {segmentIdentifier}] Status response was null for {projectId} (Attempt {attempts}). pollError was: {pollError ?? "null"}", this);
            }
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: After statusResponse check.", this);

            if (attempts >= maxPollingAttempts)
            { 
                Debug.LogWarning($"[Polling_LegacyAPI - {segmentIdentifier}] TIMEOUT. Project {projectId} did not complete after {maxPollingAttempts} attempts.", this);
                Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Exiting polling due to TIMEOUT. Current Time.time: {Time.time}", this);
                if (isSingleSegmentRun) { isProcessingBatch = false; batchProcessCoroutine = null; }
                yield break; 
            }
            
            // Enhanced logging around WaitForSeconds
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: PRE-WAIT. About to wait for {pollingIntervalSeconds} seconds. Current Time.time: {Time.time}, Time.timeScale: {Time.timeScale}, isProcessingBatch: {isProcessingBatch}, GO Active: {this.gameObject.activeInHierarchy}, Component Enabled: {this.enabled}", this);
            yield return new WaitForSeconds(pollingIntervalSeconds);
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: POST-WAIT. Resumed after WaitForSeconds. Current Time.time: {Time.time}, isProcessingBatch: {isProcessingBatch}, GO Active: {this.gameObject.activeInHierarchy}, Component Enabled: {this.enabled}", this);
            
            // This was the original log that was expected next
            Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Attempt {attempts}: Finished waiting. End of while loop iteration. Current Time.time: {Time.time}", this);
        }
        Debug.Log($"[Polling_LegacyAPI - {segmentIdentifier}] Exited polling loop for project {projectId} (e.g. max attempts reached or other break). Current Time.time: {Time.time}", this);
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); 
        #endif
    }
    // --- End of LEGACY API Processing Coroutines ---

    // Specific file post for Legacy API that requires a 'fileFieldName'
    private System.Collections.IEnumerator PostFileApiCall_Legacy<TResponse>(string url, string filePath, string fileFieldName, System.Action<TResponse?, string?> callback)
        where TResponse : class
    {
        Debug.Log($"[PostFileApiCall_Legacy] Entered. URL: {url}, FilePath: {filePath}", this);
        string? responseJson = null;
        string? error = null;
        long responseCode = -1;

        if (!File.Exists(filePath)) 
        {
            Debug.LogError($"[PostFileApiCall_Legacy] File not found at path: {filePath}", this);
            callback(null, $"File not found: {filePath}"); 
            yield break; 
        }
        Debug.Log($"[PostFileApiCall_Legacy] File exists at path: {filePath}", this);

        byte[] fileData;
        try
        {
            Debug.Log($"[PostFileApiCall_Legacy] Attempting to read file: {filePath}", this);
            fileData = File.ReadAllBytes(filePath);
            Debug.Log($"[PostFileApiCall_Legacy] Successfully read {fileData.Length} bytes from {filePath}", this);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PostFileApiCall_Legacy] Error reading file {filePath}: {ex.Message}", this);
            callback(null, $"Error reading file {filePath}: {ex.Message}");
            yield break;
        }
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection(fileFieldName, fileData, Path.GetFileName(filePath), MimeTypeMap.GetMimeType(Path.GetExtension(filePath))));
        Debug.Log($"[PostFileApiCall_Legacy] FormData prepared. FileFieldName: {fileFieldName}, FileName: {Path.GetFileName(filePath)}", this);

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
            Debug.Log($"[PostFileApiCall_Legacy] UnityWebRequest created. Method: POST, URL: {request.url}", this);
            request.timeout = 60; // Set timeout to 60 seconds
            Debug.Log($"[PostFileApiCall_Legacy] Timeout set to {request.timeout} seconds.", this);

            if (apiKeysSO != null && !string.IsNullOrEmpty(apiKeysSO.hedraApiKey))
            { 
                request.SetRequestHeader("X-API-KEY", apiKeysSO.hedraApiKey); 
                Debug.Log($"[PostFileApiCall_Legacy] X-API-KEY header set.", this);
            }
            else 
            { 
                error = "API Key not set for Legacy PostFile"; 
                Debug.LogError($"[PostFileApiCall_Legacy] Error: {error}", this);
            }

            if (error == null) 
            {
                Debug.Log($"[PostFileApiCall_Legacy] About to send web request to {url}...", this);
                yield return request.SendWebRequest();
                Debug.Log($"[PostFileApiCall_Legacy] Web request sent. Result: {request.result}, Response Code: {request.responseCode}", this);
            }

            if (error == null)
            {
                responseCode = request.responseCode;
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
            catch (System.Exception ex) { callback(null, $"JSON Parse Error: {ex.Message}. Response: {responseJson}"); }
        }
        else { callback(null, error ?? "Unknown error in PostFileApiCall_Legacy"); }
    }

    public void LoadAndDisplayManifestContent()
    {
        if (!string.IsNullOrEmpty(explicitManifestPath) && File.Exists(explicitManifestPath))
        {
            try
            {
                loadedManifestContent = File.ReadAllText(explicitManifestPath);
                Debug.Log($"Successfully loaded content from manifest: {explicitManifestPath}", this);
            }
            catch (System.Exception ex)
            {
                loadedManifestContent = $"Error loading manifest: {ex.Message}";
                Debug.LogError($"Error reading manifest content from '{explicitManifestPath}': {ex.Message}", this);
            }
        }
        else
        {
            loadedManifestContent = "Manifest path not set or file not found. Please generate or select a manifest file.";
            if (!string.IsNullOrEmpty(explicitManifestPath)) 
            {
                Debug.LogWarning($"Attempted to load manifest, but file not found at: {explicitManifestPath}", this);
            }
        }
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); 
        #endif
    }

    // Coroutine to download a file from a URL
    private System.Collections.IEnumerator DownloadFileCoroutine(string url, string localSavePath)
    {
        Debug.Log($"[DownloadFile] Attempting to download from: {url} to {localSavePath}", this);
        UnityWebRequest request = UnityWebRequest.Get(url);
        // No API key needed for S3-style URLs usually, but include if direct server access requires it
        // if (apiKeysSO != null && !string.IsNullOrEmpty(apiKeysSO.hedraApiKey)) 
        // { request.SetRequestHeader("X-API-KEY", apiKeysSO.hedraApiKey); }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                string? directory = Path.GetDirectoryName(localSavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory); // Ensure directory exists
                    Debug.Log($"[DownloadFile] Ensured directory exists or created: {directory}", this);
                }
                File.WriteAllBytes(localSavePath, request.downloadHandler.data);
                Debug.Log($"[DownloadFile] Successfully downloaded ({request.downloadedBytes} bytes) and saved to: {localSavePath}", this);
                
                #if UNITY_EDITOR
                // Try to refresh the asset database if it's in the Assets folder
                string assetDbPath = localSavePath;
                if (assetDbPath.StartsWith(Application.dataPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    assetDbPath = "Assets" + assetDbPath.Substring(Application.dataPath.Length);
                    UnityEditor.AssetDatabase.ImportAsset(assetDbPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                    Object? obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetDbPath);
                    if (obj != null) 
                    { 
                        Debug.Log($"[DownloadFile] Pinging downloaded asset in Project window: {assetDbPath}", this);
                        UnityEditor.EditorGUIUtility.PingObject(obj); 
                    }
                }
                else { Debug.Log($"[DownloadFile] Video saved outside Assets folder, not pinging: {assetDbPath}", this); }
                #endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DownloadFile] Error saving file to {localSavePath}: {ex.Message}", this);
            }
        }
        else
        {
            Debug.LogError($"[DownloadFile] Error downloading file from {url}: [{request.responseCode}] {request.error}. Response: {request.downloadHandler.text}", this);
        }
        request.Dispose();
    }

    // Simple MimeType helper (can be expanded)
    public static class MimeTypeMap
    {
        private static readonly Dictionary<string, string> _mappings = new Dictionary<string, string>(System.StringComparer.InvariantCultureIgnoreCase)
        {
            {".mp3", "audio/mpeg"},
            {".wav", "audio/wav"},
            {".ogg", "audio/ogg"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
        };
        public static string GetMimeType(string extension)
        {
            if (extension == null) throw new System.ArgumentNullException(nameof(extension));
            if (!extension.StartsWith(".")) extension = "." + extension;
            return _mappings.TryGetValue(extension, out string? mime) ? mime : "application/octet-stream";
        }
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) // Added guard against null/empty input just in case
        {
            return "unknown_filename";
        }
        string[] parts = input.Split(Path.GetInvalidFileNameChars());
        return string.Join("_", parts);
    }
} 