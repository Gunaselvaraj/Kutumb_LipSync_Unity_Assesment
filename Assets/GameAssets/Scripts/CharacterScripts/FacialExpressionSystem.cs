using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Multi-mesh facial expression system - supports blendshapes from Body, Eye, Teeth, Tongue, etc.
/// Automatically discovers all blendshapes from all meshes in character hierarchy
/// </summary>
public class FacialExpressionSystem : MonoBehaviour
{
    [Header("Character Root")]
    [Tooltip("The root character GameObject - system will find all SkinnedMeshRenderers automatically")]
    public GameObject characterRoot;
    
    [Header("Facial Expressions")]
    public List<FacialExpressionData> expressions = new List<FacialExpressionData>();
    
    [Header("Eye Blink")]
    [Tooltip("Eye blink expression - runs independently and is never reset")]
    public FacialExpressionData eyeBlinkExpression;
    public float blinkInterval = 3f;
    public float blinkDuration = 0.15f;
    
    [Header("Current State")]
    public string currentExpression = "Neutral";
    
    // All skinned mesh renderers found in character (Body, Eye, Teeth, Tongue, etc.)
    private List<SkinnedMeshRenderer> allSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();
    
    // Maps: blendshape name -> (renderer, blendshape index)
    private Dictionary<string, (SkinnedMeshRenderer renderer, int index)> blendShapeCache = new Dictionary<string, (SkinnedMeshRenderer, int)>();
    // Maps: (blendshape name, mesh name) -> (renderer, blendshape index) for duplicate names
    private Dictionary<(string, string), (SkinnedMeshRenderer renderer, int index)> blendShapeCacheWithMesh = new Dictionary<(string, string), (SkinnedMeshRenderer, int)>();
    private Dictionary<string, float> currentBlendShapeValues = new Dictionary<string, float>();
    private Dictionary<string, FacialExpressionData> expressionCache = new Dictionary<string, FacialExpressionData>();
    private FacialExpressionData activeExpression = null;
    private HashSet<string> eyeBlinkBlendShapes = new HashSet<string>();
    private Coroutine transitionCoroutine;
    private Coroutine blinkCoroutine;
    
    void Start()
    {
        // Auto-find character root if not assigned
        if (characterRoot == null)
        {
            characterRoot = gameObject;
        }
        
        // Find all skinned mesh renderers in character hierarchy
        FindAllSkinnedMeshRenderers();
        
        if (allSkinnedMeshRenderers.Count == 0)
        {
            Debug.LogError("No SkinnedMeshRenderers found in character!");
            return;
        }
        
        CacheBlendShapeIndices();
        CacheExpressions();
        
        // Cache eye blink blendshapes
        if (eyeBlinkExpression != null)
        {
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                eyeBlinkBlendShapes.Add(bs.GetBlendShapeName());
            }
        }
        
        SetExpressionImmediate("Neutral");
        
