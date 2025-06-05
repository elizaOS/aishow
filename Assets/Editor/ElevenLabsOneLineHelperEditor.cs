using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using ShowGenerator; // For ShowGeneratorApiKeys, ApiCaller, and ShowrunnerManager
using System.Linq; // For DefaultVoiceMap keys
using System.Text.RegularExpressions; // For SanitizeFileName

[CustomEditor(typeof(ElevenLabsOneLineHelper))]
public class ElevenLabsOneLineHelperEditor : Editor
{
    private ElevenLabsOneLineHelper _helperTarget;
    private TranscriptTranslator _translator;
    private string _statusMessage = "";
    private string[] _voiceNames;
    private string[] _voiceIds;
    private bool _voiceMapLoaded = false;

    // SerializedProperties
    private SerializedProperty _apiKeysConfigProp;
    private SerializedProperty _useWrapperEndpointsProp;
    private SerializedProperty _textToSpeakProp;
    private SerializedProperty _outputFileNameProp;

    private const string DirectElevenLabsApiEndpointBase = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string OutputDirectory = "Assets/AudioOutput/SingleLinesInspector";

    private void OnEnable()
    {
        _helperTarget = (ElevenLabsOneLineHelper)target;
        
        _translator = _helperTarget.GetComponent<TranscriptTranslator>();
        if (_translator == null)
        {
            _translator = _helperTarget.gameObject.AddComponent<TranscriptTranslator>();
            EditorUtility.SetDirty(_helperTarget.gameObject);
            Debug.Log("TranscriptTranslator component was added to the GameObject.");
        }
        if (_translator.apiKeysConfig == null && _helperTarget.apiKeysConfig != null)
        {
            _translator.apiKeysConfig = _helperTarget.apiKeysConfig;
            EditorUtility.SetDirty(_translator);
        }

        _apiKeysConfigProp = serializedObject.FindProperty("apiKeysConfig");
        _useWrapperEndpointsProp = serializedObject.FindProperty("useWrapperEndpoints");
        _textToSpeakProp = serializedObject.FindProperty("textToSpeak");
        _outputFileNameProp = serializedObject.FindProperty("outputFileName");
        
        LoadVoiceMap();
    }

    private void LoadVoiceMap()
    {
        bool voiceIdChanged = false;
        if (ShowrunnerManager.DefaultVoiceMap != null && ShowrunnerManager.DefaultVoiceMap.Count > 0)
        {
            _voiceNames = ShowrunnerManager.DefaultVoiceMap.Keys.ToArray();
            _voiceIds = ShowrunnerManager.DefaultVoiceMap.Values.ToArray();
            _voiceMapLoaded = true;

            string currentId = _helperTarget.selectedVoiceId;

            if (string.IsNullOrEmpty(currentId) || !_voiceIds.Contains(currentId))
            {
                if (_voiceIds.Length > 0)
                {
                    _helperTarget.selectedVoiceIndex = 0;
                    _helperTarget.selectedVoiceId = _voiceIds[0];
                    voiceIdChanged = true;
                }
            }
            else
            {
                int foundIndex = System.Array.IndexOf(_voiceIds, currentId);
                if (foundIndex != -1 && foundIndex != _helperTarget.selectedVoiceIndex)
                {
                    _helperTarget.selectedVoiceIndex = foundIndex;
                    voiceIdChanged = true;
                }
                else if (foundIndex == -1)
                {
                     if (_voiceIds.Length > 0)
                     {
                        _helperTarget.selectedVoiceIndex = 0;
                        _helperTarget.selectedVoiceId = _voiceIds[0];
                        voiceIdChanged = true;
                     }
                }
            }
            if(voiceIdChanged) EditorUtility.SetDirty(_helperTarget);
        }
        else
        {
            _voiceNames = new string[] { "(Voice Map Unavailable - Check ShowrunnerManager)" };
            _voiceIds = new string[] { "" }; 
            _helperTarget.selectedVoiceIndex = 0;
            _helperTarget.selectedVoiceId = ""; 
            _voiceMapLoaded = false;
            if(voiceIdChanged) EditorUtility.SetDirty(_helperTarget);
        }
    }

