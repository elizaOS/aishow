using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[ExecuteInEditMode]
public class TextureLoader : MonoBehaviour
{
    [Header("Texture Loader Settings")]
    public Renderer[] targetRenderers; // Reference to the renderers of the objects
    public string textureURL;       // URL for the texture to load
    public RenderTexture targetRenderTexture; // RenderTexture to update

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

                // Apply the texture to the Emission channel of all targetRenderers
                if (targetRenderers != null && targetRenderers.Length > 0)
                {
                    foreach (Renderer renderer in targetRenderers)
                    {
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.SetTexture("_EmissionMap", texture);
                            renderer.material.EnableKeyword("_EMISSION");
                            Debug.Log($"Successfully applied the texture to the Emission channel of {renderer.gameObject.name}.");
                        }
                        else
                        {
                            Debug.LogWarning("A Target Renderer or its Material is not assigned in the array.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Target Renderers array is not assigned or is empty.");
                }

                // Update the RenderTexture
                if (targetRenderTexture != null)
                {
                    Graphics.Blit(texture, targetRenderTexture);
                    Debug.Log("Successfully updated the Target RenderTexture.");
                }
                else
                {
                    Debug.LogWarning("Target RenderTexture is not assigned.");
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
