using UnityEngine;
using TMPro;

public class InfiniteScrollText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tickerText;
    [SerializeField] private string tickerMessage = "BREAKING NEWS: HELLO WORLD!";
    [SerializeField] private float scrollSpeed = 100f;

    private RectTransform containerRect;
    private TextMeshProUGUI[] tickerClones;
    private float textWidth;
    private float containerWidth;

    void Awake()
    {
        if (tickerText == null)
        {
            Debug.LogError("Ticker text not assigned!");
            enabled = false;
            return;
        }

        containerRect = GetComponent<RectTransform>();
        if (containerRect == null)
        {
            Debug.LogError("Must be attached to a RectTransform!");
            enabled = false;
            return;
        }

        // Ensure single line
        tickerText.overflowMode = TextOverflowModes.Overflow;
        tickerText.enableWordWrapping = false;
    }

    void Start()
    {
        // Set initial text
        tickerText.text = tickerMessage;
        InitializeTicker();
    }

    void InitializeTicker()
    {
        // Prevent running if not fully initialized
        if (tickerText == null || containerRect == null) return;

        // Ensure text is calculated correctly
        Canvas.ForceUpdateCanvases();
        containerWidth = containerRect.rect.width;
        
        // Calculate number of clones needed
        int cloneCount = Mathf.CeilToInt(containerWidth / tickerText.preferredWidth) + 2;
        
        // Clean up existing clones if any
        if (tickerClones != null)
        {
            for (int i = 1; i < tickerClones.Length; i++)
            {
                if (tickerClones[i] != null && tickerClones[i] != tickerText)
                {
                    Destroy(tickerClones[i].gameObject);
                }
            }
        }

        tickerClones = new TextMeshProUGUI[cloneCount];

        // Create clones horizontally
        for (int i = 0; i < cloneCount; i++)
        {
            TextMeshProUGUI clone = (i == 0) 
                ? tickerText 
                : Instantiate(tickerText, containerRect);
            
            // Ensure clone settings
            clone.overflowMode = TextOverflowModes.Overflow;
            clone.enableWordWrapping = false;
            
            // Position horizontally
            clone.rectTransform.anchorMin = new Vector2(0, 0.5f);
            clone.rectTransform.anchorMax = new Vector2(0, 0.5f);
            clone.rectTransform.pivot = new Vector2(0, 0.5f);
            clone.rectTransform.anchoredPosition = new Vector2(i * tickerText.preferredWidth, 0);

            // Ensure clone has the same text
            clone.text = tickerMessage;

            tickerClones[i] = clone;
        }

        textWidth = tickerText.preferredWidth;
    }

    void Update()
    {
        if (tickerClones == null || tickerClones.Length == 0) return;

        // Move all clones
        float movement = scrollSpeed * Time.deltaTime;
        
        for (int i = 0; i < tickerClones.Length; i++)
        {
            if (tickerClones[i] == null) continue;

            Vector2 pos = tickerClones[i].rectTransform.anchoredPosition;
            pos.x -= movement;
            tickerClones[i].rectTransform.anchoredPosition = pos;

            // Reposition if completely off screen
            if (pos.x <= -textWidth)
            {
                // Find the rightmost clone's position
                float lastCloneX = tickerClones[(i + tickerClones.Length - 1) % tickerClones.Length].rectTransform.anchoredPosition.x;
                tickerClones[i].rectTransform.anchoredPosition = new Vector2(lastCloneX + textWidth, pos.y);
            }
        }
    }

    // Method to update ticker text from script
    public void UpdateText(string newText)
    {
        if (string.IsNullOrEmpty(newText)) return;

        tickerMessage = newText;
        
        // Update the text of the original text component
        if (tickerText != null)
        {
            tickerText.text = tickerMessage;
        }

        // Reinitialize the ticker
        InitializeTicker();
    }

    // This method allows changing the text in the inspector and reinitializing
    private void OnValidate()
    {
        // Only update if in play mode and text is assigned
        if (Application.isPlaying && tickerText != null)
        {
            try 
            {
                UpdateText(tickerMessage);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error updating ticker text: " + e.Message);
            }
        }
    }
}