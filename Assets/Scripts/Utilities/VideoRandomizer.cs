using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Random = UnityEngine.Random;

/// <summary>
/// Manages a collection of video clips and plays them in random order.
/// Automatically transitions to the next clip when one finishes and
/// provides methods for manual control.
/// </summary>
public class VideoRandomizer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] videoClips;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool shuffleOnStart = true;
    [SerializeField] private float transitionDelay = 0.5f;
    [SerializeField] private bool loopFirstVideo = false;

    [Header("Debug")]
    [SerializeField] private bool logVideoChanges = true;

    // Event triggered when the video changes, passes the index of the new video
    public event Action<int> OnVideoChanged;

    // Internal state
    private List<int> playbackOrder = new List<int>();
    private int currentVideoIndex = -1;
    private int currentPlaybackIndex = -1;
    private bool isPlaying = false;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        // Validate and initialize references
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                Debug.LogWarning("VideoPlayer component added automatically to VideoRandomizer. Consider assigning it manually.");
            }
        }

        // Set up video player events
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void Start()
    {
        if (videoClips == null || videoClips.Length == 0)
        {
            Debug.LogError("No video clips assigned to VideoRandomizer.");
            return;
        }

        if (loopFirstVideo)
        {
            // In loop mode, VideoPlayer component handles the actual looping.
            videoPlayer.isLooping = true;
            if (playOnAwake && videoClips.Length > 0)
            {
                isPlaying = true; // Set our desired state
                ChangeVideo(0);   // This will prepare and play the first video
            }
            else
            {
                isPlaying = false; // Not playing on awake
                if (videoClips.Length > 0) // Prepare the first clip but don't play
                {
                    // Minimal preparation: set currentVideoIndex and potentially the clip if needed elsewhere
                    // TransitionToVideo will handle full setup when ChangeVideo is called.
                    currentVideoIndex = 0;
                    // To fully prepare without playing, one might call videoPlayer.Prepare()
                    // after setting the clip, but ChangeVideo handles this if isPlaying becomes true.
                }
            }
        }
        else // Random mode
        {
            videoPlayer.isLooping = false; // VideoRandomizer handles transitions
            if (shuffleOnStart)
            {
                ShufflePlaybackOrder();
            }

            if (playOnAwake)
            {
                Play(); // Play will set isPlaying = true and start the sequence
            }
            else
            {
                isPlaying = false; // Not playing on awake
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        // Stop any active coroutines
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
    }

    /// <summary>
    /// Shuffles the playback order for a complete new round-robin sequence.
    /// </summary>
    public void ShufflePlaybackOrder()
    {
        if (loopFirstVideo)
        {
            if (logVideoChanges) Debug.LogWarning("VideoRandomizer: ShufflePlaybackOrder called in single video loop mode. No action taken.");
            return;
        }

        playbackOrder.Clear();
        
        // Create a list of all available video indices
        for (int i = 0; i < videoClips.Length; i++)
        {
            playbackOrder.Add(i);
        }

        // Shuffle the indices using Fisher-Yates algorithm
        for (int i = playbackOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = playbackOrder[i];
            playbackOrder[i] = playbackOrder[randomIndex];
            playbackOrder[randomIndex] = temp;
        }

        currentPlaybackIndex = -1;

        if (logVideoChanges)
        {
            Debug.Log("VideoRandomizer: Playback order shuffled.");
        }
    }

    /// <summary>
    /// Starts or resumes playback of random videos.
    /// </summary>
    public void Play()
    {
        if (videoClips.Length == 0) return;

        isPlaying = true; // Set the intent to play

        if (loopFirstVideo)
        {
            videoPlayer.isLooping = true; // Ensure VideoPlayer is set to loop
            // If the current clip is not the first one, or if the player is not active (e.g. stopped/paused), change/play it.
            if (currentVideoIndex != 0 || videoPlayer.clip != videoClips[0] || !videoPlayer.isPlaying)
            {
                // ChangeVideo will handle transition and playing because isPlaying is true.
                ChangeVideo(0);
            }
            // If it's already videoClips[0] and videoPlayer.isPlaying is true, this call effectively does nothing new.
        }
        else // Random mode
        {
            videoPlayer.isLooping = false; // Ensure VideoPlayer is NOT set to loop for random mode

            // If we haven't started yet or completed the previous sequence, get a new video
            if (currentPlaybackIndex == -1 || currentPlaybackIndex >= playbackOrder.Count)
            {
                // If we completed a full sequence, reshuffle
                if (currentPlaybackIndex >= playbackOrder.Count && playbackOrder.Count > 0)
                {
                    ShufflePlaybackOrder();
                }
                NextVideo(); // NextVideo calls ChangeVideo, which will play if isPlaying is true
            }
            else
            {
                // Resume the current video if it's already assigned and paused, or get next if no clip
                if (videoPlayer.clip != null)
                {
                    if (!videoPlayer.isPlaying) // Only play if it's not already playing
                    {
                        videoPlayer.Play();
                    }
                }
                else
                {
                    NextVideo(); // If no clip but playback index is valid, get next
                }
            }
        }
    }

    /// <summary>
    /// Pauses the current video playback.
    /// </summary>
    public void Pause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        isPlaying = false; // Update our state to indicate we are now paused/not actively playing
    }

    /// <summary>
    /// Stops playback and resets the current video.
    /// </summary>
    public void Stop()
    {
        if (videoPlayer.isPlaying || videoPlayer.clip != null)
        {
            videoPlayer.Stop();
        }
        
        isPlaying = false;
        if (!loopFirstVideo) // Only reset playback index if in random mode
        {
            currentPlaybackIndex = -1;
        }
        // In loopFirstVideo mode, currentVideoIndex usually remains the index of the looping video (e.g., 0).
        // No need to reset currentVideoIndex here as it reflects the last loaded clip.
    }

    /// <summary>
    /// Advances to the next video in the randomized sequence.
    /// If the sequence is complete, it will shuffle and start a new sequence.
    /// </summary>
    public void NextVideo()
    {
        if (loopFirstVideo)
        {
            if (logVideoChanges) Debug.LogWarning("VideoRandomizer: NextVideo called in single video loop mode. No action taken.");
            return;
        }
        if (videoClips.Length == 0) return;

        // Check if we need to reshuffle (end of sequence)
        if (currentPlaybackIndex >= playbackOrder.Count - 1)
        {
            ShufflePlaybackOrder();
            currentPlaybackIndex = 0;
        }
        else
        {
            currentPlaybackIndex++;
        }

        // Get the actual video index from our playback order
        int nextVideoIndex = playbackOrder[currentPlaybackIndex];
        ChangeVideo(nextVideoIndex);
    }

    /// <summary>
    /// Plays the previous video in the randomized sequence.
    /// </summary>
    public void PreviousVideo()
    {
        if (loopFirstVideo)
        {
            if (logVideoChanges) Debug.LogWarning("VideoRandomizer: PreviousVideo called in single video loop mode. No action taken.");
            return;
        }
        if (videoClips.Length == 0) return;

        // Go to previous index or wrap around
        if (currentPlaybackIndex <= 0)
        {
            currentPlaybackIndex = playbackOrder.Count - 1;
        }
        else
        {
            currentPlaybackIndex--;
        }

        // Get the actual video index from our playback order
        int prevVideoIndex = playbackOrder[currentPlaybackIndex];
        ChangeVideo(prevVideoIndex);
    }

    /// <summary>
    /// Changes to a specific video by index.
    /// </summary>
    /// <param name="videoIndex">Index of the video in the videoClips array</param>
    public void ChangeVideo(int videoIndex)
    {
        if (videoIndex < 0 || videoIndex >= videoClips.Length)
        {
            Debug.LogError($"VideoRandomizer: Invalid video index {videoIndex}. Must be between 0 and {videoClips.Length - 1}.");
            return;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(TransitionToVideo(videoIndex));
    }

    /// <summary>
    /// Changes to a randomly selected video, different from the current one.
    /// </summary>
    public void ChangeToRandomVideo()
    {
        if (loopFirstVideo)
        {
            if (logVideoChanges) Debug.LogWarning("VideoRandomizer: ChangeToRandomVideo called in single video loop mode. No action taken.");
            return;
        }
        if (videoClips.Length <= 1) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, videoClips.Length);
        } while (newIndex == currentVideoIndex && videoClips.Length > 1);

        ChangeVideo(newIndex);
    }

    // Event handler for when a video finishes playing
    private void OnVideoFinished(VideoPlayer source)
    {
        if (loopFirstVideo) // If looping a single video, VideoPlayer handles it.
        {
            // Optionally, if OnVideoChanged event needs to be fired on each loop completion:
            // OnVideoChanged?.Invoke(currentVideoIndex);
            return;
        }

        // Original logic for random mode
        if (isPlaying)
        {
            NextVideo();
        }
    }

    // Handles the transition between videos with optional delay
    private IEnumerator TransitionToVideo(int videoIndex)
    {
        // Stop current video if playing
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Wait for transition delay if set
        if (transitionDelay > 0)
        {
            yield return new WaitForSeconds(transitionDelay);
        }

        // Update current video index
        currentVideoIndex = videoIndex;
        
        // Assign new clip
        videoPlayer.clip = videoClips[videoIndex];
        videoPlayer.isLooping = loopFirstVideo; // Set looping based on the current mode
        
        if (isPlaying) // If the randomizer is in a "playing" state, then play the new clip
        {
            videoPlayer.Play();
        }
        // If not in "playing" state (e.g. paused, or playOnAwake was false),
        // the clip is set/prepared, but not automatically played here.
        // It will play when Play() is called or isPlaying becomes true.

        if (logVideoChanges)
        {
            Debug.Log($"VideoRandomizer: Changed to video {videoIndex}: {videoClips[videoIndex].name}. Loop mode: {loopFirstVideo}");
        }

        // Trigger the event
        OnVideoChanged?.Invoke(videoIndex);
        
        transitionCoroutine = null;
    }

    /// <summary>
    /// Gets the total number of video clips available.
    /// </summary>
    public int GetVideoCount()
    {
        return videoClips?.Length ?? 0;
    }

    /// <summary>
    /// Gets the index of the currently playing video.
    /// </summary>
    public int GetCurrentVideoIndex()
    {
        return currentVideoIndex;
    }

    /// <summary>
    /// Gets the current VideoClip being played.
    /// </summary>
    public VideoClip GetCurrentVideoClip()
    {
        if (currentVideoIndex >= 0 && currentVideoIndex < videoClips.Length)
        {
            return videoClips[currentVideoIndex];
        }
        return null;
    }
} 