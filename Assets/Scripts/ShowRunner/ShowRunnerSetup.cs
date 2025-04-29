using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using TMPro;

namespace ShowRunner
{
#if UNITY_EDITOR
    public class ShowRunnerSetup
    {
        [MenuItem("Tools/Show Runner/Create Show Runner")]
        public static void CreateShowRunner()
        {
            // Create the Show Runner parent object
            GameObject showRunnerObj = new GameObject("ShowRunner");
            
            // Add the ShowRunner component
            ShowRunner showRunner = showRunnerObj.AddComponent<ShowRunner>();
            
            // Find the EventProcessor in the scene
            EventProcessor eventProcessor = Object.FindObjectOfType<EventProcessor>();
            if (eventProcessor != null)
            {
                // Assign the EventProcessor reference
                SerializedObject serializedShowRunner = new SerializedObject(showRunner);
                SerializedProperty eventProcessorProp = serializedShowRunner.FindProperty("eventProcessor");
                eventProcessorProp.objectReferenceValue = eventProcessor;
                serializedShowRunner.ApplyModifiedProperties();
                
                Debug.Log("EventProcessor found and assigned to ShowRunner.");
            }
            else
            {
                Debug.LogWarning("EventProcessor not found in the scene. Please assign it manually.");
            }
            
            // Create the UI Container
            GameObject uiContainerObj = CreateShowRunnerUIContainer();
            uiContainerObj.transform.SetParent(showRunnerObj.transform);
            
            // Set up UI references
            ShowRunnerUIContainer uiContainer = uiContainerObj.GetComponent<ShowRunnerUIContainer>();
            ShowRunnerUI showRunnerUI = uiContainerObj.GetComponentInChildren<ShowRunnerUI>();
            
            if (uiContainer != null && showRunnerUI != null)
            {
                SerializedObject serializedUI = new SerializedObject(showRunnerUI);
                SerializedProperty showRunnerProp = serializedUI.FindProperty("showRunner");
                SerializedProperty uiContainerProp = serializedUI.FindProperty("uiContainer");
                
                showRunnerProp.objectReferenceValue = showRunner;
                uiContainerProp.objectReferenceValue = uiContainer;
                serializedUI.ApplyModifiedProperties();
                
                SerializedObject serializedContainer = new SerializedObject(uiContainer);
                SerializedProperty containerShowRunnerProp = serializedContainer.FindProperty("showRunner");
                SerializedProperty containerUIControllerProp = serializedContainer.FindProperty("uiController");
                
                containerShowRunnerProp.objectReferenceValue = showRunner;
                containerUIControllerProp.objectReferenceValue = showRunnerUI;
                serializedContainer.ApplyModifiedProperties();
            }
            
            // Create Episodes directory structure if it doesn't exist
            string episodesPath = Path.Combine(Application.dataPath, "Episodes");
            if (!Directory.Exists(episodesPath))
            {
                Directory.CreateDirectory(episodesPath);
                Debug.Log($"Created directory: {episodesPath}");
                
                // Create a Resources folder within Episodes if needed
                string resourcesPath = Path.Combine(episodesPath, "Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                    Debug.Log($"Created directory: {resourcesPath}");
                }
                
                // Refresh the AssetDatabase after creating folders
                AssetDatabase.Refresh();
            }
            
            // Select the ShowRunner object in the hierarchy
            Selection.activeGameObject = showRunnerObj;
            
            Debug.Log("Show Runner setup complete!");
        }
        
