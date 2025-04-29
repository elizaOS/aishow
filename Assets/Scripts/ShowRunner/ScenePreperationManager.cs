using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class ScenePreparationManager : MonoBehaviour
{
    public IntroSequenceManager introSequenceManagerRef;

    // Event to signal when a scene should be prepared
    public event Action<string> OnScenePrepareRequested;

    private void Awake()
    {
        // Ensure that introSequenceManagerRef is assigned at runtime if it's not already set in the inspector
        if (introSequenceManagerRef == null)
        {
            introSequenceManagerRef = FindObjectOfType<IntroSequenceManager>();
            if (introSequenceManagerRef == null)
            {
                Debug.LogError("IntroSequenceManager not found in the scene!");
            }
        }
    }

    // Method to trigger the scene preparation (this can be called by other scripts)
    public void RequestScenePreparation(string sceneName)
    {
        Debug.Log($"Scene preparation requested for: {sceneName}");
        OnScenePrepareRequested?.Invoke(sceneName);
    }

    public void HandlePrepareScene(string sceneName)
    {
        // Check if the scene is already loaded
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.Log($"Scene '{sceneName}' is already loaded. Playing intro sequence.");
            StartCoroutine(PlayIntroSequence(sceneName));
            return;
        }

        // Otherwise, proceed with the normal scene preparation process
        StartCoroutine(PrepareScene(sceneName));
    }

    private IEnumerator PlayIntroSequence(string sceneName)
    {
        // Play the intro sequence
        yield return StartCoroutine(introSequenceManagerRef.StartIntroSequence());
        Debug.Log($"Intro sequence completed for scene: {sceneName}");
    }

    private IEnumerator PrepareScene(string sceneName)
    {
        // Play the intro sequence first
        yield return StartCoroutine(introSequenceManagerRef.StartIntroSequence());
        
        // Then load the scene
        yield return StartCoroutine(LoadSceneAsync(sceneName));
        
        Debug.Log($"Scene preparation completed for: {sceneName}");
    }

    // Method to load a scene asynchronously and check when it is finished
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Check if the scene is valid and included in the build settings
        if (SceneUtility.GetBuildIndexByScenePath(sceneName) >= 0)
        {
            Debug.Log($"Loading scene: {sceneName}");

            // Asynchronously load the scene
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;

            // Wait until the scene is fully loaded
            while (!asyncOperation.isDone)
            {
                Debug.Log($"Loading scene: {sceneName} ({asyncOperation.progress * 100}%)");

                // Check if the scene loading is almost complete (90% progress)
                if (asyncOperation.progress >= 0.9f)
                {
                    Debug.Log("Scene loading is almost complete (90%)...");
                    // Activate the scene when it's ready
                    asyncOperation.allowSceneActivation = true;
                }

                // Wait until the next frame
                yield return null;
            }
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' not found in the build settings.");
        }
    }
}
