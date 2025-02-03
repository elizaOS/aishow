using UnityEngine;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject prefab;
    public int poolSize = 20;

    [Header("Movement Settings")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public Vector2 yRange = new Vector2(-5, 5);
    public float boxWidth = 10f;

    [Header("Rotation Settings")]
    public bool enableRotation = false;
    public bool rotateOverTime = false;
    public float minRotationSpeed = 30f;
    public float maxRotationSpeed = 180f;
    public Vector3 rotationAxis = Vector3.forward; // Can be set in inspector to control rotation axis

    [Header("Scale Settings")]
    public bool enableRandomScale = false;
    public bool scaleOverTime = false;
    public Vector2 scaleRange = new Vector2(0.5f, 2f);
    public float scaleSpeed = 1f;

    private List<GameObject> pool;
    private Queue<MovingObject> activeObjects;

    private void Start()
    {
        InitializePool();
        StartAnimatingObjects();
    }

    private void InitializePool()
    {
        pool = new List<GameObject>(poolSize);
        activeObjects = new Queue<MovingObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

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

    private void StartAnimatingObjects()
    {
        foreach (var obj in pool)
        {
            var pooledObject = GetPooledObject();
            if (pooledObject != null)
            {
                StartMovingObject(pooledObject);
            }
        }
    }

    private void StartMovingObject(GameObject obj)
    {
        float randomSpeed = Random.Range(minSpeed, maxSpeed);
        float randomY = Random.Range(yRange.x, yRange.y);
        Vector3 startPosition = new Vector3(-boxWidth / 2, randomY, 0);
        Vector3 endPosition = new Vector3(boxWidth / 2, randomY, 0);

        obj.transform.position = startPosition;

        // Apply initial random rotation if enabled
        if (enableRotation && !rotateOverTime)
        {
            obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        // Apply initial random scale if enabled
        if (enableRandomScale && !scaleOverTime)
        {
            float randomScale = Random.Range(scaleRange.x, scaleRange.y);
            obj.transform.localScale = Vector3.one * randomScale;
        }

        MovingObject movingObject = obj.GetComponent<MovingObject>();
        if (movingObject == null)
        {
            movingObject = obj.AddComponent<MovingObject>();
        }

        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        movingObject.Initialize(
            startPosition, 
            endPosition, 
            randomSpeed,
            () => RecycleAndRestartObject(obj),
            enableRotation && rotateOverTime,
            rotationSpeed,
            rotationAxis,
            enableRandomScale && scaleOverTime,
            scaleRange,
            scaleSpeed
        );
    }

    private void RecycleAndRestartObject(GameObject obj)
    {
        float newRandomY = Random.Range(yRange.x, yRange.y);
        Vector3 newStartPosition = new Vector3(-boxWidth / 2, newRandomY, 0);
        Vector3 newEndPosition = new Vector3(boxWidth / 2, newRandomY, 0);

        // Apply new random rotation if enabled
        if (enableRotation && !rotateOverTime)
        {
            obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        // Apply new random scale if enabled
        if (enableRandomScale && !scaleOverTime)
        {
            float randomScale = Random.Range(scaleRange.x, scaleRange.y);
            obj.transform.localScale = Vector3.one * randomScale;
        }

        MovingObject movingObject = obj.GetComponent<MovingObject>();
        if (movingObject != null)
        {
            float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            movingObject.Initialize(
                newStartPosition, 
                newEndPosition, 
                Random.Range(minSpeed, maxSpeed),
                () => RecycleAndRestartObject(obj),
                enableRotation && rotateOverTime,
                rotationSpeed,
                rotationAxis,
                enableRandomScale && scaleOverTime,
                scaleRange,
                scaleSpeed
            );
        }
    }
}

public class MovingObject : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float speed;
    private System.Action onComplete;
    private bool movingToEnd;

    // Rotation variables
    private bool shouldRotate;
    private float rotationSpeed;
    private Vector3 rotationAxis;

    // Scale variables
    private bool shouldScale;
    private Vector2 scaleRange;
    private float scaleSpeed;
    private float currentScaleTime;
    private bool scalingUp = true;

    public void Initialize(
        Vector3 start, 
        Vector3 end, 
        float movementSpeed, 
        System.Action onCompleteCallback,
        bool rotate = false,
        float rotSpeed = 0f,
        Vector3 rotAxis = default,
        bool scale = false,
        Vector2 scaleMinMax = default,
        float scaleSpd = 1f
    )
    {
        startPosition = start;
        endPosition = end;
        speed = movementSpeed;
        onComplete = onCompleteCallback;
        movingToEnd = true;

        // Initialize rotation
        shouldRotate = rotate;
        rotationSpeed = rotSpeed;
        rotationAxis = rotAxis;

        // Initialize scaling
        shouldScale = scale;
        scaleRange = scaleMinMax;
        scaleSpeed = scaleSpd;
        currentScaleTime = 0f;
        
        // Set initial scale if not animating
        if (!shouldScale)
        {
            transform.localScale = Vector3.one * scaleRange.x;
        }
    }

    private void Update()
    {
        // Handle movement
        if (movingToEnd)
        {
            transform.position = Vector3.MoveTowards(transform.position, endPosition, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, endPosition) < 0.01f)
            {
                movingToEnd = false;
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, startPosition) < 0.01f)
            {
                onComplete?.Invoke();
            }
        }

        // Handle rotation
        if (shouldRotate)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }

        // Handle scaling
        if (shouldScale)
        {
            if (scalingUp)
            {
                currentScaleTime += Time.deltaTime * scaleSpeed;
                if (currentScaleTime >= 1f)
                {
                    currentScaleTime = 1f;
                    scalingUp = false;
                }
            }
            else
            {
                currentScaleTime -= Time.deltaTime * scaleSpeed;
                if (currentScaleTime <= 0f)
                {
                    currentScaleTime = 0f;
                    scalingUp = true;
                }
            }

            float currentScale = Mathf.Lerp(scaleRange.x, scaleRange.y, currentScaleTime);
            transform.localScale = Vector3.one * currentScale;
        }
    }
}