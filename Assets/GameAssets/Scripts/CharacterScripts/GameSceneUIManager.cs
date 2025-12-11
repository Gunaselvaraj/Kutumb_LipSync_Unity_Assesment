using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneUIManager : MonoBehaviour
{
    [Header("References")]
    public AnimationHandler animationHandler;
    
    [Header("UI Elements")]
    public Button playReactionButton;
    public TextMeshProUGUI buttonText;
    
    [Header("Button Text")]
    public string defaultButtonText = "Play Reaction";
    public string alternateButtonText = "Smile";
    
    void Start()
    {
        // Setup button listener
        if (playReactionButton != null)
        {
            playReactionButton.onClick.AddListener(OnPlayReactionButtonClicked);
        }
        else
        {
            Debug.LogWarning("Play Reaction Button is not assigned!");
        }
        
        // Set initial button text
        UpdateButtonText(defaultButtonText);
        
        // Validate animation handler
        if (animationHandler == null)
        {
            animationHandler = FindObjectOfType<AnimationHandler>();
            if (animationHandler == null)
            {
                Debug.LogError("AnimationHandler not found in scene!");
            }
        }
    }
    
    private void OnPlayReactionButtonClicked()
    {
        if (animationHandler != null)
        {
            Debug.Log("Play Reaction button clicked");
            animationHandler.PlayReactionSequence();
        }
        else
        {
            Debug.LogError("AnimationHandler is not assigned!");
        }
    }
    
    private void UpdateButtonText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
        else if (playReactionButton != null)
        {
            // Try to find text component in button
            TextMeshProUGUI textComponent = playReactionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up listeners
        if (playReactionButton != null)
        {
            playReactionButton.onClick.RemoveListener(OnPlayReactionButtonClicked);
        }
    }
}
