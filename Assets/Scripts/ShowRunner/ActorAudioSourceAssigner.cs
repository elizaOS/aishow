using UnityEngine;
using System.Collections.Generic;

namespace ShowRunner
{
    /// <summary>
    /// Handles audio source assignment for actors to support lip sync.
    /// Place this component on actor root objects and assign the target audio GameObject.
    /// </summary>
    public class ActorAudioSourceAssigner : MonoBehaviour
    {
        [Tooltip("The GameObject with the mesh renderer that should receive the audio source (for lip sync)")]
        [SerializeField] private GameObject targetAudioObject;
        
        [Tooltip("Whether to automatically find mesh renderers if target is not specified")]
        [SerializeField] private bool autoFindMeshRenderer = true;
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            // If target is not specified but auto-find is enabled, try to find a mesh renderer
            if (targetAudioObject == null && autoFindMeshRenderer)
            {
                FindMeshRendererChild();
            }
            
            // If we still don't have a target, use this GameObject
            if (targetAudioObject == null)
            {
                targetAudioObject = gameObject;
            }
            
            // Initialize audio source if it doesn't exist yet
            InitializeAudioSource();
        }
        
        private void FindMeshRendererChild()
        {
            // Search for child objects with mesh renderers
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            
            // Try to use a skinned mesh renderer first (for characters)
            if (skinnedMeshRenderers.Length > 0)
            {
                targetAudioObject = skinnedMeshRenderers[0].gameObject;
                return;
            }
            
            // Fall back to regular mesh renderer
            if (meshRenderers.Length > 0)
            {
                targetAudioObject = meshRenderers[0].gameObject;
                return;
            }
        }
        
        private void InitializeAudioSource()
        {
            // Check if target already has an audio source
            audioSource = targetAudioObject.GetComponent<AudioSource>();
            
            // Create one if it doesn't exist
            if (audioSource == null)
            {
                audioSource = targetAudioObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f; // 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1.0f;
                audioSource.maxDistance = 20.0f;
            }
        }
        
        /// <summary>
        /// Gets the audio source for this actor for lip sync and dialogue playback
        /// </summary>
        public AudioSource GetAudioSource()
        {
            // Create the audio source if it doesn't exist yet
            if (audioSource == null)
            {
                InitializeAudioSource();
            }
            
            return audioSource;
        }
    }
} 