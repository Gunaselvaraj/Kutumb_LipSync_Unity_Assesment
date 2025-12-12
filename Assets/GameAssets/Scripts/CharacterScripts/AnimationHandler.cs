using System.Collections;
using UnityEngine;
public class AnimationHandler : MonoBehaviour
{
    [Header("Button")]
    public GameObject PlayButton;
    public GameObject SpeakButton;

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
            SpeakButton.SetActive(true);
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
        
        if (facialSystem != null)
        {
            facialSystem.StopRandomExpression();
            facialSystem.ResetAllBlendShapes(0.1f);
        }
        
        if (simpleLipSync != null && talkAudioClip != null)
        {
            simpleLipSync.StartLipSync(talkAudioClip);
        }
    }

    public void StartTalkingImmediate()
    {
        if (animator == null)  return;
        PlayButton.SetActive(false);
        SpeakButton.SetActive(false);
        animator.SetTrigger("Talk");
        
        if (facialSystem != null)
        {
            facialSystem.StopRandomExpression();
            facialSystem.ResetAllBlendShapes(0.1f);
        }
        
        if (simpleLipSync != null && talkAudioClip != null)
        {
            simpleLipSync.StartLipSync(talkAudioClip);
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
        SpeakButton.SetActive(false);
        
        if (facialSystem != null)
        {
            facialSystem.StopRandomExpression();
            facialSystem.ResetAllBlendShapes(0.3f);
            yield return new WaitForSeconds(0.3f);
        }
        
        if (facialSystem != null && Sad != null)
        {
            facialSystem.SetExpression(Sad);
        }
        TriggerAnimation("Sad", true);
        yield return new WaitForSeconds(sadAnimationDuration);
        
        if (facialSystem != null && Happy != null)
        {
            facialSystem.ResetAllBlendShapes(ResetExpressionDuration);
            yield return new WaitForSeconds(ResetExpressionDuration);
            facialSystem.SetExpression(Happy);
        }
        TriggerAnimation("Happy", true);
        yield return new WaitForSeconds(happyAnimationDuration);
        
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
        SpeakButton.SetActive(true);
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

    public void PlayRandomExpression(){
        if (facialSystem == null) return;
        
        facialSystem.PlayRandomExpression();
    }
}
