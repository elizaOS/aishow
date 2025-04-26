using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ShowRunner
{
    [Serializable]
    public class ShowData
    {
        [JsonProperty("config")]
        public ShowConfig Config { get; set; }
        
        [JsonProperty("episodes")]
        public List<Episode> Episodes { get; set; }
    }

    [Serializable]
    public class ShowConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("actors")]
        public Dictionary<string, ActorConfig> Actors { get; set; }
    }

    [Serializable]
    public class ActorConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    [Serializable]
    public class Episode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("scenes")]
        public List<Scene> Scenes { get; set; }
    }

    [Serializable]
    public class Scene
    {
        [JsonProperty("location")]
        public string Location { get; set; }
        
        [JsonProperty("dialogue")]
        public List<Dialogue> Dialogue { get; set; }
    }

    [Serializable]
    public class Dialogue
    {
        [JsonProperty("actor")]
        public string Actor { get; set; }
        
        [JsonProperty("line")]
        public string Line { get; set; }
        
        [JsonProperty("action")]
        public string Action { get; set; }
    }
} 