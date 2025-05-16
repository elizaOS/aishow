using UnityEngine;

public class SharedMusicSpectrum : MonoBehaviour
{
    public static SharedMusicSpectrum Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource musicSource;

    [Header("Spectrum Data")]
    public float[] spectrumData = new float[1024];
    public float[] frequencyBands = new float[8];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (musicSource == null) return;
        musicSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        // Calculate frequency bands
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            float average = 0;
            int sampleCount = (int)Mathf.Pow(2, i) * 2;
            for (int j = 0; j < sampleCount; j++)
            {
                average += spectrumData[count] * (count + 1);
                count++;
            }
            average /= count;
            frequencyBands[i] = average * 10;
        }
    }
} 