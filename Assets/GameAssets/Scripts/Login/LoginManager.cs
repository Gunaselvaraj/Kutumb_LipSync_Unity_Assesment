using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button loginButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Scene Settings")]
    [SerializeField] private int nextSceneToload = 1;
    
    [Header("Animation Settings")]
    [SerializeField] private float Scaleduration = 0.5f;
    
    public event Action OnLoginSuccess;
    public event Action OnLoginFailed;
    
    private bool isLoggingIn = false;
    
    private void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        if (passwordInputField != null)
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
        }
        
        if (statusText != null)
        {
            statusText.text = "";
        }
    }
    
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        if (!Regex.IsMatch(email, pattern))
            return false;
        
        email = email.ToLower();
        return email.EndsWith("@gmail.com") || 
               email.EndsWith("@icloud.com") || 
               email.EndsWith("@me.com") || 
               email.EndsWith("@mac.com");
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
        
        string email = emailInputField != null ? emailInputField.text : "";
        string password = passwordInputField != null ? passwordInputField.text : "";
        
        if (IsValidEmail(email) && !string.IsNullOrEmpty(password))
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
            string email = emailInputField != null ? emailInputField.text : "";
            string password = passwordInputField != null ? passwordInputField.text : "";
            
            if (string.IsNullOrEmpty(email))
            {
                statusText.text = "Please enter an email";
            }
            else if (!IsValidEmail(email))
            {
                statusText.text = "Check EmailID and password ";
            }
            else if (string.IsNullOrEmpty(password))
            {
                statusText.text = "Please enter password";
            }
            else
            {
                statusText.text = "Password is wrong";
            }
            
            statusText.color = Color.red;
            
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, Scaleduration, 5, 0.5f).SetEase(Ease.InOutSine);
            
            statusText.DOFade(0f, 0.5f).SetDelay(2f);
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
