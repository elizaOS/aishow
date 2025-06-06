using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShowGenerator
{
    [Serializable]
    public class ShowConfig
    {
        public string id;
        public string name;
        public string description;
        public string creator;
        public Dictionary<string, string> prompts;
        public Dictionary<string, ShowActor> actors;
        public Dictionary<string, ShowLocation> locations;
        public ShowEpisode pilot; 
        public List<ShowEpisode> episodes;
        public Dictionary<string, string> actorVoiceMap;
    }

    [Serializable]
    public class ShowActor
    {
        public string name;
        public string gender;
        public string description;
        public string voice;
    }

    [Serializable]
    public class ShowLocation
    {
        public string name;
        public string description;
        public Dictionary<string, string> slots;
    }

    [Serializable]
    public class ShowEpisode
    {
        public string id;
        public string name;
        public string premise;
        public string summary;
        public List<ShowScene> scenes;
    }

    [Serializable]
    public class ShowScene
    {
        public string location;
        public string description;
        public string @in;
        public string @out;
        public Dictionary<string, string> cast;
        public List<ShowDialogue> dialogue;
    }

    [Serializable]
    public class ShowDialogue
    {
        public string actor;
        public string line;
        public string action;
    }
} 