using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigWeightController : MonoBehaviour
{
    public Rig[] rigs; // Array of rigs to control weights
    public float transitionSpeed = 1f; // Speed at which weights change

    private Coroutine weightCoroutine;

    public void Start(){
        AutoPopulateRigs();
    }

    // Method to populate the rigs array automatically
    [ContextMenu("Auto Populate Rigs")]
    public void AutoPopulateRigs()
    {
        rigs = GetComponentsInChildren<Rig>(); // Automatically finds all Rig components in children
    }

    // Method to smoothly change rig weights to a target value
    public void SetRigWeights(float targetWeight)
    {
        if (weightCoroutine != null)
        {
            StopCoroutine(weightCoroutine);
        }
        weightCoroutine = StartCoroutine(SmoothRigWeightChange(targetWeight));
    }

    private IEnumerator SmoothRigWeightChange(float targetWeight)
    {
        // Loop through rigs and adjust their weights
        while (true)
        {
            bool allWeightsMatched = true;

            foreach (var rig in rigs)
            {
                if (rig == null) continue;

                float currentWeight = rig.weight;
                float newWeight = Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * transitionSpeed);
                rig.weight = newWeight;

                if (!Mathf.Approximately(newWeight, targetWeight))
                {
                    allWeightsMatched = false;
                }
            }

            // If all weights are at target value, exit the coroutine
            if (allWeightsMatched)
            {
                yield break;
            }

            yield return null;
        }
    }
}
