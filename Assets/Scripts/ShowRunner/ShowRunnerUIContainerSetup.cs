using UnityEngine;
using UnityEditor;

namespace ShowRunner
{
#if UNITY_EDITOR
    public class ShowRunnerUIContainerSetup
    {
        [MenuItem("Tools/Show Runner/Create UI Container")]
        public static void CreateUIContainer()
        {
            // Create the UI Container
            GameObject containerObj = new GameObject("ShowRunnerUIContainer");
            ShowRunnerUIContainer uiContainer = containerObj.AddComponent<ShowRunnerUIContainer>();
            
            // Create the UI Controller
            GameObject uiControllerObj = new GameObject("ShowRunnerUI");
            uiControllerObj.transform.SetParent(containerObj.transform, false);
            ShowRunnerUI uiController = uiControllerObj.AddComponent<ShowRunnerUI>();
            
            // Find the ShowRunner in the scene
            ShowRunner showRunner = Object.FindObjectOfType<ShowRunner>();
            if (showRunner != null)
            {
                // Set up references for ShowRunner
                SerializedObject containerObj1 = new SerializedObject(uiContainer);
                SerializedObject uiObj1 = new SerializedObject(uiController);
                
                // Set ShowRunner reference in container
                SerializedProperty containerShowRunnerProp = containerObj1.FindProperty("showRunner");
                containerShowRunnerProp.objectReferenceValue = showRunner;
                containerObj1.ApplyModifiedProperties();
                
                // Set ShowRunner reference in UI controller
                SerializedProperty uiShowRunnerProp = uiObj1.FindProperty("showRunner");
                uiShowRunnerProp.objectReferenceValue = showRunner;
                uiObj1.ApplyModifiedProperties();
                
                //Debug.Log("ShowRunner found and assigned to UI Container.");
            }
            else
            {
                //Debug.LogWarning("ShowRunner not found in the scene. Please assign it manually.");
            }
            
            // Set up UI controller reference in container
            SerializedObject containerObj2 = new SerializedObject(uiContainer);
            SerializedProperty uiControllerProp = containerObj2.FindProperty("uiController");
            uiControllerProp.objectReferenceValue = uiController;
            containerObj2.ApplyModifiedProperties();
            
            // Set up UI container reference in UI controller
            SerializedObject uiObj2 = new SerializedObject(uiController);
            SerializedProperty uiContainerProp = uiObj2.FindProperty("uiContainer");
            uiContainerProp.objectReferenceValue = uiContainer;
            uiObj2.ApplyModifiedProperties();
            
            // Select the container in the hierarchy
            Selection.activeGameObject = containerObj;
            
            //Debug.Log("UI Container setup complete! Now assign your UI elements in the inspector.");
        }
    }
#endif
} 