using System;
using System.Collections.Generic;
using UnityEngine;

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
        public ShowConfig Config;
        
        /// <summary>List of episodes in the show</summary>
        public List<Episode> Episodes;
    }

    /// <summary>
    /// Represents a single episode in the show, containing scenes and metadata.
    /// </summary>
    [Serializable]
    public class ShowConfig
    {
        /// <summary>Unique identifier for the episode</summary>
        public string id;
        
        /// <summary>Display name of the episode</summary>
        public string name;
        
        /// <summary>Description or summary of the episode</summary>
        public string description;
        
        /// <summary>Creator of the episode</summary>
        public string creator;
        
        /// <summary>List of prompts for the episode</summary>
        public Dictionary<string, string> prompts;
        
        /// <summary>List of actors in the show</summary>
        public Dictionary<string, ActorConfig> actors;
        
        /// <summary>List of locations in the show</summary>
        public Dictionary<string, LocationConfig> locations;
    }

    /// <summary>
    /// Represents a single actor in the show, containing metadata.
    /// </summary>
    [Serializable]
    public class ActorConfig
    {
        /// <summary>Name of the actor</summary>
        public string name;
        
        /// <summary>Gender of the actor</summary>
        public string gender;
        
        /// <summary>Description of the actor</summary>
        public string description;
        
        /// <summary>Voice of the actor</summary>
        public string voice;
    }

    /// <summary>
    /// Represents a single location in the show, containing metadata.
    /// </summary>
    [Serializable]
    public class LocationConfig
    {
        /// <summary>Name of the location</summary>
        public string name;
        
        /// <summary>Description of the location</summary>
        public string description;
        
        /// <summary>List of slots in the location</summary>
        public Dictionary<string, string> slots;
    }

    /// <summary>
    /// Represents a single episode in the show, containing scenes and metadata.
    /// </summary>
    [Serializable]
    public class Episode
    {
        /// <summary>Unique identifier for the episode</summary>
        public string id;
        
        /// <summary>Display name of the episode</summary>
        public string name;
        
        /// <summary>Description or summary of the episode</summary>
        public string premise;
        
        /// <summary>List of scenes in the episode</summary>
        public string summary;
        
        /// <summary>List of scenes in the episode</summary>
        public List<Scene> scenes;
    }

    /// <summary>
    /// Represents a scene within an episode, containing dialogue and scene-specific data.
    /// </summary>
    [Serializable]
    public class Scene
    {
        /// <summary>Unique identifier for the scene</summary>
        public string location;
        
        /// <summary>Description of the scene</summary>
        public string description;
        
        /// <summary>In time of the scene</summary>
        public string inTime;
        
        /// <summary>Out time of the scene</summary>
        public string outTime;
        
        /// <summary>List of cast members in the scene</summary>
        public Dictionary<string, string> cast;
        
        /// <summary>List of dialogue entries in the scene</summary>
        public List<Dialogue> dialogue;
    }

    /// <summary>
    /// Represents a single dialogue entry, including the speaker and their line.
    /// </summary>
    [Serializable]
    public class Dialogue
    {
        /// <summary>Name of the character speaking</summary>
        public string actor;
        
        /// <summary>The dialogue line to be spoken</summary>
        public string line;
        
        /// <summary>Action or emotion to be performed while speaking</summary>
        public string action;
    }
} 