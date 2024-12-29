using System;
using System.Collections.Generic;

[Serializable]
public class EventStream
{
    public Dictionary<string, EventData> inputs;
}

[Serializable]
public class EventData
{
    public long timestamp { get; set; }
    public string type { get; set; }
    public string location { get; set; }
    public string actor { get; set; }
    public string line { get; set; }
    public string action { get; set; }
    public string episode;
    
    // Additional field for more flexible data
    public string additionalData;
}
