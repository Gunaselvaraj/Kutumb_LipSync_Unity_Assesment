using System.Collections;
using UnityEngine;

/// <summary>
/// Animation Handler using type-safe facial expression system
/// No more string-based blendshape errors!
/// </summary>
public class AnimationHandler : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    
    [Header("Facial System (Type-Safe)")]
    public FacialExpressionSystem facialSystem;
    public FacialExpressionData Happy,Sad;
    public float ResetExpressionDuration = 2f;
    
    [Header("Animation Timing")]
    public float sadAnimationDuration = 2f;
    public float happyAnimationDuration = 2f;
    public float delayBeforeAudio = 0.5f;
    public float delayAfterAudio = 0.5f;
    
    private bool isPlayingSequence = false;
    private Coroutine currentSequence;
    
    void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator is not assigned!");
        }
        
        if (facialSystem == null)
        {
            facialSystem = GetComponent<FacialExpressionSystem>();
        }
        
        InitializeFacialExpressions();
    }
    
    private void InitializeFacialExpressions()
    {
        if (facialSystem == null) return;
        
        // Expressions are now created as ScriptableObject assets
        // Use the Unity Editor to create expressions: Right-click -> Create -> Facial System -> Expression
        
        Debug.Log("Facial expression system initialized");
    }
    
    // ===== UI BUTTON METHODS =====
    
    /// <summary>
    /// Play Happy animation and facial expression
    /// </summary>
    public void PlayHappy()
    {
        if (animator != null)
        {
            animator.SetTrigger("Happy");
        }
        
        if (facialSystem != null && Happy != null)
        {
            facialSystem.SetExpression(Happy);
        }
        
        Debug.Log("Happy animation triggered from button");
    }
    
    /// <summary>
    /// Start Talk animation
    /// </summary>
    public void StartTalk()
    {
        if (animator == null) return;
        
        animator.SetTrigger("Talk");
        
        if (facialSystem != null)
        {
            // Reset to neutral (all blendshapes = 0 with transition)
            facialSystem.ResetAllBlendShapes(0.3f);
        }
        
        Debug.Log("Talk started from button");
    }
    
    /// <summary>
    /// Play full reaction sequence: Sad → Talk → Happy
    /// </summary>
    public void PlayReactionSequence()
    {
        if (isPlayingSequence)
        {
            Debug.LogWarning("Sequence already playing");
            return;
        }
        
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
        }
        
        currentSequence = StartCoroutine(ReactionSequenceCoroutine());
    }
    
    private IEnumerator ReactionSequenceCoroutine()
    {
        isPlayingSequence = true;
        
        ResetAllAnimations();
        
        // Step 1: Sad
        Debug.Log("Playing Sad expression and animation");
        if (facialSystem != null && Sad != null)
        {
            facialSystem.SetExpression(Sad);
        }
        TriggerAnimation("Sad", true);
        yield return new WaitForSeconds(sadAnimationDuration);
        
        // Step 2: Happy
        Debug.Log("Playing Happy expression and animation");
        if (facialSystem != null && Happy != null)
        {
            facialSystem.ResetAllBlendShapes(ResetExpressionDuration);
            yield return new WaitForSeconds(ResetExpressionDuration);
            facialSystem.SetExpression(Happy);
        }
        TriggerAnimation("Happy", true);
        yield return new WaitForSeconds(happyAnimationDuration);
        
        // Step 3: Sad again - Force reset first to ensure clean transition
        Debug.Log("Playing Sad expression and animation (second time)");
        if (facialSystem != null && Sad != null)
        {
            // Reset to neutral first
            facialSystem.ResetAllBlendShapes(ResetExpressionDuration);
            yield return new WaitForSeconds(ResetExpressionDuration);
            
            // Now apply Sad again
            facialSystem.SetExpression(Sad);
        }
        TriggerAnimation("Sad", true);
        yield return new WaitForSeconds(sadAnimationDuration);
        
        // Step 4: Idle (neutral - all blendshapes = 0 with transition)
        Debug.Log("Returning to Idle");
        if (facialSystem != null && Sad != null)
        {
            facialSystem.ResetAllBlendShapes(Sad.transitionDuration);
            yield return new WaitForSeconds(Sad.transitionDuration);
        }
        
        isPlayingSequence = false;
        Debug.Log("Reaction sequence completed");
    }
    
    private void TriggerAnimation(string animationName, bool isActive)
    {
        if (animator == null)
        {
            Debug.LogWarning($"Animator is null, cannot trigger animation: {animationName}");
            return;
        }
        
        if (isActive)
        {
            animator.SetTrigger(animationName);
            Debug.Log($"Animation trigger '{animationName}' activated");
        }
    }
    
    private void ResetAllAnimations()
    {
        if (animator == null) return;
        Debug.Log("All animations reset");
    }
    

    
    public void StopSequence()
    {
        if (currentSequence != null)
        {
            StopCoroutine(currentSequence);
            currentSequence = null;
        }
        
        ResetAllAnimations();
        
        if (facialSystem != null)
        {
            // Reset to neutral (all blendshapes = 0 with transition)
            facialSystem.ResetAllBlendShapes(0.3f);
        }
        
        isPlayingSequence = false;
        Debug.Log("Sequence stopped");
    }
}