    // --- Model and Language Definitions ---
    private static readonly string[] ModelDisplayNames = {
        "Multilingual v2 (Quality)", "Turbo v2.5 (Latency)", "Flash v2.5 (Fastest)", 
        "Multilingual v1", "Monolingual v1 (English)"
    };
    private static readonly string[] ModelIds = {
        "eleven_multilingual_v2", "eleven_turbo_v2_5", "eleven_flash_v2_5",
        "eleven_multilingual_v1", "eleven_monolingual_v1"
    };

    private static readonly string[] LanguageDisplayNames = {
        "English (No Translation)", "Japanese", "Chinese", "German", "Spanish", "French", 
        "Korean", "Portuguese", "Italian", "Hindi", "Indonesian", "Dutch", "Turkish", 
        "Filipino", "Polish", "Swedish", "Arabic", "Czech", "Greek", "Finnish", 
        "Croatian", "Malay", "Slovak", "Danish", "Tamil", "Ukrainian", "Russian"
    };
    private static readonly string[] LanguageCodes = {
        "en", "ja", "zh", "de", "es", "fr", 
        "ko", "pt", "it", "hi", "id", "nl", "tr", 
        "fil", "pl", "sv", "ar", "cs", "el", "fi", 
        "hr", "ms", "sk", "da", "ta", "uk", "ru"
    };
    // --- End Model and Language Definitions ---

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); 

        EditorGUILayout.LabelField("ElevenLabs Single Line Audio Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_apiKeysConfigProp, new GUIContent("API Keys Config"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (_translator != null && _helperTarget.apiKeysConfig != null)
            {
                _translator.apiKeysConfig = _helperTarget.apiKeysConfig;
                EditorUtility.SetDirty(_translator);
            }
        }
        EditorGUILayout.PropertyField(_useWrapperEndpointsProp, new GUIContent("Use Wrapper Endpoints"));
        EditorGUILayout.PropertyField(_textToSpeakProp, new GUIContent("Text to Speak", "The text you want to convert to speech."));
        EditorGUILayout.PropertyField(_outputFileNameProp, new GUIContent("Base File Name", "Base name for the output audio file (e.g., my_line). Voice name will be prepended. Do not include .mp3 here."));

        EditorGUILayout.Space();

        // Model ID Dropdown
        int currentModelIndex = System.Array.IndexOf(ModelIds, _helperTarget.modelId);
        if (currentModelIndex < 0) currentModelIndex = 0; 
        
        EditorGUI.BeginChangeCheck();
        int newModelIndex = EditorGUILayout.Popup("ElevenLabs Model", currentModelIndex, ModelDisplayNames);
        if (EditorGUI.EndChangeCheck())
        {
            _helperTarget.modelId = ModelIds[newModelIndex];
            EditorUtility.SetDirty(_helperTarget);
        }

        // Language Code and Translation Target Dropdown
        int currentLangDisplayIndex = System.Array.IndexOf(LanguageDisplayNames, _helperTarget.targetTranslationLanguage);
        if (currentLangDisplayIndex < 0) 
        {
            int langCodeIdx = System.Array.IndexOf(LanguageCodes, _helperTarget.languageCode);
            if (langCodeIdx >= 0 && langCodeIdx < LanguageDisplayNames.Length) currentLangDisplayIndex = langCodeIdx;
            else currentLangDisplayIndex = 0;
        }
        
        EditorGUI.BeginChangeCheck();
        int newLangDisplayIndex = EditorGUILayout.Popup(new GUIContent("Target Language / EL Code", "Select target language for translation and the language code for ElevenLabs."), currentLangDisplayIndex, LanguageDisplayNames);
        if (EditorGUI.EndChangeCheck())
        {
            _helperTarget.targetTranslationLanguage = LanguageDisplayNames[newLangDisplayIndex];
            _helperTarget.languageCode = LanguageCodes[newLangDisplayIndex]; 
            EditorUtility.SetDirty(_helperTarget);
        }
        EditorGUILayout.LabelField("   (ElevenLabs code: ", _helperTarget.languageCode + ", Translator target: " + _helperTarget.targetTranslationLanguage + ")");

        EditorGUILayout.Space();
        
        // --- Experimental Settings ---
        EditorGUI.BeginChangeCheck();
        bool useExperimental = EditorGUILayout.Toggle(new GUIContent("Use Experimental Settings", "Enable to configure advanced voice settings like stability and style."), _helperTarget.useExperimentalSettings);
        if (EditorGUI.EndChangeCheck())
        {
            _helperTarget.useExperimentalSettings = useExperimental;
            EditorUtility.SetDirty(_helperTarget);
        }

        if (_helperTarget.useExperimentalSettings)
        {
            EditorGUI.indentLevel++;
            _helperTarget.stability = EditorGUILayout.Slider("Stability", _helperTarget.stability, 0f, 1f);
            _helperTarget.similarityBoost = EditorGUILayout.Slider("Similarity Boost", _helperTarget.similarityBoost, 0f, 1f);
            _helperTarget.styleExaggeration = EditorGUILayout.Slider("Style Exaggeration", _helperTarget.styleExaggeration, 0f, 1f);
            _helperTarget.useSpeakerBoost = EditorGUILayout.Toggle("Use Speaker Boost", _helperTarget.useSpeakerBoost);
            EditorGUI.indentLevel--;
            EditorUtility.SetDirty(_helperTarget); // Mark dirty if any slider/toggle changes
        }
        
        EditorGUILayout.Space();

        if (!_voiceMapLoaded)
        {
            EditorGUILayout.HelpBox("Voice map from ShowrunnerManager could not be loaded. Ensure it's populated and try reloading.", MessageType.Warning);
            if (GUILayout.Button("Retry Loading Voice Map"))
            {
                LoadVoiceMap(); 
            }
        }
        EditorGUI.BeginChangeCheck();
        int newSelectedVoiceIndex = _voiceMapLoaded ? 
            EditorGUILayout.Popup("Select Voice", _helperTarget.selectedVoiceIndex, _voiceNames) : 
            _helperTarget.selectedVoiceIndex;

        if (!_voiceMapLoaded) {
             _helperTarget.selectedVoiceId = EditorGUILayout.TextField(new GUIContent("Voice ID (Map N/A)", "Enter Voice ID manually."), _helperTarget.selectedVoiceId);
        }

        if (EditorGUI.EndChangeCheck()) 
        {
            _helperTarget.selectedVoiceIndex = newSelectedVoiceIndex;
            if (_voiceMapLoaded && _voiceIds.Length > _helperTarget.selectedVoiceIndex && _helperTarget.selectedVoiceIndex >= 0)
            {
                _helperTarget.selectedVoiceId = _voiceIds[_helperTarget.selectedVoiceIndex];
            }
            EditorUtility.SetDirty(_helperTarget); 
        }
        EditorGUILayout.LabelField("Selected Voice ID:", string.IsNullOrEmpty(_helperTarget.selectedVoiceId) && _voiceMapLoaded ? "(None Selected)" : _helperTarget.selectedVoiceId);
        
        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(_helperTarget.lastTranslatedText))
        {
            EditorGUILayout.LabelField("Last Translated Text:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_helperTarget.lastTranslatedText, MessageType.None);
        }
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate and Save Audio"))
        {
            if (ValidateInputUsingTargetState()) 
            {
                string targetDirectory = Path.Combine(Application.dataPath, OutputDirectory.Replace("Assets/", ""));
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    AssetDatabase.Refresh();
                }
                _ = GenerateAudioAsync(); 
            }
        }

        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }
        
        serializedObject.ApplyModifiedProperties(); 
    }

    private bool ValidateInputUsingTargetState()
    {
        if (_translator != null && _translator.apiKeysConfig == null && _helperTarget.apiKeysConfig != null) {
             _translator.apiKeysConfig = _helperTarget.apiKeysConfig;
        }

        if (string.IsNullOrWhiteSpace(_helperTarget.textToSpeak))
        {
            _statusMessage = "Error: Text to speak cannot be empty.";
            Debug.LogError(_statusMessage);
            Debug.Log("EVENT FIRING from ValidateInput: TextToSpeak empty");
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        if (string.IsNullOrWhiteSpace(_helperTarget.selectedVoiceId))
        {
            _statusMessage = "Error: Voice ID cannot be empty.";
             if (!_voiceMapLoaded) _statusMessage += " Voice map might not be loaded.";
            Debug.LogError(_statusMessage);
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        if (_helperTarget.apiKeysConfig == null) 
        {
            _statusMessage = "Error: API Keys Config must be assigned to the Helper.";
            Debug.LogError(_statusMessage);
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        bool translationNeeded = _helperTarget.targetTranslationLanguage != "English (No Translation)" && !string.IsNullOrEmpty(_helperTarget.targetTranslationLanguage);
        if (translationNeeded)
        {
            if (_translator == null)
            {
                _statusMessage = "Error: TranscriptTranslator component is missing.";
                Debug.LogError(_statusMessage);
                 _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
                return false;
            }
            if (_translator.apiKeysConfig == null)
            {
                 _statusMessage = "Error: API Keys Config must be assigned to the TranscriptTranslator (or synced from Helper).";
                 Debug.LogError(_statusMessage);
                _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
                 return false;
            }
            string? translatorError = null;
            switch (_translator.currentApiProvider) {
                case TranscriptTranslator.ApiProvider.OpenRouter:
                    if (string.IsNullOrEmpty(_translator.apiKeysConfig.openRouterApiKey)) translatorError = "OpenRouter API Key missing for translator.";
                    break;
                case TranscriptTranslator.ApiProvider.AnthropicDirect:
                     if (string.IsNullOrEmpty(_translator.apiKeysConfig.anthropicApiKey)) translatorError = "Anthropic API Key missing for translator.";
                    break;
                case TranscriptTranslator.ApiProvider.AnthropicWrapper:
                    if (string.IsNullOrEmpty(_translator.apiKeysConfig.claudeWrapperUrl)) translatorError = "Anthropic Wrapper URL missing for translator.";
                    break;
            }
            if (translatorError != null) {
                _statusMessage = $"Error: {translatorError}";
                Debug.LogError(_statusMessage);
                _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
                return false;
            }
        }

        if (_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsWrapperUrl))
        {
            _statusMessage = "Error: Using wrapper for ElevenLabs, but ElevenLabs Wrapper URL is not configured.";
            Debug.LogError(_statusMessage);
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        if (!_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsApiKey))
        {
            _statusMessage = "Error: Not using wrapper for ElevenLabs, and ElevenLabs API Key is not configured.";
            Debug.LogError(_statusMessage);
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        if (string.IsNullOrWhiteSpace(_helperTarget.outputFileName))
        {
            _helperTarget.outputFileName = "generated_audio"; 
            EditorUtility.SetDirty(_helperTarget);
        }
        if (string.IsNullOrWhiteSpace(_helperTarget.modelId))
        {
            _statusMessage = "Error: ElevenLabs Model ID cannot be empty.";
            Debug.LogError(_statusMessage);
            _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(_helperTarget.textToSpeak, _helperTarget.lastTranslatedText, null, _statusMessage);
            return false;
        }
        return true;
    }

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unnamed";
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        string sanitizedName = Regex.Replace(name, invalidRegStr, "_");
        return sanitizedName.Replace(" ", "_").Replace("..", "_").Replace("/", "_").Replace("\\", "_");
    }

    [System.Serializable]
    private class ElevenLabsPayload // For Direct API
    {
        public string text;
        public string model_id;
        public string language_code;
        public object? voice_settings; // Can be null or the settings object
    }
    
    [System.Serializable]
    private class ElevenLabsWrapperPayload // For Wrapper API
    {
        public string text;
        public string voice_id;
        public string model_id;
        public string language_code;
        public object? voice_settings; // Can be null or the settings object
    }

    [System.Serializable]
    private class VoiceSettings // For experimental settings
    {
        public float stability;
        public float similarity_boost;
        public float style;
        public bool use_speaker_boost;
    }

    private async Task GenerateAudioAsync()
    {
        _statusMessage = "Starting generation process...";
        _helperTarget.lastTranslatedText = "";
        GUI.changed = true;
        EditorUtility.SetDirty(_helperTarget);

        string originalText = _helperTarget.textToSpeak;
        string textToSpeakToElevenLabs = originalText;
        string finalStatusMessage = "";
        byte[] audioData = null;

        bool translationAttempted = false;
        if (_helperTarget.targetTranslationLanguage != "English (No Translation)" && 
            !string.IsNullOrEmpty(_helperTarget.targetTranslationLanguage) &&
            _translator != null)
        {
            translationAttempted = true;
            _statusMessage = $"Translating '{originalText.Substring(0, Mathf.Min(originalText.Length, 20))}...' to {_helperTarget.targetTranslationLanguage}...";
            Debug.Log(_statusMessage);
            Repaint();

            var translationTcs = new TaskCompletionSource<(string translatedText, string error, bool success)>();
            _translator.CallTranslateText(
                originalText,
                _helperTarget.targetTranslationLanguage,
                "Ensure the translation is natural, accurate, and suitable for text-to-speech. Only return the translated text.",
                (translated, err, succ) => translationTcs.SetResult((translated, err, succ))
            );

            var translationResult = await translationTcs.Task;

            if (translationResult.success && !string.IsNullOrEmpty(translationResult.translatedText))
            {
                textToSpeakToElevenLabs = translationResult.translatedText;
                _helperTarget.lastTranslatedText = textToSpeakToElevenLabs;
                _statusMessage = $"Translation successful. Result: '{textToSpeakToElevenLabs.Substring(0, Mathf.Min(textToSpeakToElevenLabs.Length, 30))}...'";
                Debug.Log(_statusMessage);
            }
            else
            {
                _statusMessage = $"Translation failed: {translationResult.error}. Using original text for ElevenLabs.";
                Debug.LogError(_statusMessage);
                finalStatusMessage = $"Translation failed: {translationResult.error}. ";
            }
            EditorUtility.SetDirty(_helperTarget);
            Repaint();
        }
        else
        {
            _statusMessage = "No translation needed or translator not available. Using original text.";
            Debug.Log(_statusMessage);
            _helperTarget.lastTranslatedText = "";
            EditorUtility.SetDirty(_helperTarget);
             Repaint();
        }

        _statusMessage = $"Requesting audio from ElevenLabs for: '{textToSpeakToElevenLabs.Substring(0, Mathf.Min(textToSpeakToElevenLabs.Length, 30))}...' (Voice: {_helperTarget.selectedVoiceId}, Model: {_helperTarget.modelId}, EL Lang Code: {_helperTarget.languageCode})";
        Debug.Log(_statusMessage);
        Repaint();

        string voiceName = "unknown_voice";
        if (_voiceMapLoaded && _helperTarget.selectedVoiceIndex >= 0 && _helperTarget.selectedVoiceIndex < _voiceNames.Length)
        {
            voiceName = _voiceNames[_helperTarget.selectedVoiceIndex];
        }
        voiceName = SanitizeFileName(voiceName);
        string userBaseName = SanitizeFileName(_helperTarget.outputFileName);
        if (string.IsNullOrWhiteSpace(userBaseName)) userBaseName = "audio";
        
        string langPart = string.IsNullOrEmpty(_helperTarget.languageCode) || _helperTarget.languageCode.ToLower() == "en" ? "" : $"_{_helperTarget.languageCode}";
        if (_helperTarget.targetTranslationLanguage != "English (No Translation)" && !string.IsNullOrEmpty(_helperTarget.targetTranslationLanguage)) {
            langPart = $"_{SanitizeFileName(_helperTarget.targetTranslationLanguage.Split(' ')[0])}";
        }

        string finalBaseName = $"{voiceName}{langPart}_{userBaseName}";
        string extension = ".mp3";
        string currentFileNameWithExtension = finalBaseName + extension;
        
        string relativeAssetDirectoryForPath = OutputDirectory;
        if (relativeAssetDirectoryForPath.StartsWith("Assets/"))
        {
            relativeAssetDirectoryForPath = relativeAssetDirectoryForPath.Substring("Assets/".Length);
        }
        string fullDirectoryPath = Path.Combine(Application.dataPath, relativeAssetDirectoryForPath);
        string fullDiskOutputPath = Path.Combine(fullDirectoryPath, currentFileNameWithExtension);
        int counter = 1;
        while (File.Exists(fullDiskOutputPath))
        {
            currentFileNameWithExtension = $"{finalBaseName}_{counter}{extension}";
            fullDiskOutputPath = Path.Combine(fullDirectoryPath, currentFileNameWithExtension);
            counter++;
        }
        string assetRelativePathForRefresh = Path.Combine(OutputDirectory, currentFileNameWithExtension);

        const int MAX_RETRIES = 3;
        const int BASE_DELAY_MS = 1000;
        int attempt = 0;

        while (attempt < MAX_RETRIES)
        {
            attempt++;
            try
            {
                string endpointUrl;
                object payloadObject;
                Dictionary<string, string> headers = null;

                if (_helperTarget.useWrapperEndpoints)
                {
                    endpointUrl = _helperTarget.apiKeysConfig.elevenLabsWrapperUrl;
                    var wrapperPayload = new ElevenLabsWrapperPayload
                    {
                        text = textToSpeakToElevenLabs,
                        voice_id = _helperTarget.selectedVoiceId,
                        model_id = _helperTarget.modelId,
                        language_code = _helperTarget.languageCode
                    };

                    if (_helperTarget.useExperimentalSettings)
                    {
                        wrapperPayload.voice_settings = new VoiceSettings
                        {
                            stability = _helperTarget.stability,
                            similarity_boost = _helperTarget.similarityBoost,
                            style = _helperTarget.styleExaggeration,
                            use_speaker_boost = _helperTarget.useSpeakerBoost
                        };
                    }

                    payloadObject = wrapperPayload;
                }
                else
                {
                    endpointUrl = DirectElevenLabsApiEndpointBase + _helperTarget.selectedVoiceId;
                    var directPayload = new ElevenLabsPayload
                    {
                        text = textToSpeakToElevenLabs,
                        model_id = _helperTarget.modelId,
                        language_code = _helperTarget.languageCode
                    };

                    if (_helperTarget.useExperimentalSettings)
                    {
                        directPayload.voice_settings = new VoiceSettings
                        {
                            stability = _helperTarget.stability,
                            similarity_boost = _helperTarget.similarityBoost,
                            style = _helperTarget.styleExaggeration,
                            use_speaker_boost = _helperTarget.useSpeakerBoost
                        };
                    }

                    payloadObject = directPayload;
                    headers = new Dictionary<string, string> { { "xi-api-key", _helperTarget.apiKeysConfig.elevenLabsApiKey } };
                }
                
                _statusMessage = $"Attempt {attempt}/{MAX_RETRIES} for ElevenLabs audio generation...";

                string truncatedTextForLog = textToSpeakToElevenLabs;
                if (truncatedTextForLog.Length > 50)
                {
                    truncatedTextForLog = truncatedTextForLog.Substring(0, 50) + "...";
                }
                Debug.Log($"Attempt {attempt}/{MAX_RETRIES} to generate audio from ElevenLabs. Using text: \"{truncatedTextForLog}\" Voice ID: {_helperTarget.selectedVoiceId}. Model: {_helperTarget.modelId}. EL Language Code: {_helperTarget.languageCode}. Endpoint: {( _helperTarget.useWrapperEndpoints ? "wrapper" : "direct API" )}.");
                Repaint();

                audioData = await ApiCaller.PostJsonForBytesAsync(endpointUrl, payloadObject, headers);

                if (audioData != null && audioData.Length > 0)
                {
                    File.WriteAllBytes(fullDiskOutputPath, audioData);
                    AssetDatabase.Refresh(); 
                    _statusMessage = $"Audio saved: {assetRelativePathForRefresh}";
                    Debug.Log(_statusMessage);
                    finalStatusMessage += _statusMessage;
                    Debug.Log("EVENT FIRING from GenerateAudioAsync: Success");
                    _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(originalText, textToSpeakToElevenLabs, audioData, translationAttempted && !string.IsNullOrEmpty(finalStatusMessage.Trim()) ? finalStatusMessage.Trim() : null);
                    EditorUtility.SetDirty(target); 
                    Repaint();
                    return; 
                }
                else
                {
                    _statusMessage = $"ElevenLabs attempt {attempt} failed: No audio data returned.";
                    Debug.LogWarning(_statusMessage);
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"ElevenLabs attempt {attempt} failed: {ex.Message}";
                Debug.LogError($"{_statusMessage}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
            }

            if (attempt < MAX_RETRIES)
            {
                _statusMessage += $" Retrying ElevenLabs in {BASE_DELAY_MS * attempt / 1000}s...";
                Repaint();
                await Task.Delay(BASE_DELAY_MS * attempt);
            }
        }

        if (audioData == null || audioData.Length == 0)
        {
            _statusMessage = $"Failed to generate audio from ElevenLabs after {MAX_RETRIES} attempts. Check console.";
            Debug.LogError(_statusMessage);
            finalStatusMessage += _statusMessage;
        }
        
        Debug.Log("EVENT FIRING from GenerateAudioAsync: Failure/End");
        _helperTarget.OnAudioGeneratedWithTranslation?.Invoke(originalText, textToSpeakToElevenLabs, null, finalStatusMessage.Trim());
        EditorUtility.SetDirty(target); 
        Repaint();
        GUI.changed = true; 
    }
} 