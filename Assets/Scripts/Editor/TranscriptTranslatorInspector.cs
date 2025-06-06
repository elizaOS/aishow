#nullable enable
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO; // Required for File operations
// using System.Linq; // Not strictly needed for this version but often useful with collections

[CustomEditor(typeof(TranscriptTranslator))]
public class TranscriptTranslatorInspector : Editor
{
    private TranscriptTranslator? _translatorInstance;
    private SerializedProperty? apiKeysConfigProp;
    private SerializedProperty? currentApiProviderProp;
    private SerializedProperty? openRouterModelNameProp;

    private string _textInputPath = ""; // For the text file browser
    private string _textOutputPath = ""; // For saving the translated text

    private string _targetLanguage = "English";
    private int _selectedLanguageIndex = 0; // Default to English or first in list
    private static readonly string[] _supportedLanguages = { 
        "English", "Spanish", "French", "German", "Japanese", "Korean", "Chinese (Simplified)", 
        "Italian", "Portuguese", "Russian", "Arabic", "Hindi", "Dutch", "Swedish", "Norwegian", "Danish", "Finnish"
    }; // Added more languages

    private string _customInstructions = "Ensure the translation is natural and fluent.";
    private string _textToTranslate = "Enter text here..."; 
    private string _currentSystemPromptPreview = ""; // For displaying the constructed prompt

    // For scrolling text areas
    private Vector2 _textToTranslateScrollPos; // For the main input text area
    private Vector2 _customInstructionsScrollPos;
    private Vector2 _systemPromptScrollPos; 
    private Vector2 _rawInputDisplayScrollPos; // For displaying the raw input that was processed
    private Vector2 _rawOutputScrollPos;
    // private Vector2 _displayableOutputScrollPos; // Commented out as it was redundant
    // private Vector2 _errorScrollPos; // Commented out, HelpBox is fine for errors

    private const string EditorPrefsPrefix = "TranscriptTranslatorInspector.";

    private void OnEnable()
    {
        _translatorInstance = (TranscriptTranslator)target;
        apiKeysConfigProp = serializedObject.FindProperty("apiKeysConfig");
        currentApiProviderProp = serializedObject.FindProperty("currentApiProvider");
        openRouterModelNameProp = serializedObject.FindProperty("openRouterModelName");

        _textInputPath = EditorPrefs.GetString(EditorPrefsPrefix + "TextInputPath", Application.dataPath);
        _textOutputPath = EditorPrefs.GetString(EditorPrefsPrefix + "TextOutputPath", Application.dataPath);

        _selectedLanguageIndex = EditorPrefs.GetInt(EditorPrefsPrefix + "SelectedLanguageIndex", GetDefaultLanguageIndex());
        // Ensure index is valid, then set target language
        if (_selectedLanguageIndex < 0 || _selectedLanguageIndex >= _supportedLanguages.Length) {
            _selectedLanguageIndex = GetDefaultLanguageIndex();
        }
        _targetLanguage = _supportedLanguages[_selectedLanguageIndex];
        
        _customInstructions = EditorPrefs.GetString(EditorPrefsPrefix + "CustomInstructions", "Ensure the translation is natural and fluent.");
        _textToTranslate = EditorPrefs.GetString(EditorPrefsPrefix + "TextToTranslate", "Enter text here...");

        UpdateSystemPromptPreview(); // Initialize the prompt preview
    }

    private int GetDefaultLanguageIndex()
    {
        int englishIndex = System.Array.IndexOf(_supportedLanguages, "English");
        return englishIndex >= 0 ? englishIndex : 0;
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(EditorPrefsPrefix + "TextInputPath", _textInputPath);
        EditorPrefs.SetString(EditorPrefsPrefix + "TextOutputPath", _textOutputPath);
        EditorPrefs.SetInt(EditorPrefsPrefix + "SelectedLanguageIndex", _selectedLanguageIndex);
        EditorPrefs.SetString(EditorPrefsPrefix + "CustomInstructions", _customInstructions);
        EditorPrefs.SetString(EditorPrefsPrefix + "TextToTranslate", _textToTranslate);
    }

