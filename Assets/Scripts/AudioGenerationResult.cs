using System;

[Serializable]
public class AudioGenerationResult
{
    public string timestamp;
    public string originalText;
    public string textUsedForSpeech; // This was 'translatedText' in the event, or original if no translation
    public string audioDataBase64; // byte[] will be converted to base64 string for JSON
    public string errorMessage;
    public bool wasSuccess;
} 