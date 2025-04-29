using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Data structures for managing show content, including episodes, scenes, and dialogue.
/// These classes are used to serialize and deserialize show data from JSON files.
/// </summary>
namespace ShowRunner
{
    /// <summary>
    /// Root container for show data, containing metadata and a list of episodes.
    /// </summary>
    [Serializable]
    public class ShowData
    {
        /// <summary>The name of the show</summary>
        [JsonProperty("config")]
        public ShowConfig Config { get; set; }
        
        /// <summary>List of episodes in the show</summary>
        [JsonProperty("episodes")]
        public List<Episode> Episodes { get; set; }
    }

    /// <summary>
    /// Represents a single episode in the show, containing scenes and metadata.
    /// </summary>
    [Serializable]
    public class ShowConfig
    {
        /// <summary>Unique identifier for the episode</summary>
        [JsonProperty("name")]
        public string name { get; set; }
        
        /// <summary>List of actors in the show</summary>
        [JsonProperty("actors")]
        public Dictionary<string, ActorConfig> actors { get; set; }
    }

    /// <summary>
    /// Represents a single actor in the show, containing metadata.
    /// </summary>
    [Serializable]
    public class ActorConfig
    {
        /// <summary>Name of the actor</summary>
        [JsonProperty("name")]
        public string name { get; set; }
    }

    /// <summary>
    /// Represents a single episode in the show, containing scenes and metadata.
    /// </summary>
    [Serializable]
    public class Episode
    {
        /// <summary>Unique identifier for the episode</summary>
        [JsonProperty("id")]
        public string id { get; set; }
        
        /// <summary>Display name of the episode</summary>
        [JsonProperty("name")]
        public string name { get; set; }
        
        /// <summary>Description or summary of the episode</summary>
        [JsonProperty("premise")]
        public string premise { get; set; }
        
        /// <summary>List of scenes in the episode</summary>
        [JsonProperty("summary")]
        public string summary { get; set; }
        
        /// <summary>List of scenes in the episode</summary>
        [JsonProperty("scenes")]
        public List<Scene> scenes { get; set; }
    }

    /// <summary>
    /// Represents a scene within an episode, containing dialogue and scene-specific data.
    /// </summary>
    [Serializable]
    public class Scene
    {
        /// <summary>Unique identifier for the scene</summary>
        [JsonProperty("location")]
        public string location { get; set; }
        
        /// <summary>List of dialogue entries in the scene</summary>
        [JsonProperty("dialogue")]
        public List<Dialogue> dialogue { get; set; }
    }

    /// <summary>
    /// Represents a single dialogue entry, including the speaker and their line.
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        /// <summary>Name of the character speaking</summary>
        [JsonProperty("actor")]
        public string actor { get; set; }
        
        /// <summary>The dialogue line to be spoken</summary>
        [JsonProperty("line")]
        public string line { get; set; }
        
        /// <summary>Action or emotion to be performed while speaking</summary>
        [JsonProperty("action")]
        public string action { get; set; }
    }
} 