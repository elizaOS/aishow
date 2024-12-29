using UnityEngine;
using System.IO;

public class FirebaseConfig : MonoBehaviour
{
    private static FirebaseConfig instance;
    public static FirebaseConfig Instance => instance;

    public string InputsUrl { get; private set; }
    public string OutputsBaseUrl { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Ensure persistence
            LoadConfiguration();
        }
        else
        {
            Destroy(gameObject); // Avoid duplicate instances
        }
    }

    private void LoadConfiguration()
    {
        // Path to the .env file
        string envPath = Path.Combine(Application.dataPath, "../.env");

        // Check if the file exists
        if (!File.Exists(envPath))
        {
            Debug.LogError("Missing .env file at path: " + envPath);
            return;
        }

        //Debug.Log("Loading configuration from .env file...");
        try
        {
            // Read each line from the .env file
            foreach (string line in File.ReadAllLines(envPath))
            {
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                // Split key-value pairs
                string[] parts = line.Split(new[] { '=' }, 2); // Allow '=' in the value
                if (parts.Length != 2)
                {
                    Debug.LogWarning($"Skipping malformed line in .env file: {line}");
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                // Match keys and set properties
                switch (key)
                {
                    case "FIREBASE_INPUTS_URL":
                        InputsUrl = value;
                        break;
                    case "FIREBASE_OUTPUTS_BASE_URL":
                        OutputsBaseUrl = value;
                        break;
                    default:
                        Debug.LogWarning($"Unknown key in .env file: {key}");
                        break;
                }
            }

            // Check if all required variables were loaded
            if (string.IsNullOrEmpty(InputsUrl))
                Debug.LogError("FIREBASE_INPUTS_URL is missing in .env file.");
            if (string.IsNullOrEmpty(OutputsBaseUrl))
                Debug.LogError("FIREBASE_OUTPUTS_BASE_URL is missing in .env file.");

            //Debug.Log($"Configuration loaded successfully: InputsUrl={InputsUrl}, OutputsBaseUrl={OutputsBaseUrl}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error reading .env file: {ex.Message}");
        }
    }
}
