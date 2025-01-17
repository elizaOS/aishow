using UnityEngine; // Make sure this is at the top of the file
using System.Collections.Generic;

public class GenericObjectPooler<T> where T : MonoBehaviour
{
    public GameObject prefab; // The prefab to instantiate
    public int poolSize = 20; // The size of the pool
    private Queue<T> pool; // The pool of objects

    // Initializes the pool
    public void InitializePool()
    {
        pool = new Queue<T>();

        // Create the pool of objects
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab); // Use GameObject.Instantiate here
            obj.SetActive(false); // Start with the object inactive
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                pool.Enqueue(component); // Add it to the pool
            }
        }
    }

    // Gets an object from the pool, or instantiates one if the pool is empty
    public T GetObject()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true); // Activate the object
            return obj;
        }
        else
        {
            GameObject obj = GameObject.Instantiate(prefab); // Instantiate a new object if the pool is empty
            obj.SetActive(true);
            T component = obj.GetComponent<T>();
            return component;
        }
    }

    // Returns an object back to the pool
    public void ReturnObject(T obj)
    {
        obj.gameObject.SetActive(false); // Deactivate the object
        pool.Enqueue(obj); // Add it back to the pool
    }

    // Method to get all objects in the pool (useful for deactivation in OnDestroy)
    public IEnumerable<T> GetAllObjects()
    {
        return pool;
    }
}
