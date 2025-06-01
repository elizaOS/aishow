using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using ShowGenerator; 
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

[CustomEditor(typeof(ElevenLabsExperimentalSettingsHelper))]
public class ElevenLabsExperimentalSettingsHelperEditor : Editor
{
    private ElevenLabsExperimentalSettingsHelper _helperTarget;
    private string _statusMessage = "";
    private string[] _voiceNames;
    private string[] _voiceIds;
    private bool _voiceMapLoaded = false;

    // SerializedProperties
    private SerializedProperty _apiKeysConfigProp;
    private SerializedProperty _useWrapperEndpointsProp;
    private SerializedProperty _textToSpeakProp;
    private SerializedProperty _outputFileNameProp;
    private SerializedProperty _stabilityProp;
    private SerializedProperty _similarityBoostProp;
    private SerializedProperty _styleExaggerationProp;
    private SerializedProperty _useSpeakerBoostProp;

    private const string DirectElevenLabsApiEndpointBase = "https://api.elevenlabs.io/v1/text-to-speech/";
    private const string OutputDirectory = "Assets/AudioOutput/SingleLinesExperimental"; // Different output directory

    private void OnEnable()
    {
        _helperTarget = (ElevenLabsExperimentalSettingsHelper)target;
        
        _apiKeysConfigProp = serializedObject.FindProperty("apiKeysConfig");
        _useWrapperEndpointsProp = serializedObject.FindProperty("useWrapperEndpoints");
        _textToSpeakProp = serializedObject.FindProperty("textToSpeak");
        _outputFileNameProp = serializedObject.FindProperty("outputFileName");
        _stabilityProp = serializedObject.FindProperty("stability");
        _similarityBoostProp = serializedObject.FindProperty("similarityBoost");
        _styleExaggerationProp = serializedObject.FindProperty("styleExaggeration");
        _useSpeakerBoostProp = serializedObject.FindProperty("useSpeakerBoost");
        
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
                if (_voiceIds.Length > 0) { _helperTarget.selectedVoiceIndex = 0; _helperTarget.selectedVoiceId = _voiceIds[0]; voiceIdChanged = true; }
            }
            else
            {
                int foundIndex = System.Array.IndexOf(_voiceIds, currentId);
                if (foundIndex != -1 && foundIndex != currentIndex) { _helperTarget.selectedVoiceIndex = foundIndex; voiceIdChanged = true; }
                else if (foundIndex == -1) { if (_voiceIds.Length > 0) { _helperTarget.selectedVoiceIndex = 0; _helperTarget.selectedVoiceId = _voiceIds[0]; voiceIdChanged = true; } }
            }
            if(voiceIdChanged) EditorUtility.SetDirty(_helperTarget);
        }
        else
        {
            _voiceNames = new string[] { "(Voice Map Unavailable)" }; _voiceIds = new string[] { "" }; 
            _helperTarget.selectedVoiceIndex = 0; _helperTarget.selectedVoiceId = ""; 
            _voiceMapLoaded = false; Debug.LogWarning("Experimental Editor: Voice Map unavailable.");
            EditorUtility.SetDirty(_helperTarget);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("ElevenLabs Experimental Audio Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_apiKeysConfigProp);
        EditorGUILayout.PropertyField(_useWrapperEndpointsProp);
        EditorGUILayout.PropertyField(_textToSpeakProp);
        EditorGUILayout.PropertyField(_outputFileNameProp, new GUIContent("Base File Name", "Base name. Voice & iteration handled. No .mp3"));
        EditorGUILayout.Space();

        if (!_voiceMapLoaded)
        {
            EditorGUILayout.HelpBox("Voice map unavailable.", MessageType.Warning);
            if (GUILayout.Button("Retry Load Map")) LoadVoiceMap();
            EditorGUI.BeginChangeCheck();
            string manualId = EditorGUILayout.TextField("Manual Voice ID", _helperTarget.selectedVoiceId);
            if(EditorGUI.EndChangeCheck()) { _helperTarget.selectedVoiceId = manualId; EditorUtility.SetDirty(_helperTarget); }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUILayout.Popup("Select Voice", _helperTarget.selectedVoiceIndex, _voiceNames);
            if (EditorGUI.EndChangeCheck())
            {
                _helperTarget.selectedVoiceIndex = newIdx;
                if (_voiceIds.Length > newIdx && newIdx >= 0) _helperTarget.selectedVoiceId = _voiceIds[newIdx];
                else _helperTarget.selectedVoiceId = "";
                EditorUtility.SetDirty(_helperTarget);
            }
            EditorGUILayout.LabelField("Selected Voice ID:", _helperTarget.selectedVoiceId);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Experimental Voice Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_stabilityProp);
        EditorGUILayout.PropertyField(_similarityBoostProp);
        EditorGUILayout.PropertyField(_styleExaggerationProp);
        EditorGUILayout.PropertyField(_useSpeakerBoostProp);
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Audio (Experimental Settings)"))
        {
            if (ValidateInput()) 
            {
                string targetDir = Path.Combine(Application.dataPath, OutputDirectory.Replace("Assets/", ""));
                if (!Directory.Exists(targetDir)) { Directory.CreateDirectory(targetDir); AssetDatabase.Refresh(); }
                _ = GenerateAudioAsync(); 
            }
        }
        if (!string.IsNullOrEmpty(_statusMessage)) EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(_helperTarget.textToSpeak)) { _statusMessage = "Text empty."; return false; }
        if (string.IsNullOrWhiteSpace(_helperTarget.selectedVoiceId)) { _statusMessage = "Voice ID empty."; return false; }
        if (_helperTarget.apiKeysConfig == null) { _statusMessage = "API Keys not set."; return false; }
        if (_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsWrapperUrl)) { _statusMessage = "Wrapper URL missing."; return false; }
        if (!_helperTarget.useWrapperEndpoints && string.IsNullOrEmpty(_helperTarget.apiKeysConfig.elevenLabsApiKey)) { _statusMessage = "API Key missing."; return false; }
        if (string.IsNullOrWhiteSpace(_helperTarget.outputFileName)) { _helperTarget.outputFileName = "exp_audio"; EditorUtility.SetDirty(_helperTarget); _outputFileNameProp.stringValue = "exp_audio"; }
        return true;
    }

    private string SanitizeFileName(string name) { /* ... same as before ... */ 
        if (string.IsNullOrWhiteSpace(name)) return "unnamed";
        string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
        string sanitizedName = Regex.Replace(name, invalidRegStr, "_");
        return sanitizedName.Replace(" ", "_").Replace("..", "_").Replace("/", "_").Replace("\\", "_");
    }

    [System.Serializable] private class VoiceSettings { public float stability; public float similarity_boost; public float style; public bool use_speaker_boost; }
    [System.Serializable] private class ExpElevenLabsPayload { public string text; public string model_id = "eleven_multilingual_v2"; public VoiceSettings voice_settings; }
    [System.Serializable] private class ExpElevenLabsWrapperPayload { public string text; public string voice_id; public VoiceSettings voice_settings; }

    private async Task GenerateAudioAsync()
    {
        _statusMessage = "Generating (Experimental)..."; GUI.changed = true;
        string voiceName = (_voiceMapLoaded && _helperTarget.selectedVoiceIndex < _voiceNames.Length && _helperTarget.selectedVoiceIndex >=0) ? _voiceNames[_helperTarget.selectedVoiceIndex] : "unknown";
        string finalBaseName = $"{SanitizeFileName(voiceName)}_{SanitizeFileName(_helperTarget.outputFileName)}";
        string currentFileName = finalBaseName + ".mp3";
        string dirPath = Path.Combine(Application.dataPath, OutputDirectory.Replace("Assets/", ""));
        string fullPath = Path.Combine(dirPath, currentFileName);
        int c = 1; while (File.Exists(fullPath)) { currentFileName = $"{finalBaseName}_{c++}.mp3"; fullPath = Path.Combine(dirPath, currentFileName); }
        string assetRelPath = Path.Combine(OutputDirectory, currentFileName);

        VoiceSettings vs = new VoiceSettings { stability = _helperTarget.stability, similarity_boost = _helperTarget.similarityBoost, style = _helperTarget.styleExaggeration, use_speaker_boost = _helperTarget.useSpeakerBoost };
        byte[] audioData = null; int attempt = 0;

        while (attempt < 3)
        {
            attempt++; object payloadObj;
            string url = _helperTarget.useWrapperEndpoints ? _helperTarget.apiKeysConfig.elevenLabsWrapperUrl : DirectElevenLabsApiEndpointBase + _helperTarget.selectedVoiceId;
            Dictionary<string, string> headers = new Dictionary<string, string>();

            if (_helperTarget.useWrapperEndpoints) payloadObj = new ExpElevenLabsWrapperPayload { text = _helperTarget.textToSpeak, voice_id = _helperTarget.selectedVoiceId, voice_settings = vs };
            else { payloadObj = new ExpElevenLabsPayload { text = _helperTarget.textToSpeak, voice_settings = vs }; headers.Add("xi-api-key", _helperTarget.apiKeysConfig.elevenLabsApiKey); headers.Add("Accept", "audio/mpeg"); }
            
            Debug.Log($"Exp Gen Attempt {attempt}: '{_helperTarget.textToSpeak.Substring(0, Mathf.Min(_helperTarget.textToSpeak.Length, 20))}...' Voice: {_helperTarget.selectedVoiceId}");
            try { audioData = await ApiCaller.PostJsonForBytesAsync(url, payloadObj, headers); }
            catch (System.Exception ex) { _statusMessage = $"Attempt {attempt} Error: {ex.Message}"; Debug.LogError(_statusMessage + "\n" + ex.StackTrace); audioData = null; }

            if (audioData != null && audioData.Length > 0) { File.WriteAllBytes(fullPath, audioData); AssetDatabase.Refresh(); _statusMessage = $"Saved: {assetRelPath}"; Debug.Log(_statusMessage); EditorUtility.SetDirty(target); return; }
            else { _statusMessage = $"Attempt {attempt} failed: No data."; Debug.LogWarning(_statusMessage); }
            if (attempt < 3) { _statusMessage += " Retrying..."; await Task.Delay(1000 * attempt); }
            GUI.changed = true;
        }
        _statusMessage = "Failed after 3 attempts."; Debug.LogError(_statusMessage); EditorUtility.SetDirty(target); GUI.changed = true;
    }
} 