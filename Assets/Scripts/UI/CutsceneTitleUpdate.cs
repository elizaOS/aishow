using UnityEngine;
using TMPro;
using ShowRunner; // Assuming ShowRunner namespace

/// <summary>
/// Updates UI TextMeshPro elements to display the current episode's name
/// and premise. Ensures the target TextMeshPro GameObjects are active before updating.
/// </summary>
public class CutsceneTitleUpdate : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI episodeNameText; // Renamed for clarity, was sceneText
    [SerializeField] private TextMeshProUGUI episodePremiseText; // Renamed for clarity, was descriptionText

    void Start()
    {
        if (ShowRunner.ShowRunner.Instance != null)
        {
            // Subscribe to the modified event that passes episode name and premise
            ShowRunner.ShowRunner.Instance.OnEpisodeSelectedForDisplay += UpdateEpisodeInfo;
        }
        else
        {
            Debug.LogError("CutsceneTitleUpdate: ShowRunner.Instance is null. UI updates will not occur.");
        }

        // Initialize text fields to a default state
        UpdateEpisodeName("Episode Name");
        UpdateEpisodePremise("Episode Premise");
    }

    void OnDestroy()
    {
        if (ShowRunner.ShowRunner.Instance != null)
        {
            ShowRunner.ShowRunner.Instance.OnEpisodeSelectedForDisplay -= UpdateEpisodeInfo;
        }
    }

    /// <summary>
    /// Ensures the GameObject of the TextMeshProUGUI element is active.
    /// </summary>
    /// <param name="textElement">The TextMeshProUGUI element to check and activate.</param>
    private void EnsureTextObjectActive(TextMeshProUGUI textElement)
    {
        if (textElement != null && !textElement.gameObject.activeSelf)
        {
            textElement.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Handles the event when an episode is selected, updating both name and premise.
    /// </summary>
    /// <param name="episodeName">The name of the selected episode.</param>
    /// <param name="episodePremise">The premise of the selected episode.</param>
    private void UpdateEpisodeInfo(string episodeName, string episodePremise)
    {
        UpdateEpisodeName(episodeName);
        UpdateEpisodePremise(episodePremise);
    }

    /// <summary>
    /// Updates the episode name TextMeshPro component.
    /// </summary>
    /// <param name="name">The name of the current episode.</param>
    public void UpdateEpisodeName(string name)
    {
        if (episodeNameText != null)
        {
            EnsureTextObjectActive(episodeNameText);
            episodeNameText.text = name;
        }
        else
        {
            Debug.LogWarning("CutsceneTitleUpdate: episodeNameText reference is not set.");
        }
    }

    /// <summary>
    /// Updates the episode premise TextMeshPro component.
    /// </summary>
    /// <param name="premise">The premise of the current episode.</param>
    public void UpdateEpisodePremise(string premise)
    {
        if (episodePremiseText != null)
        {
            EnsureTextObjectActive(episodePremiseText);
            episodePremiseText.text = premise;
        }
        else
        {
            Debug.LogWarning("CutsceneTitleUpdate: episodePremiseText reference is not set.");
        }
    }
} 