        // Start eye blink loop
        if (eyeBlinkExpression != null)
        {
            blinkCoroutine = StartCoroutine(EyeBlinkLoop());
        }
    }
    
    /// <summary>
    /// Find all SkinnedMeshRenderers in the character hierarchy
    /// </summary>
    private void FindAllSkinnedMeshRenderers()
    {
        allSkinnedMeshRenderers.Clear();
        
        if (characterRoot == null) return;
        
        SkinnedMeshRenderer[] renderers = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        allSkinnedMeshRenderers.AddRange(renderers);
        
        Debug.Log($"Found {allSkinnedMeshRenderers.Count} SkinnedMeshRenderers: " + 
                  string.Join(", ", allSkinnedMeshRenderers.Select(r => r.gameObject.name)));
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
        
        Debug.Log($"Cached {expressionCache.Count} facial expressions");
    }
    
    /// <summary>
    /// Cache all blendshapes from all meshes in character
    /// </summary>
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
                
                // Store with just the name (first occurrence only)
                if (!blendShapeCache.ContainsKey(shapeName))
                {
                    blendShapeCache[shapeName] = (renderer, i);
                }
                
                // Also store with mesh name for specific targeting
                blendShapeCacheWithMesh[(shapeName, meshName)] = (renderer, i);
            }
        }
        
        Debug.Log($"Cached {blendShapeCache.Count} unique blend shape names and {blendShapeCacheWithMesh.Count} total blend shapes from {allSkinnedMeshRenderers.Count} meshes");
    }
    
    /// <summary>
    /// Set expression with smooth transition using expression data directly
    /// </summary>
    public void SetExpression(FacialExpressionData expression, float? customDuration = null)
    {
        if (expression == null)
        {
            Debug.LogWarning("Expression data is null!");
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
    
    /// <summary>
    /// Set expression with smooth transition using expression name
    /// </summary>
    public void SetExpression(string expressionName, float? customDuration = null)
    {
        FacialExpressionData expression = GetExpression(expressionName);
        
        if (expression == null)
        {
            Debug.LogWarning($"Expression '{expressionName}' not found!");
            return;
        }
        
        SetExpression(expression, customDuration);
    }
    
    /// <summary>
    /// Set expression immediately without transition using expression data
    /// </summary>
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
        
        // Reset old expression blendshapes first (before changing activeExpression)
        ResetAllBlendShapes();
        
        // Now set new active expression
        activeExpression = expression;
        
        // Apply new expression blendshapes
        foreach (var bs in expression.blendShapes)
        {
            SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
        }
        
        currentExpression = expression.expressionName;
    }
    
    /// <summary>
    /// Set expression immediately without transition using expression name
    /// </summary>
    public void SetExpressionImmediate(string expressionName)
    {
        FacialExpressionData expression = GetExpression(expressionName);
        SetExpressionImmediate(expression);
    }
    
    private IEnumerator TransitionToExpression(FacialExpressionData targetExpression, float duration)
    {
        // Get blendshapes from current active expression to reset
        Dictionary<string, float> startValues = new Dictionary<string, float>();
        
        if (activeExpression != null)
        {
            foreach (var bs in activeExpression.blendShapes)
            {
                string shapeName = bs.GetBlendShapeName();
                // Skip eye blink blendshapes
                if (eyeBlinkBlendShapes.Contains(shapeName)) continue;
                
                if (currentBlendShapeValues.ContainsKey(shapeName))
                {
                    startValues[shapeName] = currentBlendShapeValues[shapeName];
                }
            }
        }
        
        // Get target values for new expression
        Dictionary<string, float> targetValues = new Dictionary<string, float>();
        
        foreach (var bs in targetExpression.blendShapes)
        {
            string shapeName = bs.GetBlendShapeName();
            // Skip eye blink blendshapes in regular expressions
            if (eyeBlinkBlendShapes.Contains(shapeName)) continue;
            
            targetValues[shapeName] = bs.weight;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Reset old expression blendshapes to 0
            foreach (var kvp in startValues)
            {
                if (!targetValues.ContainsKey(kvp.Key))
                {
                    float currentValue = Mathf.Lerp(kvp.Value, 0f, t);
                    SetBlendShapeValue(kvp.Key, currentValue);
                }
            }
            
            // Transition to new expression blendshapes
            foreach (var kvp in targetValues)
            {
                float startValue = startValues.ContainsKey(kvp.Key) ? startValues[kvp.Key] : 0f;
                float currentValue = Mathf.Lerp(startValue, kvp.Value, t);
                SetBlendShapeValue(kvp.Key, currentValue);
            }
            
            yield return null;
        }
        
        // Ensure old blendshapes are exactly 0
        foreach (var kvp in startValues)
        {
            if (!targetValues.ContainsKey(kvp.Key))
            {
                SetBlendShapeValue(kvp.Key, 0f);
            }
        }
        
        // Ensure new blendshapes are exact values
        foreach (var bs in targetExpression.blendShapes)
        {
            SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
        }
    }
    
    /// <summary>
    /// Set a single blendshape value using string
    /// </summary>
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
    
    /// <summary>
    /// Set a blendshape value on a specific mesh
    /// </summary>
    public void SetBlendShapeValue(string blendShapeName, string meshName, float weight)
    {
        var key = (blendShapeName, meshName);
        
        if (!blendShapeCacheWithMesh.ContainsKey(key))
        {
            // Fallback to name-only version
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
    
    /// <summary>
    /// Get current value of a blendshape using string
    /// </summary>
    public float GetCurrentBlendShapeValue(string blendShapeName)
    {
        return currentBlendShapeValues.ContainsKey(blendShapeName) ? currentBlendShapeValues[blendShapeName] : 0f;
    }
    
    /// <summary>
    /// Get all available blendshape names from all meshes
    /// </summary>
    public List<string> GetAllBlendShapeNames()
    {
        return new List<string>(blendShapeCache.Keys);
    }
    
    /// <summary>
    /// Get all blendshapes grouped by mesh
    /// </summary>
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
    
    /// <summary>
    /// Reset all blendshapes to 0 immediately (except eye blink)
    /// </summary>
    public void ResetAllBlendShapes()
    {
        if (activeExpression != null)
        {
            // Only reset blendshapes from the active expression
            foreach (var bs in activeExpression.blendShapes)
            {
                string shapeName = bs.GetBlendShapeName();
                // Skip eye blink blendshapes
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
            // No active expression, reset everything except eye blink
            foreach (var kvp in blendShapeCache)
            {
                // Skip eye blink blendshapes
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
    
    /// <summary>
    /// Reset all blendshapes to 0 with smooth transition (except eye blink)
    /// </summary>
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
        
        // Only get start values for non-eye-blink blendshapes
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
        
        // Ensure all are exactly 0 (except eye blink)
        foreach (var kvp in startValues)
        {
            SetBlendShapeValue(kvp.Key, 0f);
        }
    }
    
    private FacialExpressionData GetExpression(string name)
    {
        return expressionCache.ContainsKey(name) ? expressionCache[name] : null;
    }
    
    /// <summary>
    /// Eye blink loop - runs continuously and independently
    /// </summary>
    private IEnumerator EyeBlinkLoop()
    {
        while (true)
        {
            // Wait for random interval
            float waitTime = Random.Range(blinkInterval * 0.7f, blinkInterval * 1.3f);
            yield return new WaitForSeconds(waitTime);
            
            // Blink (close eyes) with smooth transition
            float transitionDuration = eyeBlinkExpression.transitionDuration;
            float elapsed = 0f;
            
            // Close eyes
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
            
            // Ensure fully closed
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                SetBlendShapeValue(bs.GetBlendShapeName(), bs.weight);
            }
            
            // Wait for blink duration
            yield return new WaitForSeconds(blinkDuration);
            
            // Open eyes with smooth transition
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
            
            // Ensure fully open
            foreach (var bs in eyeBlinkExpression.blendShapes)
            {
                SetBlendShapeValue(bs.GetBlendShapeName(), 0f);
            }
        }
    }
    
    /// <summary>
    /// Refresh/rebuild the blendshape cache (call after character changes)
    /// </summary>
    public void RefreshBlendShapes()
    {
        FindAllSkinnedMeshRenderers();
        CacheBlendShapeIndices();
    }
}
