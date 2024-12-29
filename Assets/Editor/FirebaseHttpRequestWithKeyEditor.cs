using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FirebaseHttpRequestWithKey))]
public class FirebaseHttpRequestWithKeyEditor : Editor
{
    private FirebaseHttpRequestWithKey firebaseScript;
    private ScenePreparationManager scenePreparationManager;

    private void OnEnable()
    {
        firebaseScript = (FirebaseHttpRequestWithKey)target;
        scenePreparationManager = firebaseScript.GetComponent<ScenePreparationManager>();

        if (scenePreparationManager != null)
        {
            // Subscribe to the scene preparation event
            scenePreparationManager.OnScenePrepareRequested += HandleScenePrepareRequested;
        }
    }

    private void OnDisable()
    {
        if (scenePreparationManager != null)
        {
            // Unsubscribe from the event to avoid memory leaks
            scenePreparationManager.OnScenePrepareRequested -= HandleScenePrepareRequested;
        }
    }

    public override void OnInspectorGUI()
    {
        // Display the Firebase URL for inputs
        //EditorGUILayout.LabelField("Inputs Firebase URL", EditorStyles.boldLabel);
        //firebaseScript.InputsUrl = EditorGUILayout.TextField("Inputs URL", firebaseScript.InputsUrl);

        // Display the Outputs URL base
        //EditorGUILayout.LabelField("Outputs Firebase URL Base", EditorStyles.boldLabel);
        //firebaseScript.OutputsBaseUrl = EditorGUILayout.TextField("Outputs Base URL", firebaseScript.OutputsBaseUrl);

        // Display polling interval for fetching inputs
        EditorGUILayout.LabelField("Polling Interval", EditorStyles.boldLabel);
        firebaseScript.pollingInterval = EditorGUILayout.FloatField("Polling Interval (Seconds)", firebaseScript.pollingInterval);

        // Section header for polling control
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Polling Control", EditorStyles.boldLabel);

        // Start/Stop Polling Buttons
        if (GUILayout.Button("Start Polling"))
        {
            firebaseScript.StartPollingInputs();
        }

        if (GUILayout.Button("Stop Polling"))
        {
            firebaseScript.StopPollingInputs();
        }

        // Scene preparation simulation
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Preparation", EditorStyles.boldLabel);

        // Display the message about scene preparation callback
        if (scenePreparationManager != null)
        {
            EditorGUILayout.LabelField("Scene Load Callback is Assigned", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.LabelField("No Scene Load Callback Assigned", EditorStyles.label);
        }

        if (GUILayout.Button("Simulate PrepareScene Event"))
        {
            var testEventData = new EventData { type = "prepareScene", location = "TestScene" };
            firebaseScript.HandleInputsEvent("{\"inputs\": {\"testEvent\": " + JsonUtility.ToJson(testEventData) + "}}");
        }

        // Current polling status
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Polling Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Polling is {(firebaseScript.IsPolling ? "Active" : "Inactive")}", EditorStyles.label);

        // Output event section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Send Output Event", EditorStyles.boldLabel);

        if (GUILayout.Button("Write 'prepareSceneComplete' to Outputs"))
        {
            // Now call the method on ScenePreparationManager, not FirebaseHttpRequestWithKey
            if (scenePreparationManager != null)
            {
                scenePreparationManager.WritePrepareSceneCompleteEvent("TestScene");
            }
        }

        // Add space before ending the inspector
        EditorGUILayout.Space();
        if (GUILayout.Button("Stop"))
        {
            Debug.Log("Test stopped.");
        }

        // Mark as dirty
        EditorUtility.SetDirty(firebaseScript);
    }

    // Handle the scene preparation request event
    private void HandleScenePrepareRequested(string sceneName)
    {
        Debug.Log($"Scene {sceneName} is being prepared.");
        // Here you can handle what happens when the scene is requested to be prepared
    }
}
