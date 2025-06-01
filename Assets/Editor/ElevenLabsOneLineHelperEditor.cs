using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using ShowGenerator; // For ShowGeneratorApiKeys, ApiCaller, and ShowrunnerManager
using Newtonsoft.Json; // For serializing the payload
using System.Linq; // For DefaultVoiceMap keys
using System.Text.RegularExpressions; // For SanitizeFileName

[CustomEditor(typeof(ElevenLabsOneLineHelper))]
public class ElevenLabsOneLineHelperEditor : Editor
{
    private ElevenLabsOneLineHelper _helperTarget;
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
        
        _apiKeysConfigProp = serializedObject.FindProperty("apiKeysConfig");
        _useWrapperEndpointsProp = serializedObject.FindProperty("useWrapperEndpoints");
        _textToSpeakProp = serializedObject.FindProperty("textToSpeak");
        _outputFileNameProp = serializedObject.FindProperty("outputFileName");
        
        LoadVoiceMap();
    }

    private void LoadVoiceMap()
    {
        if (ShowrunnerManager.DefaultVoiceMap != null && ShowrunnerManager.DefaultVoiceMap.Count > 0)
        {
            _voiceNames = ShowrunnerManager.DefaultVoiceMap.Keys.ToArray();
            _voiceIds = ShowrunnerManager.DefaultVoiceMap.Values.ToArray();
            _voiceMapLoaded = true;

            bool voiceIdChanged = false;
            int currentIndex = _helperTarget.selectedVoiceIndex;
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
                if (foundIndex != -1 && foundIndex != currentIndex)
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
            _voiceNames = new string[] { "(Voice Map Unavailable)" };
            _voiceIds = new string[] { "" }; 
            _helperTarget.selectedVoiceIndex = 0;
            _helperTarget.selectedVoiceId = ""; 
            _voiceMapLoaded = false;
            Debug.LogWarning("ElevenLabsOneLineHelperEditor: ShowrunnerManager.DefaultVoiceMap is null or empty. Voice dropdown will be limited.");
            EditorUtility.SetDirty(_helperTarget);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); 

        EditorGUILayout.LabelField("ElevenLabs Single Line Audio Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_apiKeysConfigProp, new GUIContent("API Keys Config"));
        EditorGUILayout.PropertyField(_useWrapperEndpointsProp, new GUIContent("Use Wrapper Endpoints"));
        EditorGUILayout.PropertyField(_textToSpeakProp, new GUIContent("Text to Speak", "The text you want to convert to speech."));
        EditorGUILayout.PropertyField(_outputFileNameProp, new GUIContent("Base File Name", "Base name for the output audio file (e.g., my_line). Voice name will be prepended. Do not include .mp3 here."));

        EditorGUILayout.Space();

        if (!_voiceMapLoaded)
        {
            EditorGUILayout.HelpBox("Voice map from ShowrunnerManager could not be loaded. Please ensure ShowrunnerManager.DefaultVoiceMap is populated.", MessageType.Warning);
            if (GUILayout.Button("Retry Loading Voice Map"))
            {
                LoadVoiceMap(); 
            }
            EditorGUI.BeginChangeCheck();
            string manualVoiceId = EditorGUILayout.TextField(new GUIContent("Manual Voice ID Fallback", "Enter Voice ID manually if map is unavailable."), _helperTarget.selectedVoiceId);
            if(EditorGUI.EndChangeCheck())
            {
                _helperTarget.selectedVoiceId = manualVoiceId;
                EditorUtility.SetDirty(_helperTarget);
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            int newSelectedVoiceIndex = EditorGUILayout.Popup("Select Voice", _helperTarget.selectedVoiceIndex, _voiceNames);
            if (EditorGUI.EndChangeCheck()) 
            {
                _helperTarget.selectedVoiceIndex = newSelectedVoiceIndex;
                if (_voiceIds.Length > _helperTarget.selectedVoiceIndex && _helperTarget.selectedVoiceIndex >= 0)
                {
                    _helperTarget.selectedVoiceId = _voiceIds[_helperTarget.selectedVoiceIndex];
                }
                else 
                {
                    _helperTarget.selectedVoiceId = ""; 
                }
                EditorUtility.SetDirty(_helperTarget); 
            }
            EditorGUILayout.LabelField("Selected Voice ID:", _helperTarget.selectedVoiceId);
        }
        EditorGUILayout.Space();

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
        if (string.IsNullOrWhiteSpace(_helperTarget.textToSpeak))
        {
            _statusMessage = "Error: Text to speak cannot be empty.";
            Debug.LogError(_statusMessage);
            return false;
        }
        if (string.IsNullOrWhiteSpace(_helperTarget.selectedVoiceId))
        {
            _statusMessage = "Error: Voice ID cannot be empty (or is not selected from map).";
             if (!_voiceMapLoaded) _statusMessage += " Voice map is not loaded.";
            Debug.LogError(_statusMessage);
            return false;
        }
        if (_helperTarget.apiKeysConfig == null) 
        {
            _statusMessage = "Error: API Keys Config must be assigned.";
            Debug.LogError(_statusMessage);
            return false;
        }
        if (_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsWrapperUrl))
        {
            _statusMessage = "Error: Using wrapper, but ElevenLabs Wrapper URL is not configured in API Keys.";
            Debug.LogError(_statusMessage);
            return false;
        }
        if (!_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsApiKey))
        {
            _statusMessage = "Error: Not using wrapper, and ElevenLabs API Key is not configured.";
            Debug.LogError(_statusMessage);
            return false;
        }
        if (string.IsNullOrWhiteSpace(_helperTarget.outputFileName))
        {
            _helperTarget.outputFileName = "generated_audio"; 
            EditorUtility.SetDirty(_helperTarget);
            _outputFileNameProp.stringValue = "generated_audio"; 
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
        public string model_id = "eleven_multilingual_v2"; 
        public object voice_settings = null; // Reverted to object, set to null
    }
    
    [System.Serializable]
    private class ElevenLabsWrapperPayload // For Wrapper API
    {
        public string text;
        public string voice_id;
        // voice_settings field removed
    }

    private async Task GenerateAudioAsync()
    {
        _statusMessage = "Generating audio...";
        GUI.changed = true; 

        string voiceName = "unknown_voice";
        if (_voiceMapLoaded && _helperTarget.selectedVoiceIndex >= 0 && _helperTarget.selectedVoiceIndex < _voiceNames.Length)
        {
            voiceName = _voiceNames[_helperTarget.selectedVoiceIndex];
        }
        voiceName = SanitizeFileName(voiceName);
        string userBaseName = SanitizeFileName(_helperTarget.outputFileName);
        if (string.IsNullOrWhiteSpace(userBaseName)) userBaseName = "audio";
        string finalBaseName = $"{voiceName}_{userBaseName}";
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
        byte[] audioData = null;

        while (attempt < MAX_RETRIES)
        {
            attempt++;
            try
            {
                string endpointUrl;
                object payloadObj;
                Dictionary<string, string> headers = new Dictionary<string, string>();

                if (_helperTarget.useWrapperEndpoints)
                {
                    endpointUrl = _helperTarget.apiKeysConfig.elevenLabsWrapperUrl;
                    payloadObj = new ElevenLabsWrapperPayload 
                    { 
                        text = _helperTarget.textToSpeak, 
                        voice_id = _helperTarget.selectedVoiceId
                        // voice_settings removed
                    };
                }
                else // Direct API Call
                {
                    endpointUrl = DirectElevenLabsApiEndpointBase + _helperTarget.selectedVoiceId;
                    payloadObj = new ElevenLabsPayload 
                    { 
                        text = _helperTarget.textToSpeak,
                        voice_settings = null // Explicitly null for direct API as per reverted state
                    };
                    headers.Add("xi-api-key", _helperTarget.apiKeysConfig.elevenLabsApiKey);
                    headers.Add("Accept", "audio/mpeg"); 
                }
                
                Debug.Log($"Attempt {attempt}/{MAX_RETRIES} to generate audio for: \"{_helperTarget.textToSpeak}\" using voice ID: {_helperTarget.selectedVoiceId}. Using {( _helperTarget.useWrapperEndpoints ? "wrapper" : "direct API" )}.");

                audioData = await ApiCaller.PostJsonForBytesAsync(endpointUrl, payloadObj, headers);

                if (audioData != null && audioData.Length > 0)
                {
                    File.WriteAllBytes(fullDiskOutputPath, audioData);
                    AssetDatabase.Refresh(); 
                    _statusMessage = $"Audio saved successfully: {assetRelativePathForRefresh}";
                    Debug.Log(_statusMessage);
                    EditorUtility.SetDirty(target); 
                    return; 
                }
                else
                {
                    _statusMessage = $"Attempt {attempt} failed: No audio data returned.";
                    Debug.LogWarning(_statusMessage);
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Attempt {attempt} failed: {ex.Message}";
                Debug.LogError($"{_statusMessage}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
            }

            if (attempt < MAX_RETRIES)
            {
                _statusMessage += $" Retrying in {BASE_DELAY_MS * attempt / 1000}s...";
                await Task.Delay(BASE_DELAY_MS * attempt);
            }
             GUI.changed = true; 
        }

        if (audioData == null || audioData.Length == 0)
        {
            _statusMessage = $"Failed to generate audio after {MAX_RETRIES} attempts. Check console for errors.";
            Debug.LogError(_statusMessage);
        }
        EditorUtility.SetDirty(target); 
        GUI.changed = true; 
    }
} 