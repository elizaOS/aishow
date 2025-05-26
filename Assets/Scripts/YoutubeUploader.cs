using UnityEngine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles uploading videos to YouTube using the YouTube Data API v3.
/// </summary>
public class YoutubeUploader : MonoBehaviour
{
    [Header("YouTube API Settings")]
    [Tooltip("Path to the client_secrets.json file relative to the StreamingAssets folder.")]
    public string clientSecretsPath = "client_secrets.json";

    [Header("Video Details")]
    [Tooltip("Path to the video file to upload.")]
    public string videoPathToUpload;
    public string videoTitle = "My Unity Video";
    public string videoDescription = "Uploaded from Unity!";
    [Tooltip("Comma-separated tags for the video.")]
    public string videoTags = "unity,gamedev";
    [Tooltip("YouTube video category ID. '22' is People & Blogs. See https://developers.google.com/youtube/v3/docs/videoCategories/list")]
    public string videoCategoryId = "22"; // Default to "People & Blogs"
    public enum PrivacyStatus { Public, Private, Unlisted }
    public PrivacyStatus videoPrivacyStatus = PrivacyStatus.Private;

    private UserCredential _credential;

    // Fields for Editor Script feedback
    [HideInInspector] public string currentUploadStatus = "Idle";
    [HideInInspector] public float currentUploadProgress = 0f; // 0.0 to 1.0
    private long _totalBytesToUpload = 0;
    private bool _isUploading = false; // To help editor repaint

    /// <summary>
    /// Authenticates with Google and initiates the video upload process.
    /// This method should be called to start the upload, e.g., from a UI button or editor script.
    /// </summary>
    public async void UploadVideo()
    {
        if (_isUploading)
        {
            Debug.LogWarning("Upload already in progress.");
            currentUploadStatus = "Upload already in progress.";
            return;
        }

        if (string.IsNullOrEmpty(videoPathToUpload))
        {
            Debug.LogError("Video path to upload is not set.");
            currentUploadStatus = "Error: Video path not set.";
            _isUploading = false;
            return;
        }

        if (!File.Exists(videoPathToUpload))
        {
            Debug.LogError($"Video file not found at path: {videoPathToUpload}");
            currentUploadStatus = "Error: Video file not found.";
            _isUploading = false;
            return;
        }

        _isUploading = true;
        currentUploadStatus = "Starting...";
        currentUploadProgress = 0f;
        try
        {
            _totalBytesToUpload = new FileInfo(videoPathToUpload).Length;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting video file info: {ex.Message}");
            currentUploadStatus = "Error: Could not read video file info.";
            _isUploading = false;
            return;
        }

        try
        {
            await AuthenticateAsync();
            if (_credential != null)
            {
                await UploadVideoAsync();
            }
            else
            {
                Debug.LogError("Authentication failed. Cannot upload video.");
                currentUploadStatus = "Authentication failed.";
                _isUploading = false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred during the upload process: {ex.Message}\n{ex.StackTrace}");
            currentUploadStatus = $"Error: {ex.Message}";
            currentUploadProgress = 0f;
            _isUploading = false;
        }
    }

    /// <summary>
    /// Handles OAuth 2.0 authentication.
    /// </summary>
    private async Task AuthenticateAsync()
    {
        Debug.Log("Starting authentication...");
        currentUploadStatus = "Authenticating...";

        string secretsFilePath = Path.Combine(Application.streamingAssetsPath, clientSecretsPath);

        if (!File.Exists(secretsFilePath))
        {
            Debug.LogError($"Client secrets file not found at: {secretsFilePath}. " +
                           "Please ensure client_secrets.json is in the StreamingAssets folder and the path is correct.");
            currentUploadStatus = "Error: client_secrets.json not found.";
            _credential = null;
            return;
        }

        ClientSecrets clientSecrets;
        try
        {
            using (var stream = new FileStream(secretsFilePath, FileMode.Open, FileAccess.Read))
            {
                clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading client secrets file: {ex.Message}");
            currentUploadStatus = "Error: Could not read client_secrets.json.";
            _credential = null;
            return;
        }

        string[] scopes = { YouTubeService.Scope.YoutubeUpload };
        var dataStorePath = Path.Combine(Application.persistentDataPath, "YouTube.Auth.Store");
        var dataStore = new Google.Apis.Util.Store.FileDataStore(dataStorePath, true);

        try
        {
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                scopes,
                "user",
                CancellationToken.None,
                dataStore
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"OAuth Error: {ex.Message}");
            currentUploadStatus = $"OAuth Error: {ex.Message}";
            _credential = null;
            return;
        }

        if (_credential != null)
        {
            Debug.Log("Authentication successful.");
            currentUploadStatus = "Authentication successful."; 
            if (_credential.Token.IsStale)
            {
                currentUploadStatus = "Refreshing token...";
                Debug.Log("Access token is stale, attempting to refresh...");
                bool success = false;
                try
                {
                    success = await _credential.RefreshTokenAsync(CancellationToken.None);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Token refresh error: {ex.Message}");
                    currentUploadStatus = $"Token refresh error: {ex.Message}";
                    _credential = null; 
                    return;
                }

                if (success)
                {
                    Debug.Log("Access token refreshed successfully.");
                    currentUploadStatus = "Token refreshed. Ready to upload.";
                }
                else
                {
                    Debug.LogError("Failed to refresh access token.");
                    currentUploadStatus = "Failed to refresh token.";
                    _credential = null; 
                }
            }
            else
            {
                currentUploadStatus = "Ready to upload.";
            }
        }
        else
        {
            Debug.LogError("Authentication resulted in null credentials.");
            currentUploadStatus = "Authentication failed (null credentials).";
        }
    }

    /// <summary>
    /// Uploads the specified video file to YouTube.
    /// </summary>
    private async Task UploadVideoAsync()
    {
        if (_credential == null)
        {
            Debug.LogError("Not authenticated. Please authenticate first.");
            currentUploadStatus = "Error: Not authenticated.";
            _isUploading = false;
            return;
        }

        Debug.Log($"Starting video upload for: {videoPathToUpload}");
        currentUploadStatus = "Preparing video for upload...";

        var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = _credential,
            ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
        });

