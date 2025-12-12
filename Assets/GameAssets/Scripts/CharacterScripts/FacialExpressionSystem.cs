using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class FacialExpressionSystem : MonoBehaviour
{
    [Header("Character Root")]
    public GameObject characterRoot;
    
    [Header("Facial Expressions")]
    public List<FacialExpressionData> expressions = new List<FacialExpressionData>();
    
    [Header("Eye Blink")]
    public FacialExpressionData eyeBlinkExpression;
    public float blinkInterval = 3f;
    public float blinkDuration = 0.15f;
    
    [Header("Current State")]
    public string currentExpression = "Neutral";
    
    private List<SkinnedMeshRenderer> allSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();
    
    private Dictionary<string, (SkinnedMeshRenderer renderer, int index)> blendShapeCache = new Dictionary<string, (SkinnedMeshRenderer, int)>();
    private Dictionary<(string, string), (SkinnedMeshRenderer renderer, int index)> blendShapeCacheWithMesh = new Dictionary<(string, string), (SkinnedMeshRenderer, int)>();
    private Dictionary<string, float> currentBlendShapeValues = new Dictionary<string, float>();
    private Dictionary<string, FacialExpressionData> expressionCache = new Dictionary<string, FacialExpressionData>();
    private FacialExpressionData activeExpression = null;
    private HashSet<string> eyeBlinkBlendShapes = new HashSet<string>();
    private Coroutine transitionCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine randomExpressionCoroutine;
    
    void Start()
    {
        if (characterRoot == null)
        {
            characterRoot = gameObject;
        }
        
        FindAllSkinnedMeshRenderers();
        
        if (allSkinnedMeshRenderers.Count == 0)
        {
            return;
        }
        
        CacheBlendShapeIndices();
        CacheExpressions();
        
        if (eyeBlinkExpression != null)
        {
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                eyeBlinkBlendShapes.Add(bs.GetBlendShapeName());
            }
        }
        
        SetExpressionImmediate("Neutral");
        
        if (eyeBlinkExpression != null)
        {
            blinkCoroutine = StartCoroutine(EyeBlinkLoop());
        }
    }
    private void FindAllSkinnedMeshRenderers()
    {
        allSkinnedMeshRenderers.Clear();
        
        if (characterRoot == null) return;
        
        SkinnedMeshRenderer[] renderers = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        allSkinnedMeshRenderers.AddRange(renderers);
    }
    
    private void CacheExpressions()
    {
        expressionCache.Clear();
        
        foreach (var expression in expressions)
        {
            if (expression != null)
            {
                expressionCache[expression.expressionName] = expression;
            }
        }
    }
    private void CacheBlendShapeIndices()
    {
        blendShapeCache.Clear();
        blendShapeCacheWithMesh.Clear();
        
        foreach (var renderer in allSkinnedMeshRenderers)
        {
            if (renderer == null || renderer.sharedMesh == null) continue;
            
            Mesh mesh = renderer.sharedMesh;
            string meshName = renderer.gameObject.name;
            
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                
                if (!blendShapeCache.ContainsKey(shapeName))
                {
                    blendShapeCache[shapeName] = (renderer, i);
                }
                
                blendShapeCacheWithMesh[(shapeName, meshName)] = (renderer, i);
            }
        }
    }
    public void SetExpression(FacialExpressionData expression, float? customDuration = null)
    {
        if (expression == null)
        {
            return;
        }
        
        activeExpression = expression;
        float duration = customDuration ?? expression.transitionDuration;
        
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        transitionCoroutine = StartCoroutine(TransitionToExpression(expression, duration));
        currentExpression = expression.expressionName;
    }
    public void SetExpression(string expressionName, float? customDuration = null)
    {
        FacialExpressionData expression = GetExpression(expressionName);
        
        if (expression == null)
        {
            return;
        }
        
        SetExpression(expression, customDuration);
    }
    public void SetExpressionImmediate(FacialExpressionData expression)
    {
        if (expression == null)
        {
            ResetAllBlendShapes();
            activeExpression = null;
            return;
        }
        
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        
        ResetAllBlendShapes();
        
        activeExpression = expression;
        
        foreach (var bs in expression.blendShapes)
        {
            SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
        }
        
        currentExpression = expression.expressionName;
    }
    public void SetExpressionImmediate(string expressionName)
    {
        FacialExpressionData expression = GetExpression(expressionName);
        SetExpressionImmediate(expression);
    }
    
    private IEnumerator TransitionToExpression(FacialExpressionData targetExpression, float duration)
    {
        Dictionary<string, float> startValues = new Dictionary<string, float>();
        
        if (activeExpression != null)
        {
            foreach (var bs in activeExpression.blendShapes)
            {
                string shapeName = bs.GetBlendShapeName();
                if (eyeBlinkBlendShapes.Contains(shapeName)) continue;
                
                if (currentBlendShapeValues.ContainsKey(shapeName))
                {
                    startValues[shapeName] = currentBlendShapeValues[shapeName];
                }
            }
        }
        
        Dictionary<string, float> targetValues = new Dictionary<string, float>();
        
        foreach (var bs in targetExpression.blendShapes)
        {
            string shapeName = bs.GetBlendShapeName();
            if (eyeBlinkBlendShapes.Contains(shapeName)) continue;
            
            targetValues[shapeName] = bs.weight;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            foreach (var kvp in startValues)
            {
                if (!targetValues.ContainsKey(kvp.Key))
                {
                    float currentValue = Mathf.Lerp(kvp.Value, 0f, t);
                    SetBlendShapeValue(kvp.Key, currentValue);
                }
            }
            
            foreach (var kvp in targetValues)
            {
                float startValue = startValues.ContainsKey(kvp.Key) ? startValues[kvp.Key] : 0f;
                float currentValue = Mathf.Lerp(startValue, kvp.Value, t);
                SetBlendShapeValue(kvp.Key, currentValue);
            }
            
            yield return null;
        }
        
        foreach (var kvp in startValues)
        {
            if (!targetValues.ContainsKey(kvp.Key))
            {
                SetBlendShapeValue(kvp.Key, 0f);
            }
        }
        
        foreach (var bs in targetExpression.blendShapes)
        {
            SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
        }
    }
    public void SetBlendShapeValue(string blendShapeName, float weight)
    {
        if (!blendShapeCache.ContainsKey(blendShapeName))
        {
            return;
        }
        
        var (renderer, index) = blendShapeCache[blendShapeName];
        if (renderer != null)
        {
            renderer.SetBlendShapeWeight(index, weight);
            currentBlendShapeValues[blendShapeName] = weight;
        }
    }
    public void SetBlendShapeValue(string blendShapeName, string meshName, float weight)
    {
        var key = (blendShapeName, meshName);
        
        if (!blendShapeCacheWithMesh.ContainsKey(key))
        {
            SetBlendShapeValue(blendShapeName, weight);
            return;
        }
        
        var (renderer, index) = blendShapeCacheWithMesh[key];
        if (renderer != null)
        {
            renderer.SetBlendShapeWeight(index, weight);
            currentBlendShapeValues[$"{meshName}.{blendShapeName}"] = weight;
        }
    }
    public float GetCurrentBlendShapeValue(string blendShapeName)
    {
        return currentBlendShapeValues.ContainsKey(blendShapeName) ? currentBlendShapeValues[blendShapeName] : 0f;
    }
    public List<string> GetAllBlendShapeNames()
    {
        return new List<string>(blendShapeCache.Keys);
    }
    public Dictionary<string, List<string>> GetBlendShapesByMesh()
    {
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
        
        foreach (var renderer in allSkinnedMeshRenderers)
        {
            if (renderer == null || renderer.sharedMesh == null) continue;
            
            string meshName = renderer.gameObject.name;
            List<string> shapes = new List<string>();
            
            Mesh mesh = renderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                shapes.Add(mesh.GetBlendShapeName(i));
            }
            
            result[meshName] = shapes;
        }
        
        return result;
    }
    public void ResetAllBlendShapes()
    {
        if (activeExpression != null)
        {
            foreach (var bs in activeExpression.blendShapes)
            {
                string shapeName = bs.GetBlendShapeName();
                if (eyeBlinkBlendShapes.Contains(shapeName)) continue;
                
                if (blendShapeCache.ContainsKey(shapeName))
                {
                    var (renderer, index) = blendShapeCache[shapeName];
                    if (renderer != null)
                    {
                        renderer.SetBlendShapeWeight(index, 0f);
                        currentBlendShapeValues[shapeName] = 0f;
                    }
                }
            }
        }
        else
        {
            foreach (var kvp in blendShapeCache)
            {
                if (eyeBlinkBlendShapes.Contains(kvp.Key)) continue;
                
                var (renderer, index) = kvp.Value;
                if (renderer != null)
                {
                    renderer.SetBlendShapeWeight(index, 0f);
                    currentBlendShapeValues[kvp.Key] = 0f;
                }
            }
        }
        
        activeExpression = null;
    }
    public void ResetAllBlendShapes(float duration)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        activeExpression = null;
        transitionCoroutine = StartCoroutine(TransitionToReset(duration));
    }
    
    private IEnumerator TransitionToReset(float duration)
    {
        Dictionary<string, float> startValues = new Dictionary<string, float>();
        
        foreach (var kvp in currentBlendShapeValues)
        {
            if (!eyeBlinkBlendShapes.Contains(kvp.Key) && kvp.Value > 0f)
            {
                startValues[kvp.Key] = kvp.Value;
            }
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            foreach (var kvp in startValues)
            {
                float currentValue = Mathf.Lerp(kvp.Value, 0f, t);
                SetBlendShapeValue(kvp.Key, currentValue);
            }
            
            yield return null;
        }
        
        foreach (var kvp in startValues)
        {
            SetBlendShapeValue(kvp.Key, 0f);
        }
    }
    
    private FacialExpressionData GetExpression(string name)
    {
        return expressionCache.ContainsKey(name) ? expressionCache[name] : null;
    }
    
    public void PlayRandomExpression()
    {
        if (expressions.Count == 0) return;
        
        var availableExpressions = expressions.Where(e => e != null && e != eyeBlinkExpression && e != activeExpression).ToList();
        
        if (availableExpressions.Count == 0) return;
        
        int randomIndex = Random.Range(0, availableExpressions.Count);
        randomExpressionCoroutine = StartCoroutine(PlayRandomExpressionCoroutine(availableExpressions[randomIndex]));
    }
    
    public void StopRandomExpression()
    {
        if (randomExpressionCoroutine != null)
        {
            StopCoroutine(randomExpressionCoroutine);
            randomExpressionCoroutine = null;
            
            if (activeExpression != null)
            {
                foreach (var bs in activeExpression.blendShapes)
                {
                    SetBlendShapeValue(bs.blendShapeName, 0f);
                }
            }
        }
    }
    
    private IEnumerator PlayRandomExpressionCoroutine(FacialExpressionData newExpression)
    {
        if (activeExpression != null)
        {
            float transitionDuration = activeExpression.transitionDuration;
            float elapsed = 0f;
            
            Dictionary<string, float> startValues = new Dictionary<string, float>();
            foreach (var bs in activeExpression.blendShapes)
            {
                startValues[bs.blendShapeName] = GetCurrentBlendShapeValue(bs.blendShapeName);
            }
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                
                foreach (var bs in activeExpression.blendShapes)
                {
                    float startValue = startValues.ContainsKey(bs.blendShapeName) ? startValues[bs.blendShapeName] : 0f;
                    float value = Mathf.Lerp(startValue, 0f, t);
                    SetBlendShapeValue(bs.blendShapeName, value);
                }
                
                yield return null;
            }
            
            foreach (var bs in activeExpression.blendShapes)
            {
                SetBlendShapeValue(bs.blendShapeName, 0f);
            }
        }
        
        SetExpression(newExpression);
    }
    
    private IEnumerator EyeBlinkLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(blinkInterval * 0.7f, blinkInterval * 1.3f);
            yield return new WaitForSeconds(waitTime);
            
            float transitionDuration = eyeBlinkExpression.transitionDuration;
            float elapsed = 0f;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                
                foreach (var bs in eyeBlinkExpression.blendShapes)
                {
                    float value = Mathf.Lerp(0f, bs.weight, t);
                    SetBlendShapeValue(bs.GetBlendShapeName(), value);
                }
                
                yield return null;
            }
            
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
            }
            
            yield return new WaitForSeconds(blinkDuration);
            
            elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                
                foreach (var bs in eyeBlinkExpression.blendShapes)
                {
                    float value = Mathf.Lerp(bs.weight, 0f, t);
                    SetBlendShapeValue(bs.GetBlendShapeName(), value);
                }
                
                yield return null;
            }
            
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                SetBlendShapeValue(bs.GetBlendShapeName(), 0f);
            }
        }
    }
    public void RefreshBlendShapes()
    {
        FindAllSkinnedMeshRenderers();
        CacheBlendShapeIndices();
    }
}
