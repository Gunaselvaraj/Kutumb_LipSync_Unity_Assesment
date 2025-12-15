using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
using Firebase;
using Firebase.Auth;
#endif

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;

    [Header("Sign Up References")]
    [SerializeField] private TMP_InputField SignUP_NameInputField;
    [SerializeField] private TMP_InputField SignUP_EmailInputField;
    [SerializeField] private TMP_InputField SignUP_PassWord,SignUP_ConfirmPasswordInputField;

    
    [Header("Scene Settings")]
    [SerializeField] private int nextSceneToload = 1;
    
    [Header("Animation Settings")]
    [SerializeField] private float Scaleduration = 0.5f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 1f;
    [SerializeField] private int shakeVibrato = 10;
    
    [Header("UI Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    
    public event Action OnLoginSuccess;
    public event Action OnLoginFailed;
    
    private bool isLoggingIn = false;
    
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
    private FirebaseAuth auth;
    private bool firebaseInitialized = false;
#endif
    
    private void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        InitializeFirebase();
#endif
        if (passwordInputField != null)
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
        }
        if (SignUP_PassWord != null)
        {
            SignUP_PassWord.contentType = TMP_InputField.ContentType.Password;
        }
        if (SignUP_ConfirmPasswordInputField != null)
        {
            SignUP_ConfirmPasswordInputField.contentType = TMP_InputField.ContentType.Password;
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
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        if (firebaseInitialized)
        {
            var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => loginTask.IsCompleted);
            if (loginTask.Exception != null)
            {
                ShowLoginFailedFirebase(loginTask.Exception);
            }
            else
            {
                ShowLoginSuccess();
            }
        }
        else
        {
            ShowLoginFailed();
        }
#else
        if (IsValidEmail(email) && !string.IsNullOrEmpty(password))
        {
            ShowLoginSuccess();
        }
        else
        {
            ShowLoginFailed();
        }
#endif
        isLoggingIn = false;
    }
    
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseInitialized = true;
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }
    
    private void ShowLoginFailedFirebase(System.AggregateException exception)
    {
        if (statusText != null)
        {
            string message = "Login failed";
            foreach (var ex in exception.InnerExceptions)
            {
                if (ex is FirebaseException firebaseEx)
                {
                    var errorCode = (AuthError)firebaseEx.ErrorCode;
                    if (errorCode == AuthError.UserNotFound)
                    {
                        message = "Register first";
                        break;
                    }
                    else if (errorCode == AuthError.WrongPassword)
                    {
                        message = "Wrong password";
                        break;
                    }
                    else if (errorCode == AuthError.InvalidEmail)
                    {
                        message = "Invalid email format";
                        break;
                    }
                    else
                    {
                        message = firebaseEx.Message;
                        if (message.Contains("INTERNAL") || message.Contains("internal error"))
                            message = "An unknown error occurred. Please try again.";
                    }
                }
            }
            statusText.text = message;
            statusText.color = Color.red;
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, Scaleduration, 5, 0.5f);
            statusText.DOFade(0f, 0.5f).SetDelay(2f);
            statusText.gameObject.SetActive(true);
        }
        OnLoginFailed?.Invoke();
    }
#endif
    
    private void ShowLoginSuccess()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        string userName = null;
        if (auth != null && auth.CurrentUser != null)
        {
            userName = auth.CurrentUser.DisplayName;
            if (string.IsNullOrEmpty(userName))
            {
                // Try to use email prefix as fallback
                string email = auth.CurrentUser.Email;
                if (!string.IsNullOrEmpty(email) && email.Contains("@"))
                {
                    userName = email.Substring(0, email.IndexOf('@'));
                }
            }
        }
        if (statusText != null)
        {
            statusText.text = !string.IsNullOrEmpty(userName) ? $"Welcome !! {userName}" : "Login Success!";
            statusText.color = Color.green;
            statusText.transform.localScale = Vector3.zero;
            statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            statusText.alpha = 0f;
            statusText.DOFade(1f, 0.3f);
        }
#else
        if (statusText != null)
        {
            statusText.text = "Login Success!";
            statusText.color = Color.green;
            statusText.transform.localScale = Vector3.zero;
            statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            statusText.alpha = 0f;
            statusText.DOFade(1f, 0.3f);
        }
