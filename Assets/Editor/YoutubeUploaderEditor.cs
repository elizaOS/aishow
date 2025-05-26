using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(YoutubeUploader))]
public class YoutubeUploaderEditor : Editor
{
    private YoutubeUploader _uploader;
    private SerializedProperty _clientSecretsPathProp;
    private SerializedProperty _videoPathToUploadProp;
    private SerializedProperty _videoTitleProp;
    private SerializedProperty _videoDescriptionProp;
    private SerializedProperty _videoTagsProp;
    private SerializedProperty _videoCategoryIdProp;
    private SerializedProperty _videoPrivacyStatusProp;

    // Cache GUIContent for performance and consistency
    private static readonly GUIContent _videoFilePathLabel = new GUIContent("Video File Path");

    private void OnEnable()
    {
        _uploader = (YoutubeUploader)target;
        _clientSecretsPathProp = serializedObject.FindProperty("clientSecretsPath");
        _videoPathToUploadProp = serializedObject.FindProperty("videoPathToUpload");
        _videoTitleProp = serializedObject.FindProperty("videoTitle");
        _videoDescriptionProp = serializedObject.FindProperty("videoDescription");
        _videoTagsProp = serializedObject.FindProperty("videoTags");
        _videoCategoryIdProp = serializedObject.FindProperty("videoCategoryId");
        _videoPrivacyStatusProp = serializedObject.FindProperty("videoPrivacyStatus");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("YouTube API Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_clientSecretsPathProp);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Video Details", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        // Use EditorGUILayout.TextField instead of PropertyField for the path
        string currentVideoPath = _videoPathToUploadProp.stringValue;
        string newVideoPath = EditorGUILayout.TextField(_videoFilePathLabel, currentVideoPath);
        if (newVideoPath != currentVideoPath)
        {
            _videoPathToUploadProp.stringValue = newVideoPath;
        }

        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string directoryToOpen = Application.dataPath;
            if (!string.IsNullOrEmpty(currentVideoPath))
            {
                try
                {
                    string directoryOfVideoFile = Path.GetDirectoryName(currentVideoPath);
                    if (!string.IsNullOrEmpty(directoryOfVideoFile) && Directory.Exists(directoryOfVideoFile))
                    {
                        directoryToOpen = directoryOfVideoFile;
                    }
                }
                catch (System.ArgumentException ex) 
                {
                    Debug.LogWarning($"Could not parse directory from video path '{currentVideoPath}': {ex.Message}. Defaulting browse dialog.");
                }
                catch (System.Exception ex) // Catch any other unexpected path errors
                {
                     Debug.LogError($"Unexpected error getting directory from video path '{currentVideoPath}': {ex.Message}. Defaulting browse dialog.");
                }
            }
            
            string selectedPath = EditorUtility.OpenFilePanel("Select Video File", directoryToOpen, "mp4,mov,avi,flv,wmv");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _videoPathToUploadProp.stringValue = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(_videoTitleProp);
        EditorGUILayout.PropertyField(_videoDescriptionProp);
        EditorGUILayout.PropertyField(_videoTagsProp);
        EditorGUILayout.PropertyField(_videoCategoryIdProp);
        EditorGUILayout.PropertyField(_videoPrivacyStatusProp);

        EditorGUILayout.Space(20); // Increased space

        GUI.enabled = !_uploader.IsUploading;
        if (GUILayout.Button("Upload Video to YouTube", GUILayout.Height(30)))
        {
            if (!_uploader.IsUploading) 
            {
                _uploader.currentUploadStatus = "Initiating upload...";
                _uploader.currentUploadProgress = 0f;
                // No need to call Repaint() here; it will repaint naturally.
                _uploader.UploadVideo(); 
            }
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Progress Feedback
        bool isErrorStatus = _uploader.currentUploadStatus.ToLower().Contains("error") || _uploader.currentUploadStatus.ToLower().Contains("failed");
        bool isCompletedStatus = _uploader.currentUploadStatus.ToLower().Contains("complete");

        if (_uploader.IsUploading || (_uploader.currentUploadProgress > 0 && _uploader.currentUploadProgress < 1.0f && !isErrorStatus))
        {
            string progressBarText = $"Uploading ({_uploader.currentUploadProgress * 100:F1}%)";
            if (!string.IsNullOrEmpty(_uploader.currentUploadStatus) && _uploader.currentUploadStatus.Length < 40 && !isErrorStatus && !isCompletedStatus && _uploader.currentUploadProgress == 0)
            {
                // Show initial statuses like "Authenticating..." or "Starting..." if short
                progressBarText = _uploader.currentUploadStatus;
            }
            Rect r = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(20));
            EditorGUI.ProgressBar(r, _uploader.currentUploadProgress, progressBarText);
            
            // If a longer status message exists alongside uploading, display it too
            if (_uploader.currentUploadStatus.Length >= 40 && !isErrorStatus && !isCompletedStatus)
            {
                EditorGUILayout.HelpBox(_uploader.currentUploadStatus, MessageType.Info, true);
            }
        }
        
        // Display specific messages for idle, completed, or error states
        if (!string.IsNullOrEmpty(_uploader.currentUploadStatus) && _uploader.currentUploadStatus != "Idle")
        {
            if (isErrorStatus)
            {
                EditorGUILayout.HelpBox(_uploader.currentUploadStatus, MessageType.Error, true);
            }
            else if (isCompletedStatus && _uploader.currentUploadProgress >= 1.0f)
            {
                EditorGUILayout.HelpBox(_uploader.currentUploadStatus, MessageType.Info, true);
            }
            else if (!_uploader.IsUploading && !isCompletedStatus) // Other non-uploading, non-completed statuses
            {
                EditorGUILayout.HelpBox(_uploader.currentUploadStatus, MessageType.Info, true);
            }
        }

        if (_uploader.IsUploading)
        {
            Repaint(); 
        }

        serializedObject.ApplyModifiedProperties();
    }
} 