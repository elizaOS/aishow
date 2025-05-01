using UnityEditor;
using UnityEngine;
using UnityEngine.Video; // Required for VideoClip
using System.Collections.Generic;
using ShowRunner; // Required to access CommercialManager, CommercialBreak, Commercial

namespace ShowRunner.Editor
{
    /// <summary>
    /// Custom editor for the CommercialManager component.
    /// Provides a more user-friendly interface for managing commercial breaks and settings.
    /// </summary>
    [CustomEditor(typeof(CommercialManager))]
    public class CommercialManagerEditor : UnityEditor.Editor // Explicitly use UnityEditor.Editor
    {
        private SerializedProperty videoDisplayProp;
        private SerializedProperty videoPlayerProp;
        private SerializedProperty commercialCanvasProp;
        private SerializedProperty blackFadePanelProp;
        private SerializedProperty commercialBreaksProp;
        private SerializedProperty skipAllCommercialsProp;
        private SerializedProperty skipFirstNSceneChangesProp;
        private SerializedProperty fadeInDurationProp;
        private SerializedProperty holdDurationProp;
        private SerializedProperty fadeOutDurationProp;

        private bool showBreaksFoldout = true;

        private void OnEnable()
        {
            // Cache serialized properties for efficiency and undo support
            videoDisplayProp = serializedObject.FindProperty("videoDisplay");
            videoPlayerProp = serializedObject.FindProperty("videoPlayer");
            commercialCanvasProp = serializedObject.FindProperty("commercialCanvas");
            blackFadePanelProp = serializedObject.FindProperty("blackFadePanel");
            commercialBreaksProp = serializedObject.FindProperty("commercialBreaks");
            skipAllCommercialsProp = serializedObject.FindProperty("skipAllCommercials");
            skipFirstNSceneChangesProp = serializedObject.FindProperty("skipFirstNSceneChanges");
            fadeInDurationProp = serializedObject.FindProperty("fadeInDuration");
            holdDurationProp = serializedObject.FindProperty("holdDuration");
            fadeOutDurationProp = serializedObject.FindProperty("fadeOutDuration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Always start with this

            EditorGUILayout.LabelField("Component References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(videoDisplayProp);
            EditorGUILayout.PropertyField(videoPlayerProp);
            EditorGUILayout.PropertyField(commercialCanvasProp);
            EditorGUILayout.PropertyField(blackFadePanelProp);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skipAllCommercialsProp);
            EditorGUILayout.PropertyField(skipFirstNSceneChangesProp);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fadeInDurationProp);
            EditorGUILayout.PropertyField(holdDurationProp);
            EditorGUILayout.PropertyField(fadeOutDurationProp);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Commercial Breaks Configuration", EditorStyles.boldLabel);

            // Add new break button
            if (GUILayout.Button("Add Commercial Break"))
            {
                commercialBreaksProp.InsertArrayElementAtIndex(commercialBreaksProp.arraySize);
                SerializedProperty newBreakProp = commercialBreaksProp.GetArrayElementAtIndex(commercialBreaksProp.arraySize - 1);
                
                // Initialize new break properties
                newBreakProp.FindPropertyRelative("breakName").stringValue = "Break " + (commercialBreaksProp.arraySize);
                newBreakProp.FindPropertyRelative("skipThisBreak").boolValue = false;
                newBreakProp.FindPropertyRelative("commercials").ClearArray(); // Ensure commercials list is empty
            }

            EditorGUILayout.Space();

            // Display all breaks using ReorderableList for better UX (Optional but recommended)
            // For simplicity here, we'll use a standard loop with foldout
            showBreaksFoldout = EditorGUILayout.Foldout(showBreaksFoldout, "Commercial Breaks List", true);
            if (showBreaksFoldout)
            {
                EditorGUI.indentLevel++;
                if (commercialBreaksProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No commercial breaks defined. Click 'Add Commercial Break' to create one.", MessageType.Info);
                }

                for (int i = 0; i < commercialBreaksProp.arraySize; i++)
                {
                    SerializedProperty breakProp = commercialBreaksProp.GetArrayElementAtIndex(i);
                    SerializedProperty breakNameProp = breakProp.FindPropertyRelative("breakName");
                    SerializedProperty skipBreakProp = breakProp.FindPropertyRelative("skipThisBreak");
                    SerializedProperty commercialsListProp = breakProp.FindPropertyRelative("commercials");

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Break Header (Name and Remove Button)
                    EditorGUILayout.BeginHorizontal();
                    breakProp.isExpanded = EditorGUILayout.Foldout(breakProp.isExpanded, breakNameProp.stringValue, true);
                    if (GUILayout.Button("Remove Break", GUILayout.Width(100)))
                    {
                        // Display confirmation dialog before removing
                        if (EditorUtility.DisplayDialog("Remove Commercial Break?", 
                                                        $"Are you sure you want to remove the break '{breakNameProp.stringValue}'?", 
                                                        "Remove", "Cancel"))
                        {
                            commercialBreaksProp.DeleteArrayElementAtIndex(i);
                            EditorGUILayout.EndHorizontal(); // Need to end horizontal before breaking loop
                            EditorGUILayout.EndVertical();
                            break; // Exit loop since the array is modified
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // Break Content (if foldout is expanded)
                    if (breakProp.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(breakNameProp, new GUIContent("Break Name"));
                        EditorGUILayout.PropertyField(skipBreakProp, new GUIContent("Skip This Break"));
                        
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Commercials in this Break", EditorStyles.miniBoldLabel);
                        
                        // Add Commercial Button for this break
                        if (GUILayout.Button("Add Commercial"))
                        {
                            commercialsListProp.InsertArrayElementAtIndex(commercialsListProp.arraySize);
                            SerializedProperty newCommercial = commercialsListProp.GetArrayElementAtIndex(commercialsListProp.arraySize - 1);
                            newCommercial.FindPropertyRelative("name").stringValue = "Commercial " + (commercialsListProp.arraySize);
                            newCommercial.FindPropertyRelative("videoClip").objectReferenceValue = null;
                        }
                        
                        // Display Commercials List
                        if (commercialsListProp.arraySize == 0)
                        {
                            EditorGUILayout.HelpBox("No commercials in this break. Click 'Add Commercial' to add one.", MessageType.Info);
                        }

                        for (int j = 0; j < commercialsListProp.arraySize; j++)
                        {
                             SerializedProperty commercialProp = commercialsListProp.GetArrayElementAtIndex(j);
                             SerializedProperty commercialNameProp = commercialProp.FindPropertyRelative("name");
                             SerializedProperty videoClipProp = commercialProp.FindPropertyRelative("videoClip");

                             EditorGUILayout.BeginVertical(EditorStyles.textArea); // Box for each commercial
                             EditorGUILayout.BeginHorizontal();
                             EditorGUILayout.PropertyField(commercialNameProp, GUIContent.none, GUILayout.ExpandWidth(true)); // Name on left
                             
                             // Remove Commercial Button
                             if (GUILayout.Button("X", GUILayout.Width(25)))
                             {
                                 commercialsListProp.DeleteArrayElementAtIndex(j);
                                 EditorGUILayout.EndHorizontal();
                                 EditorGUILayout.EndVertical();
                                 break; // Exit inner loop
                             }
                             EditorGUILayout.EndHorizontal();
                             
                             // Video Clip field
                             EditorGUILayout.PropertyField(videoClipProp, new GUIContent("Video Clip"));
                             EditorGUILayout.EndVertical();
                             EditorGUILayout.Space(2); // Spacing between commercials
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(); // Space between breaks
                }
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties(); // Always end with this
        }
    }
} 