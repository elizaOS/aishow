#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using ShowRunner; // Required to access ShowRunner and OutroCaller
using System; // Required for DateTime

namespace ShowRunner.Utility
{
    /// <summary>
    /// Manages video recording of the show.
    /// Starts recording when the intro sequence begins and stops when the outro sequence (video + fade) completes.
    /// Recordings are saved to [Project Folder]/ShowRecordings.
    /// </summary>
    public class ShowRecorder : MonoBehaviour
    {
        [Header("Recording Settings")] 
        [Tooltip("Width of the output video in pixels.")]
        public int outputWidth = 1920;
        [Tooltip("Height of the output video in pixels.")]
        public int outputHeight = 1080;
        [Tooltip("Recording frame rate (frames per second).")]
        public float frameRate = 30.0f;
        [Tooltip("Video encoding quality.")]
        public CoreEncoderSettings.VideoEncodingQuality videoQuality = CoreEncoderSettings.VideoEncodingQuality.High;
        // We'll keep MP4 (H.264) as the default codec for simplicity, 
        // but this could be exposed as an enum too if more flexibility is needed.

        private RecorderController m_RecorderController;
        private MovieRecorderSettings m_Settings = null;
        private ShowRunner m_ShowRunnerInstance;
        private OutroCaller m_OutroCallerInstance;
        private IntroSequenceManager m_IntroSequenceManagerInstance;

        private bool m_IsRecording = false;
        private string m_CurrentRecordingPath = string.Empty;

        void OnEnable()
        {
            // Find ShowRunner and OutroCaller instances in the scene
            m_ShowRunnerInstance = FindObjectOfType<ShowRunner>();
            m_OutroCallerInstance = FindObjectOfType<OutroCaller>();
            m_IntroSequenceManagerInstance = FindObjectOfType<IntroSequenceManager>();

            if (m_ShowRunnerInstance == null)
            {
                Debug.LogError("ShowRecorder: ShowRunner instance not found. Recording will use 'UnknownEpisode' for naming.");
                // Not disabling, as we might still want to record even if episode ID is generic
            }

            if (m_IntroSequenceManagerInstance == null)
            {
                Debug.LogError("ShowRecorder: IntroSequenceManager instance not found. Recording will not start automatically.");
                enabled = false;
                return;
            }

            if (m_OutroCallerInstance == null)
            {
                Debug.LogError("ShowRecorder: OutroCaller instance not found. Recording will not stop automatically.");
                // Decide if this is critical enough to disable. For now, let's allow starting.
            }

            // Subscribe to events
            m_IntroSequenceManagerInstance.OnIntroSequenceActualStart += StartShowRecording;
            if (m_OutroCallerInstance != null) // Only subscribe if found
            {
                m_OutroCallerInstance.OnOutroVideoAndFadeComplete += StopShowRecording;
            }
            
            Debug.Log("ShowRecorder enabled and subscribed to IntroSequenceManager and OutroCaller events.");
        }

        void OnDisable()
        {
            // Unsubscribe from events
            if (m_IntroSequenceManagerInstance != null)
            {
                m_IntroSequenceManagerInstance.OnIntroSequenceActualStart -= StartShowRecording;
            }
            if (m_OutroCallerInstance != null)
            {
                m_OutroCallerInstance.OnOutroVideoAndFadeComplete -= StopShowRecording;
            }

            if (m_IsRecording)
            {
                Debug.LogWarning("ShowRecorder disabled during an active recording. Stopping recording.");
                StopRecordingLogic();
            }
            Debug.Log("ShowRecorder disabled and unsubscribed from events.");
        }

        void StartShowRecording()
        {
            if (m_IsRecording)
            {
                Debug.LogWarning("ShowRecorder: Received start signal, but already recording.");
                return;
            }

            Debug.Log("ShowRecorder: Received OnIntroSequenceActualStart event. Attempting to start recording.");
            InitializeAndStartRecording();
        }

        void StopShowRecording()
        {
            if (!m_IsRecording)
            {
                Debug.LogWarning("ShowRecorder: Received stop signal, but not currently recording.");
                return;
            }
            Debug.Log("ShowRecorder: Received OnOutroVideoAndFadeComplete event. Attempting to stop recording.");
            StopRecordingLogic();
        }

