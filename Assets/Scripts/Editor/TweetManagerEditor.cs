using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TweetManager))]
public class TweetManagerEditor : Editor
{
    // Editable fields for testing in the inspector
    private Texture profilePicTex;
    private new string name = "";
    private string url = "";
    private string date = "";
    private string tweet = "";
    private Texture mediaTex;
    private string photoCredit = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Shows layoutGroupPanel, etc.

        TweetManager manager = (TweetManager)target;
        if (manager == null) return;

        GameObject panelToToggle = manager.layoutGroupPanel; // Use the assigned panel

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tweet Control (Editor Test)", EditorStyles.boldLabel);

        if (panelToToggle == null)
        {
            EditorGUILayout.HelpBox("The 'Layout Group Panel' is NOT ASSIGNED in the TweetManager's Inspector. Please assign it to enable editor controls.", MessageType.Error);
            // Disable controls if panel is not assigned
            GUI.enabled = false;
        }
        else
        {
            EditorGUILayout.LabelField($"Target Panel: {panelToToggle.name} (Active: {panelToToggle.activeSelf})");
        }

        // Data input fields
        profilePicTex = (Texture)EditorGUILayout.ObjectField("Profile Pic", profilePicTex, typeof(Texture), false);
        name = EditorGUILayout.TextField("Name", name);
        url = EditorGUILayout.TextField("URL", url);
        date = EditorGUILayout.TextField("Date", date);
        tweet = EditorGUILayout.TextField("Tweet Text", tweet);
        mediaTex = (Texture)EditorGUILayout.ObjectField("Tweet Media", mediaTex, typeof(Texture), false);
        photoCredit = EditorGUILayout.TextField("Photo Credit", photoCredit);

        if (GUILayout.Button("Load Data & Show/Refresh Tweet"))
        {
            if (panelToToggle == null) return;

            // 1. Turn off the panel before loading new data
            if (panelToToggle.activeSelf)
            {
                panelToToggle.SetActive(false);
            }

            // 2. Load data by calling direct setters
            // This is generally more reliable for editor scripts than relying on events.
            manager.SetProfilePic(profilePicTex);
            manager.SetName(name);
            manager.SetURL(url);
            manager.SetDate(date);
            manager.SetTweetText(tweet);
            manager.SetTweetMedia(mediaTex); // SetTweetMedia contains its own refresh logic for Play mode
            manager.SetPhotoCredit(photoCredit);

            // 3. Activate the panel
            panelToToggle.SetActive(true);

            // If in Edit mode, force a repaint of editor views to ensure UI elements controlled by TweetManager update visually.
            if (!Application.isPlaying)
            {
                InternalEditorUtility.RepaintAllViews();
                // Also, if TweetManager itself has [ExecuteAlways] and updates UI components directly in setters,
                // marking it dirty can help ensure those changes are saved and reflected.
                EditorUtility.SetDirty(manager); 
            }
        }

        if (GUILayout.Button("Hide Tweet Panel"))
        {
            if (panelToToggle != null)
            {
                panelToToggle.SetActive(false);
            }
        }
        
        // Re-enable GUI if it was disabled
        if (panelToToggle == null) GUI.enabled = true;
    }
} 