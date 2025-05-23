using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MusicNodController))]
public class MusicNodControllerEditor : Editor
{
    private bool showDebugWindow = false;
    private Vector2 scrollPosition;
    private List<float> beatHistory = new List<float>();
    private const int MAX_HISTORY = 50;
    private float[] frequencyBands = new float[8];
    private float currentBeatValue = 0f;

    private void OnEnable()
    {
        // Start repainting the editor window
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        // Stop repainting when disabled
        EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        MusicNodController controller = (MusicNodController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Enable Nodding"))
        {
            controller.EnableNodding();
        }
        
        if (GUILayout.Button("Disable Nodding"))
        {
            controller.DisableNodding();
        }
        
        if (GUILayout.Button("Toggle Nodding"))
        {
            controller.ToggleNodding();
        }

        EditorGUILayout.Space();
        showDebugWindow = EditorGUILayout.Foldout(showDebugWindow, "Debug Visualization");
        
        if (showDebugWindow)
        {
            DrawDebugWindow(controller);
        }

        // Force continuous repaint while debug window is open
        if (showDebugWindow)
        {
            Repaint();
        }
    }

    private void DrawDebugWindow(MusicNodController controller)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Get the current values through reflection
        var frequencyBandsField = controller.GetType().GetField("frequencyBands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bandBufferField = controller.GetType().GetField("bandBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var beatThresholdField = controller.GetType().GetField("beatThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (frequencyBandsField != null && bandBufferField != null && beatThresholdField != null)
        {
            frequencyBands = (float[])frequencyBandsField.GetValue(controller);
            float[] bandBuffer = (float[])bandBufferField.GetValue(controller);
            float beatThreshold = (float)beatThresholdField.GetValue(controller);

            // Draw frequency bands
            EditorGUILayout.LabelField("Frequency Bands", EditorStyles.boldLabel);
            for (int i = 0; i < 8; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Band {i}", GUILayout.Width(60));
                Rect rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, bandBuffer[i], $"{bandBuffer[i]:F2}");
                EditorGUILayout.EndHorizontal();
            }

            // Draw beat detection
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Beat Detection", EditorStyles.boldLabel);
            
            // Calculate current beat value
            float weightedSum = 0f;
            float totalWeight = 0f;
            for (int i = 0; i < 8; i++)
            {
                weightedSum += bandBuffer[i] * controller.GetBandWeight(i);
                totalWeight += controller.GetBandWeight(i);
            }
            currentBeatValue = weightedSum / totalWeight;

            // Update beat history
            beatHistory.Add(currentBeatValue);
            if (beatHistory.Count > MAX_HISTORY)
                beatHistory.RemoveAt(0);

            // Draw beat threshold slider
            float newThreshold = EditorGUILayout.Slider("Beat Threshold", beatThreshold, 0f, 5f);
            if (newThreshold != beatThreshold)
            {
                beatThresholdField.SetValue(controller, newThreshold);
            }

            // Draw beat visualization
            Rect beatRect = EditorGUILayout.GetControlRect(false, 50);
            DrawBeatVisualization(beatRect, currentBeatValue, beatThreshold);

            // Draw beat history graph
            Rect graphRect = EditorGUILayout.GetControlRect(false, 100);
            DrawBeatHistoryGraph(graphRect);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBeatVisualization(Rect rect, float currentValue, float threshold)
    {
        // Draw background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        // Draw threshold line
        float thresholdY = rect.y + rect.height * (1f - threshold / 5f);
        EditorGUI.DrawRect(new Rect(rect.x, thresholdY, rect.width, 1f), Color.red);

        // Draw current value
        float valueY = rect.y + rect.height * (1f - currentValue / 5f);
        float valueHeight = Mathf.Max(1f, rect.height * (currentValue / 5f));
        EditorGUI.DrawRect(new Rect(rect.x, valueY, rect.width, valueHeight), Color.green);

        // Draw beat indicator
        if (currentValue > threshold)
        {
            float beatSize = 20f;
            float beatX = rect.x + rect.width - beatSize;
            float beatY = rect.y + (rect.height - beatSize) / 2f;
            EditorGUI.DrawRect(new Rect(beatX, beatY, beatSize, beatSize), Color.red);
        }
    }

    private void DrawBeatHistoryGraph(Rect rect)
    {
        // Draw background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        if (beatHistory.Count > 1)
        {
            // Draw line graph
            Handles.BeginGUI();
            Handles.color = Color.green;
            
            float xStep = rect.width / (MAX_HISTORY - 1);
            float yScale = rect.height / 5f; // Assuming max value is 5

            for (int i = 1; i < beatHistory.Count; i++)
            {
                float x1 = rect.x + (i - 1) * xStep;
                float y1 = rect.y + rect.height - beatHistory[i - 1] * yScale;
                float x2 = rect.x + i * xStep;
                float y2 = rect.y + rect.height - beatHistory[i] * yScale;
                
                Handles.DrawLine(new Vector3(x1, y1, 0), new Vector3(x2, y2, 0));
            }
            
            Handles.EndGUI();
        }
    }
} 