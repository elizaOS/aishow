using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoNotDestory : MonoBehaviour
{
     private static DoNotDestory instance;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
