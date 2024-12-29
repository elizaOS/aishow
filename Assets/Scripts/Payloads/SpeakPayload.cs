using System;
using System.Collections.Generic;

namespace SpeakData
{
    [System.Serializable]
    public class SpeakPayload
    {
        public Show show;
        public Dialogue dialogue;
    }

    [System.Serializable]
    public class Show
    {
        public Dictionary<string, Actor> actors;
        public Dictionary<string, Location> locations;
    }

    [System.Serializable]
    public class Actor
    {
        public string name;
        public string vrm;
        public string voice;
    }

    [System.Serializable]
    public class Location
    {
        public string name;
        public string description;
        public Dictionary<string, string> slots;
    }

    [System.Serializable]
    public class Dialogue
    {
        public string actor;
        public string line;
        public string action;
    }
}
