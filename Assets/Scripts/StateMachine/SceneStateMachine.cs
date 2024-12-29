using System;
using UnityEngine;

public class SceneStateMachine : MonoBehaviour
{
    /* // Enum to represent different scene states
    public enum SceneState
    {
        Idle,
        StartingSoon,
        RollIntro,
        MainScene,
        MainSceneLocation2,
        MainSceneLocation3,
        Commercial,
        Outro,
        Credits,
        DirectorMode
    }

    // Current state of the scene
    private SceneState currentState;

    private void OnEnable()
    {
        StateManager.OnSceneStateChanged += HandleSceneStateChange;
    }

    private void OnDisable()
    {
        StateManager.OnSceneStateChanged -= HandleSceneStateChange;
    }

    private void HandleSceneStateChange(string state)
    {
        // Try to parse the incoming state to SceneState enum
        if (Enum.TryParse(state, out SceneState newState))
        {
            currentState = newState;
            ExecuteStateAction(newState);
        }
        else
        {
            Debug.LogWarning($"Received an invalid state: {state}");
        }
    }

    private void ExecuteStateAction(SceneState state)
    {
        switch (state)
        {
            case SceneState.Idle:
                PrepareForNextAction();
                break;

            case SceneState.StartingSoon:
                StartCountdownOrIntro();
                break;

            case SceneState.RollIntro:
                PlayIntroVideoOrAnimation();
                break;

            case SceneState.MainScene:
                LoadMainScene();
                break;

            case SceneState.MainSceneLocation2:
                TransitionToLocation2();
                break;

            case SceneState.MainSceneLocation3:
                TransitionToLocation3();
                break;

            case SceneState.Commercial:
                StartCommercialBreak();
                break;

            case SceneState.Outro:
                PlayOutro();
                break;

            case SceneState.Credits:
                DisplayCredits();
                break;

            case SceneState.DirectorMode:
                ActivateDirectorMode();
                break;

            default:
                Debug.LogWarning("Unknown state in scene flow");
                break;
        }
    }

    private void PrepareForNextAction()
    {
        Debug.Log("Scene is idle, preparing for next action.");
        // Reset scene or prepare for the next action
    }

    private void StartCountdownOrIntro()
    {
        Debug.Log("Starting soon! Countdown begins or intro is shown.");
        // Show a countdown or intro animation before starting the next phase
    }

    private void PlayIntroVideoOrAnimation()
    {
        Debug.Log("Rolling intro...");
        // Play the introductory animation/video or scene transition
    }

    private void LoadMainScene()
    {
        Debug.Log("Loading main scene...");
        // Transition to the main gameplay scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }

    private void TransitionToLocation2()
    {
        Debug.Log("Transitioning to location 2...");
        // Logic to transition to location 2 in the main scene
    }

    private void TransitionToLocation3()
    {
        Debug.Log("Transitioning to location 3...");
        // Logic to transition to location 3 in the main scene
    }

    private void StartCommercialBreak()
    {
        Debug.Log("Starting commercial break...");
        // Pause game and play a commercial video or screen
    }

    private void PlayOutro()
    {
        Debug.Log("Playing outro...");
        // Play outro sequence
    }

    private void DisplayCredits()
    {
        Debug.Log("Displaying credits...");
        // Show the credits screen
    }

    private void ActivateDirectorMode()
    {
        Debug.Log("Activating Director Mode...");
        // Switch to a director mode, possibly for behind-the-scenes control or special editing mode
    } */
}
