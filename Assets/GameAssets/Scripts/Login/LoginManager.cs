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
    [SerializeField] private int nextSceneToload = 1;
    
    [Header("Login Settings")]
    [SerializeField] private bool allowLogin = true;
    
    [Header("Animation Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 30f;
    [SerializeField] private int shakeVibrato = 10;
    
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
        
        yield return new WaitForSeconds(0.5f);
        
        if (allowLogin)
        {
            ShowLoginSuccess();
        }
        else
        {
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
            
            statusText.transform.localScale = Vector3.zero;
            statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            
            statusText.alpha = 0f;
            statusText.DOFade(1f, 0.3f);
        }
        
        OnLoginSuccess?.Invoke();
        
        StartCoroutine(LoadNextScene());
    }
    
    private void ShowLoginFailed()
    {
        if (statusText != null)
        {
            statusText.text = "Invalid Login";
            statusText.color = Color.red;
            
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            
            statusText.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                .SetEase(Ease.OutQuad);
            
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, shakeDuration, 5, 0.5f);
        }
        
        OnLoginFailed?.Invoke();
    }
    
    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextSceneToload);
    }
    
    private void OnDestroy()
    {
        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        }
        
        if (statusText != null)
        {
            statusText.transform.DOKill();
            statusText.DOKill();
        }
    }
}
