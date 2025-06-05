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

    [Header("Video Details (for Inspector-driven uploads)")]
    [Tooltip("Path to the video file to upload if using parameterless UploadVideo().")]
    public string videoPathToUpload;
    public string videoTitle = "My Unity Video";
    public string videoDescription = "Uploaded from Unity!";
    [Tooltip("Comma-separated tags for the video.")]
    public string videoTags = "unity,gamedev";
    [Tooltip("YouTube video category ID. '22' is People & Blogs. See https://developers.google.com/youtube/v3/docs/videoCategories/list")]
    public string videoCategoryId = "22"; // Default to "People & Blogs"
    public enum PrivacyStatus { Public, Private, Unlisted }
    public PrivacyStatus videoPrivacyStatus = PrivacyStatus.Private;
    [Tooltip("Path to the thumbnail file if using parameterless UploadVideo(). Optional.")]
    public string thumbnailPathToUpload;

    private UserCredential _credential;
    private YouTubeService _youtubeService; // Store the service instance

    // Fields for Editor Script feedback
    [HideInInspector] public string currentUploadStatus = "Idle";
    [HideInInspector] public float currentUploadProgress = 0f; // 0.0 to 1.0
    private long _totalBytesToUpload = 0;
    private long _currentBytesUploaded = 0; // For combined progress
    private bool _isUploading = false; // To help editor repaint
    private string _uploadedVideoId = null;

    // Store active paths for progress calculation during an UploadVideoWithDetails operation
    private string _activeVideoPath;
    private string _activeThumbnailPath;

    /// <summary>
    /// Authenticates with Google and initiates the video upload process using details from Inspector.
    /// This method should be called to start the upload, e.g., from a UI button or editor script.
    /// </summary>
    public async void UploadVideo()
    {
        // Use existing public fields to call the detailed upload method
        string[] tagsArray = string.IsNullOrEmpty(videoTags) ? new string[0] : videoTags.Split(',');
        await UploadVideoWithDetails(videoPathToUpload, videoTitle, videoDescription, tagsArray, videoCategoryId, videoPrivacyStatus, thumbnailPathToUpload);
    }

    /// <summary>
    /// Authenticates and uploads a video with specified details and an optional thumbnail.
    /// </summary>
    /// <param name="videoPath">Full path to the video file.</param>
    /// <param name="title">Video title.</param>
    /// <param name="description">Video description.</param>
    /// <param name="tags">Array of video tags.</param>
    /// <param name="categoryId">YouTube category ID.</param>
    /// <param name="privacy">Video privacy status.</param>
    /// <param name="thumbnailPath">Full path to the thumbnail image. Optional, can be null or empty.</param>
    /// <returns>The ID of the uploaded video if successful, otherwise null.</returns>
    public async Task<string> UploadVideoWithDetails(string videoPath, string title, string description, string[] tags, string categoryId, PrivacyStatus privacy, string thumbnailPath)
    {
        if (_isUploading)
        {
            Debug.LogWarning("Upload already in progress.");
            currentUploadStatus = "Upload already in progress.";
            return null;
        }

        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError("Video path to upload is not set.");
            currentUploadStatus = "Error: Video path not set.";
            _isUploading = false;
            return null;
        }

        if (!File.Exists(videoPath))
        {
            Debug.LogError($"Video file not found at path: {videoPath}");
            currentUploadStatus = "Error: Video file not found.";
            _isUploading = false;
            return null;
        }
        _isUploading = true;
        currentUploadStatus = "Starting...";
        currentUploadProgress = 0f;
        _currentBytesUploaded = 0;
        _uploadedVideoId = null;
        _activeVideoPath = videoPath; // Store for progress callbacks
        _activeThumbnailPath = thumbnailPath; // Store for progress callbacks

        try
        {
            _totalBytesToUpload = new FileInfo(videoPath).Length;
            if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
            {
                _totalBytesToUpload += new FileInfo(thumbnailPath).Length; // Add thumbnail size for more accurate overall progress
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting file info: {ex.Message}");
            currentUploadStatus = "Error: Could not read file info.";
            _isUploading = false;
            return null;
        }

        try
        {
            await AuthenticateAsync();
            if (_credential != null)
            {
                _youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
                });

                await UploadVideoFileAsync(videoPath, title, description, tags, categoryId, privacy);

                if (!string.IsNullOrEmpty(_uploadedVideoId) && !string.IsNullOrEmpty(thumbnailPath))
                {
                    await UploadThumbnailAsync(_uploadedVideoId, thumbnailPath);
                }
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
        finally
        {
            if (currentUploadStatus.StartsWith("Uploading") || currentUploadStatus.StartsWith("Processing") || currentUploadStatus.StartsWith("Thumbnail"))
            {
                // If it's stuck in an uploading state but an error occurred or didn't complete.
                if (!_isUploading) // Check if it wasn't reset by a specific failure
                {
                    // Do nothing, already handled
                }
                else if (!currentUploadStatus.Contains("Complete"))
                {
                     Debug.LogWarning("Upload process ended without explicit completion or failure state. Setting to Idle.");
                     currentUploadStatus = "Upload ended or failed implicitly.";
                }
            }
             _isUploading = false; // Ensure this is always reset
        }
        return _uploadedVideoId; // Return the video ID (will be null if upload failed before ID was obtained)
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
            // currentUploadStatus = "Authentication successful."; // This will be overwritten by token refresh or ready status
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
                    currentUploadStatus = "Token refreshed. Ready for operations.";
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
                currentUploadStatus = "Authenticated. Ready for operations.";
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
    private async Task UploadVideoFileAsync(string videoPath, string title, string description, string[] tags, string categoryId, PrivacyStatus privacy)
    {
        if (_credential == null || _youtubeService == null)
        {
            Debug.LogError("Not authenticated or YouTube service not initialized. Please authenticate first.");
            currentUploadStatus = "Error: Not authenticated/service not ready.";
            // _isUploading = false; // Handled by caller
            return;
        }

        Debug.Log($"Starting video upload for: {videoPath}");
        currentUploadStatus = "Preparing video for upload...";
        _currentBytesUploaded = 0; // Reset for video part

        var video = new Video();
        video.Snippet = new VideoSnippet
        {
            Title = title,
            Description = description,
            Tags = tags,
            CategoryId = categoryId
        };
        video.Status = new VideoStatus
        {
            PrivacyStatus = privacy.ToString().ToLowerInvariant()
        };

        using (var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read))
        {
            var videosInsertRequest = _youtubeService.Videos.Insert(video, "snippet,status",
                fileStream, "video/*");
            videosInsertRequest.ChunkSize = Google.Apis.YouTube.v3.VideosResource.InsertMediaUpload.DefaultChunkSize;
            videosInsertRequest.ProgressChanged += OnVideoUploadProgress;
            videosInsertRequest.ResponseReceived += OnVideoUploadResponseReceived;

            Debug.Log("Beginning resumable video upload...");
            currentUploadStatus = "Uploading Video (0%)...";
            await videosInsertRequest.UploadAsync(CancellationToken.None);
            // _isUploading will be set to false by OnVideoUploadResponseReceived or if it fails by caller
        }
    }

    /// <summary>
    /// Handles progress updates for the video upload.
    /// </summary>
    private void OnVideoUploadProgress(Google.Apis.Upload.IUploadProgress progress)
    {
        long videoFileLength = 0;
        try {
            // Use the stored _activeVideoPath for the current operation
             if (string.IsNullOrEmpty(_uploadedVideoId) && !string.IsNullOrEmpty(_activeVideoPath) && File.Exists(_activeVideoPath)) {
                 videoFileLength = new FileInfo(_activeVideoPath).Length;
             } else if (!string.IsNullOrEmpty(_activeVideoPath) && File.Exists(_activeVideoPath)) { // fallback if videoId is somehow set
                  videoFileLength = new FileInfo(_activeVideoPath).Length;
             }
        } catch (System.Exception ex) {
            Debug.LogWarning($"Could not get video file length for progress: {ex.Message}");
        }


        switch (progress.Status)
        {
            case UploadStatus.Starting:
                currentUploadStatus = "Video Upload starting...";
                currentUploadProgress = _totalBytesToUpload > 0 ? (float)_currentBytesUploaded / _totalBytesToUpload : 0f;
                Debug.Log("Video Upload starting...");
                break;
            case UploadStatus.Uploading:
                _currentBytesUploaded = progress.BytesSent; // Update based on video progress only for this part
                if (_totalBytesToUpload > 0)
                {
                    currentUploadProgress = (float)_currentBytesUploaded / _totalBytesToUpload;
                } else if (videoFileLength > 0) { // If _totalBytesToUpload isn't good, use videoFileLength
                     currentUploadProgress = (float)progress.BytesSent / videoFileLength;
                }
                else
                {
                    currentUploadProgress = 0f; 
                }
                currentUploadStatus = $"Uploading Video ({currentUploadProgress * 100:F1}%): {progress.BytesSent / (1024*1024):F2}MB sent";
                Debug.Log($"Video Uploading: {progress.BytesSent} bytes sent ({currentUploadProgress * 100:F1}%).");
                break;
            case UploadStatus.Completed:
                _currentBytesUploaded = videoFileLength > 0 ? videoFileLength : progress.BytesSent; // Max out video portion
                currentUploadProgress = _totalBytesToUpload > 0 ? (float)_currentBytesUploaded / _totalBytesToUpload : 1f;
                currentUploadStatus = "Video Uploaded. Waiting for server processing...";
                Debug.Log("Video upload stream completed. Waiting for YouTube processing and response...");
                break;
            case UploadStatus.Failed:
                Debug.LogError($"Video upload failed: {progress.Exception?.Message}");
                currentUploadStatus = $"Video upload failed: {progress.Exception?.Message}";
                // currentUploadProgress is not reset here, shows failure point.
                // _isUploading = false; // Handled by caller's finally block
                _uploadedVideoId = null; // Ensure no thumbnail upload is attempted
                break;
            case UploadStatus.NotStarted:
                currentUploadStatus = "Video Upload not started.";
                Debug.Log("Video Upload not started yet.");
                break;
        }
    }

    /// <summary>
    /// Handles the response received after the video upload is completed.
    /// </summary>
    private void OnVideoUploadResponseReceived(Video video)
    {
        _uploadedVideoId = video.Id;
        Debug.Log($"Video upload completed. Video ID: {_uploadedVideoId}");
        // Don't set to "Upload Complete!" yet if thumbnail is pending
        // currentUploadStatus will be updated by thumbnail logic or by caller if no thumbnail
        // currentUploadProgress will be updated by thumbnail logic or by caller
    }

    /// <summary>
    /// Uploads a thumbnail for the specified video ID.
    /// </summary>
    private async Task UploadThumbnailAsync(string videoId, string thumbnailPath)
    {
        if (string.IsNullOrEmpty(thumbnailPath) || !File.Exists(thumbnailPath))
        {
            Debug.LogWarning("Thumbnail path is invalid or file does not exist. Skipping thumbnail upload.");
            // If video upload was the last step, finalize status.
            if (!string.IsNullOrEmpty(_uploadedVideoId)) {
                 currentUploadStatus = $"Video Upload Complete! ID: {_uploadedVideoId}. No thumbnail or skipped.";
                 currentUploadProgress = 1f; // Mark as fully complete.
            }
            return;
        }

        if (_credential == null || _youtubeService == null)
        {
            Debug.LogError("Not authenticated or YouTube service not initialized for thumbnail upload.");
            currentUploadStatus = "Error: Not authenticated for thumbnail.";
            return;
        }
        
        Debug.Log($"Starting thumbnail upload for Video ID: {videoId}, Path: {thumbnailPath}");
        currentUploadStatus = "Preparing thumbnail...";

        try
        {
            using (var thumbnailStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read))
            {
                var thumbnailsSetRequest = _youtubeService.Thumbnails.Set(videoId, thumbnailStream, "image/jpeg"); // Assuming JPEG, adjust if other formats are used
                thumbnailsSetRequest.ProgressChanged += OnThumbnailUploadProgress;
                thumbnailsSetRequest.ResponseReceived += OnThumbnailUploadResponseReceived;

                currentUploadStatus = "Uploading Thumbnail (0%)...";
                await thumbnailsSetRequest.UploadAsync(CancellationToken.None);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Thumbnail upload failed for video {videoId}: {ex.Message}");
            currentUploadStatus = $"Thumbnail upload failed: {ex.Message}";
            // _isUploading will be handled by the main calling method's finally block
            // If video was uploaded, it's partially successful.
            if (!string.IsNullOrEmpty(_uploadedVideoId)) {
                 currentUploadStatus = $"Video Uploaded (ID: {_uploadedVideoId}), but Thumbnail FAILED: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Handles progress updates for the thumbnail upload.
    /// </summary>
    private void OnThumbnailUploadProgress(Google.Apis.Upload.IUploadProgress progress)
    {
        long thumbnailFileLength = 0;
        long videoPortionLength = _currentBytesUploaded; // Bytes from video (should be relatively stable by now)

        try {
             // Use the stored _activeThumbnailPath for the current operation
             if (!string.IsNullOrEmpty(_activeThumbnailPath) && File.Exists(_activeThumbnailPath)) {
                  thumbnailFileLength = new FileInfo(_activeThumbnailPath).Length;
             }
        } catch (System.Exception ex) {
             Debug.LogWarning($"Could not get thumbnail file length for progress: {ex.Message}");
        }

        switch (progress.Status)
        {
            case UploadStatus.Starting:
                currentUploadStatus = "Thumbnail Upload starting...";
                // Progress should reflect video + start of thumbnail
                currentUploadProgress = _totalBytesToUpload > 0 ? (float)videoPortionLength / _totalBytesToUpload : 0.9f; // Estimate 90% if no total
                Debug.Log("Thumbnail Upload starting...");
                break;
            case UploadStatus.Uploading:
                long currentThumbnailBytes = progress.BytesSent;
                if (_totalBytesToUpload > 0)
                {
                    currentUploadProgress = (float)(videoPortionLength + currentThumbnailBytes) / _totalBytesToUpload;
                }
                else if (thumbnailFileLength > 0) // Use individual thumbnail progress if total isn't good
                {
                     currentUploadProgress = (float)currentThumbnailBytes / thumbnailFileLength; // This shows only thumbnail part
                     currentUploadStatus = $"Uploading Thumbnail ({currentUploadProgress * 100:F1}%)";
                } else {
                    currentUploadProgress = 0.95f; // Estimate
                }
                currentUploadStatus = $"Uploading Thumbnail ({currentUploadProgress * 100:F1}%): {progress.BytesSent / (1024.0):F2}KB sent";
                Debug.Log($"Thumbnail Uploading: {progress.BytesSent} bytes sent ({currentUploadProgress * 100:F1}%).");
                break;
            case UploadStatus.Completed:
                // ResponseReceived will handle the final status message.
                // currentUploadStatus = "Thumbnail Uploaded. Processing...";
                // currentUploadProgress should be near 1.0f now if _totalBytesToUpload was accurate.
                Debug.Log("Thumbnail upload stream completed.");
                break;
            case UploadStatus.Failed:
                Debug.LogError($"Thumbnail upload failed: {progress.Exception?.Message}");
                currentUploadStatus = $"Thumbnail upload failed: {progress.Exception?.Message}";
                // _isUploading = false; // Handled by caller's finally block
                // If video was uploaded, it's partially successful.
                 if (!string.IsNullOrEmpty(_uploadedVideoId)) {
                    currentUploadStatus = $"Video Uploaded (ID: {_uploadedVideoId}), but Thumbnail FAILED: {progress.Exception?.Message}";
                }
                break;
            case UploadStatus.NotStarted:
                currentUploadStatus = "Thumbnail Upload not started.";
                Debug.Log("Thumbnail Upload not started yet.");
                break;
        }
    }

    /// <summary>
    /// Handles the response received after the thumbnail upload is completed.
    /// </summary>
    private void OnThumbnailUploadResponseReceived(ThumbnailSetResponse response) // Correct type is ThumbnailSetResponse
    {
        // ThumbnailSetResponse doesn't directly confirm the URL of the thumbnail in the same way as Video.
        // Success is implied if no exception during upload and this callback is hit.
        Debug.Log($"Thumbnail upload successful for Video ID: {_uploadedVideoId}.");
        currentUploadStatus = $"Upload Complete! Video ID: {_uploadedVideoId}. Thumbnail set.";
        currentUploadProgress = 1f;
        // _isUploading = false; // Handled by caller's finally block
    }

    /// <summary>
    /// Public accessor for the editor script to know if an upload is active.
    /// </summary>
    public bool IsUploading => _isUploading;

    /// <summary>
    /// Gets an existing playlist by title or creates it if not found.
    /// </summary>
    /// <param name="desiredTitle">The title of the playlist to find or create.</param>
    /// <param name="description">Description for the playlist if created.</param>
    /// <param name="privacy">Privacy status for the playlist if created (e.g., "public", "private").</param>
    /// <param name="defaultLanguage">Optional ISO 639-1 language code for the playlist's default language (e.g., "en", "ko", "zh-CN").</param>
    /// <returns>The Playlist ID if found or created, null otherwise.</returns>
    public async Task<string> GetOrCreatePlaylistAsync(string desiredTitle, string description, string privacy, string defaultLanguage = null)
    {
        if (_youtubeService == null)
        {
            Debug.LogError("[YoutubeUploader] YouTube service not initialized. Cannot manage playlists. Ensure authentication succeeded.");
            currentUploadStatus = "Error: YT Service not ready for playlists";
            return null;
        }

        currentUploadStatus = $"Checking playlist: {desiredTitle}...";
        Debug.Log($"[YoutubeUploader] Checking playlist: {desiredTitle}");

        try
        {
            var playlistListRequest = _youtubeService.Playlists.List("snippet");
            playlistListRequest.Mine = true; // Only search playlists owned by the authenticated user
            var response = await playlistListRequest.ExecuteAsync();

            foreach (var playlist in response.Items)
            {
                if (playlist.Snippet.Title.Equals(desiredTitle, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[YoutubeUploader] Found existing playlist: '{desiredTitle}' (ID: {playlist.Id})");
                    currentUploadStatus = $"Found playlist: {desiredTitle}";
                    return playlist.Id;
                }
            }

            Debug.Log($"[YoutubeUploader] Playlist '{desiredTitle}' not found. Creating new playlist...");
            currentUploadStatus = $"Creating playlist: {desiredTitle}...";

            var newPlaylistData = new Playlist();
            newPlaylistData.Snippet = new PlaylistSnippet
            {
                Title = desiredTitle,
                Description = description
            };
            if (!string.IsNullOrEmpty(defaultLanguage))
            {
                newPlaylistData.Snippet.DefaultLanguage = defaultLanguage;
            }

            newPlaylistData.Status = new PlaylistStatus
            {
                PrivacyStatus = privacy // e.g., "public", "private", "unlisted"
            };

            var insertRequest = _youtubeService.Playlists.Insert(newPlaylistData, "snippet,status");
            Playlist createdPlaylist = await insertRequest.ExecuteAsync();
            
            if (createdPlaylist != null && !string.IsNullOrEmpty(createdPlaylist.Id))
            {
                Debug.Log($"[YoutubeUploader] Successfully created playlist '{createdPlaylist.Snippet.Title}' (ID: {createdPlaylist.Id})");
                currentUploadStatus = $"Created playlist: {createdPlaylist.Snippet.Title}";
                return createdPlaylist.Id;
            }
            else
            {
                Debug.LogError($"[YoutubeUploader] Failed to create playlist '{desiredTitle}'. Response was null or ID empty.");
                currentUploadStatus = $"Error creating playlist '{desiredTitle}'.";
                return null;
            }
        }
        catch (Google.GoogleApiException ex)
        {
            Debug.LogError($"[YoutubeUploader] Google API Error finding/creating playlist '{desiredTitle}': {ex.Error.Message} (Code: {ex.Error.Code})");
            currentUploadStatus = $"API Error with playlist '{desiredTitle}'.";
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[YoutubeUploader] System Error finding or creating playlist '{desiredTitle}': {ex.Message}");
            currentUploadStatus = $"Error with playlist '{desiredTitle}'.";
            return null;
        }
    }

    /// <summary>
    /// Adds a video to a specified YouTube playlist.
    /// </summary>
    /// <param name="videoId">The ID of the video to add.</param>
    /// <param name="playlistId">The ID of the playlist.</param>
    public async Task AddVideoToPlaylistAsync(string videoId, string playlistId)
    {
        if (_youtubeService == null)
        {
            Debug.LogError("[YoutubeUploader] YouTube service not initialized. Cannot add video to playlist.");
            currentUploadStatus = "Error: YT Service not ready to add to playlist";
            return;
        }
        if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(playlistId))
        {
            Debug.LogError("[YoutubeUploader] Video ID or Playlist ID is null/empty. Cannot add to playlist.");
            return;
        }

        currentUploadStatus = $"Adding video {videoId} to playlist {playlistId}...";
        Debug.Log($"[YoutubeUploader] Adding video {videoId} to playlist {playlistId}...");

        try
        {
            var newPlaylistItem = new PlaylistItem();
            newPlaylistItem.Snippet = new PlaylistItemSnippet
            {
                PlaylistId = playlistId,
                ResourceId = new ResourceId
                {
                    Kind = "youtube#video",
                    VideoId = videoId
                }
                // To add to the beginning of the playlist, set Position:
                // Position = 0L 
            };

            var insertRequest = _youtubeService.PlaylistItems.Insert(newPlaylistItem, "snippet");
            await insertRequest.ExecuteAsync();

            Debug.Log($"[YoutubeUploader] Successfully added video {videoId} to playlist {playlistId}.");
            currentUploadStatus = $"Added video to playlist {playlistId}.";
        }
        catch (Google.GoogleApiException ex)
        {
             Debug.LogError($"[YoutubeUploader] Google API Error adding video {videoId} to playlist {playlistId}: {ex.Error.Message} (Code: {ex.Error.Code})");
             currentUploadStatus = $"API Error adding video to playlist.";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[YoutubeUploader] Error adding video {videoId} to playlist {playlistId}: {ex.Message}");
            currentUploadStatus = $"Error adding video to playlist: {ex.Message}";
        }
    }
} 