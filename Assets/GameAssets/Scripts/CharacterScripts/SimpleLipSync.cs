using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
[RequireComponent(typeof(AudioSource))]
public class SimpleLipSync : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    
    [Header("Facial System Integration")]
    public FacialExpressionSystem facialSystem;
    
    [Header("Lip Sync Configuration")]
    public LipSyncData lipSyncData;
    
    [Header("Lip Sync Settings")]
    [Tooltip("Duration to play lip-sync animation (in seconds)")]
    public float duration = 5f;
    [Range(0f, 1f)]
    public float silenceTransitionSpeed = 0.3f;
    
    [Header("Advanced Settings")]
    public bool useAudioDuration = true;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private Coroutine lipSyncCoroutine;
    private bool isPlaying = false;
    
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
            return;
        }
        
        if (lipSyncData == null)
        {
            Debug.LogError("LipSyncData not assigned!");
            return;
        }
        
        lipSyncData.Initialize();
    }
    public void StartLipSync(AudioClip clip)
    {
        if (audioSource == null || facialSystem == null)
        {
            return;
        }
        
        if (lipSyncData == null)
        {
            return;
        }
        
        StopLipSync();
        
        audioSource.clip = clip;
        audioSource.Play();
        
        lipSyncCoroutine = StartCoroutine(LipSyncCoroutine());
        isPlaying = true;
    }
    public void StartLipSync()
    {
        if (audioSource == null || audioSource.clip == null)
        {
            return;
        }
        
        if (lipSyncData == null)
        {
            return;
        }
        
        StopLipSync();
        
        audioSource.Play();
        lipSyncCoroutine = StartCoroutine(LipSyncCoroutine());
        isPlaying = true;
    }
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
        
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
            
            if (gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(ReturnToSilenceCoroutine());
            }
            else
            {
                ReturnToSilenceImmediate();
            }
        }
    }
    
    private void ReturnToSilenceImmediate()
    {
        if (lipSyncData == null || facialSystem == null) return;
        
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
    }
    
    private IEnumerator ReturnToSilenceCoroutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        Dictionary<string, float> startValues = new Dictionary<string, float>();
        foreach (var bs in lipSyncData.blendShapes)
        {
            startValues[bs.blendShapeName] = bs.currentValue;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
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
    }
    
    private IEnumerator LipSyncCoroutine()
    {
        lipSyncData.Initialize();
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            UpdateMouthShapes();
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isPlaying = false;
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
        
        lipSyncData.UpdateBlendShapes(Time.deltaTime);
        
        foreach (var bs in lipSyncData.blendShapes)
        {
            if (string.IsNullOrEmpty(bs.meshName))
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.currentValue);
            }
            else
            {
                facialSystem.SetBlendShapeValue(bs.blendShapeName, bs.meshName, bs.currentValue);
            }
        }
        
    }
    public void PlayWithLipSync(AudioClip clip)
    {
        StartLipSync(clip);
    }
    public bool IsPlaying()
    {
        return isPlaying && audioSource != null && audioSource.isPlaying;
    }
    
    void OnDestroy()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
        }
        
        ReturnToSilenceImmediate();
    }
    
    void OnDisable()
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
        
        if (lipSyncData != null)
        {
            lipSyncData.KillAllTweens();
        }
        
        isPlaying = false;
    }
}
