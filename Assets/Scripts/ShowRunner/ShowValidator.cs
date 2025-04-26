using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ShowRunner
{
    public class ShowValidator : MonoBehaviour
    {
        [SerializeField] private ShowRunner showRunner;
        [SerializeField] private string episodesRootPath = "Episodes";
        [SerializeField] private TextMeshProUGUI validationResultText;
        [SerializeField] private Button validateButton;
        [SerializeField] private Button fixButton;

        private ShowData showData;
        private List<string> missingAudioFiles = new List<string>();
        private List<string> validationMessages = new List<string>();

        private void Start()
        {
            if (validateButton != null)
                validateButton.onClick.AddListener(StartValidation);
                
            if (fixButton != null)
            {
                fixButton.onClick.AddListener(FixAudioFiles);
                fixButton.interactable = false; // Disable until validation runs
            }
        }

        public void StartValidation()
        {
            if (showRunner == null)
            {
                showRunner = FindObjectOfType<ShowRunner>();
                if (showRunner == null)
                {
                    Debug.LogError("ShowRunner not found! Validation cannot proceed.");
                    AddValidationMessage("ERROR: ShowRunner not found!");
                    return;
                }
            }

            ClearResults();
            StartCoroutine(ValidateShowData());
        }

        private IEnumerator ValidateShowData()
        {
            // Get the show data from the ShowRunner
            showData = showRunner.GetShowData();
            
            if (showData == null)
            {
                AddValidationMessage("ERROR: No show data found! Make sure to load the show data first.");
                yield break;
            }

            AddValidationMessage($"Validating show: {showData.Config.Name}");
            AddValidationMessage($"Found {showData.Episodes.Count} episodes");

            yield return StartCoroutine(ValidateAudioFiles());

            // Display validation summary
            if (missingAudioFiles.Count > 0)
            {
                AddValidationMessage($"VALIDATION FAILED: {missingAudioFiles.Count} audio files missing!");
                
                // Only display the first 10 missing files to avoid overwhelming the UI
                int displayCount = Mathf.Min(missingAudioFiles.Count, 10);
                for (int i = 0; i < displayCount; i++)
                {
                    AddValidationMessage($"  - {missingAudioFiles[i]}");
                }
                
                if (missingAudioFiles.Count > 10)
                {
                    AddValidationMessage($"  ... and {missingAudioFiles.Count - 10} more missing files");
                }
                
                // Enable fix button
                if (fixButton != null)
                    fixButton.interactable = true;
            }
            else
            {
                AddValidationMessage("VALIDATION SUCCESSFUL: All audio files found!");
                
                // Disable fix button since no fixes needed
                if (fixButton != null)
                    fixButton.interactable = false;
            }
        }

        private IEnumerator ValidateAudioFiles()
        {
            missingAudioFiles.Clear();
            int filesChecked = 0;
            
            foreach (var episode in showData.Episodes)
            {
                AddValidationMessage($"Checking episode: {episode.Id} - {episode.Title}");
                
                // Check if episode folder exists under Resources
                string episodeFolder = Path.Combine(Application.dataPath, "Resources", episodesRootPath, episode.Id);
                string audioFolder = Path.Combine(episodeFolder, "audio");
                
                if (!Directory.Exists(episodeFolder))
                {
                    AddValidationMessage($"Episode folder not found in Resources: {episodeFolder}");
                    missingAudioFiles.Add($"{episode.Id} (folder missing)");
                    continue;
                }
                
                if (!Directory.Exists(audioFolder))
                {
                    AddValidationMessage($"Audio folder not found in Resources: {audioFolder}");
                    missingAudioFiles.Add($"{episode.Id}/audio (folder missing)");
                    continue;
                }
                
                // Check each audio file
                for (int sceneIndex = 0; sceneIndex < episode.Scenes.Count; sceneIndex++)
                {
                    var scene = episode.Scenes[sceneIndex];
                    
                    for (int dialogueIndex = 0; dialogueIndex < scene.Dialogue.Count; dialogueIndex++)
                    {
                        // Construct the audio file path
                        string audioFileName = $"{episode.Id}_{sceneIndex + 1}_{dialogueIndex + 1}.mp3";
                        string audioFilePath = Path.Combine(audioFolder, audioFileName);
                        
                        // Try Resources folder first
                        string resourcePath = $"{episodesRootPath}/{episode.Id}/audio/{audioFileName}".Replace(".mp3", "");
                        AudioClip resourceClip = Resources.Load<AudioClip>(resourcePath);
                        
                        if (resourceClip == null && !File.Exists(audioFilePath))
                        {
                            missingAudioFiles.Add(audioFileName);
                        }
                        
                        filesChecked++;
                        
                        // Yield every 20 files to avoid blocking the main thread
                        if (filesChecked % 20 == 0)
                        {
                            AddValidationMessage($"Checked {filesChecked} files so far...");
                            yield return null;
                        }
                    }
                }
            }
            
            AddValidationMessage($"Finished checking {filesChecked} audio files");
        }

        public void FixAudioFiles()
        {
            StartCoroutine(CreatePlaceholderAudioFiles());
        }

        private IEnumerator CreatePlaceholderAudioFiles()
        {
            if (missingAudioFiles.Count == 0)
            {
                AddValidationMessage("No missing files to fix.");
                yield break;
            }

            AddValidationMessage("Creating placeholder directories and audio files...");
            int filesCreated = 0;

            // Get a placeholder audio file to copy
            AudioClip placeholderClip = Resources.Load<AudioClip>("placeholder_audio");
            if (placeholderClip == null)
            {
                AddValidationMessage("ERROR: No placeholder audio found in Resources/placeholder_audio");
                yield break;
            }

            foreach (var episode in showData.Episodes)
            {
                // Create episode directory if needed
                string episodeFolder = Path.Combine(Application.dataPath, episodesRootPath, episode.Id);
                if (!Directory.Exists(episodeFolder))
                {
                    Directory.CreateDirectory(episodeFolder);
                    AddValidationMessage($"Created episode directory: {episode.Id}");
                }
                
                // Create audio subdirectory if needed
                string audioFolder = Path.Combine(episodeFolder, "audio");
                if (!Directory.Exists(audioFolder))
                {
                    Directory.CreateDirectory(audioFolder);
                    AddValidationMessage($"Created audio directory: {episode.Id}/audio");
                }
                
                // Create placeholder audio files
                for (int sceneIndex = 0; sceneIndex < episode.Scenes.Count; sceneIndex++)
                {
                    var scene = episode.Scenes[sceneIndex];
                    
                    for (int dialogueIndex = 0; dialogueIndex < scene.Dialogue.Count; dialogueIndex++)
                    {
                        string audioFileName = $"{episode.Id}_{sceneIndex + 1}_{dialogueIndex + 1}.mp3";
                        string audioFilePath = Path.Combine(audioFolder, audioFileName);
                        
                        if (!File.Exists(audioFilePath))
                        {
                            // In a real implementation, you would create audio files here
                            // For demonstration purposes, we'll just log that we would create it
                            #if UNITY_EDITOR
                            // Copy placeholder audio to the target location
                            string placeholderPath = UnityEditor.AssetDatabase.GetAssetPath(placeholderClip);
                            if (!string.IsNullOrEmpty(placeholderPath))
                            {
                                File.Copy(placeholderPath, audioFilePath);
                                filesCreated++;
                                
                                if (filesCreated % 10 == 0)
                                {
                                    AddValidationMessage($"Created {filesCreated} placeholder files");
                                    yield return null;
                                }
                            }
                            #else
                            AddValidationMessage($"Would create: {audioFilePath}");
                            filesCreated++;
                            #endif
                        }
                    }
                }
            }
            
            AddValidationMessage($"Fix complete. Created {filesCreated} placeholder audio files.");
            
            #if UNITY_EDITOR
            // Refresh AssetDatabase after creating files
            UnityEditor.AssetDatabase.Refresh();
            #endif
            
            // Run validation again to verify the fixes
            yield return new WaitForSeconds(1.0f);
            StartValidation();
        }

        private void ClearResults()
        {
            validationMessages.Clear();
            missingAudioFiles.Clear();
            UpdateValidationUI();
        }

        private void AddValidationMessage(string message)
        {
            Debug.Log(message);
            validationMessages.Add(message);
            UpdateValidationUI();
        }

        private void UpdateValidationUI()
        {
            if (validationResultText != null)
            {
                validationResultText.text = string.Join("\n", validationMessages);
            }
        }
    }
} 