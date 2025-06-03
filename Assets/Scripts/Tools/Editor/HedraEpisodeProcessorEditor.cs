#nullable enable
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO; // Required for Path operations

[CustomEditor(typeof(HedraEpisodeProcessor))]
public class HedraEpisodeProcessorEditor : Editor
{
    private SerializedProperty? sourceEpisodeJsonPathProp;
    private SerializedProperty? explicitManifestPathProp;
    private SerializedProperty? outputSubfolderNameProp;
    private SerializedProperty? defaultAspectRatioProp;
    private SerializedProperty? defaultResolutionProp;
    private SerializedProperty? screenshotExtensionProp;
    private SerializedProperty? audioExtensionProp;
    private SerializedProperty? defaultVoiceIdProp;
    private SerializedProperty? apiKeysSOProp;
    private SerializedProperty? segmentIndexToProcessProp;
    private SerializedProperty? loadedManifestContentProp;
    private SerializedProperty? pollingIntervalSecondsProp;
    private SerializedProperty? maxPollingAttemptsProp;
    private SerializedProperty? defaultNewApiAiModelIdProp;
    private SerializedProperty? selectedApiModeProp;
    private SerializedProperty? processBatchStrictlySequentiallyProp;
    private SerializedProperty? delayBetweenConcurrentSubmissionsProp;
    private SerializedProperty? delayBetweenStrictSequentialSubmissionsProp;

    private bool showInputOutputSettings = true;
    private bool showHedraDefaults = true;
    private bool showSegmentContentDefaults = true;
    private bool showProcessingControls = true;
    private bool showManifestManagement = true;
    private bool showPollingConfig = true;
    private bool showApiConfig = true;
    private bool showBatchOptions = true;

