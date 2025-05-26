using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HeadlineScroller : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI tickerText;
    [SerializeField] public float scrollSpeed = 100f;
    [SerializeField] private TMP_FontAsset emojiFont; // Reference to the emoji font asset

    private RectTransform textRect;
    private RectTransform parentRect;

    void Start()
    {
        textRect = tickerText.rectTransform;
        parentRect = GetComponent<RectTransform>();

        // Ensure we have a font that supports emojis
        if (emojiFont == null)
        {
            // Try to find the EmojiOne font asset
            emojiFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/EmojiOne");
            if (emojiFont == null)
            {
                Debug.LogWarning("Emoji font not found. Some characters may not display correctly.");
            }
            else
            {
                tickerText.font = emojiFont;
            }
        }

        // Add ContentSizeFitter if not present
        if (tickerText.GetComponent<ContentSizeFitter>() == null)
        {
            var fitter = tickerText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Set anchors and pivot for left alignment
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(0, 0.5f);
        textRect.pivot = new Vector2(0, 0.5f);

        UpdateTextPosition();
    }

    void Update()
    {
        // Force layout update
        tickerText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);

        float textWidth = textRect.rect.width;
        float parentWidth = parentRect.rect.width;

        Vector2 pos = textRect.anchoredPosition;
        pos.x -= scrollSpeed * Time.deltaTime;

        if (pos.x + textWidth <= 0)
        {
            pos.x = parentWidth;
        }

        textRect.anchoredPosition = pos;
    }

    public void UpdateTextPosition()
    {
        // Call this after changing the text
        tickerText.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        textRect.anchoredPosition = new Vector2(parentRect.rect.width, 0);
    }
} 