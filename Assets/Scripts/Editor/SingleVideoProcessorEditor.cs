#nullable enable
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(SingleVideoProcessor))]
public class SingleVideoProcessorEditor : Editor
{
    private SerializedProperty apiKeysSOProp;
    private SerializedProperty selectedApiModeProp;
    private SerializedProperty audioFilePathProp;
    private SerializedProperty imageFilePathProp;
    private SerializedProperty promptTextProp;
    private SerializedProperty outputFolderNameProp;
    private SerializedProperty outputFileNameProp;
    private SerializedProperty aspectRatioProp;
    private SerializedProperty resolutionProp;
    private SerializedProperty aiModelIdProp;
    private SerializedProperty pollingIntervalSecondsProp;
    private SerializedProperty maxPollingAttemptsProp;

    private bool showApiSettings = true;
    private bool showInputSettings = true;
    private bool showOutputSettings = true;
    private bool showVideoSettings = true;
    private bool showPollingSettings = true;

    private void OnEnable()
    {
        apiKeysSOProp = serializedObject.FindProperty("apiKeysSO");
        selectedApiModeProp = serializedObject.FindProperty("selectedApiMode");
        audioFilePathProp = serializedObject.FindProperty("audioFilePath");
        imageFilePathProp = serializedObject.FindProperty("imageFilePath");
        promptTextProp = serializedObject.FindProperty("promptText");
        outputFolderNameProp = serializedObject.FindProperty("outputFolderName");
        outputFileNameProp = serializedObject.FindProperty("outputFileName");
        aspectRatioProp = serializedObject.FindProperty("aspectRatio");
        resolutionProp = serializedObject.FindProperty("resolution");
        aiModelIdProp = serializedObject.FindProperty("aiModelId");
        pollingIntervalSecondsProp = serializedObject.FindProperty("pollingIntervalSeconds");
        maxPollingAttemptsProp = serializedObject.FindProperty("maxPollingAttempts");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SingleVideoProcessor processor = (SingleVideoProcessor)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Single Video Processor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // API Settings
        showApiSettings = EditorGUILayout.Foldout(showApiSettings, "API Settings", true);
        if (showApiSettings)
        {
            EditorGUILayout.PropertyField(apiKeysSOProp);
            EditorGUILayout.PropertyField(selectedApiModeProp);
        }

        // Input Settings
        showInputSettings = EditorGUILayout.Foldout(showInputSettings, "Input Settings", true);
        if (showInputSettings)
        {
            // Audio File
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(audioFilePathProp);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Audio File", "", "wav,mp3,ogg");
                if (!string.IsNullOrEmpty(path))
                {
                    audioFilePathProp.stringValue = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Image File
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(imageFilePathProp);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Image File", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    imageFilePathProp.stringValue = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Prompt Text
            EditorGUILayout.PropertyField(promptTextProp, GUILayout.Height(60));
        }

        // Output Settings
        showOutputSettings = EditorGUILayout.Foldout(showOutputSettings, "Output Settings", true);
        if (showOutputSettings)
        {
            EditorGUILayout.PropertyField(outputFolderNameProp);
            EditorGUILayout.PropertyField(outputFileNameProp);
        }

        // Video Settings
        showVideoSettings = EditorGUILayout.Foldout(showVideoSettings, "Video Settings", true);
        if (showVideoSettings)
        {
            EditorGUILayout.PropertyField(aspectRatioProp);
            EditorGUILayout.PropertyField(resolutionProp);
            EditorGUILayout.PropertyField(aiModelIdProp);
        }

        // Polling Settings
        showPollingSettings = EditorGUILayout.Foldout(showPollingSettings, "Polling Settings", true);
        if (showPollingSettings)
        {
            EditorGUILayout.PropertyField(pollingIntervalSecondsProp);
            EditorGUILayout.PropertyField(maxPollingAttemptsProp);
        }

        EditorGUILayout.Space();

        // Process Button
        GUI.enabled = !string.IsNullOrEmpty(audioFilePathProp.stringValue) && 
                     !string.IsNullOrEmpty(imageFilePathProp.stringValue) &&
                     !string.IsNullOrEmpty(promptTextProp.stringValue);

        if (GUILayout.Button("Process Video"))
        {
            processor.StartProcessing();
        }

        GUI.enabled = true;

        if (GUILayout.Button("Stop Processing"))
        {
            processor.StopProcessing();
        }

        serializedObject.ApplyModifiedProperties();
    }
} 