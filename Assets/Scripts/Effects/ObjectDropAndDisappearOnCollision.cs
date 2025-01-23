using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class ObjectDropAndDisappearOnCollision : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject prefab;
    public int poolSize = 20;
    
    [Header("Spawn Settings")]
    public float spawnHeight = 10f;
    public float spawnWidth = 5f;
    public float dropDelay = 0.2f; // Delay between each object drop
    
    [Header("Animation Settings")]
    public float scaleDownDuration = 1f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private List<PooledObject> objectPool;
    private bool isDropping = false;
    
    private void Start()
    {
        InitializePool();
    }
    
    private void InitializePool()
    {
        objectPool = new List<PooledObject>();
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            var pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.Initialize(this);
            
            // Make the spawned object a child of this object
            obj.transform.SetParent(transform);
            
            // Ensure object has rigidbody
            if (!obj.GetComponent<Rigidbody>())
            {
                var rb = obj.AddComponent<Rigidbody>();
                rb.useGravity = true;
            }
            
            // Ensure object has collider if it doesn't already have one
            if (!obj.GetComponent<Collider>())
            {
                obj.AddComponent<BoxCollider>();
            }
            
            obj.SetActive(false);
            objectPool.Add(pooledObj);
        }
    }
    
    public void StartDropSequence()
    {
        if (!isDropping)
        {
            StartCoroutine(DropSequence());
        }
    }
    
    private IEnumerator DropSequence()
    {
        isDropping = true;
        
        foreach (var obj in objectPool)
        {
            if (!obj.gameObject.activeInHierarchy)
            {
                ResetObject(obj);
                obj.gameObject.SetActive(true);
                yield return new WaitForSeconds(dropDelay);
            }
        }
        
        isDropping = false;
    }
    
    public void ResetObject(PooledObject obj)
    {
        // Calculate spawn position relative to this object's position
        float randomX = Random.Range(-spawnWidth, spawnWidth);
        Vector3 localSpawnPosition = new Vector3(randomX, spawnHeight, 0);
        Vector3 worldSpawnPosition = transform.TransformPoint(localSpawnPosition);
        
        // Reset position
        obj.transform.position = worldSpawnPosition;
        
        // Reset scale
        obj.transform.localScale = Vector3.one;
        
        // Reset physics
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset the scaling state
        obj.ResetScaling();
    }
    
    public IEnumerator ScaleDownAndReset(PooledObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / scaleDownDuration;
            float scaleMultiplier = scaleCurve.Evaluate(normalizedTime); // Removed the 1- to fix scaling direction
            
            obj.transform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }
        
        obj.gameObject.SetActive(false);
        obj.ResetScaling(); // Ensure scaling state is reset when done
    }
}

// Modify PooledObject to include impact detection
public class PooledObject : MonoBehaviour
{
    private ObjectDropAndDisappearOnCollision manager;
    private bool isScaling = false;
    
    public void Initialize(ObjectDropAndDisappearOnCollision dropManager)
    {
        manager = dropManager;
    }

    public void ResetScaling()
    {
        isScaling = false;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check for avatar impact reaction component
        var avatarReaction = collision.gameObject.GetComponent<AvatarImpactReaction>();
        if (avatarReaction != null)
        {
            // Calculate impact point and velocity
            Vector3 impactPoint = collision.contacts[0].point;
            Vector3 impactVelocity = collision.relativeVelocity;
            
            // Trigger avatar reaction
            avatarReaction.ReactToImpact(impactPoint, impactVelocity);
        }
        
        // Original scaling behavior
        if (!isScaling && !collision.gameObject.GetComponent<PooledObject>())
        {
            isScaling = true;
            StartCoroutine(ScaleDownSequence());
        }
    }
    
    private IEnumerator ScaleDownSequence()
    {
        yield return StartCoroutine(manager.ScaleDownAndReset(this));
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ObjectDropAndDisappearOnCollision))]
public class ObjectDropAndDisappearEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ObjectDropAndDisappearOnCollision script = (ObjectDropAndDisappearOnCollision)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Drop Objects", GUILayout.Height(30)))
        {
            script.StartDropSequence();
        }
    }
}
#endif