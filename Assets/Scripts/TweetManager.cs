using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the display of a tweet panel, allowing dynamic updates to all tweet fields.
/// Attach this script to the root Tweet GameObject.
/// </summary>
[ExecuteAlways] // Allows updates in Edit mode as well as Play mode
public class TweetManager : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private RawImage profilePic;
    
    [Header("Author Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text urlText;
    [SerializeField] private TMP_Text hyphenText;
    [SerializeField] private TMP_Text dateText;

    [Header("Tweet Content")]
    [SerializeField] private TMP_Text tweetText;

    [Header("Media")]
    [SerializeField] private RectTransform tweetMediaContainer; // Assign this to the parent container in the Inspector

    [SerializeField] private RawImage tweetMedia;
    [SerializeField] private TMP_Text photoCreditText;

    [Header("Panel Refresh")]
    [SerializeField] private GameObject panelToRefresh; // Assign the panel that needs to be toggled in the Inspector

    [Header("Layout Root")]
    [SerializeField] private RectTransform layoutRoot; // Assign the top-level tweet panel or parent RectTransform

    [Header("Layout Group Panel")]
    public GameObject layoutGroupPanel; // Assign the parent with the Layout Group. Made public for editor script access.

    /// <summary>
    /// Event for updating the tweet UI. Subscribe and invoke this event to update all tweet fields (except hyphen).
    /// </summary>
    public event Action<Texture, string, string, string, string, Texture, string> OnTweetUpdate;

    private void Awake()
    {
        // Subscribe the internal handler to the event
        OnTweetUpdate += HandleTweetUpdate;
    }

    /// <summary>
    /// Handles tweet updates by setting all fields except hyphen.
    /// </summary>
    private void HandleTweetUpdate(
        Texture profilePicTex,
        string name,
        string url,
        string date,
        string tweet,
        Texture mediaTex,
        string photoCredit)
    {
        SetProfilePic(profilePicTex);
        SetName(name);
        SetURL(url);
        SetDate(date);
        SetTweetText(tweet);
        SetTweetMedia(mediaTex);
        SetPhotoCredit(photoCredit);
    }

    /// <summary>
    /// Call this method to update the tweet via the event.
    /// </summary>
    public void UpdateTweet(
        Texture profilePicTex,
        string name,
        string url,
        string date,
        string tweet,
        Texture mediaTex,
        string photoCredit)
    {
        OnTweetUpdate?.Invoke(profilePicTex, name, url, date, tweet, mediaTex, photoCredit);
    }

    /// <summary>
    /// Sets the profile picture.
    /// </summary>
    public void SetProfilePic(Texture texture)
    {
        if (profilePic != null)
            profilePic.texture = texture;
    }

    /// <summary>
    /// Sets the tweet author's name.
    /// </summary>
    public void SetName(string name)
    {
        if (nameText != null)
            nameText.text = name;
    }

    /// <summary>
    /// Sets the tweet author's URL.
    /// </summary>
    public void SetURL(string url)
    {
        if (urlText != null)
            urlText.text = url;
    }

    /// <summary>
    /// Sets the hyphen text (usually a separator).
    /// </summary>
    public void SetHyphen(string hyphen)
    {
        if (hyphenText != null)
            hyphenText.text = hyphen;
    }

    /// <summary>
    /// Sets the tweet date.
    /// </summary>
    public void SetDate(string date)
    {
        if (dateText != null)
            dateText.text = date;
    }

    /// <summary>
    /// Sets the main tweet text.
    /// </summary>
    public void SetTweetText(string text)
    {
        if (tweetText != null)
            tweetText.text = text;
    }

    /// <summary>
    /// Sets the tweet media image and rescales both the container and image to fit a 320x320 box, preserving aspect ratio.
    /// </summary>
    public void SetTweetMedia(Texture texture)
    {
        if (tweetMedia != null && tweetMediaContainer != null)
        {
            tweetMedia.texture = texture;

            if (texture != null)
            {
                float containerSize = 320f;
                float texWidth = texture.width;
                float texHeight = texture.height;
                float aspect = texWidth / texHeight;

                float newWidth, newHeight;

                if (aspect > 1f)
                {
                    newWidth = containerSize;
                    newHeight = containerSize / aspect;
                }
                else if (aspect < 1f)
                {
                    newHeight = containerSize;
                    newWidth = containerSize * aspect;
                }
                else
                {
                    newWidth = containerSize;
                    newHeight = containerSize;
                }

                tweetMediaContainer.sizeDelta = new Vector2(newWidth, newHeight);
                tweetMedia.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
            }

#if !UNITY_EDITOR
            // Toggle the layout group parent to force a refresh
            if (Application.isPlaying && layoutGroupPanel != null && layoutGroupPanel.activeInHierarchy)
            {
                StartCoroutine(RefreshLayoutGroupPanelCoroutine());
            }
#endif
        }
    }

    private IEnumerator RefreshLayoutGroupPanelCoroutine()
    {
        layoutGroupPanel.SetActive(false);
        yield return null; // Wait one frame
        layoutGroupPanel.SetActive(true);

        // Extra robust: force layout update after toggling
        Canvas.ForceUpdateCanvases();
        var rect = layoutGroupPanel.GetComponent<RectTransform>();
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    /// <summary>
    /// Sets the photo credit text.
    /// </summary>
    public void SetPhotoCredit(string credit)
    {
        if (photoCreditText != null)
            photoCreditText.text = credit;
    }
}  