    private void OnEnable()
    {
        sourceEpisodeJsonPathProp = serializedObject.FindProperty("sourceEpisodeJsonPath");
        explicitManifestPathProp = serializedObject.FindProperty("explicitManifestPath");
        outputSubfolderNameProp = serializedObject.FindProperty("outputSubfolderName");
        defaultAspectRatioProp = serializedObject.FindProperty("defaultAspectRatio");
        defaultResolutionProp = serializedObject.FindProperty("defaultResolution");
        screenshotExtensionProp = serializedObject.FindProperty("screenshotExtension");
        audioExtensionProp = serializedObject.FindProperty("audioExtension");
        defaultVoiceIdProp = serializedObject.FindProperty("defaultVoiceId");
        apiKeysSOProp = serializedObject.FindProperty("apiKeysSO");
        segmentIndexToProcessProp = serializedObject.FindProperty("segmentIndexToProcess");
        loadedManifestContentProp = serializedObject.FindProperty("loadedManifestContent");
        pollingIntervalSecondsProp = serializedObject.FindProperty("pollingIntervalSeconds");
        maxPollingAttemptsProp = serializedObject.FindProperty("maxPollingAttempts");
        defaultNewApiAiModelIdProp = serializedObject.FindProperty("defaultNewApiAiModelId");
        selectedApiModeProp = serializedObject.FindProperty("selectedApiMode");
        processBatchStrictlySequentiallyProp = serializedObject.FindProperty("processBatchStrictlySequentially");
        delayBetweenConcurrentSubmissionsProp = serializedObject.FindProperty("delayBetweenConcurrentSubmissions");
        delayBetweenStrictSequentialSubmissionsProp = serializedObject.FindProperty("delayBetweenStrictSequentialSubmissions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        HedraEpisodeProcessor processor = (HedraEpisodeProcessor)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hedra Episode Processor Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // API Configuration Section
        showApiConfig = EditorGUILayout.Foldout(showApiConfig, "API Configuration", true, EditorStyles.foldoutHeader);
        if (showApiConfig)
        {
            if (apiKeysSOProp != null) EditorGUILayout.PropertyField(apiKeysSOProp, new GUIContent("API Keys SO"));
            if (selectedApiModeProp != null) EditorGUILayout.PropertyField(selectedApiModeProp, new GUIContent("Selected API Mode"));
            EditorGUILayout.HelpBox("Ensure the Base URL in API Keys SO is correctly set for the selected API Mode.", MessageType.Info);
        }
        EditorGUILayout.Space();

        // Input and Output Configuration Section
        showInputOutputSettings = EditorGUILayout.Foldout(showInputOutputSettings, "Input & Output Settings", true, EditorStyles.foldoutHeader);
        if (showInputOutputSettings)
        {
            EditorGUILayout.LabelField("Source Episode JSON", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (sourceEpisodeJsonPathProp != null) EditorGUILayout.PropertyField(sourceEpisodeJsonPathProp, new GUIContent("Source JSON Path"));
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string initialPath = "Assets";
                if (sourceEpisodeJsonPathProp != null && !string.IsNullOrEmpty(sourceEpisodeJsonPathProp.stringValue) && File.Exists(sourceEpisodeJsonPathProp.stringValue))
                {
                    initialPath = Path.GetDirectoryName(sourceEpisodeJsonPathProp.stringValue) ?? "Assets";
                }
                string path = EditorUtility.OpenFilePanel("Select Source Episode JSON", initialPath, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    if (sourceEpisodeJsonPathProp != null)
                    {
                        if (path.StartsWith(Application.dataPath, System.StringComparison.OrdinalIgnoreCase))
                        {
                            sourceEpisodeJsonPathProp.stringValue = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            sourceEpisodeJsonPathProp.stringValue = path;
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
            if (outputSubfolderNameProp != null) EditorGUILayout.PropertyField(outputSubfolderNameProp, new GUIContent("Output Subfolder Name"));
        }
        EditorGUILayout.Space();

        // Manifest Management Section
        showManifestManagement = EditorGUILayout.Foldout(showManifestManagement, "Manifest Management", true, EditorStyles.foldoutHeader);
        if (showManifestManagement)
        {
            EditorGUILayout.LabelField("Explicit Manifest Path (for Processing)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (explicitManifestPathProp != null) EditorGUILayout.PropertyField(explicitManifestPathProp, new GUIContent("Hedra Manifest Path"));
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string initialManifestPath = "Assets";
                if (explicitManifestPathProp != null && !string.IsNullOrEmpty(explicitManifestPathProp.stringValue) && File.Exists(explicitManifestPathProp.stringValue))
                {
                    initialManifestPath = Path.GetDirectoryName(explicitManifestPathProp.stringValue) ?? "Assets";
                }
                else if (sourceEpisodeJsonPathProp != null && !string.IsNullOrEmpty(sourceEpisodeJsonPathProp.stringValue) && 
                         outputSubfolderNameProp != null && File.Exists(sourceEpisodeJsonPathProp.stringValue))
                {
                    string? sourceDir = Path.GetDirectoryName(sourceEpisodeJsonPathProp.stringValue);
                    if (!string.IsNullOrEmpty(sourceDir))
                    {
                        initialManifestPath = Path.Combine(sourceDir, outputSubfolderNameProp.stringValue);
                        if (!Directory.Exists(initialManifestPath)) initialManifestPath = sourceDir;
                    }
                }
                string manifestPath = EditorUtility.OpenFilePanel("Select Hedra Manifest JSON", initialManifestPath, "json");
                if (!string.IsNullOrEmpty(manifestPath))
                {
                    if (explicitManifestPathProp != null)
                    {
                        if (manifestPath.StartsWith(Application.dataPath, System.StringComparison.OrdinalIgnoreCase))
                        {
                            explicitManifestPathProp.stringValue = "Assets" + manifestPath.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            explicitManifestPathProp.stringValue = manifestPath;
                        }
                        processor.LoadAndDisplayManifestContent();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Hedra Manifest from Source JSON"))
            {
                processor.GenerateHedraManifest();
            }
            if (GUILayout.Button("Reload/View Explicit Manifest Content"))
            {
                processor.LoadAndDisplayManifestContent();
            }
            if (loadedManifestContentProp != null) EditorGUILayout.PropertyField(loadedManifestContentProp, new GUIContent("Loaded Manifest Content (Read-Only)"));
            EditorGUILayout.HelpBox("The manifest is used by both API modes. Generate it first, then select the API mode for processing.", MessageType.Info);
        }
        EditorGUILayout.Space();

        // Processing Controls Section
        showProcessingControls = EditorGUILayout.Foldout(showProcessingControls, "Processing Controls", true, EditorStyles.foldoutHeader);
        if (showProcessingControls)
        {
            if (processor.IsProcessingBatch())
            {
                GUI.color = Color.red;
                if (GUILayout.Button("Stop Batch Processing"))
                {
                    processor.StopBatchProcessing();
                }
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Processing is currently active. Stop to enable other actions.", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Start Batch Processing Selected Manifest"))
                {
                    processor.StartBatchProcessing();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Single Segment Processing", EditorStyles.boldLabel);
                if (segmentIndexToProcessProp != null) EditorGUILayout.PropertyField(segmentIndexToProcessProp, new GUIContent("Segment Index to Process"));
                if (GUILayout.Button("Process Selected Segment from Manifest"))
                {
                    processor.ProcessSingleSegmentWrapper();
                }
            }
        }
        EditorGUILayout.Space();

        // Batch Processing Options Section
        showBatchOptions = EditorGUILayout.Foldout(showBatchOptions, "Batch Processing Options", true, EditorStyles.foldoutHeader);
        if (showBatchOptions)
        {
            if (processBatchStrictlySequentiallyProp != null) EditorGUILayout.PropertyField(processBatchStrictlySequentiallyProp, new GUIContent("Strictly Sequential Batch"));
            if (delayBetweenConcurrentSubmissionsProp != null) EditorGUILayout.PropertyField(delayBetweenConcurrentSubmissionsProp, new GUIContent("Delay Concurrent Jobs (s)"));
            if (delayBetweenStrictSequentialSubmissionsProp != null) EditorGUILayout.PropertyField(delayBetweenStrictSequentialSubmissionsProp, new GUIContent("Delay Strict Sequential (s)"));
        }
        EditorGUILayout.Space();

        // Hedra Defaults Section
        showHedraDefaults = EditorGUILayout.Foldout(showHedraDefaults, "Hedra Output Defaults (for Manifest Generation)", true, EditorStyles.foldoutHeader);
        if (showHedraDefaults)
        {
            if (defaultAspectRatioProp != null) EditorGUILayout.PropertyField(defaultAspectRatioProp, new GUIContent("Default Aspect Ratio"));
            if (defaultResolutionProp != null) EditorGUILayout.PropertyField(defaultResolutionProp, new GUIContent("Default Resolution"));
            if (screenshotExtensionProp != null) EditorGUILayout.PropertyField(screenshotExtensionProp, new GUIContent("Screenshot Extension"));
            if (audioExtensionProp != null) EditorGUILayout.PropertyField(audioExtensionProp, new GUIContent("Audio Extension"));
            if (defaultNewApiAiModelIdProp != null) EditorGUILayout.PropertyField(defaultNewApiAiModelIdProp, new GUIContent("Default AI Model ID (New API)"));
        }
        EditorGUILayout.Space();

        // Segment Content Defaults Section
        showSegmentContentDefaults = EditorGUILayout.Foldout(showSegmentContentDefaults, "Segment Content Defaults (Optional)", true, EditorStyles.foldoutHeader);
        if (showSegmentContentDefaults)
        {
            if (defaultVoiceIdProp != null) EditorGUILayout.PropertyField(defaultVoiceIdProp, new GUIContent("Default Voice ID (Legacy API)"));
        }
        EditorGUILayout.Space();

        // Polling Configuration Section
        showPollingConfig = EditorGUILayout.Foldout(showPollingConfig, "Polling Configuration", true, EditorStyles.foldoutHeader);
        if (showPollingConfig)
        {
            if (pollingIntervalSecondsProp != null) EditorGUILayout.PropertyField(pollingIntervalSecondsProp, new GUIContent("Polling Interval (seconds)"));
            if (maxPollingAttemptsProp != null) EditorGUILayout.PropertyField(maxPollingAttemptsProp, new GUIContent("Max Polling Attempts"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif 