        internal void InitializeAndStartRecording()
        {
            if (m_RecorderController != null && m_RecorderController.IsRecording())
            {
                Debug.LogWarning("ShowRecorder: Recording is already in progress. Aborting new recording initialization.");
                return;
            }

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            m_RecorderController = new RecorderController(controllerSettings);

            string episodeId = "UnknownEpisode";
            if (m_ShowRunnerInstance != null)
            {
                episodeId = m_ShowRunnerInstance.GetCurrentEpisodeId() ?? "UnknownEpisode";
            }
            
            // Define the new path structure
            var mediaOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "Resources", "Episodes", episodeId, "recordings"));
            
            if (!mediaOutputFolder.Exists)
            {
                mediaOutputFolder.Create();
                 #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh(); // Refresh to show the new folder in Unity Editor
                #endif
            }

            m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            m_Settings.name = "Show Episode Recorder";
            m_Settings.Enabled = true;

            m_Settings.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = videoQuality,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            
            m_Settings.CaptureAlpha = false; 
            m_Settings.CaptureAudio = true;

            m_Settings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = outputWidth,
                OutputHeight = outputHeight
            };

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{episodeId}_{timestamp}"; // Filename itself still includes episodeId for clarity
            m_CurrentRecordingPath = Path.Combine(mediaOutputFolder.FullName, fileName);
            m_Settings.OutputFile = m_CurrentRecordingPath; 

            controllerSettings.AddRecorderSettings(m_Settings);
            controllerSettings.SetRecordModeToManual(); 
            controllerSettings.FrameRate = frameRate;

            RecorderOptions.VerboseMode = false; 

            m_RecorderController.PrepareRecording();
            if (m_RecorderController.StartRecording())
            {
                m_IsRecording = true;
                Debug.Log($"ShowRecorder: Started recording. Output will be saved to directory: {mediaOutputFolder.FullName}, with base name: {fileName}");
            }
            else
            {
                Debug.LogError("ShowRecorder: Failed to start recording.");
                if (m_RecorderController != null)
                {
                    // m_RecorderController.Release(); // Removed as it caused CS1061
                    m_RecorderController = null;
                }
            }
        }

        internal void StopRecordingLogic()
        {
            if (m_RecorderController != null && m_RecorderController.IsRecording())
            {
                m_RecorderController.StopRecording();
                Debug.Log($"ShowRecorder: Recording stopped. File saved: {m_CurrentRecordingPath}.mp4 (or other extension based on codec)");
            }
            else
            {
                Debug.LogWarning("ShowRecorder: StopRecordingLogic called, but no active recording or recorder found.");
            }

            if (m_RecorderController != null)
            {
                m_RecorderController = null; 
            }
            if (m_Settings != null)
            {
                ScriptableObject.Destroy(m_Settings); 
                m_Settings = null;
            }
            
            m_IsRecording = false;
            m_CurrentRecordingPath = string.Empty;
        }

        public FileInfo GetOutputFile()
        {
            if (m_Settings == null || string.IsNullOrEmpty(m_Settings.OutputFile) || m_Settings.EncoderSettings == null)
                return null;

            // Cast to CoreEncoderSettings to access Codec property
            CoreEncoderSettings encoderSettings = m_Settings.EncoderSettings as CoreEncoderSettings; 

            if (encoderSettings == null)
            {
                Debug.LogWarning("ShowRecorder: Could not cast EncoderSettings to CoreEncoderSettings. Cannot determine file extension.");
                return new FileInfo(m_Settings.OutputFile + ".unknown"); // Fallback
            }
            
            string extension = "." + encoderSettings.Codec.ToString().ToLower();
            if(encoderSettings.Codec == CoreEncoderSettings.OutputCodec.WEBM) 
            {
                extension = ".webm";
            }
            // Add more specific extension handling if other codecs are used
            else if (encoderSettings.Codec == CoreEncoderSettings.OutputCodec.MP4)
            {
                extension = ".mp4";
            }
            // Add other codecs as necessary, e.g., ProRes
            // else if (encoderSettings.Codec == CoreEncoderSettings.OutputCodec.ProRes)
            // {
            //     extension = ".mov"; 
            // }

            return new FileInfo(m_Settings.OutputFile + extension);
        }
    }
}

#endif 