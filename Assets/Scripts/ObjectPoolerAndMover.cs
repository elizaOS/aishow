using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolerAndMover : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject prefab;
    public int poolSize = 20;

    [Header("Movement Settings")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public Vector2 yRange = new Vector2(-5, 5); // Y range for the spread of the objects
    public float boxWidth = 10f;  // The width of the box (not used for X, but could be for movement)

    [Header("Scale Settings")]
    public float scaleDownSpeed = 0.5f;  // Time in seconds to scale to zero
    public Vector2 scaleRange = new Vector2(0.5f, 2f);  // Starting scale range for objects

    private List<GameObject> pool;

    private void Start()
    {
        InitializePool();
    }

    // Initialize the pool of objects
    private void InitializePool()
    {
        pool = new List<GameObject>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    // Get an inactive object from the pool
    private GameObject GetPooledObject()
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }
        return null;
    }

    // Start spawning and animating objects
    public void SpawnObjects()
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                StartMovingObject(obj);
            }
        }
    }

    // Start moving and scaling the object
    private void StartMovingObject(GameObject obj)
    {
        // Set random speed and position
        float randomSpeed = Random.Range(minSpeed, maxSpeed);
        float randomY = Random.Range(yRange.x, yRange.y);  // Random Y position from the Y range
        Vector3 startPosition = new Vector3(0f, randomY, 0f);  // Start at the origin (0, 0) but random Y
        obj.transform.position = startPosition;

        // Set random starting scale
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        obj.transform.localScale = Vector3.one * randomScale;

        // Start the scaling down and movement process
        StartCoroutine(MoveAndScaleObject(obj, randomSpeed));
    }

    // Move the object gently and scale it down to zero over time
    private System.Collections.IEnumerator MoveAndScaleObject(GameObject obj, float speed)
    {
        Vector3 startPosition = obj.transform.position;
        Vector3 endPosition = new Vector3(boxWidth / 2, obj.transform.position.y, 0);  // Move towards the right side
        float elapsedTime = 0f;

        // Gradual scale down to zero
        Vector3 initialScale = obj.transform.localScale;

        while (obj.activeInHierarchy)
        {
            // Move the object smoothly towards the end position
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, endPosition, speed * Time.deltaTime);

            // Scale the object down over time
            elapsedTime += Time.deltaTime;
            float scaleProgress = Mathf.Clamp01(elapsedTime / scaleDownSpeed);  // Normalize progress over time
            obj.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, scaleProgress);

            // Check if object has scaled down to zero and needs to restart
            if (scaleProgress >= 1f)
            {
                // Reset the object and deactivate it
                ResetObject(obj);
                yield break; // Exit the coroutine after resetting the object
            }

            yield return null; // Wait for the next frame
        }
    }

    // Reset the object and restart its cycle
    private void ResetObject(GameObject obj)
    {
        // Reset position to the origin with a random Y position
        float randomY = Random.Range(yRange.x, yRange.y);  // Spread out Y positions
        Vector3 newStartPosition = new Vector3(0f, randomY, 0f);  // Start at the origin (0, 0) with random Y
        obj.transform.position = newStartPosition;

        // Reset scale to initial scale range for the next animation
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        obj.transform.localScale = Vector3.one * randomScale;

        // Deactivate the object to return it to the pool
        obj.SetActive(false);
    }
}
