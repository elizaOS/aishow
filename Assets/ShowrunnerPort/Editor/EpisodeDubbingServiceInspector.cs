using UnityEngine;
using UnityEditor;
using System.IO; // Required for Path operations
using System.Linq; // For LINQ operations on arrays for popup
using System.Collections.Generic; // For List

namespace ShowGenerator
{
    [CustomEditor(typeof(EpisodeDubbingService))]
    public class EpisodeDubbingServiceInspector : Editor
    {
        private EpisodeDubbingService service;

        // Input fields for Create Dubbing Job
        private string audioFilePath = "";
        private string projectName = "";
        private int numSpeakers = 0;
        private bool watermark = false;
        private int startTime = 0;
        private int endTime = 0;
        private bool highestResolution = false;
        private bool dropBackgroundAudio = false;
        private bool useProfanityFilter = false;
        private bool dubbingStudio = false;
        private bool disableVoiceCloning = false;
        private int selectedModeIndex = 0; // 0: Automatic, 1: Manual
        private static readonly string[] modeOptions = { "Automatic", "Manual" };
        private bool showAdvancedOptions = false;

        // Input fields for Get Dubbed Audio / Transcript
        private string saveAudioPath = "Assets/DubbedAudio.mp3";
        private enum TranscriptFormat { srt, webvtt }
        private TranscriptFormat transcriptFormatType = TranscriptFormat.srt;
        private Vector2 transcriptScrollPos;

        // Language selection
        private static readonly string[] supportedLanguageDisplayNames = { 
            "Auto Detect", "English", "Spanish", "Korean", "Chinese (Simplified)", "French", "German", "Japanese", 
            "Portuguese", "Italian", "Russian", "Hindi", "Arabic", "Polish", "Dutch", "Swedish", "Turkish", "Indonesian"
        };
        private static readonly string[] supportedLanguageCodesForAPI = { 
            "auto", "en", "es", "ko", "zh-CN", "fr", "de", "ja", "pt", "it", "ru", "hi", "ar", "pl", "nl", "sv", "tr", "id"
        };

        private int selectedTargetLangIndex = 3; // Default to Korean (ko), assuming "Auto Detect" is at index 0
        private int selectedSourceLangIndex = 0; // Default to "Auto Detect"
        private int selectedAudioLangIndex = 3;  
        private int selectedTranscriptLangIndex = 3;
        
        // To store the active dubbing ID from creation, used by other sections
        private string activeDubbingId = "";

        private void OnEnable()
        {
            service = (EpisodeDubbingService)target;
            // Ensure default language indices are valid considering the array sizes
            selectedTargetLangIndex = Mathf.Clamp(selectedTargetLangIndex, 0, supportedLanguageDisplayNames.Length - 1);
            selectedSourceLangIndex = Mathf.Clamp(selectedSourceLangIndex, 0, supportedLanguageDisplayNames.Length - 1);
            selectedAudioLangIndex = Mathf.Clamp(selectedAudioLangIndex, 0, supportedLanguageDisplayNames.Length - 1);
            selectedTranscriptLangIndex = Mathf.Clamp(selectedTranscriptLangIndex, 0, supportedLanguageDisplayNames.Length - 1);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("apiKeys"));

