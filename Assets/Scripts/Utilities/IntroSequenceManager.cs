using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;

[Serializable]
public class IntroStep
{
    public GameObject stepObject;
    public bool useFadeIn = true;
    public bool useFadeOut = true;
}

public class IntroSequenceManager : MonoBehaviour
{
    public IntroStep[] introSteps;
    private int currentStepIndex = 0;
    private static IntroSequenceManager instance;
    public Image fadeOverlay;
    public bool useFade = true; // Global fade toggle remains as failsafe
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

        // Set fade overlay to be on top of everything
        if (fadeOverlay != null)
        {
            Canvas fadeCanvas = fadeOverlay.GetComponent<Canvas>();
            if (fadeCanvas != null)
            {
                fadeCanvas.sortingOrder = 999;
            }
            else
            {
                Canvas parentCanvas = fadeOverlay.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    parentCanvas.sortingOrder = 999;
                }
            }
            fadeOverlay.raycastTarget = false;
        }

        foreach (var step in introSteps)
        {
            if (step?.stepObject != null)
            {
                step.stepObject.SetActive(false);
            }
        }

        // Start with black screen
        SetFadeOverlayAlpha(1f);
    }

    private void SetFadeOverlayAlpha(float alpha)
    {
        if (fadeOverlay != null)
        {
            Color color = fadeOverlay.color;
            color.a = alpha;
            fadeOverlay.color = color;
        }
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (fadeOverlay == null || !useFade)
        {
            Debug.LogWarning("Fade overlay is missing or disabled. Skipping fade.");
            yield break;
        }

        float startAlpha = fadeOverlay.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            SetFadeOverlayAlpha(newAlpha);
            yield return null;
        }

        SetFadeOverlayAlpha(targetAlpha); // Ensure final alpha is set
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
            SetFadeOverlayAlpha(1f);
            yield return new WaitForSeconds(0.1f);
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

            // Fade to black if needed for this step
            if (useFade && currentStep.useFadeOut)
            {
                yield return StartCoroutine(FadeOverlay(1f, 0.2f));
            }

            // Deactivate previous step while black
            if (currentStepIndex > 0 && introSteps[currentStepIndex - 1]?.stepObject != null)
            {
                introSteps[currentStepIndex - 1].stepObject.SetActive(false);
            }

            // Activate current step
            currentStep.stepObject.SetActive(true);
            yield return null;

            // Handle video preparation if needed
            VideoPlayer videoPlayer = currentStep.stepObject.GetComponent<VideoPlayer>();
            if (videoPlayer != null && !videoPlayer.isPrepared)
            {
                yield return new WaitUntil(() => videoPlayer.isPrepared);
            }

            // Fade in to show content if needed for this step
            if (useFade && currentStep.useFadeIn)
            {
                yield return StartCoroutine(FadeOverlay(0f, 0.2f));
            }

            bool isStepCompleted = false;

            // Animation handling
            Animator animator = currentStep.stepObject.GetComponent<Animator>();
            if (animator != null)
            {
                isStepCompleted = true;
                yield return StartCoroutine(WaitForAnimation(animator));
            }

            // Video handling
            if (videoPlayer != null)
            {
                isStepCompleted = true;
                videoPlayer.Play();
                yield return new WaitUntil(() => !videoPlayer.isPlaying);
            }

            // Fallback logic for steps without animations or videos
            if (!isStepCompleted)
            {
                Debug.LogWarning($"No animation or video detected for step {currentStepIndex}. Defaulting to 3-second wait.");
                yield return new WaitForSeconds(3f);
            }

            Debug.Log($"Step {currentStepIndex} completed.");
            currentStepIndex++;
        }

        // Final fade in to transparent
        if (useFade)
        {
            Debug.Log("Fading overlay to transparent...");
            yield return StartCoroutine(FadeOverlay(0f, 0.2f));
            SetFadeOverlayAlpha(0f); // Ensure final alpha is set
        }

        OnSequenceComplete();
    }

    private IEnumerator WaitForAnimation(Animator animator)
    {
        AnimatorStateInfo animationStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = animationStateInfo.length;

        Debug.Log($"Animation length is {clipLength} seconds. Waiting for it to finish.");

        float elapsed = 0f;
        float timeout = clipLength + 1f; // Add a small buffer for safety

        while ((animator.IsInTransition(0) || animationStateInfo.normalizedTime < 1.0f) && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            animationStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("Timeout reached while waiting for animation. Continuing...");
        }
    }

    private void OnSequenceComplete()
    {
        Debug.Log("Intro sequence completed.");
    }
}
