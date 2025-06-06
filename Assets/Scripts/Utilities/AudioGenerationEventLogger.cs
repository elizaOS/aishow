using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AudioGenerationEventLogger : MonoBehaviour
{
    public string outputSubfolder = "AudioGenerationLogs";

    public void LogAudioGenerationResult(string originalText, string textUsedForSpeech, byte[] audioData, string errorMessage)
    {
        AudioGenerationResult result = new AudioGenerationResult
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"),
            originalText = originalText,
            textUsedForSpeech = textUsedForSpeech,
            audioDataBase64 = (audioData != null && audioData.Length > 0) ? Convert.ToBase64String(audioData) : null,
            errorMessage = string.IsNullOrEmpty(errorMessage) ? null : errorMessage,
            wasSuccess = (audioData != null && audioData.Length > 0) && string.IsNullOrEmpty(errorMessage)
        };

        string jsonPayload = JsonUtility.ToJson(result, true);
        SavePayloadToFile(jsonPayload, result.timestamp);
    }

    private void SavePayloadToFile(string jsonPayload, string timestamp)
    {
        string rootPath = Application.dataPath; // Assets folder
        string folderPath = Path.Combine(rootPath, outputSubfolder);

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Debug.Log($"Created directory for audio generation logs: {folderPath}");
            }

            string fileName = $"audio_gen_log_{timestamp}.json";
            string filePath = Path.Combine(folderPath, fileName);

            File.WriteAllText(filePath, jsonPayload);
            Debug.Log($"Audio generation payload saved to: {filePath.Replace(Application.dataPath, "Assets")}");

#if UNITY_EDITOR
            AssetDatabase.Refresh(); // Refresh AssetDatabase to show the new file in Project window
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save audio generation payload: {ex.Message}");
        }
    }
} 