using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
[CreateAssetMenu(fileName = "New LipSync Data", menuName = "Facial System/LipSync Data", order = 2)]
public class LipSyncData : ScriptableObject
{
    [System.Serializable]
    public class LipSyncBlendShape
    {
        public string blendShapeName;
        
        public string meshName;
        public bool isStatic = false;
        
        [Header("Static Value (if isStatic = true)")]
        [Range(0, 100)]
        public float staticValue = 100f;
        
        [Header("Animated Range (if isStatic = false)")]
        [Range(0, 100)]
        public float minValue = 50f;
        
        [Range(0, 100)]
        public float maxValue = 100f;
        
        [Header("Animation Settings")]
        [Range(0.1f, 2f)]
        public float changeInterval = 0.3f;
        public Ease easeType = Ease.InOutSine;
        
        [Header("Delayed Animation (if not static)")]
        public bool useDelayedAnimation = false;
        [Range(0, 100)]
        public float initialValue = 100f;
        public float animationDelay = 0.5f;
        
        [System.NonSerialized]
        public float currentValue;
        [System.NonSerialized]
        public float targetValue;
        [System.NonSerialized]
        public float timer;
        [System.NonSerialized]
        public float elapsedTime;
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
        public void Initialize()
        {
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
            }
            
            if (isStatic)
            {
                currentValue = staticValue;
                targetValue = staticValue;
            }
            else if (useDelayedAnimation)
            {
                currentValue = initialValue;
                targetValue = initialValue;
            }
            else
            {
                currentValue = Random.Range(minValue, maxValue);
                targetValue = Random.Range(minValue, maxValue);
            }
            timer = 0f;
            elapsedTime = 0f;
        }
        public void Update(float deltaTime)
        {
            if (isStatic)
            {
                currentValue = staticValue;
                return;
            }
            
            elapsedTime += deltaTime;
            
            if (useDelayedAnimation && elapsedTime < animationDelay)
            {
                currentValue = initialValue;
                return;
            }
            
            timer += deltaTime;
            
            if (timer >= changeInterval)
            {
                timer = 0f;
                targetValue = Random.Range(minValue, maxValue);
                
                if (activeTween != null && activeTween.IsActive())
                {
                    activeTween.Kill();
                }
                
                activeTween = DOTween.To(() => currentValue, x => currentValue = x, targetValue, changeInterval)
                    .SetEase(easeType)
                    .SetUpdate(UpdateType.Normal);
            }
        }
        public void ResetToSilence()
        {
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
    public List<LipSyncBlendShape> blendShapes = new List<LipSyncBlendShape>();
    
    [Header("Preview")]
    [TextArea(2, 4)]
    public string description = "Lip-sync data for talking animation";
    public void Initialize()
    {
        foreach (var bs in blendShapes)
        {
            bs.Initialize();
        }
    }
    public void UpdateBlendShapes(float deltaTime)
    {
        foreach (var bs in blendShapes)
        {
            bs.Update(deltaTime);
        }
    }
    public void ResetToSilence()
    {
        foreach (var bs in blendShapes)
        {
            bs.ResetToSilence();
        }
    }
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
