using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Import TextMeshPro namespace
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SceneData; // Import the SceneData namespace where your serialized classes are defined
using UnityEngine.SceneManagement; // Import Scene Management namespace

public class ScenePayloadManager : MonoBehaviour
{
    // Dictionary to map actor names to GameObjects
    private Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>();

    public void LoadScene(string json)
    {
        try
        {
            Debug.Log("Loading scene payload...");

            // Use Newtonsoft.Json to parse the JSON
            JObject payload = JObject.Parse(json);

            // Log the parsed payload to check its structure
            Debug.Log("Parsed JSON Payload: " + payload.ToString());

            // Process actors
            var actorsToken = payload["show"]?["actors"];
            if (actorsToken != null)
            {
                var actors = actorsToken.ToObject<Dictionary<string, Actor>>();
                foreach (var actor in actors)
                {
                    // Log actor details or perform operations
                    Debug.Log($"Actor: {actor.Value.name}, VRM: {actor.Value.vrm}, Voice: {actor.Value.voice}");

                    GameObject actorObject = FindOrCreateActor(actor.Value.name, actor.Value.vrm);
                    this.actors[actor.Value.name] = actorObject;
                }
            }
            else
            {
                Debug.LogWarning("Actors data is missing.");
            }

            // Process locations
            var locations = payload["show"]?["locations"] as JObject;
            string locationName = payload["scene"]?["location"]?.ToString();

            if (!string.IsNullOrEmpty(locationName))
            {
                Debug.Log($"Location: {locationName}");

                // Load the Unity scene matching the location name
                LoadUnityScene(locationName);
            }
            else
            {
                Debug.LogWarning("Location name is missing. Cannot load Unity scene.");
            }

            if (locations != null && locations[locationName] != null)
            {
                var currentLocation = locations[locationName].ToObject<Location>();

                Debug.Log($"Location: {currentLocation?.name ?? "Unnamed Location"}, Description: {currentLocation?.description ?? "No description"}");

                if (currentLocation?.slots != null)
                {
                    foreach (var slot in currentLocation.slots)
                    {
                        Debug.Log($"Slot: {slot.Key}, Description: {slot.Value}");
                    }
                }
                else
                {
                    Debug.LogWarning("Location slots are missing.");
                }
            }
            else
            {
                Debug.LogWarning("Locations dictionary is null or missing.");
            }

            // Process cast
            var cast = payload["scene"]?["cast"] as JObject;
            if (cast != null)
            {
                foreach (var castEntry in cast)
                {
                    Debug.Log($"Slot: {castEntry.Key}, Actor: {castEntry.Value}");
                    string actorName = castEntry.Value.ToString();

                    if (this.actors.ContainsKey(actorName))
                    {
                        Debug.Log($"Assigning actor {actorName} to slot {castEntry.Key}.");
                    }
                    else
                    {
                        Debug.LogWarning($"Actor {actorName} not found for slot {castEntry.Key}.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Scene cast is null or empty.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading scene payload: {ex.Message}");
        }
    }

    private GameObject FindOrCreateActor(string actorName, string vrmUrl)
    {
        GameObject actor = GameObject.Find(actorName);
        if (actor == null)
        {
            actor = new GameObject(actorName); // Placeholder
            Debug.Log($"Created new GameObject for actor: {actorName}");
            // Load VRM or prefab here
        }
        return actor;
    }

    private void LoadUnityScene(string locationName)
    {
        // Check if the scene exists in the build settings
        if (IsSceneInBuild(locationName))
        {
            Debug.Log($"Loading Unity scene: {locationName}");
            SceneManager.LoadScene(locationName);
        }
        else
        {
            Debug.LogError($"Scene '{locationName}' not found in the build settings. Please ensure it is added to the build.");
        }
    }

    private bool IsSceneInBuild(string sceneName)
    {
        // Get all scenes in the build settings
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(path);

            if (sceneNameFromPath.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
