using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class TimedExpression
{
    public FacialExpressionData expression;
    public float triggerTime;
    public float transitionSpeed = 0.3f;
}

[RequireComponent(typeof(AudioSource))]
public class SimpleLipSync : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    
    [Header("Facial System Integration")]
    public FacialExpressionSystem facialSystem;
    
    [Header("Lip Sync Configuration")]
    public LipSyncData lipSyncData;
    
    [Header("Timed Expressions")]
    public List<TimedExpression> timedExpressions = new List<TimedExpression>();
    
    [Header("Lip Sync Settings")]
    public float duration = 5f;
    [Range(0f, 1f)]
    public float silenceTransitionSpeed = 0.3f;
    
    [Header("Advanced Settings")]
    public bool useAudioDuration = true;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private Coroutine lipSyncCoroutine;
    private Coroutine expressionCoroutine;
    private bool isPlaying = false;
    private HashSet<int> triggeredExpressions = new HashSet<int>();
    
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
        
        if (expressionCoroutine != null)
        {
            StopCoroutine(expressionCoroutine);
            expressionCoroutine = null;
        }
        
        triggeredExpressions.Clear();
        
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
        triggeredExpressions.Clear();
        
        if (timedExpressions.Count > 0 && facialSystem != null)
        {
            expressionCoroutine = StartCoroutine(TimedExpressionsCoroutine());
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            UpdateMouthShapes();
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isPlaying = false;
        
        if (expressionCoroutine != null)
        {
            StopCoroutine(expressionCoroutine);
            expressionCoroutine = null;
        }
        
        ResetAllTriggeredExpressions();
        
        StartCoroutine(ReturnToSilenceCoroutine());
        
        AnimationHandler anim = GetComponent<AnimationHandler>();
        if(anim != null)
        {
            anim.PlayIdle();
        }
    }
    
    private void ResetAllTriggeredExpressions()
    {
        if (facialSystem == null || timedExpressions.Count == 0) return;
        
        foreach (int index in triggeredExpressions)
        {
            if (index < timedExpressions.Count && timedExpressions[index].expression != null)
            {
                var expression = timedExpressions[index].expression;
                foreach (var bs in expression.blendShapes)
                {
                    facialSystem.SetBlendShapeValue(bs.blendShapeName, 0f);
                }
            }
        }
        
        triggeredExpressions.Clear();
    }
    
    private IEnumerator TimedExpressionsCoroutine()
    {
        var sortedExpressions = new List<TimedExpression>(timedExpressions);
        sortedExpressions.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
        
        float elapsed = 0f;
        int currentIndex = 0;
        
        while (currentIndex < sortedExpressions.Count && elapsed < duration)
        {
            var timedExpr = sortedExpressions[currentIndex];
            
            if (elapsed >= timedExpr.triggerTime && !triggeredExpressions.Contains(currentIndex))
            {
                if (timedExpr.expression != null)
                {
                    ResetPreviousExpressionBlendShapes(timedExpr);
                    
                    yield return new WaitForSeconds(timedExpr.transitionSpeed * 0.5f);
                    
                    facialSystem.SetExpression(timedExpr.expression, timedExpr.transitionSpeed);
                    triggeredExpressions.Add(currentIndex);
                }
                currentIndex++;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    private void ResetPreviousExpressionBlendShapes(TimedExpression currentExpression)
    {
        if (facialSystem == null || currentExpression.expression == null) return;
        
        var currentBlendShapes = new HashSet<string>();
        foreach (var bs in currentExpression.expression.blendShapes)
        {
            currentBlendShapes.Add(bs.blendShapeName);
        }
        
        foreach (var timedExpr in timedExpressions)
        {
            if (timedExpr.expression == null || timedExpr == currentExpression) continue;
            
            foreach (var bs in timedExpr.expression.blendShapes)
            {
                if (!currentBlendShapes.Contains(bs.blendShapeName))
                {
                    facialSystem.SetBlendShapeValue(bs.blendShapeName, 0f);
                }
            }
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
        
        if (expressionCoroutine != null)
        {
            StopCoroutine(expressionCoroutine);
            expressionCoroutine = null;
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
        triggeredExpressions.Clear();
    }
}
