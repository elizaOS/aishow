using UnityEngine;
using ShowGenerator;

public class ShowrunnerLoader : MonoBehaviour
{
    [ContextMenu("Load Dummy Show Config")]
    public void LoadShowFromJson(string path)
    {
        Debug.Log($"[ShowrunnerLoader] Loading show config from: {path} (stub)");
        // TODO: Replace with real JSON loading
        var dummyConfig = new ShowConfig();
        dummyConfig.id = "dummy";
        Debug.Log("[ShowrunnerLoader] Dummy show config loaded.");
    }
} 