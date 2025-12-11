using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Scene Settings")]
    [SerializeField] private string nextSceneName = "2.GameScene";
    
    [Header("Login Settings")]
    [Tooltip("Enable to allow login, disable to simulate invalid login")]
    [SerializeField] private bool allowLogin = true;
    
    [Header("Animation Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 30f;
    [SerializeField] private int shakeVibrato = 10;
    
    // Events
    public event Action OnLoginSuccess;
    public event Action OnLoginFailed;
    
    private bool isLoggingIn = false;
    
    private void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        if (statusText != null)
        {
            statusText.text = "";
        }
    }
    
    public void OnLoginButtonClicked()
    {
        if (isLoggingIn) return;
        
        StartCoroutine(LoginCoroutine());
    }
    
    private IEnumerator LoginCoroutine()
    {
        isLoggingIn = true;
        
        // Simulate login delay
        yield return new WaitForSeconds(0.5f);
        
        if (allowLogin)
        {
            // Success
            ShowLoginSuccess();
        }
        else
        {
            // Failure
            ShowLoginFailed();
        }
        
        isLoggingIn = false;
    }
    
    private void ShowLoginSuccess()
    {
        if (statusText != null)
        {
            statusText.text = "Login Success!";
            statusText.color = Color.green;
            
            // Scale animation
            statusText.transform.localScale = Vector3.zero;
            statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            
            // Fade in
            statusText.alpha = 0f;
            statusText.DOFade(1f, 0.3f);
        }
        
        OnLoginSuccess?.Invoke();
        
        // Load next scene
        StartCoroutine(LoadNextScene());
    }
    
    private void ShowLoginFailed()
    {
        if (statusText != null)
        {
            statusText.text = "Invalid Login";
            statusText.color = Color.red;
            
            // Reset transform
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            
            // Shake animation
            statusText.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                .SetEase(Ease.OutQuad);
            
            // Pulse scale
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, shakeDuration, 5, 0.5f);
        }
        
        OnLoginFailed?.Invoke();
    }
    
    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
    
    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        }
        
        // Kill any active tweens
        if (statusText != null)
        {
            statusText.transform.DOKill();
            statusText.DOKill();
        }
    }
}
