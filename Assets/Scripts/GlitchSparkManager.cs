using UnityEngine;
using System.Collections.Generic;

public class GlitchSparkManager : MonoBehaviour
{
    [Header("Spark Settings")]
    public GameObject sparkPrefab;
    public int poolSize = 20;
    public float sparkLifetime = 1f;
    public float burstForce = 10f;
    public Transform sparkOrigin;

    [Header("Physics Settings")]
    public float upwardForce = 5f; // Adds upward bias to prevent ground sticking
    public float bounceMultiplier = 0.5f; // How much bounce to maintain
    public LayerMask groundLayer; // To check for ground collisions

    private GenericObjectPooler<GlitchSpark> pooler; // Pool for GlitchSpark objects
    private Dictionary<GameObject, float> activeSparkTimes; // Track active sparks and their lifetimes

    private void Start()
    {
        pooler = new GenericObjectPooler<GlitchSpark>();
        pooler.prefab = sparkPrefab; // This is correct, as the pooler expects a GameObject
        pooler.poolSize = poolSize;
        pooler.InitializePool(); // Ensure this initializes the pool before usage

        activeSparkTimes = new Dictionary<GameObject, float>();
    }


    private void Update()
    {
        // Handle lifetime and ground checks
        List<GameObject> sparksToDeactivate = new List<GameObject>();

        foreach (var spark in activeSparkTimes.Keys)
        {
            if (spark != null && spark.activeInHierarchy)
            {
                float timeAlive = Time.time - activeSparkTimes[spark];

                // Check lifetime
                if (timeAlive >= sparkLifetime)
                {
                    sparksToDeactivate.Add(spark); // Deactivate spark if lifetime has expired
                    continue;
                }

                // Check if stuck in ground
                Rigidbody rb = spark.GetComponent<Rigidbody>();
                if (rb != null && Physics.Raycast(spark.transform.position, Vector3.down, 0.1f, groundLayer))
                {
                    // Add small upward force if nearly stopped and touching ground
                    if (rb.velocity.magnitude < 1f)
                    {
                        rb.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
                    }
                }
            }
        }

        // Deactivate expired or stuck sparks
        foreach (var spark in sparksToDeactivate)
        {
            DeactivateSpark(spark);
        }
    }

    public void TriggerGlitch()
    {
        // Reset all active sparks first
        foreach (var spark in new List<GameObject>(activeSparkTimes.Keys))
        {
            DeactivateSpark(spark);
        }

        // Create new burst of sparks from the pool
        for (int i = 0; i < poolSize; i++)
        {
            GlitchSpark spark = pooler.GetObject();
            if (spark != null)
            {
                ActivateSpark(spark.gameObject); // Activate each spark from the pool
            }
        }
    }

    private void ActivateSpark(GameObject spark)
{
    if (sparkOrigin == null) return;

    // Reset position to origin
    spark.transform.position = sparkOrigin.position;

    // Random direction with upward bias
    Vector3 randomDir = Random.insideUnitSphere;
    randomDir.y = Mathf.Abs(randomDir.y); // Ensure some upward direction
    randomDir = randomDir.normalized;

    // Random scale
    float randomScale = Random.Range(0.1f, 0.5f);
    spark.transform.localScale = Vector3.one * randomScale;

    Rigidbody rb = spark.GetComponent<Rigidbody>();
    if (rb != null)
    {
        // Reset physics state
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Apply new burst force with upward bias
        Vector3 burstVelocity = (randomDir * burstForce) + (Vector3.up * upwardForce);
        rb.AddForce(burstVelocity, ForceMode.Impulse);

        // Add some spin
        rb.AddTorque(Random.insideUnitSphere * burstForce, ForceMode.Impulse);
    }

    // Clear trail if present
    TrailRenderer trail = spark.GetComponent<TrailRenderer>();
    if (trail != null)
    {
        trail.Clear();
    }

    activeSparkTimes[spark] = Time.time;
    spark.SetActive(true);
}


    private void DeactivateSpark(GameObject spark)
{
    if (spark != null)
    {
        Rigidbody rb = spark.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        activeSparkTimes.Remove(spark);
        pooler.ReturnObject(spark.GetComponent<GlitchSpark>()); // Return the GlitchSpark component to the pool
        spark.SetActive(false);
    }
}


    private void OnDestroy()
    {
        // Ensure the pooler has been initialized properly
        if (pooler != null)
        {
            foreach (var spark in pooler.GetAllObjects())
            {
                if (spark != null)
                {
                    DeactivateSpark(spark.gameObject);
                }
            }
        }
        else
        {
            Debug.LogWarning("Pooler is not initialized.");
        }
    }

}