        private static GameObject CreateShowRunnerUIContainer()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("ShowRunnerUI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            GraphicRaycaster graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create UI Container
            GameObject containerObj = new GameObject("UIContainer");
            containerObj.transform.SetParent(canvasObj.transform, false);
            ShowRunnerUIContainer uiContainer = containerObj.AddComponent<ShowRunnerUIContainer>();
            
            // Create Control Panel
            GameObject panelObj = new GameObject("ControlPanel");
            panelObj.transform.SetParent(containerObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Create UI Controller
            GameObject uiControllerObj = new GameObject("UIController");
            uiControllerObj.transform.SetParent(containerObj.transform, false);
            ShowRunnerUI uiController = uiControllerObj.AddComponent<ShowRunnerUI>();
            
            // Create UI Elements
            CreateUIElements(panelObj, uiContainer);
            
            // Set up container references
            SerializedObject serializedContainer = new SerializedObject(uiContainer);
            
            // Set Canvas references
            SerializedProperty canvasProp = serializedContainer.FindProperty("mainCanvas");
            SerializedProperty scalerProp = serializedContainer.FindProperty("canvasScaler");
            SerializedProperty raycasterProp = serializedContainer.FindProperty("graphicRaycaster");
            
            canvasProp.objectReferenceValue = canvas;
            scalerProp.objectReferenceValue = canvasScaler;
            raycasterProp.objectReferenceValue = graphicRaycaster;
            
            // Set Panel references
            SerializedProperty panelProp = serializedContainer.FindProperty("controlPanel");
            SerializedProperty panelBgProp = serializedContainer.FindProperty("controlPanelBackground");
            
            panelProp.objectReferenceValue = panelRect;
            panelBgProp.objectReferenceValue = panelImage;
            
            serializedContainer.ApplyModifiedProperties();
            
            return canvasObj;
        }
        
        private static void CreateUIElements(GameObject panel, ShowRunnerUIContainer container)
        {
            // Create Episode Dropdown
            GameObject dropdownObj = CreateUIElement("EpisodeDropdown", panel.transform);
            RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(300, 40);
            dropdownRect.anchoredPosition = new Vector2(170, 100);
            
            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            Image dropdownImage = dropdownObj.AddComponent<Image>();
            
            // Create Load Button
            GameObject loadButtonObj = CreateUIElement("LoadButton", panel.transform);
            RectTransform loadButtonRect = loadButtonObj.GetComponent<RectTransform>();
            loadButtonRect.sizeDelta = new Vector2(120, 40);
            loadButtonRect.anchoredPosition = new Vector2(340, 100);
            
            Button loadButton = loadButtonObj.AddComponent<Button>();
            Image loadButtonImage = loadButtonObj.AddComponent<Image>();
            CreateTextElement("Text", loadButtonObj.transform, "Load");
            
            // Create Next Button
            GameObject nextButtonObj = CreateUIElement("NextButton", panel.transform);
            RectTransform nextButtonRect = nextButtonObj.GetComponent<RectTransform>();
            nextButtonRect.sizeDelta = new Vector2(120, 40);
            nextButtonRect.anchoredPosition = new Vector2(120, 50);
            
            Button nextButton = nextButtonObj.AddComponent<Button>();
            nextButton.interactable = false;
            Image nextButtonImage = nextButtonObj.AddComponent<Image>();
            CreateTextElement("Text", nextButtonObj.transform, "Next");
            
            // Create Play Button
            GameObject playButtonObj = CreateUIElement("PlayButton", panel.transform);
            RectTransform playButtonRect = playButtonObj.GetComponent<RectTransform>();
            playButtonRect.sizeDelta = new Vector2(120, 40);
            playButtonRect.anchoredPosition = new Vector2(250, 50);
            
            Button playButton = playButtonObj.AddComponent<Button>();
            playButton.interactable = false;
            Image playButtonImage = playButtonObj.AddComponent<Image>();
            CreateTextElement("Text", playButtonObj.transform, "Play");
            
            // Create Pause Button
            GameObject pauseButtonObj = CreateUIElement("PauseButton", panel.transform);
            RectTransform pauseButtonRect = pauseButtonObj.GetComponent<RectTransform>();
            pauseButtonRect.sizeDelta = new Vector2(120, 40);
            pauseButtonRect.anchoredPosition = new Vector2(380, 50);
            
            Button pauseButton = pauseButtonObj.AddComponent<Button>();
            pauseButton.interactable = false;
            Image pauseButtonImage = pauseButtonObj.AddComponent<Image>();
            CreateTextElement("Text", pauseButtonObj.transform, "Pause");
            
            // Create Status Text
            GameObject statusObj = CreateUIElement("StatusText", panel.transform);
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(500, 30);
            statusRect.anchoredPosition = new Vector2(260, 10);
            
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "Ready";
            statusText.color = Color.white;
            statusText.fontSize = 16;
            statusText.alignment = TextAlignmentOptions.Center;
            
            // Set up container references
            SerializedObject serializedContainer = new SerializedObject(container);
            
            SerializedProperty dropdownProp = serializedContainer.FindProperty("episodeDropdown");
            SerializedProperty loadButtonProp = serializedContainer.FindProperty("loadButton");
            SerializedProperty nextButtonProp = serializedContainer.FindProperty("nextButton");
            SerializedProperty playButtonProp = serializedContainer.FindProperty("playButton");
            SerializedProperty pauseButtonProp = serializedContainer.FindProperty("pauseButton");
            SerializedProperty statusTextProp = serializedContainer.FindProperty("statusText");
            
            dropdownProp.objectReferenceValue = dropdown;
            loadButtonProp.objectReferenceValue = loadButton;
            nextButtonProp.objectReferenceValue = nextButton;
            playButtonProp.objectReferenceValue = playButton;
            pauseButtonProp.objectReferenceValue = pauseButton;
            statusTextProp.objectReferenceValue = statusText;
            
            serializedContainer.ApplyModifiedProperties();
        }
        
        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            return obj;
        }
        
        private static TextMeshProUGUI CreateTextElement(string name, Transform parent, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.color = Color.white;
            tmpText.fontSize = 18;
            tmpText.alignment = TextAlignmentOptions.Center;
            
            return tmpText;
        }
    }
#endif
} 