using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq; // For language dropdowns
using System.Text;

namespace ShowGenerator
{
    [CustomEditor(typeof(JsonTranslationService))]
    public class JsonTranslationServiceInspector : Editor
    {
        private JsonTranslationService _service;
        private SerializedProperty _apiKeysConfigProperty;
        private SerializedProperty _currentApiProviderProperty;
        private SerializedProperty _openRouterModelNameProperty;
        private SerializedProperty _prettyPrintOutputProperty;

        private string _jsonInputPath = "";
        private string _jsonOutputPath = "";
        private Vector2 _inputJsonScrollPosition;
        private Vector2 _outputJsonScrollPosition;
        private Vector2 _customInstructionsScrollPosition;

        // To match JsonTranslationService internal state for display
        private string _displayableTranslatedJson = "";
        private string _translationError = "";
        private bool _isProcessing = false;
        private bool _lastTranslationWasValidJson = false;

        private string _targetLanguage = "Korean"; // Default or load from service state if needed
        private int _selectedLanguageIndex = 0; // Default to Korean
        private static readonly string[] _supportedLanguages = { "Korean", "Chinese", "Spanish", "French", "German", "Japanese", "English" };
        private string _customInstructions = "";
        private string _currentSystemPromptPreview = ""; // For displaying the constructed prompt

        private void OnEnable()
        {
            _service = (JsonTranslationService)target;
            _apiKeysConfigProperty = serializedObject.FindProperty("apiKeysConfig");
            _currentApiProviderProperty = serializedObject.FindProperty("currentApiProvider");
            _openRouterModelNameProperty = serializedObject.FindProperty("openRouterModelName");
            _prettyPrintOutputProperty = serializedObject.FindProperty("prettyPrintOutput");

            // Initialize paths if they are empty or load from EditorPrefs if you want persistence
            if (string.IsNullOrEmpty(_jsonInputPath))
            {
                _jsonInputPath = Application.dataPath;
            }
            if (string.IsNullOrEmpty(_jsonOutputPath))
            {
                _jsonOutputPath = Application.dataPath;
            }
            _targetLanguage = _supportedLanguages[_selectedLanguageIndex]; // Ensure target language is set from index
            UpdateSystemPromptPreview(); // Initialize the prompt preview
        }

