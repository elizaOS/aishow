using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using System;

[Serializable]
public class IntroStep
{
    public GameObject stepObject;
}

public class IntroSequenceManager : MonoBehaviour
{
    public IntroStep[] introSteps;
    private int currentStepIndex = 0;
    private static IntroSequenceManager instance;
    public bool isEnabled = true; // Enables or disables the intro sequence

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        foreach (var step in introSteps)
        {
            if (step?.stepObject != null)
            {
                step.stepObject.SetActive(false);
            }
        }
    }

    public void StartTestIntroSequence()
    {
        StartCoroutine(StartIntroSequence());
    }

    public IEnumerator StartIntroSequence()
    {
        if (!isEnabled)
        {
            Debug.Log("Intro sequence is disabled. Skipping.");
            OnSequenceComplete();
            yield break;
        }

        if (introSteps.Length > 0)
        {
            currentStepIndex = 0;
            yield return StartCoroutine(PlayIntroSequence());
        }
        else
        {
            Debug.LogWarning("No intro steps assigned!");
            OnSequenceComplete();
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        while (currentStepIndex < introSteps.Length)
        {
            IntroStep currentStep = introSteps[currentStepIndex];

            if (currentStep?.stepObject == null)
            {
                currentStepIndex++;
                continue;
            }

            Debug.Log($"Starting step {currentStepIndex}: {currentStep.stepObject.name}");

            // Activate the current step
            currentStep.stepObject.SetActive(true);

            // Wait for 2 frames to ensure the new step is fully rendered before deactivating the previous step
           

            // Deactivate the previous step
            if (currentStepIndex > 0 && introSteps[currentStepIndex - 1]?.stepObject != null)
            {
              
                introSteps[currentStepIndex - 1].stepObject.SetActive(false);
            }

            // Check for Animator component and play animation if present
            Animator animator = currentStep.stepObject.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                float animationLength = GetAnimationClipLength(animator);
                if (animationLength > 0)
                {
                    animator.Play(0); // Play the default animation state
                    yield return new WaitForSeconds(animationLength);
                }
                else
                {
                    Debug.LogWarning($"Animator on {currentStep.stepObject.name} has no valid animation clip.");
                    yield return new WaitForSeconds(3f); // Default wait time
                }
            }
            else
            {
                // Check for VideoPlayer and play video if present
                VideoPlayer videoPlayer = currentStep.stepObject.GetComponent<VideoPlayer>();
                if (videoPlayer != null)
                {
                    // Clear the RenderTexture to avoid displaying stale frames
                    if (videoPlayer.targetTexture != null)
                    {
                        RenderTexture renderTexture = videoPlayer.targetTexture;
                        RenderTexture.active = renderTexture;
                        GL.Clear(true, true, Color.black);
                        RenderTexture.active = null;
                    }

                    // Reset and prepare the video
                    videoPlayer.Stop();
                    videoPlayer.frame = 0;
                    videoPlayer.Prepare();
                    yield return new WaitUntil(() => videoPlayer.isPrepared);

                    // Play the video and wait for the first frame to update
                    videoPlayer.Play();
                    yield return new WaitUntil(() => videoPlayer.frame > 0);

                    // Wait for the video to finish playing
                    yield return new WaitUntil(() => !videoPlayer.isPlaying);

                    // Cleanup video player
                    videoPlayer.Stop();
                    videoPlayer.frame = 0;
                    videoPlayer.targetTexture.Release();
                }
                else
                {
                    Debug.LogWarning($"No video or animation detected for step {currentStepIndex}. Defaulting to 3-second wait.");
                    yield return new WaitForSeconds(3f);
                }
            }

            Debug.Log($"Step {currentStepIndex} completed.");
            currentStepIndex++;
        }

        OnSequenceComplete();
    }


    private float GetAnimationClipLength(Animator animator)
    {
        // Get the runtime animator controller
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null || controller.animationClips.Length == 0)
        {
            return 0f;
        }

        // Return the length of the first animation clip
        return controller.animationClips[0].length;
    }

    private void OnSequenceComplete()
    {
        Debug.Log("Intro sequence completed.");

        // Deactivate the last step if it exists
        if (introSteps.Length > 0 && introSteps[currentStepIndex - 1]?.stepObject != null)
        {
            introSteps[currentStepIndex - 1].stepObject.SetActive(false);
        }

        // Reset VideoPlayers in all steps
        foreach (var step in introSteps)
        {
            if (step?.stepObject != null)
            {
                VideoPlayer videoPlayer = step.stepObject.GetComponent<VideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.Stop();
                    videoPlayer.frame = 0;
                    if (videoPlayer.targetTexture != null)
                    {
                        videoPlayer.targetTexture.Release();
                    }
                }
            }
        }

        // Optionally, disable the intro sequence after completion
        isEnabled = false;
    }
}
