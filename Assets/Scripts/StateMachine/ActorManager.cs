using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using SpeakData;

public class ActorManager
{
    private Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>();

   public void RegisterActorsFromCameras()
{
    // Get all root GameObjects in the scene (including inactive ones)
    GameObject[] rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

    // Create a list to keep track of actors
    List<GameObject> allActorObjects = new List<GameObject>();

    // Recursively find all GameObjects (active and inactive) in the scene
    foreach (var root in rootGameObjects)
    {
        allActorObjects.AddRange(FindAllGameObjectsInHierarchy(root));
    }

    // Now, process all the found GameObjects
    foreach (var actor in allActorObjects)
    {
        ActorCamera camera = actor.GetComponent<ActorCamera>();
        if (camera != null)
        {
            string actorName = camera.actorName;
            //Debug.Log($"Found actor camera with actorName: {actorName}"); // Log the found actor name
            if (!string.IsNullOrEmpty(actorName) && !actors.ContainsKey(actorName))
            {
                actors[actorName] = actor;  // Register actor by name
                //Debug.Log($"Registered actor: {actorName}");
            }
            else
            {
                //Debug.LogWarning($"Actor camera with invalid or empty actorName: {actorName}");
            }
        }
    }

    // Debugging all registered actors
    //Debug.Log("Actors registered from ActorCamera components:");
    foreach (var actor in actors)
    {
        //Debug.Log($"Actor: {actor.Key}");
    }
}

// Helper method to recursively find all GameObjects in the scene (active and inactive)
private List<GameObject> FindAllGameObjectsInHierarchy(GameObject parent)
{
    List<GameObject> allObjects = new List<GameObject>();

    // Add the parent GameObject itself
    allObjects.Add(parent);

    // Recursively add all children of this GameObject (if any)
    foreach (Transform child in parent.transform)
    {
        allObjects.AddRange(FindAllGameObjectsInHierarchy(child.gameObject));
    }

    return allObjects;
}


    // Retrieve an actor by its name
    public GameObject GetActor(string actorName)
    {
        if (actors.ContainsKey(actorName))
        {
            return actors[actorName];
        }
        else
        {
            Debug.LogWarning($"Actor {actorName} not found in actorManager.");
            return null;
        }
    }

}

