using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneData
{
    [System.Serializable]
    public class ScenePayload
    {
        public Show show;
        public Scene scene;
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
    public class Scene
    {
        public string location;
        public string @in;
        public string @out;
        public Dictionary<string, string> cast;
    }

    [System.Serializable]
    public class Location
    {
        public string name;
        public string description;
        public Dictionary<string, string> slots;
    }

    [System.Serializable]
    public class Slot
    {
        public string slotName;
        public string description;
    }

    [System.Serializable]
    public class Dialogue
    {
        public string actor;
        public string line;
        public string action;
    }

    [System.Serializable]
    public class CastEntry
    {
        public string slot;
        public string actorName;
    }
}