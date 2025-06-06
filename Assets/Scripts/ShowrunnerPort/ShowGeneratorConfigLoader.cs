using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic; // Added for List<ShowEpisode>

namespace ShowGenerator
{
    [System.Serializable]
    public class ShowConfigWrapper
    {
        public ShowConfig config;
        public System.Collections.Generic.List<ShowEpisode> episodes;
    }

    public static class ShowGeneratorConfigLoader
    {
        // Loads a ShowConfig from a JSON file at the given path
        public static ShowConfig LoadFromJson(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Show config JSON not found at: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            var wrapper = JsonConvert.DeserializeObject<ShowConfigWrapper>(json);
            if (wrapper == null || wrapper.config == null)
            {
                Debug.LogError("Failed to parse show config wrapper or config.");
                return null;
            }
            // Attach episodes if present at the root
            if (wrapper.episodes != null)
                wrapper.config.episodes = wrapper.episodes;

            // Ensure important fields are not null
            if (wrapper.config.actors == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'actors'. Initializing empty dictionary.");
                wrapper.config.actors = new Dictionary<string, ShowActor>();
            }
            if (wrapper.config.locations == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'locations'. Initializing empty dictionary.");
                wrapper.config.locations = new Dictionary<string, ShowLocation>();
            }
            if (wrapper.config.prompts == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'prompts'. Initializing empty dictionary.");
                wrapper.config.prompts = new Dictionary<string, string>();
            }
            if (wrapper.config.actorVoiceMap == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'actorVoiceMap'. Initializing empty dictionary.");
                wrapper.config.actorVoiceMap = new Dictionary<string, string>();
            }
            if (wrapper.config.episodes == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'episodes'. Initializing empty list.");
                wrapper.config.episodes = new List<ShowEpisode>();
            }
            // Add checks for basic info fields and pilot
            if (string.IsNullOrEmpty(wrapper.config.id))
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'id'. Setting to empty string.");
                wrapper.config.id = "";
            }
            if (string.IsNullOrEmpty(wrapper.config.name))
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'name'. Setting to empty string.");
                wrapper.config.name = "";
            }
            if (string.IsNullOrEmpty(wrapper.config.description))
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'description'. Setting to empty string.");
                wrapper.config.description = "";
            }
            if (string.IsNullOrEmpty(wrapper.config.creator))
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'creator'. Setting to empty string.");
                wrapper.config.creator = "";
            }
            if (wrapper.config.pilot == null)
            {
                Debug.LogWarning("[DEBUG] Loaded config is missing 'pilot'. Initializing new ShowEpisode as pilot.");
                wrapper.config.pilot = new ShowEpisode();
            }
            Debug.Log($"[DEBUG] Loaded config: actors={wrapper.config.actors?.Count ?? 0}, locations={wrapper.config.locations?.Count ?? 0}, prompts={wrapper.config.prompts?.Count ?? 0}, actorVoiceMap={wrapper.config.actorVoiceMap?.Count ?? 0}, episodes={wrapper.config.episodes?.Count ?? 0}");

            // Post-load: Auto-populate actorVoiceMap with web app defaults if missing or empty
            if (wrapper.config.actorVoiceMap.Count == 0)
            {
                wrapper.config.actorVoiceMap = new System.Collections.Generic.Dictionary<string, string>();
                // Use the DefaultVoiceMap from ShowrunnerManager
                foreach (var kvp in ShowrunnerManager.DefaultVoiceMap)
                {
                    if (!wrapper.config.actorVoiceMap.ContainsKey(kvp.Key))
                        wrapper.config.actorVoiceMap[kvp.Key] = kvp.Value;
                }
            }
            return wrapper.config;
        }

        // Saves a ShowConfig to a JSON file at the given path, using the wrapper structure.
        public static void SaveToJson(ShowConfig config, string path)
        {
            var wrapper = new ShowConfigWrapper { config = config, episodes = config.episodes };
            string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            File.WriteAllText(path, json);
            Debug.Log($"[ShowGeneratorConfigLoader] Saved ShowConfig to: {path}");
        }
    }
} 