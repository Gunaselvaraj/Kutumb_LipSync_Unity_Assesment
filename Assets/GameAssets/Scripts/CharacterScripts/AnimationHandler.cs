using System.Collections;
using UnityEngine;
public class AnimationHandler : MonoBehaviour
{
    [Header("Button")]
    public GameObject PlayButton;

    [Header("Animation Settings")]
    public Animator animator;
    
    [Header("Facial System (Type-Safe)")]
    public FacialExpressionSystem facialSystem;
    public FacialExpressionData Happy,Sad;
    public float ResetExpressionDuration = 2f;
    
    [Header("Lip Sync System")]
    public SimpleLipSync simpleLipSync;
    public AudioClip talkAudioClip;
    
    [Header("Animation Timing")]
    public float sadAnimationDuration = 2f;
    public float happyAnimationDuration = 2f;
    public float delayBeforeAudio = 0.5f;
    public float delayAfterAudio = 0.5f;
    
    private bool isPlayingSequence = false;
    private Coroutine currentSequence;
    
    void Start()
    {
        if (facialSystem == null)
        {
            facialSystem = GetComponent<FacialExpressionSystem>();
        }
        
        if (simpleLipSync == null)
        {
            simpleLipSync = GetComponent<SimpleLipSync>();
        }
        
        StartCoroutine(StartTalk());
    }
    

    public void PlayIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger("Idle");
            PlayButton.SetActive(true);
        }
    }
    
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
    }
    public IEnumerator StartTalk()
    {
        yield return new WaitForSeconds(delayBeforeAudio);

        if (animator == null) yield break;
        animator.SetTrigger("Talk");
        
        if (simpleLipSync != null && talkAudioClip != null)
        {
            simpleLipSync.StartLipSync(talkAudioClip);
        }
        else if (facialSystem != null)
        {
            facialSystem.ResetAllBlendShapes(0.3f);
        }
    }
    public void PlayReactionSequence()
    {
        if (isPlayingSequence)
        {
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
        PlayButton.SetActive(false);
        // Step 1: Sad
        if (facialSystem != null && Sad != null)
        {
            facialSystem.SetExpression(Sad);
        }
        TriggerAnimation("Sad", true);
        yield return new WaitForSeconds(sadAnimationDuration);
        
        // Step 2: Happy
        if (facialSystem != null && Happy != null)
        {
            facialSystem.ResetAllBlendShapes(ResetExpressionDuration);
            yield return new WaitForSeconds(ResetExpressionDuration);
            facialSystem.SetExpression(Happy);
        }
        TriggerAnimation("Happy", true);
        yield return new WaitForSeconds(happyAnimationDuration);
        
        // Step 3: Sad again - Force reset first to ensure clean transition
        if (facialSystem != null && Sad != null)
        {
            facialSystem.ResetAllBlendShapes(ResetExpressionDuration);
            yield return new WaitForSeconds(ResetExpressionDuration);
            
            facialSystem.SetExpression(Sad);
        }
        TriggerAnimation("Sad", true);
        yield return new WaitForSeconds(sadAnimationDuration);
        
        if (facialSystem != null && Sad != null)
        {
            facialSystem.ResetAllBlendShapes(Sad.transitionDuration);
            yield return new WaitForSeconds(Sad.transitionDuration);
        }
        PlayButton.SetActive(true);
        isPlayingSequence = false;
    }
    
    private void TriggerAnimation(string animationName, bool isActive)
    {
        if (animator == null)
        {
            return;
        }
        
        if (isActive)
        {
            animator.SetTrigger(animationName);
        }
    }
    
    private void ResetAllAnimations()
    {
        if (animator == null) return;
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
            facialSystem.ResetAllBlendShapes(0.3f);
        }
        
        isPlayingSequence = false;
    }
}
