using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Simple duration-based lip-sync using LipSyncData configuration
/// Animates blend shapes with configurable behavior (static or random ranges)
/// Uses DOTween for smooth eased transitions
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SimpleLipSync : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Audio source that plays the speech audio")]
    public AudioSource audioSource;
    
    [Header("Facial System Integration")]
    [Tooltip("Reference to the facial expression system")]
    public FacialExpressionSystem facialSystem;
    
    [Header("Lip Sync Configuration")]
    [Tooltip("Lip-sync data defining blend shapes and animation behavior")]
    public LipSyncData lipSyncData;
    
    [Header("Lip Sync Settings")]
    [Tooltip("Duration to play lip-sync animation (in seconds)")]
    public float duration = 5f;
    
    [Tooltip("Transition speed when returning to silence")]
    [Range(0f, 1f)]
    public float silenceTransitionSpeed = 0.3f;
    
    [Header("Advanced Settings")]
    [Tooltip("Use audio duration instead of volume detection")]
    public bool useAudioDuration = true;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private variables
    private Coroutine lipSyncCoroutine;
    private bool isPlaying = false;
    private bool isTalking = false;
    private bool isReturningToSilence = false;
    
    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    void Start()
    {
        if (facialSystem == null)
        {
            Debug.LogError("FacialExpressionSystem not assigned!");
            return;
        }
        
        if (lipSyncData == null)
        {
            Debug.LogError("LipSyncData not assigned!");
            return;
        }
        
        // Initialize lip-sync data
        lipSyncData.Initialize();
    }
    
    /// <summary>
    /// Start lip-sync with audio clip
    /// </summary>
    public void StartLipSync(AudioClip clip)
    {
        if (audioSource == null || facialSystem == null)
        {
            Debug.LogError("AudioSource or FacialSystem not assigned!");
            return;
        }
        
        if (lipSyncData == null)
        {
            Debug.LogError("LipSyncData not assigned!");
            return;
        }
        
        StopLipSync();
        
        audioSource.clip = clip;
        audioSource.Play();
        
        lipSyncCoroutine = StartCoroutine(LipSyncCoroutine());
        isPlaying = true;
        
        Debug.Log($"Started simple lip-sync for: {clip.name}");
    }
    
    /// <summary>
    /// Start lip-sync with currently loaded audio
    /// </summary>
    public void StartLipSync()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogError("No audio clip loaded!");
            return;
        }
        
        if (lipSyncData == null)
        {
            Debug.LogError("LipSyncData not assigned!");
            return;
        }
        
        StopLipSync();
        
        audioSource.Play();
        lipSyncCoroutine = StartCoroutine(LipSyncCoroutine());
        isPlaying = true;
        
        Debug.Log($"Started simple lip-sync for: {audioSource.clip.name}");
    }
    
    /// <summary>
    /// Stop lip-sync and return to silence
    /// </summary>
    public void StopLipSync()
    {
        if (lipSyncCoroutine != null)
        {
            StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = null;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        isPlaying = false;
        isTalking = false;
        
        // Return to silence
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
            
            // Only start coroutine if game object is active
            if (gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(ReturnToSilenceCoroutine());
            }
            else
            {
                // Immediately set to silence if inactive
                ReturnToSilenceImmediate();
            }
        }
        
        Debug.Log("Stopped simple lip-sync");
    }
    
    private void ReturnToSilenceImmediate()
    {
        if (lipSyncData == null || facialSystem == null) return;
        
        // Immediately set all blend shapes to 0
        foreach (var bs in lipSyncData.blendShapes)
        {
            if (string.IsNullOrEmpty(bs.meshName))
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, 0f);
            }
            else
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.meshName, 0f);
            }
        }
        
        lipSyncData.ResetToSilence();
        isReturningToSilence = false;
    }
    
    private IEnumerator ReturnToSilenceCoroutine()
    {
        isReturningToSilence = true;
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Store current values
        Dictionary<string, float> startValues = new Dictionary<string, float>();
        foreach (var bs in lipSyncData.blendShapes)
        {
            startValues[bs.blendShapeName] = bs.currentValue;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Lerp all blend shapes to 0
            foreach (var bs in lipSyncData.blendShapes)
            {
                float startValue = startValues[bs.blendShapeName];
                float currentValue = Mathf.Lerp(startValue, 0f, t);
                
                if (string.IsNullOrEmpty(bs.meshName))
                {
                    facialSystem.SetBlendShapeValue(bs.blendShapeName, currentValue);
                }
                else
                {
                    facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.meshName, currentValue);
                }
            }
            
            yield return null;
        }
        
        // Ensure all at 0
        foreach (var bs in lipSyncData.blendShapes)
        {
            if (string.IsNullOrEmpty(bs.meshName))
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, 0f);
            }
            else
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.meshName, 0f);
            }
        }
        
        lipSyncData.ResetToSilence();
        isReturningToSilence = false;
    }
    
    private IEnumerator LipSyncCoroutine()
    {
        lipSyncData.Initialize();
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            UpdateMouthShapes();
            elapsed += Time.deltaTime;
            yield return null; // Update every frame for smooth animation
        }
        
        // Duration finished, return to silence
        isPlaying = false;
        isTalking = false;
        StartCoroutine(ReturnToSilenceCoroutine());
        
        AnimationHandler anim = GetComponent<AnimationHandler>();
        if(anim != null)
        {
            anim.PlayIdle();
        }
    }
    
    private void UpdateMouthShapes()
    {
        if (lipSyncData == null || facialSystem == null) return;
        
        // Update all blend shapes in lip-sync data
        lipSyncData.UpdateBlendShapes(Time.deltaTime);
        
        // Apply values to facial system
        foreach (var bs in lipSyncData.blendShapes)
        {
            // Check if mesh name is specified
            if (string.IsNullOrEmpty(bs.meshName))
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.currentValue);
            }
            else
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.meshName, bs.currentValue);
            }
        }
        
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            string debugMsg = "LipSync: ";
            foreach (var bs in lipSyncData.blendShapes)
            {
                string name = string.IsNullOrEmpty(bs.meshName) ? bs.blendShapeName : $"{bs.blendShapeName}({bs.meshName})";
                debugMsg += $"{name}={bs.currentValue:F1} ";
            }
            Debug.Log(debugMsg);
        }
    }
    
    /// <summary>
    /// Play audio with automatic lip-sync
    /// </summary>
    public void PlayWithLipSync(AudioClip clip)
    {
        StartLipSync(clip);
    }
    
    /// <summary>
    /// Check if lip-sync is currently active
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying && audioSource != null && audioSource.isPlaying;
    }
    
    void OnDestroy()
    {
        // Stop audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Kill all tweens
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
        }
        
        // Reset to silence immediately (can't use coroutines in OnDestroy)
        ReturnToSilenceImmediate();
    }
    
    void OnDisable()
    {
        // Clean up when disabled
        if (lipSyncCoroutine != null)
        {
            StopCoroutine(lipSyncCoroutine);
            lipSyncCoroutine = null;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
        }
        
        isPlaying = false;
        isTalking = false;
    }
}