        private void UpdateSystemPromptPreview()
        {
            // This logic should mirror the prompt construction in JsonTranslationService.cs
            StringBuilder systemPromptBuilder = new StringBuilder();
            systemPromptBuilder.AppendLine($"You are an expert JSON translation assistant. Your task is to translate the textual content of the provided JSON structure from its original language into {_targetLanguage}.");
            systemPromptBuilder.AppendLine("Key requirements:");
            systemPromptBuilder.AppendLine("1. Preserve the exact JSON structure, including all keys, arrays, and nested objects.");
            systemPromptBuilder.AppendLine("2. Only translate the string values associated with keys. Do not translate keys themselves, booleans, numbers, or null values.");
            systemPromptBuilder.AppendLine("3. Be context-aware: If the JSON represents a script (e.g., with scenes, dialogue, actions), ensure translations maintain the original intent, tone, and character voices.");
            systemPromptBuilder.AppendLine("4. Handle technical terms, proper nouns, and culturally specific references appropriately for the target language, either by translating them or keeping them in the original language if that's standard practice.");
            if (!string.IsNullOrEmpty(_customInstructions))
            {
                systemPromptBuilder.AppendLine("5. Follow these additional user-provided instructions carefully:");
                systemPromptBuilder.AppendLine(_customInstructions);
                systemPromptBuilder.AppendLine("6. IMPORTANT: Your response MUST be ONLY the translated JSON object/string, starting with `{` and ending with `}` (or `[` and `]` if it's a JSON array). Do not include any explanatory text, markdown, or any other characters before or after the JSON string itself.");
            }
            else
            {
                systemPromptBuilder.AppendLine("5. IMPORTANT: Your response MUST be ONLY the translated JSON object/string, starting with `{` and ending with `}` (or `[` and `]` if it's a JSON array). Do not include any explanatory text, markdown, or any other characters before or after the JSON string itself.");
            }
            _currentSystemPromptPreview = systemPromptBuilder.ToString();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool needsRepaint = false;

            EditorGUILayout.LabelField("JSON Translation Service Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_apiKeysConfigProperty, new GUIContent("API Keys Config"));
            if (_service.apiKeysConfig == null)
            {
                EditorGUILayout.HelpBox("API Keys Config is not assigned. This is required for all operations.", MessageType.Error);
            }
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_currentApiProviderProperty, new GUIContent("API Provider"));
            JsonTranslationService.ApiProvider selectedProvider = (JsonTranslationService.ApiProvider)_currentApiProviderProperty.enumValueIndex;

            // Display API key status based on selected provider
            if (_service.apiKeysConfig != null)
            {
                switch (selectedProvider)
                {
                    case JsonTranslationService.ApiProvider.OpenRouter:
                        if (string.IsNullOrEmpty(_service.apiKeysConfig.openRouterApiKey))
                        {
                            EditorGUILayout.HelpBox("OpenRouter API Key is missing in API Keys Config.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("OpenRouter API Key found.", MessageType.Info);
                        }
                        EditorGUILayout.PropertyField(_openRouterModelNameProperty, new GUIContent("OpenRouter Model Name"));
                        break;
                    case JsonTranslationService.ApiProvider.AnthropicDirect:
                        if (string.IsNullOrEmpty(_service.apiKeysConfig.anthropicApiKey))
                        {
                            EditorGUILayout.HelpBox("Anthropic API Key is missing in API Keys Config.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Anthropic API Key found.", MessageType.Info);
                        }
                        EditorGUILayout.HelpBox($"Using Anthropic Model: {(!string.IsNullOrEmpty(_service.apiKeysConfig.claudeModelName) ? _service.apiKeysConfig.claudeModelName : "claude-3-opus-20240229")}", MessageType.Info);
                        break;
                    case JsonTranslationService.ApiProvider.AnthropicWrapper:
                        if (string.IsNullOrEmpty(_service.apiKeysConfig.claudeWrapperUrl))
                        {
                            EditorGUILayout.HelpBox("Anthropic Wrapper URL is missing in API Keys Config.", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"Anthropic Wrapper URL found", MessageType.Info);
                        }
                        EditorGUILayout.HelpBox($"Using Anthropic Model (via wrapper): {(!string.IsNullOrEmpty(_service.apiKeysConfig.claudeModelName) ? _service.apiKeysConfig.claudeModelName : "claude-3-opus-20240229")}", MessageType.Info);
                        break;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign API Keys Config to see provider-specific checks.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Input JSON", EditorStyles.boldLabel);
            // Input JSON Area
            EditorGUILayout.BeginHorizontal();
            _jsonInputPath = EditorGUILayout.TextField("Load JSON from Path", _jsonInputPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Load JSON File", Path.GetDirectoryName(_jsonInputPath), "json");
                if (!string.IsNullOrEmpty(path))
                {
                    _jsonInputPath = path;
                    try
                    {
                        _service.rawJsonInput = File.ReadAllText(_jsonInputPath);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error loading JSON input file: {ex.Message}");
                        _service.rawJsonInput = $"Error loading file: {ex.Message}";
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Or Paste JSON here:");
            _inputJsonScrollPosition = EditorGUILayout.BeginScrollView(_inputJsonScrollPosition, GUILayout.Height(150));
            _service.rawJsonInput = EditorGUILayout.TextArea(_service.rawJsonInput ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Translation Parameters", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _selectedLanguageIndex = EditorGUILayout.Popup("Target Language", _selectedLanguageIndex, _supportedLanguages);
            if (EditorGUI.EndChangeCheck())
            {
                _targetLanguage = _supportedLanguages[_selectedLanguageIndex];
                UpdateSystemPromptPreview();
                needsRepaint = true;
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Custom Instructions (Optional)");
            _customInstructionsScrollPosition = EditorGUILayout.BeginScrollView(_customInstructionsScrollPosition, GUILayout.Height(100));
            _customInstructions = EditorGUILayout.TextArea(_customInstructions ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSystemPromptPreview();
                needsRepaint = true;
            }

            EditorGUILayout.PropertyField(_prettyPrintOutputProperty, new GUIContent("Pretty-Print Output JSON"));

            // Display the constructed system prompt
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("System Prompt Preview (Read-Only)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_currentSystemPromptPreview, MessageType.None);

            EditorGUILayout.Space();
            if (GUILayout.Button("Translate JSON", GUILayout.Height(30)))
            {
                if (_service.apiKeysConfig == null)
                {
                    _translationError = "API Keys Config is not set. Cannot translate.";
                    Debug.LogError(_translationError);
                }
                else if (string.IsNullOrEmpty(_service.rawJsonInput))
                {
                    _translationError = "Input JSON is empty. Cannot translate.";
                    Debug.LogError(_translationError);
                }
                else
                {
                    _isProcessing = true;
                    _translationError = null;
                    _displayableTranslatedJson = "Processing...";
                    _service.CallTranslateJson(_service.rawJsonInput, _targetLanguage, _customInstructions, 
                        (rawOutput, error, isValidJson) =>
                        {
                            _isProcessing = false;
                            _translationError = error;
                            _lastTranslationWasValidJson = isValidJson;
                            if (isValidJson && rawOutput != null)
                            {
                                _displayableTranslatedJson = _service.displayableTranslatedJson; // Use service's processed version
                            }
                            else if (error != null)
                            {
                                _displayableTranslatedJson = $"Error: {error}\nRaw Output (if any):\n{_service.rawTranslatedJsonOutput}";
                            }
                            else
                            {
                                _displayableTranslatedJson = "Translation completed, but output was empty or invalid and no specific error reported.";
                            }
                            Repaint(); // Ensure UI updates after callback
                        });
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Translation Output", EditorStyles.boldLabel);

            if (_isProcessing)
            {
                EditorGUILayout.HelpBox("Processing... Please wait.", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(_translationError))
            {
                EditorGUILayout.HelpBox(_translationError, MessageType.Error);
            }
            else if (!string.IsNullOrEmpty(_service.rawTranslatedJsonOutput))
            {
                if (_lastTranslationWasValidJson)
                {
                    EditorGUILayout.HelpBox("Translation successful!", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Translation completed, but the output might not be valid JSON. Check error messages and raw output.", MessageType.Warning);
                }
            }

            _outputJsonScrollPosition = EditorGUILayout.BeginScrollView(_outputJsonScrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(_displayableTranslatedJson ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            _jsonOutputPath = EditorGUILayout.TextField("Save Output to Path", _jsonOutputPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string path = EditorUtility.SaveFilePanel("Save Translated JSON", Path.GetDirectoryName(_jsonOutputPath), "translated_output", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    _jsonOutputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Output to File"))
            {
                if (string.IsNullOrEmpty(_service.rawTranslatedJsonOutput))
                {
                    Debug.LogError("No translated output available to save.");
                }
                else if (string.IsNullOrEmpty(_jsonOutputPath) || _jsonOutputPath == Application.dataPath)
                {
                    Debug.LogError("Please specify a valid save file path using 'Browse...'");
                }
                else
                {
                    try
                    {
                        File.WriteAllText(_jsonOutputPath, _service.rawTranslatedJsonOutput); // Save the raw, not the potentially pretty-printed one for consistency
                        AssetDatabase.Refresh(); // Refresh asset database if saving within project
                        Debug.Log($"Translated JSON saved to {_jsonOutputPath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error saving JSON output file: {ex.Message}");
                    }
                }
            }

            if (needsRepaint)
            {
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
} 