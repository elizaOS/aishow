using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextureLoader))]
public class TextureLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();

        // Add a button to test the texture loading
        TextureLoader textureLoader = (TextureLoader)target;
        if (GUILayout.Button("Load Emissive Texture"))
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test button only works in Play Mode.");
            }
            else
            {
                textureLoader.LoadEmissiveTexture();
            }
        }
    }
}