            if (service.apiKeys == null)
            {
                EditorGUILayout.HelpBox("Please assign the ShowGeneratorApiKeys asset.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            if (string.IsNullOrEmpty(service.apiKeys.elevenLabsApiKey))
            {
                EditorGUILayout.HelpBox("ElevenLabs API Key is not set in the assigned ShowGeneratorApiKeys asset.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // --- 1. Create Dubbing Job Section ---
            EditorGUILayout.Space();
            GUILayout.Label("1. Create Dubbing Job", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            audioFilePath = EditorGUILayout.TextField(new GUIContent("Audio/Video File Path", "Path to the local audio or video file to be dubbed."), audioFilePath);
            if (GUILayout.Button("Browse...", GUILayout.Width(75)))
            {
                string path = EditorUtility.OpenFilePanel("Select Audio or Video File", "", "mp3,wav,m4a,mp4,mov,avi");
                if (!string.IsNullOrEmpty(path)) audioFilePath = path;
            }
            EditorGUILayout.EndHorizontal();
            
            selectedTargetLangIndex = EditorGUILayout.Popup(new GUIContent("Target Language", "The language to dub the content into."), selectedTargetLangIndex, supportedLanguageDisplayNames);
            string targetLanguageCode = supportedLanguageCodesForAPI[selectedTargetLangIndex];

            projectName = EditorGUILayout.TextField(new GUIContent("Project Name (Optional)", "An optional name for the dubbing project."), projectName);
            selectedSourceLangIndex = EditorGUILayout.Popup(new GUIContent("Source Language (Optional)", "The original language of the file. Defaults to auto-detection by the API."), selectedSourceLangIndex, supportedLanguageDisplayNames);
            string sourceLanguageCode = supportedLanguageCodesForAPI[selectedSourceLangIndex];

            numSpeakers = EditorGUILayout.IntField(new GUIContent("Number of Speakers", "Number of speakers to use for dubbing. Set to 0 to automatically detect."), numSpeakers);

            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                watermark = EditorGUILayout.Toggle(new GUIContent("Add Watermark", "Whether to apply a watermark to the output video (if applicable)."), watermark);
                startTime = EditorGUILayout.IntField(new GUIContent("Start Time (seconds)", "Optional. Start time of the source video/audio to dub, in seconds from the beginning."), startTime);
                endTime = EditorGUILayout.IntField(new GUIContent("End Time (seconds)", "Optional. End time of the source video/audio to dub, in seconds from the beginning."), endTime);
                highestResolution = EditorGUILayout.Toggle(new GUIContent("Use Highest Resolution", "Whether to use the highest resolution available for video dubbing."), highestResolution);
                dropBackgroundAudio = EditorGUILayout.Toggle(new GUIContent("Drop Background Audio", "Advanced: Whether to drop background audio from the final dub. Can improve quality for speeches/monologues."), dropBackgroundAudio);
                useProfanityFilter = EditorGUILayout.Toggle(new GUIContent("[BETA] Use Profanity Filter", "Whether transcripts should have profanities censored with '[censored]'."), useProfanityFilter);
                dubbingStudio = EditorGUILayout.Toggle(new GUIContent("Prepare for Dubbing Studio", "Whether to prepare the dub for edits in Dubbing Studio or as a dubbing resource."), dubbingStudio);
                disableVoiceCloning = EditorGUILayout.Toggle(new GUIContent("[BETA] Disable Voice Cloning", "Instead of voice cloning, use a similar voice from the ElevenLabs Voice Library."), disableVoiceCloning);
                selectedModeIndex = EditorGUILayout.Popup(new GUIContent("Mode", "'Automatic' or 'Manual'. Manual mode is only supported when creating a Dubbing Studio project."),selectedModeIndex, modeOptions);
                EditorGUI.indentLevel--;
            }

            if (GUILayout.Button("Create Dubbing Job"))
            {
                string mode = modeOptions[selectedModeIndex].ToLower();
                HandleCreateDubbingJob(targetLanguageCode, sourceLanguageCode, mode);
            }
            if (!string.IsNullOrEmpty(service.creationResult))
                EditorGUILayout.HelpBox(service.creationResult, MessageType.Info);

            // --- 2. Get Dubbing Status Section ---
            EditorGUILayout.Space();
            GUILayout.Label("2. Get Dubbing Status", EditorStyles.boldLabel);
            activeDubbingId = EditorGUILayout.TextField(new GUIContent("Dubbing ID", "The ID of the dubbing project to check."), activeDubbingId);
            if (GUILayout.Button("Get Status"))
            {
                HandleGetDubbingStatus();
            }
            if (!string.IsNullOrEmpty(service.statusResult))
                EditorGUILayout.HelpBox(service.statusResult, MessageType.Info);

            // --- 3. Get Dubbed Audio File Section ---
            EditorGUILayout.Space();
            GUILayout.Label("3. Get Dubbed Audio File", EditorStyles.boldLabel);
            selectedAudioLangIndex = EditorGUILayout.Popup(new GUIContent("Language Code", "The language code of the dubbed audio to retrieve. Must be one of the target languages of the dubbing project."), selectedAudioLangIndex, supportedLanguageDisplayNames.Skip(1).ToArray()); // Skip "Auto Detect"
            string audioLangCode = supportedLanguageCodesForAPI[selectedAudioLangIndex + 1]; // Adjust index due to Skip

            EditorGUILayout.BeginHorizontal();
            saveAudioPath = EditorGUILayout.TextField(new GUIContent("Save Audio/Video Path", "Local path to save the downloaded dubbed file."), saveAudioPath);
            if (GUILayout.Button("Browse...", GUILayout.Width(75)))
            {
                string suggestedName = string.IsNullOrEmpty(activeDubbingId) ? "dubbed_file" : $"{activeDubbingId}_{audioLangCode}";
                string defaultExtension = "mp3"; // Default for audio output
                if (!string.IsNullOrEmpty(audioFilePath)) {
                    string originalExt = Path.GetExtension(audioFilePath).ToLower();
                    if (originalExt == ".mp4" || originalExt == ".mov" || originalExt == ".avi") defaultExtension = "mp4"; // If source was video, suggest mp4
                }

                string path = EditorUtility.SaveFilePanel("Save Dubbed File As...", 
                                                        string.IsNullOrEmpty(saveAudioPath) ? Application.dataPath : Path.GetDirectoryName(saveAudioPath), 
                                                        suggestedName,
                                                        defaultExtension);
                if (!string.IsNullOrEmpty(path)) saveAudioPath = path;
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Get Dubbed Audio/Video"))
            {
                HandleGetDubbedAudioFile(audioLangCode);
            }
            if (!string.IsNullOrEmpty(service.getAudioResult))
                EditorGUILayout.HelpBox(service.getAudioResult, MessageType.Info);

            // --- 4. Get Dubbed Transcript Section ---
            EditorGUILayout.Space();
            GUILayout.Label("4. Get Dubbed Transcript", EditorStyles.boldLabel);
            selectedTranscriptLangIndex = EditorGUILayout.Popup(new GUIContent("Language Code", "The language code of the transcript to retrieve."), selectedTranscriptLangIndex, supportedLanguageDisplayNames.Skip(1).ToArray()); // Skip "Auto Detect"
            string transcriptLangCode = supportedLanguageCodesForAPI[selectedTranscriptLangIndex + 1]; // Adjust index
            transcriptFormatType = (TranscriptFormat)EditorGUILayout.EnumPopup(new GUIContent("Format Type", "The format for the subtitle file (SRT or WebVTT)."), transcriptFormatType);
            if (GUILayout.Button("Get Dubbed Transcript"))
            {
                HandleGetDubbedTranscript(transcriptLangCode);
            }
            if (!string.IsNullOrEmpty(service.getTranscriptResult))
                EditorGUILayout.HelpBox(service.getTranscriptResult, MessageType.Info);
            
            if (!string.IsNullOrEmpty(service.transcriptContent))
            {
                EditorGUILayout.LabelField("Transcript Content:", EditorStyles.miniBoldLabel);
                transcriptScrollPos = EditorGUILayout.BeginScrollView(transcriptScrollPos, GUILayout.Height(100));
                EditorGUILayout.TextArea(service.transcriptContent, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleCreateDubbingJob(string targetLangCode, string sourceLangCode, string mode)
        {
            if (string.IsNullOrEmpty(audioFilePath) || string.IsNullOrEmpty(targetLangCode) || targetLangCode.ToLower() == "auto")
            {
                service.creationResult = "Error: Audio/Video file path and a specific Target Language must be selected.";
                return;
            }
            service.creationResult = "Processing..."; 
            service.CallCreateDubbingJob(audioFilePath, targetLangCode, projectName, sourceLangCode, numSpeakers,
                                           watermark, startTime, endTime, highestResolution, dropBackgroundAudio,
                                           useProfanityFilter, dubbingStudio, disableVoiceCloning, mode,
                (id, expectedDuration, error) => {
                    if (!string.IsNullOrEmpty(id))
                    {
                        activeDubbingId = id;
                        int langIndex = System.Array.IndexOf(supportedLanguageCodesForAPI, targetLangCode);
                        if(langIndex != -1 && langIndex > 0) { // Ensure it's not "auto" for audio/transcript lang defaults
                            selectedAudioLangIndex = langIndex -1; // Adjust for skipped "Auto Detect"
                            selectedTranscriptLangIndex = langIndex -1; // Adjust for skipped "Auto Detect"
                        }
                    }
                });
        }

        private void HandleGetDubbingStatus()
        {
            if (string.IsNullOrEmpty(activeDubbingId))
            {
                service.statusResult = "Error: Dubbing ID cannot be empty. Create a job first or enter an ID.";
                return;
            }
            service.statusResult = "Processing...";
            service.CallGetDubbingStatus(activeDubbingId, (status, error) => { });
        }

        private void HandleGetDubbedAudioFile(string audioLangCode)
        {
            if (string.IsNullOrEmpty(activeDubbingId) || string.IsNullOrEmpty(audioLangCode) || string.IsNullOrEmpty(saveAudioPath) || audioLangCode.ToLower() == "auto")
            {
                service.getAudioResult = "Error: Dubbing ID, a specific Language Code, and Save Path cannot be empty.";
                return;
            }
            service.getAudioResult = "Processing...";
            service.CallGetDubbedAudioFile(activeDubbingId, audioLangCode, saveAudioPath,
                (success, message) => {
                    if (success)
                    {
                        if (saveAudioPath.StartsWith("Assets") || saveAudioPath.StartsWith(Application.dataPath))
                        {
                            AssetDatabase.Refresh(); 
                        }
                    }
                });
        }

        private void HandleGetDubbedTranscript(string transcriptLangCode)
        {
            if (string.IsNullOrEmpty(activeDubbingId) || string.IsNullOrEmpty(transcriptLangCode) || transcriptLangCode.ToLower() == "auto")
            {
                service.getTranscriptResult = "Error: Dubbing ID and a specific Language Code cannot be empty.";
                service.transcriptContent = "";
                return;
            }
            service.getTranscriptResult = "Processing...";
            service.transcriptContent = ""; 
            service.CallGetDubbedTranscript(activeDubbingId, transcriptLangCode, transcriptFormatType.ToString(),
                (content, error) => { });
        }
    }
} 