        var video = new Video();
        video.Snippet = new VideoSnippet
        {
            Title = videoTitle,
            Description = videoDescription,
            Tags = videoTags.Split(','),
            CategoryId = videoCategoryId
        };
        video.Status = new VideoStatus
        {
            PrivacyStatus = videoPrivacyStatus.ToString().ToLowerInvariant()
        };

        using (var fileStream = new FileStream(videoPathToUpload, FileMode.Open, FileAccess.Read))
        {
            var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status",
                fileStream, "video/*");
            videosInsertRequest.ChunkSize = Google.Apis.YouTube.v3.VideosResource.InsertMediaUpload.DefaultChunkSize; // Uses the default 10MB chunk size
            videosInsertRequest.ProgressChanged += OnUploadProgress;
            videosInsertRequest.ResponseReceived += OnUploadResponseReceived;

            Debug.Log("Beginning resumable upload...");
            currentUploadStatus = "Uploading (0%)...";
            await videosInsertRequest.UploadAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Handles progress updates for the video upload.
    /// </summary>
    private void OnUploadProgress(Google.Apis.Upload.IUploadProgress progress)
    {
        switch (progress.Status)
        {
            case UploadStatus.Starting:
                currentUploadStatus = "Upload starting...";
                currentUploadProgress = 0f;
                Debug.Log("Upload starting...");
                break;
            case UploadStatus.Uploading:
                if (_totalBytesToUpload > 0)
                {
                    currentUploadProgress = (float)progress.BytesSent / _totalBytesToUpload;
                }
                else
                {
                    currentUploadProgress = 0f; // Avoid division by zero if _totalBytesToUpload wasn't set
                }
                currentUploadStatus = $"Uploading ({currentUploadProgress * 100:F1}%): {progress.BytesSent / (1024*1024):F2}MB / {_totalBytesToUpload / (1024*1024):F2}MB";
                Debug.Log($"Uploading: {progress.BytesSent} bytes sent ({currentUploadProgress * 100:F1}%).");
                break;
            case UploadStatus.Completed:
                // ResponseReceived will handle the final status message for completion.
                // currentUploadStatus = "Upload processing..."; 
                // currentUploadProgress = 1f;
                Debug.Log("Upload status: Completed. Waiting for response...");
                break;
            case UploadStatus.Failed:
                Debug.LogError($"Upload failed: {progress.Exception?.Message}");
                currentUploadStatus = $"Upload failed: {progress.Exception?.Message}";
                currentUploadProgress = 0f;
                _isUploading = false;
                break;
            case UploadStatus.NotStarted:
                currentUploadStatus = "Upload not started.";
                Debug.Log("Upload not started yet.");
                break;
        }
    }

    /// <summary>
    /// Handles the response received after the video upload is completed.
    /// </summary>
    private void OnUploadResponseReceived(Video video)
    {
        Debug.Log($"Video upload completed. Video ID: {video.Id}");
        currentUploadStatus = $"Upload Complete! Video ID: {video.Id}";
        currentUploadProgress = 1f;
        _isUploading = false;
    }

    /// <summary>
    /// Public accessor for the editor script to know if an upload is active.
    /// </summary>
    public bool IsUploading => _isUploading;

    // Example of how to call this from another script or a UI button:
    // public YoutubeUploader uploader; // Assign in inspector
    // public void StartUploadProcess()
    // {
    //     uploader.videoPathToUpload = "C:/path/to/your/video.mp4"; // Set this dynamically
    //     uploader.UploadVideo();
    // }
} 