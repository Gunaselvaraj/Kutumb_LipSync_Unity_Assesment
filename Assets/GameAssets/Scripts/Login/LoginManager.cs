using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
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
    [SerializeField] private TMP_InputField SignUP_PassWord;
    [SerializeField] private TMP_InputField SignUP_ConfirmPasswordInputField;

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

    private bool isLoggingIn;

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
    private FirebaseAuth auth;
    private bool firebaseInitialized;
#endif

    private void Start()
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        InitializeFirebase();
#endif
        passwordInputField.contentType = TMP_InputField.ContentType.Password;
        SignUP_PassWord.contentType = TMP_InputField.ContentType.Password;
        SignUP_ConfirmPasswordInputField.contentType = TMP_InputField.ContentType.Password;
        statusText.text = "";
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseInitialized = true;
            }
            else
            {
                Debug.LogError("Firebase dependency error: " + task.Result);
            }
        });
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

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        var task = auth.SignInWithEmailAndPasswordAsync(
            emailInputField.text,
            passwordInputField.text
        );

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
            ShowLoginFailedFirebase(task.Exception);
        else
            ShowLoginSuccess();
#else
        ShowLoginSuccess();
#endif
        isLoggingIn = false;
    }

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
    public void OnGoogleSignInButtonClicked()
    {
        if (!firebaseInitialized)
        {
            ShowLoginFailed("Firebase not initialized");
            return;
        }

        var providerData = new FederatedOAuthProviderData(GoogleAuthProvider.ProviderId);
        var provider = new FederatedOAuthProvider(providerData);
        auth.SignInWithProviderAsync(provider).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                ShowLoginFailed("Google Sign-In failed");
            }
            else
            {
                ShowLoginSuccess();
            }
        });
    }
#endif

    public void OnSignUpButtonClicked()
    {
        if (isLoggingIn) return;
        StartCoroutine(SignUpCoroutine());
    }

    private IEnumerator SignUpCoroutine()
    {
        isLoggingIn = true;
        yield return new WaitForSeconds(0.5f);

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        var task = auth.CreateUserWithEmailAndPasswordAsync(
            SignUP_EmailInputField.text,
            SignUP_PassWord.text
        );

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            ShowSignUpFailedFirebase(task.Exception);
        }
        else
        {
            var profile = new UserProfile { DisplayName = SignUP_NameInputField.text };
            auth.CurrentUser.UpdateUserProfileAsync(profile);
            ShowSignUpSuccess();
            ShowLoginPanel();
        }
#endif
        isLoggingIn = false;
    }

    private void ShowLoginSuccess()
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        string userName = null;
        if (auth != null && auth.CurrentUser != null)
        {
            userName = auth.CurrentUser.DisplayName;
            if (string.IsNullOrEmpty(userName))
            {
                string email = auth.CurrentUser.Email;
                if (!string.IsNullOrEmpty(email) && email.Contains("@"))
                {
                    userName = email.Substring(0, email.IndexOf('@'));
                }
            }
        }
        statusText.text = !string.IsNullOrEmpty(userName) ? $"Login Success! Welcome, {userName}" : "Login Success!";
#else
        statusText.text = "Login Success!";
#endif
        statusText.color = Color.green;
        statusText.transform.localScale = Vector3.zero;
        statusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        statusText.DOFade(1f, 0.3f);
        OnLoginSuccess?.Invoke();
        StartCoroutine(LoadNextScene());
    }

    private void ShowLoginFailed(string msg)
    {
        statusText.text = msg;
        statusText.color = Color.red;
        statusText.transform.DOPunchScale(Vector3.one * 0.2f, Scaleduration);
        statusText.DOFade(0f, 0.5f).SetDelay(2f);
        OnLoginFailed?.Invoke();
    }

    private void ShowLoginFailedFirebase(AggregateException ex)
    {
        ShowLoginFailed(ex.InnerExceptions[0].Message);
    }

    private void ShowSignUpFailedFirebase(AggregateException ex)
    {
        ShowLoginFailed(ex.InnerExceptions[0].Message);
    }

    private void ShowSignUpSuccess()
    {
        statusText.text = "Sign up successful!";
        statusText.color = Color.green;
        statusText.transform.DOScale(1f, 0.5f);
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(nextSceneToload);
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
    }

    public void ShowSignupPanel()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
    }
}
