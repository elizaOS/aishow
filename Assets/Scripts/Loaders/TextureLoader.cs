using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[ExecuteInEditMode]
public class TextureLoader : MonoBehaviour
{
    [Header("Texture Loader Settings")]
    public Renderer targetRenderer; // Reference to the renderer of the object
    public string textureURL;       // URL for the texture to load

    public GameObject mediaTvReference; // Reference to the TV in the news room 


    public void Awake(){
        SetRenderersEnabled(mediaTvReference, false);  // To enable
    }

    // Public method to load the texture into the Emission channel
    public void LoadEmissiveTexture()
    {
        if (!string.IsNullOrEmpty(textureURL))
        {
            StartCoroutine(LoadTextureCoroutine(textureURL));
        }
        else
        {
            Debug.LogWarning("Texture URL is empty or invalid.");
        }
    }

    private IEnumerator LoadTextureCoroutine(string url)
    {
        // Use UnityWebRequest to download the texture
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error loading texture: {webRequest.error}");
            }
            else
            {
                // Get the downloaded texture
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);

                // Apply the texture to the Emission channel
                if (targetRenderer != null && targetRenderer.material != null)
                {
                    targetRenderer.material.SetTexture("_EmissionMap", texture);
                    targetRenderer.material.EnableKeyword("_EMISSION");
                    Debug.Log("Successfully applied the texture to the Emission channel.");
                }
                else
                {
                    Debug.LogWarning("Target Renderer or Material is not assigned.");
                }
            }
        }
    }

        private void SetRenderersEnabled(GameObject target, bool isEnabled) // this is to turn on and off the media tv
    {
        MeshRenderer[] renderers = target?.GetComponentsInChildren<MeshRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = isEnabled;
            }
        }
        else
        {
            Debug.LogWarning($"No Renderers found on {target?.name} or its children!");
        }
    }
}