#endif
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
    
    public void OnSignUpButtonClicked()
    {
        if (isLoggingIn) return;
        StartCoroutine(SignUpCoroutine());
    }

    private IEnumerator SignUpCoroutine()
    {
        isLoggingIn = true;
        yield return new WaitForSeconds(0.5f);
        string name = SignUP_NameInputField != null ? SignUP_NameInputField.text : "";
        string email = SignUP_EmailInputField != null ? SignUP_EmailInputField.text : "";
        string password = SignUP_PassWord != null ? SignUP_PassWord.text : "";
        string confirmPassword = SignUP_ConfirmPasswordInputField != null ? SignUP_ConfirmPasswordInputField.text : "";
        if (string.IsNullOrEmpty(name))
        {
            ShowSignUpFailed("User Name is empty");
            isLoggingIn = false;
            yield break;
        }
        if (string.IsNullOrEmpty(email))
        {
            ShowSignUpFailed("Email field is empty");
            isLoggingIn = false;
            yield break;
        }
        if (password != confirmPassword)
        {
            ShowSignUpFailed("Password does not match");
            isLoggingIn = false;
            yield break;
        }
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
        if (firebaseInitialized)
        {
            var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => signUpTask.IsCompleted);
            if (signUpTask.Exception != null)
            {
                ShowSignUpFailedFirebase(signUpTask.Exception);
            }
            else
            {
                var user = signUpTask.Result.User;
                // Set display name
                UserProfile profile = new UserProfile { DisplayName = name };
                var updateProfileTask = user.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(() => updateProfileTask.IsCompleted);
                if (updateProfileTask.Exception != null)
                {
                    // Delete the user if profile update failed
                    user.DeleteAsync();
                    ShowSignUpFailed("Profile update failed. Please try again.");
                }
                else
                {
                    ShowSignUpSuccess();
                    ShowLoginPanel(); // Switch to login panel after successful registration
                }
            }
        }
        else
        {
            ShowSignUpFailed("Firebase not initialized");
        }
#else
        // Fallback for non-Firebase environments
        ShowSignUpSuccess();
        ShowLoginPanel();
#endif
        isLoggingIn = false;
    }

    private void ShowSignUpFailedFirebase(System.AggregateException exception)
    {
        if (statusText != null)
        {
            string message = "Sign up failed";
            foreach (var ex in exception.InnerExceptions)
            {
                if (ex is FirebaseException firebaseEx)
                {
                    switch ((AuthError)firebaseEx.ErrorCode)
                    {
                        case AuthError.EmailAlreadyInUse:
                            message = "Email already in use";
                            break;
                        case AuthError.InvalidEmail:
                            message = "Invalid email format";
                            break;
                        case AuthError.WeakPassword:
                            message = "Password is too weak";
                            break;
                        default:
                            message = firebaseEx.Message;
                            break;
                    }
                }
            }
            statusText.text = message;
            statusText.color = Color.red;
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            statusText.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                .SetEase(Ease.OutQuad);
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, shakeDuration, 5, 0.5f);
            statusText.DOFade(0f, 0.5f).SetDelay(2f);
        }
        OnLoginFailed?.Invoke();
    }

    private void ShowSignUpSuccess()
    {
        if (statusText != null)
        {
            statusText.text = "Sign up successful!";
            statusText.color = Color.green;
            statusText.transform.localScale = Vector3.zero;
            statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            statusText.alpha = 0f;
            statusText.DOFade(1f, 0.3f);
            // Ensure statusText is visible above both panels
            statusText.gameObject.SetActive(true);
        }
        OnLoginSuccess?.Invoke();
        StartCoroutine(LoadNextScene());
    }

    private void ShowSignUpFailed(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = Color.red;
            statusText.transform.localScale = Vector3.one;
            statusText.alpha = 1f;
            statusText.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                .SetEase(Ease.OutQuad);
            statusText.transform.DOPunchScale(Vector3.one * 0.2f, shakeDuration, 5, 0.5f);
            statusText.DOFade(0f, 0.5f).SetDelay(2f);
            // Ensure statusText is visible above both panels
            statusText.gameObject.SetActive(true);
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
        if (statusText != null)
        {
            statusText.transform.DOKill();
            statusText.DOKill();
        }
    }

    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (signupPanel != null) signupPanel.SetActive(false);
    }
    public void ShowSignupPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (signupPanel != null) signupPanel.SetActive(true);
    }
}
