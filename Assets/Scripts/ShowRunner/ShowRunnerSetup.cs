using UnityEngine;
using UnityEditor;
using System.IO;

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
            
            // Create the UI Canvas
            GameObject canvasObj = CreateShowRunnerUI();
            canvasObj.transform.SetParent(showRunnerObj.transform);
            
            // Set up UI references
            ShowRunnerUI showRunnerUI = canvasObj.GetComponentInChildren<ShowRunnerUI>();
            if (showRunnerUI != null)
            {
                SerializedObject serializedUI = new SerializedObject(showRunnerUI);
                SerializedProperty showRunnerProp = serializedUI.FindProperty("showRunner");
                showRunnerProp.objectReferenceValue = showRunner;
                serializedUI.ApplyModifiedProperties();
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
        
        private static GameObject CreateShowRunnerUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("ShowRunnerUI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create UI Panel
            GameObject panelObj = new GameObject("ControlPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            // Add programmatically constructed TMP_Dropdown hierarchy
            // Root dropdown object
            GameObject dropdownObj = new GameObject("EpisodeDropdown", typeof(RectTransform), typeof(TMPro.TMP_Dropdown), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            dropdownObj.transform.SetParent(panelObj.transform, false);
            var dropdownRT = dropdownObj.GetComponent<RectTransform>();
            dropdownRT.sizeDelta = new Vector2(300, 40);
            dropdownRT.anchoredPosition = new Vector2(170, 100);
            var dropdown = dropdownObj.GetComponent<TMPro.TMP_Dropdown>();
            // Caption Text
            GameObject captionObj = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            captionObj.transform.SetParent(dropdownObj.transform, false);
            var captionRT = captionObj.GetComponent<RectTransform>();
            captionRT.anchorMin = new Vector2(0, 0);
            captionRT.anchorMax = new Vector2(1, 1);
            captionRT.offsetMin = new Vector2(10, 6);
            captionRT.offsetMax = new Vector2(-25, -7);
            var captionText = captionObj.GetComponent<TMPro.TextMeshProUGUI>();
            captionText.text = "Select...";
            dropdown.captionText = captionText;
            // Arrow icon (empty image by default)
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            var arrowRT = arrowObj.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0);
            arrowRT.anchorMax = new Vector2(1, 1);
            arrowRT.sizeDelta = new Vector2(20, 20);
            arrowRT.anchoredPosition = new Vector2(-15, 0);
            // Template for dropdown list
            GameObject templateObj = new GameObject("Template", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            var templateRT = templateObj.GetComponent<RectTransform>();
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta = new Vector2(0, 150);
            // Viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            viewportObj.transform.SetParent(templateObj.transform, false);
            var viewportRT = viewportObj.GetComponent<RectTransform>();
            viewportRT.anchorMin = new Vector2(0, 0);
            viewportRT.anchorMax = new Vector2(1, 1);
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            // Content container
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = Vector2.zero;
            var layout = contentObj.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = contentObj.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            // Item template
            GameObject itemObj = new GameObject("Item", typeof(RectTransform), typeof(UnityEngine.UI.Toggle), typeof(UnityEngine.UI.Image));
            itemObj.transform.SetParent(contentObj.transform, false);
            var itemRT = itemObj.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 20);
            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            var itemLabelRT = itemLabelObj.GetComponent<RectTransform>();
            itemLabelRT.anchorMin = new Vector2(0, 0);
            itemLabelRT.anchorMax = new Vector2(1, 1);
            itemLabelRT.offsetMin = new Vector2(20, 1);
            itemLabelRT.offsetMax = new Vector2(-10, -2);
            var itemLabel = itemLabelObj.GetComponent<TMPro.TextMeshProUGUI>();
            // Assign dropdown references
            dropdown.template = templateRT;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            
            GameObject loadButtonObj = CreateUIElement("LoadButton", panelObj.transform);
            RectTransform loadButtonRect = loadButtonObj.GetComponent<RectTransform>();
            loadButtonRect.sizeDelta = new Vector2(120, 40);
            loadButtonRect.anchoredPosition = new Vector2(340, 100);
            UnityEngine.UI.Button loadButton = loadButtonObj.AddComponent<UnityEngine.UI.Button>();
            TMPro.TextMeshProUGUI loadButtonText = CreateTextElement("Text", loadButtonObj.transform, "Load");
            
            GameObject nextButtonObj = CreateUIElement("NextButton", panelObj.transform);
            RectTransform nextButtonRect = nextButtonObj.GetComponent<RectTransform>();
            nextButtonRect.sizeDelta = new Vector2(120, 40);
            nextButtonRect.anchoredPosition = new Vector2(120, 50);
            UnityEngine.UI.Button nextButton = nextButtonObj.AddComponent<UnityEngine.UI.Button>();
            nextButton.interactable = false; // Start disabled until episode is loaded
            TMPro.TextMeshProUGUI nextButtonText = CreateTextElement("Text", nextButtonObj.transform, "Next");
            
            GameObject playButtonObj = CreateUIElement("PlayButton", panelObj.transform);
            RectTransform playButtonRect = playButtonObj.GetComponent<RectTransform>();
            playButtonRect.sizeDelta = new Vector2(120, 40);
            playButtonRect.anchoredPosition = new Vector2(250, 50);
            UnityEngine.UI.Button playButton = playButtonObj.AddComponent<UnityEngine.UI.Button>();
            playButton.interactable = false; // Start disabled until episode is loaded
            TMPro.TextMeshProUGUI playButtonText = CreateTextElement("Text", playButtonObj.transform, "Play");
            
            GameObject pauseButtonObj = CreateUIElement("PauseButton", panelObj.transform);
            RectTransform pauseButtonRect = pauseButtonObj.GetComponent<RectTransform>();
            pauseButtonRect.sizeDelta = new Vector2(120, 40);
            pauseButtonRect.anchoredPosition = new Vector2(380, 50);
            UnityEngine.UI.Button pauseButton = pauseButtonObj.AddComponent<UnityEngine.UI.Button>();
            pauseButton.interactable = false; // Start disabled until playback is started
            TMPro.TextMeshProUGUI pauseButtonText = CreateTextElement("Text", pauseButtonObj.transform, "Pause");
            
            GameObject statusObj = CreateUIElement("StatusText", panelObj.transform);
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(500, 30);
            statusRect.anchoredPosition = new Vector2(260, 10);
            TMPro.TextMeshProUGUI statusText = statusObj.AddComponent<TMPro.TextMeshProUGUI>();
            statusText.text = "Ready";
            statusText.color = Color.white;
            statusText.fontSize = 16;
            statusText.alignment = TMPro.TextAlignmentOptions.Center;
            
            // Add ShowRunnerUI component
            ShowRunnerUI showRunnerUI = panelObj.AddComponent<ShowRunnerUI>();
            
            // Set up references
            SerializedObject serializedUI = new SerializedObject(showRunnerUI);
            SerializedProperty dropdownProp = serializedUI.FindProperty("episodeDropdown");
            SerializedProperty loadButtonProp = serializedUI.FindProperty("loadButton");
            SerializedProperty nextButtonProp = serializedUI.FindProperty("nextButton");
            SerializedProperty playButtonProp = serializedUI.FindProperty("playButton");
            SerializedProperty pauseButtonProp = serializedUI.FindProperty("pauseButton");
            SerializedProperty statusTextProp = serializedUI.FindProperty("statusText");
            
            dropdownProp.objectReferenceValue = dropdown;
            loadButtonProp.objectReferenceValue = loadButton;
            nextButtonProp.objectReferenceValue = nextButton;
            playButtonProp.objectReferenceValue = playButton;
            pauseButtonProp.objectReferenceValue = pauseButton;
            statusTextProp.objectReferenceValue = statusText;
            
            serializedUI.ApplyModifiedProperties();
            
            return canvasObj;
        }
        
        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            return obj;
        }
        
        private static TMPro.TextMeshProUGUI CreateTextElement(string name, Transform parent, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.color = Color.white;
            tmpText.fontSize = 18;
            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
            
            return tmpText;
        }
    }
#endif
} 