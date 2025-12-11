using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Lip-sync data configuration - defines blend shapes and behavior for talking animation
/// </summary>
[CreateAssetMenu(fileName = "New LipSync Data", menuName = "Facial System/LipSync Data", order = 2)]
public class LipSyncData : ScriptableObject
{
    [System.Serializable]
    public class LipSyncBlendShape
    {
        [Tooltip("Name of the blend shape")]
        public string blendShapeName;
        
        [Tooltip("Name of the mesh this blend shape belongs to (optional, for identification)")]
        public string meshName;
        
        [Tooltip("Is this a static value or does it animate?")]
        public bool isStatic = false;
        
        [Header("Static Value (if isStatic = true)")]
        [Range(0, 100)]
        [Tooltip("Fixed value when isStatic is true")]
        public float staticValue = 100f;
        
        [Header("Animated Range (if isStatic = false)")]
        [Range(0, 100)]
        [Tooltip("Minimum value for random animation")]
        public float minValue = 50f;
        
        [Range(0, 100)]
        [Tooltip("Maximum value for random animation")]
        public float maxValue = 100f;
        
        [Header("Animation Settings")]
        [Range(0.1f, 2f)]
        [Tooltip("How often this blend shape changes (seconds)")]
        public float changeInterval = 0.3f;
        
        [Tooltip("Easing curve for smooth transitions")]
        public Ease easeType = Ease.InOutSine;
        
        // Runtime variables (not serialized)
        [System.NonSerialized]
        public float currentValue;
        [System.NonSerialized]
        public float targetValue;
        [System.NonSerialized]
        public float timer;
        [System.NonSerialized]
        public Tweener activeTween;
        
        public LipSyncBlendShape()
        {
            blendShapeName = "";
            isStatic = false;
            staticValue = 100f;
            minValue = 50f;
            maxValue = 100f;
            changeInterval = 0.3f;
            easeType = Ease.InOutSine;
        }
        
        public LipSyncBlendShape(string name, bool isStaticValue, float value)
        {
            blendShapeName = name;
            isStatic = isStaticValue;
            if (isStaticValue)
            {
                staticValue = value;
                targetValue = value;
                currentValue = value;
            }
            else
            {
                minValue = 50f;
                maxValue = 100f;
            }
            changeInterval = 0.3f;
            easeType = Ease.InOutSine;
        }
        
        /// <summary>
        /// Initialize runtime values
        /// </summary>
        public void Initialize()
        {
            // Kill any existing tween
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
            }
            
            if (isStatic)
            {
                currentValue = staticValue;
                targetValue = staticValue;
            }
            else
            {
                currentValue = Random.Range(minValue, maxValue);
                targetValue = Random.Range(minValue, maxValue);
            }
            timer = 0f;
        }
        
        /// <summary>
        /// Update the blend shape value - checks if it's time to start a new tween
        /// </summary>
        public void Update(float deltaTime)
        {
            if (isStatic)
            {
                currentValue = staticValue;
                return;
            }
            
            // Update timer
            timer += deltaTime;
            
            // Check if it's time to change target
            if (timer >= changeInterval)
            {
                timer = 0f;
                targetValue = Random.Range(minValue, maxValue);
                
                // Kill previous tween and start new one
                if (activeTween != null && activeTween.IsActive())
                {
                    activeTween.Kill();
                }
                
                // Create smooth tween to new target
                activeTween = DOTween.To(() => currentValue, x => currentValue = x, targetValue, changeInterval)
                    .SetEase(easeType)
                    .SetUpdate(UpdateType.Normal);
            }
        }
        
        /// <summary>
        /// Reset to silence (0)
        /// </summary>
        public void ResetToSilence()
        {
            // Kill any active tween
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
            }
            
            currentValue = 0f;
            targetValue = 0f;
            timer = 0f;
        }
    }
    
    [Header("Blend Shapes Configuration")]
    [Tooltip("List of blend shapes to animate during lip-sync")]
    public List<LipSyncBlendShape> blendShapes = new List<LipSyncBlendShape>();
    
    [Header("Preview")]
    [Tooltip("Description of this lip-sync configuration")]
    [TextArea(2, 4)]
    public string description = "Lip-sync data for talking animation";
    
    /// <summary>
    /// Initialize all blend shapes
    /// </summary>
    public void Initialize()
    {
        foreach (var bs in blendShapes)
        {
            bs.Initialize();
        }
    }
    
    /// <summary>
    /// Update all blend shapes
    /// </summary>
    public void UpdateBlendShapes(float deltaTime)
    {
        foreach (var bs in blendShapes)
        {
            bs.Update(deltaTime);
        }
    }
    
    /// <summary>
    /// Reset all blend shapes to silence
    /// </summary>
    public void ResetToSilence()
    {
        foreach (var bs in blendShapes)
        {
            bs.ResetToSilence();
        }
    }
    
    /// <summary>
    /// Cleanup all tweens
    /// </summary>
    public void KillAllTweens()
    {
        foreach (var bs in blendShapes)
        {
            if (bs.activeTween != null && bs.activeTween.IsActive())
            {
                bs.activeTween.Kill();
            }
        }
    }
    
    /// <summary>
    /// Get blend shape value by name
    /// </summary>
    public float GetBlendShapeValue(string name)
    {
        foreach (var bs in blendShapes)
        {
            if (bs.blendShapeName == name)
            {
                return bs.currentValue;
            }
        }
        return 0f;
    }
}
