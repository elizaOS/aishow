using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(YouTubeMetadataGenerator))]
public class YouTubeMetadataGeneratorEditor : Editor
{
    // Helper method to get language code, assuming YouTubeTranscriptGenerator is accessible and has GetLanguageCode
    // This is a simplified local version for editor logic; ideally, it mirrors YouTubeTranscriptGenerator's logic or calls it.
    // For direct use in editor, we need generatorScript.transcriptGeneratorReference to be non-null.
    private string GetEditorLanguageCode(YouTubeTranscriptGenerator transcriptGenerator, string languageName)
    {
        if (transcriptGenerator == null) return null; // Or handle error
        // This is a stand-in. We'd need to call transcriptGenerator.GetLanguageCode(languageName)
        // but that method is not static and requires an instance. For the editor, we assume the names are the primary ID.
        // The actual GetLanguageCode is in YouTubeTranscriptGenerator.cs and is instance-based.
        // For this editor, we primarily work with names from targetTranslationLanguages.
        // The critical part is that what we SAVE is a name, and what we COMPARE later might involve codes.

        // Let's try to use the actual GetLanguageCode method from the referenced script
        // Note: This is a bit of a workaround to call an instance method. 
        // A cleaner way might involve making GetLanguageCode static if it doesn't rely on instance state, 
        // or having a shared utility.
        // For now, we assume transcriptGenerator is the instance from generatorScript.transcriptGeneratorReference
        if (transcriptGenerator.GetType().GetMethod("GetLanguageCode") != null)
        {
            var method = transcriptGenerator.GetType().GetMethod("GetLanguageCode", new[] { typeof(string) });
            if (method != null)
            {
                return (string)method.Invoke(transcriptGenerator, new object[] { languageName });
            }
        }
        // Fallback if reflection fails or method not found (should not happen if script is correct)
        if (languageName.Equals("Chinese (Simplified)", System.StringComparison.OrdinalIgnoreCase)) return "ch";
        if (languageName.Equals("Korean", System.StringComparison.OrdinalIgnoreCase)) return "ko";
        return languageName; // Default to name if no code mapping here
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        YouTubeMetadataGenerator generatorScript = (YouTubeMetadataGenerator)target;

        // Draw API Configuration, Metadata Settings, Connections (excluding uploadLanguagePreference for now)
        DrawPropertiesExcluding(serializedObject, "m_Script", "uploadLanguagePreference", "lastGeneratedOriginalLLMPrompt", "lastGeneratedTranslatedLLMPrompt");

        // Custom dropdown for uploadLanguagePreference
        List<string> languageChoices = new List<string> { "Original", "None" };
        if (generatorScript.transcriptGeneratorReference != null && generatorScript.transcriptGeneratorReference.targetTranslationLanguages != null)
        {
            languageChoices.AddRange(generatorScript.transcriptGeneratorReference.targetTranslationLanguages.Where(lang => !string.IsNullOrEmpty(lang) && !languageChoices.Contains(lang)));
        }

        string currentStoredPreference = generatorScript.uploadLanguagePreference;
        int currentIndex = languageChoices.IndexOf(currentStoredPreference);

        if (currentIndex == -1) // Stored preference might be a code or an unlisted name
        {
            if (generatorScript.transcriptGeneratorReference != null)
            {
                for (int i = 0; i < languageChoices.Count; i++)
                {
                    string choiceName = languageChoices[i];
                    if (choiceName == "Original" || choiceName == "None") continue;

                    // Use reflection to call GetLanguageCode on the referenced instance
                    string codeForChoice = null;
                    try {
                        MethodInfo getCodeMethod = typeof(YouTubeTranscriptGenerator).GetMethod("GetLanguageCode", new[] { typeof(string) });
                        if (getCodeMethod != null) {
                            codeForChoice = (string)getCodeMethod.Invoke(generatorScript.transcriptGeneratorReference, new object[] { choiceName });
                        }
                    } catch (System.Exception ex) {
                        Debug.LogWarning($"Editor: Could not reflect GetLanguageCode: {ex.Message}");
                    }

                    if (codeForChoice != null && codeForChoice.Equals(currentStoredPreference, System.StringComparison.OrdinalIgnoreCase))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
            if (currentIndex == -1) // If still not found, default to "Original" or first if "Original" isn't there (should be)
            {
                currentIndex = languageChoices.IndexOf("Original");
                if (currentIndex == -1) currentIndex = 0;
            }
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Upload Language Preference", currentIndex, languageChoices.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            generatorScript.uploadLanguagePreference = languageChoices[newIndex];
            EditorUtility.SetDirty(generatorScript);
        }
        
        if (generatorScript.transcriptGeneratorReference == null)
        {
            EditorGUILayout.HelpBox("Assign the YouTubeTranscriptGenerator reference to ensure accurate language dropdown population and matching.", MessageType.Warning);
        }
        EditorGUILayout.Space();

        // Draw Debugging fields last
        EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lastGeneratedOriginalLLMPrompt"), GUILayout.Height(60));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lastGeneratedTranslatedLLMPrompt"), GUILayout.Height(60));

        serializedObject.ApplyModifiedProperties();
    }
} 