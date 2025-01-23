using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PointOfInterestController : MonoBehaviour
{
    [System.Serializable]
    public class PointOfInterest
    {
        public Transform target;
        public float weight;
        public float maxWeight = 1f;
    }

    public MultiAimConstraint multiAimConstraint;
    public List<PointOfInterest> pointsOfInterest = new List<PointOfInterest>();
    
    [Header("Transition Settings")]
    public ValueRandomizer.Range transitionSpeedRange = new ValueRandomizer.Range(0.8f, 1.2f);
    public ValueRandomizer.Range delayRange = new ValueRandomizer.Range(1f, 3f);
    public float variationPercent = 0.15f;
    public bool randomGazing = true;

    private int currentTargetIndex = -1;
    private Coroutine transitionCoroutine;

    void Start()
    {
        PopulatePointsOfInterest();
        if (pointsOfInterest.Count > 0)
        {
            PickRandomTarget();
        }
    }

    void Update()
    {
        if (randomGazing && transitionCoroutine == null)
        {
            PickRandomTarget();
        }
    }

    public void PickSpecificTarget(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= pointsOfInterest.Count)
        {
            Debug.LogWarning("Invalid target index.");
            return;
        }

        randomGazing = false;
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionWeights(targetIndex));
    }

    public void EnableRandomGazing()
    {
        randomGazing = true;
    }

    private void PickRandomTarget()
    {
        if (pointsOfInterest.Count <= 1) return;

        int newTargetIndex;
        do
        {
            newTargetIndex = Random.Range(0, pointsOfInterest.Count);
        } while (newTargetIndex == currentTargetIndex);

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionWeights(newTargetIndex));
    }

    private float SmoothEaseInOut(float t)
    {
        if (t < 0.5f)
        {
            return 16 * t * t * t * t * t;
        }
        else
        {
            float f = ((2 * t) - 2);
            return 0.5f * f * f * f * f * f + 1;
        }
    }

    private IEnumerator TransitionWeights(int targetIndex)
    {
        currentTargetIndex = targetIndex;
        var sourceObjects = multiAimConstraint.data.sourceObjects;
        float[] initialWeights = new float[pointsOfInterest.Count];
        float[] targetWeights = new float[pointsOfInterest.Count];

        float transitionSpeed = ValueRandomizer.GetRandomInRange(transitionSpeedRange, variationPercent * 0.25f);
        
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < pointsOfInterest.Count; i++)
        {
            initialWeights[i] = pointsOfInterest[i].weight;
            if (i == targetIndex)
            {
                targetWeights[i] = ValueRandomizer.AddVariation(pointsOfInterest[i].maxWeight, variationPercent * 0.05f);
            }
            else
            {
                targetWeights[i] = Random.value * 0.02f;
            }
        }

        float transitionProgress = 0f;
        while (transitionProgress < 1f)
        {
            float speedMultiplier = Mathf.Lerp(0.5f, 1f, transitionProgress);
            transitionProgress += Time.deltaTime * transitionSpeed * speedMultiplier;
            float t = Mathf.Clamp01(SmoothEaseInOut(transitionProgress));

            for (int i = 0; i < pointsOfInterest.Count; i++)
            {
                pointsOfInterest[i].weight = Mathf.Lerp(initialWeights[i], targetWeights[i], t);
                WeightedTransform wt = sourceObjects[i];
                wt.weight = pointsOfInterest[i].weight;
                sourceObjects.SetWeight(i, wt.weight);
            }

            multiAimConstraint.data.sourceObjects = sourceObjects;
            yield return null;
        }

        float delay = ValueRandomizer.GetRandomInRange(delayRange, variationPercent * 0.5f);
        yield return new WaitForSeconds(delay);

        transitionCoroutine = null;
    }

    private void PopulatePointsOfInterest()
    {
        if (multiAimConstraint == null)
        {
            Debug.LogError("MultiAimConstraint is not assigned.");
            return;
        }

        pointsOfInterest.Clear();
        var data = multiAimConstraint.data.sourceObjects;

        for (int i = 0; i < data.Count; i++)
        {
            WeightedTransform sourceObject = data[i];
            pointsOfInterest.Add(new PointOfInterest
            {
                target = sourceObject.transform,
                weight = sourceObject.weight,
                maxWeight = 1f
            });
        }
    }
}