    private void UpdateSystemPromptPreview()
    {
        if (_translatorInstance == null) return;

        // This logic should mirror the prompt construction in TranscriptTranslator.cs
        StringBuilder systemPromptBuilder = new StringBuilder();
        systemPromptBuilder.AppendLine($"You are an expert translation assistant. Your task is to translate the provided text from its original language into {_targetLanguage}.");
        systemPromptBuilder.AppendLine("Key requirements:");
        systemPromptBuilder.AppendLine("1. Translate the text accurately, maintaining the original meaning, tone, and style as much as possible.");
        systemPromptBuilder.AppendLine("2. Handle idiomatic expressions, slang, and cultural nuances appropriately for the target language.");
        systemPromptBuilder.AppendLine("3. If specific terminology or proper nouns are present, translate them correctly or keep them in the original language if that is the standard practice in the target language and context.");
        if (!string.IsNullOrEmpty(_customInstructions))
        {
            systemPromptBuilder.AppendLine("4. Follow these additional user-provided instructions carefully:");
            systemPromptBuilder.AppendLine(_customInstructions);
            systemPromptBuilder.AppendLine("5. IMPORTANT: Your response MUST be ONLY the translated text. Do not include any explanatory text, markdown, or any other characters before or after the translated text itself.");
        }
        else
        {
            systemPromptBuilder.AppendLine("4. IMPORTANT: Your response MUST be ONLY the translated text. Do not include any explanatory text, markdown, or any other characters before or after the translated text itself.");
        }
        _currentSystemPromptPreview = systemPromptBuilder.ToString();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        bool needsRepaint = false;

        if (_translatorInstance == null) return;

        EditorGUILayout.LabelField("API Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(apiKeysConfigProp, new GUIContent("API Keys Config"));
        EditorGUILayout.PropertyField(currentApiProviderProp, new GUIContent("API Provider"));

        if (_translatorInstance.currentApiProvider == TranscriptTranslator.ApiProvider.OpenRouter)
        {
            EditorGUILayout.PropertyField(openRouterModelNameProp, new GUIContent("OpenRouter Model"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Translation Input", EditorStyles.boldLabel);

        // Text file browser
        EditorGUILayout.BeginHorizontal();
        _textInputPath = EditorGUILayout.TextField("Load Text from Path", _textInputPath);
        if (GUILayout.Button("Browse...", GUILayout.Width(70)))
        {
            string directory = string.IsNullOrEmpty(Path.GetDirectoryName(_textInputPath)) ? Application.dataPath : Path.GetDirectoryName(_textInputPath)!;
            string path = EditorUtility.OpenFilePanel("Load Text File", directory, "txt,transcript");
            if (!string.IsNullOrEmpty(path))
            {
                _textInputPath = path;
                try
                {
                    _textToTranslate = File.ReadAllText(_textInputPath);
                    _translatorInstance.rawTextInput = _textToTranslate; // Update service's raw input immediately
                    needsRepaint = true; // To update TextArea
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error loading text input file: {ex.Message}");
                    _textToTranslate = $"Error loading file: {ex.Message}";
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Or Paste Text to Translate Here:");
        EditorGUI.BeginChangeCheck();
        _textToTranslateScrollPos = EditorGUILayout.BeginScrollView(_textToTranslateScrollPos, GUILayout.Height(100));
        _textToTranslate = EditorGUILayout.TextArea(_textToTranslate, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        if (EditorGUI.EndChangeCheck())
        {
             // If user manually edits text, update the rawTextInput on the service instance
            _translatorInstance.rawTextInput = _textToTranslate;
        }

        EditorGUI.BeginChangeCheck();
        _selectedLanguageIndex = EditorGUILayout.Popup("Target Language", _selectedLanguageIndex, _supportedLanguages);
        if (EditorGUI.EndChangeCheck())
        {
            _targetLanguage = _supportedLanguages[_selectedLanguageIndex];
            UpdateSystemPromptPreview();
            needsRepaint = true;
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Custom Instructions (Optional):", EditorStyles.miniBoldLabel);
        _customInstructionsScrollPos = EditorGUILayout.BeginScrollView(_customInstructionsScrollPos, GUILayout.Height(60));
        _customInstructions = EditorGUILayout.TextArea(_customInstructions, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        if (EditorGUI.EndChangeCheck())
        {
            UpdateSystemPromptPreview();
            needsRepaint = true;
        }

        // Display the constructed system prompt
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("System Prompt Preview (Read-Only)", EditorStyles.boldLabel);
        _systemPromptScrollPos = EditorGUILayout.BeginScrollView(_systemPromptScrollPos, GUILayout.Height(100));
        EditorGUILayout.TextArea(_currentSystemPromptPreview, EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (_translatorInstance.isProcessing)
        {
            EditorGUILayout.HelpBox("Processing... Please wait.", MessageType.Info);
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button("Translate Text", GUILayout.Height(30));
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            if (GUILayout.Button("Translate Text", GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(_textToTranslate) && !string.IsNullOrEmpty(_targetLanguage))
                {
                    _translatorInstance.rawTextInput = _textToTranslate; // Ensure latest input is on the service
                    _translatorInstance.CallTranslateText(_textToTranslate, _targetLanguage, _customInstructions ?? "", 
                        (translatedText, error, success) =>
                    {
                        // Repaint is handled by EditorApplication.delayCall in original code if still needed
                        // Forcing repaint here can be good after async operations complete
                        Repaint(); 
                    });
                }
                else
                {
                    EditorUtility.DisplayDialog("Input Error", "Text to translate and target language cannot be empty.", "OK");
                    Debug.LogError("Text input and target language cannot be empty.");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Last Translation Results", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Original Input Text:", EditorStyles.miniBoldLabel);
        if (!string.IsNullOrEmpty(_translatorInstance.rawTextInput))
        {
            _rawInputDisplayScrollPos = EditorGUILayout.BeginScrollView(_rawInputDisplayScrollPos, GUILayout.Height(80));
            EditorGUILayout.TextArea(_translatorInstance.rawTextInput, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No raw text input was processed.", MessageType.None);
        }

        EditorGUILayout.LabelField("Translated Text Output:", EditorStyles.miniBoldLabel);
        if (!string.IsNullOrEmpty(_translatorInstance.rawTranslatedTextOutput))
        {
            _rawOutputScrollPos = EditorGUILayout.BeginScrollView(_rawOutputScrollPos, GUILayout.Height(150));
            EditorGUILayout.TextArea(_translatorInstance.rawTranslatedTextOutput, EditorStyles.textArea, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            // Save translated text section
            EditorGUILayout.BeginHorizontal();
            _textOutputPath = EditorGUILayout.TextField("Save Output to Path", _textOutputPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(70)))
            {
                string directory = string.IsNullOrEmpty(Path.GetDirectoryName(_textOutputPath)) ? Application.dataPath : Path.GetDirectoryName(_textOutputPath)!;
                string fileName = string.IsNullOrEmpty(Path.GetFileName(_textOutputPath)) ? "translated_output.txt" : Path.GetFileName(_textOutputPath);
                string path = EditorUtility.SaveFilePanel("Save Translated Text", directory, fileName, "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _textOutputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Output to File", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(_translatorInstance.rawTranslatedTextOutput))
                {
                    EditorUtility.DisplayDialog("Save Error", "No translated output available to save.", "OK");
                }
                else if (string.IsNullOrEmpty(_textOutputPath) || _textOutputPath == Application.dataPath)
                {
                     EditorUtility.DisplayDialog("Save Error", "Please specify a valid save file path using 'Browse...' (cannot be the root Assets folder directly for safety, pick a subfolder or a specific file name).", "OK");
                }
                else
                {
                    try
                    {
                        File.WriteAllText(_textOutputPath, _translatorInstance.rawTranslatedTextOutput);
                        Debug.Log($"Translated text saved to {_textOutputPath}");
                        // Optionally refresh AssetDatabase if saved within project, though not always necessary for .txt
                        // if (_textOutputPath.StartsWith(Application.dataPath)) { AssetDatabase.Refresh(); }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error saving text output file: {ex.Message}");
                        EditorUtility.DisplayDialog("Save Error", $"Could not save file: {ex.Message}", "OK");
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No translated text output to display.", MessageType.None);
        }
        
        if (!string.IsNullOrEmpty(_translatorInstance.translationError))
        {
            EditorGUILayout.LabelField("Translation Error:", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(_translatorInstance.translationError, MessageType.Error);
        }

        if (needsRepaint)
        {
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }
} 