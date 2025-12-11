using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_IOS
using UnityEngine.SignInWithApple;
#endif

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button appleSignInButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Scene Settings")]
    [SerializeField] private string humanoidSceneName = "2.GameScene";
    
    [Header("Login Settings")]
    [SerializeField] private bool enableFallbackCredentials = true; // For editor testing
    
    // Events
    public event Action<string, string> OnLoginSuccess; // userId, email
    public event Action<string> OnLoginFailed;
    
    // Apple ID authentication state
    private string currentUserId;
    private string currentUserEmail;
    private bool isAuthenticating = false;
    
    private void Start()
    {
        // Setup Apple Sign In button listener
        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.AddListener(OnAppleSignInButtonClicked);
        }
        
        // Clear status text
        UpdateStatusText("");
        
        // Check if user is already logged in
        CheckExistingCredentials();
    }
    
    private void CheckExistingCredentials()
    {
        // Check for saved Apple ID credentials
        string savedUserId = PlayerPrefs.GetString("AppleUserId", "");
        
        if (!string.IsNullOrEmpty(savedUserId))
        {
            Debug.Log("Found existing Apple ID credentials");
            UpdateStatusText("Found existing login session");
            
#if UNITY_IOS
            // Verify credential state on iOS
            StartCoroutine(CheckCredentialState(savedUserId));
#else
            // For testing in editor
            if (enableFallbackCredentials)
            {
                AutoLoginWithSavedCredentials(savedUserId);
            }
#endif
        }
    }
    
#if UNITY_IOS
    private IEnumerator CheckCredentialState(string userId)
    {
        var request = new SignInWithApple.GetCredentialStateRequest();
        request.userID = userId;
        
        yield return request.GetCredentialState((state) =>
        {
            switch (state)
            {
                case SignInWithApple.CredentialState.Authorized:
                    AutoLoginWithSavedCredentials(userId);
                    break;
                    
                case SignInWithApple.CredentialState.Revoked:
                    Debug.Log("Apple ID credentials revoked");
                    ClearSavedCredentials();
                    break;
                    
                case SignInWithApple.CredentialState.NotFound:
                    Debug.Log("Apple ID credentials not found");
                    ClearSavedCredentials();
                    break;
            }
        });
    }
#endif
    
    public void OnAppleSignInButtonClicked()
    {
        if (isAuthenticating)
        {
            Debug.LogWarning("Authentication already in progress");
            return;
        }
        
        StartCoroutine(SignInWithApple());
    }
    
    private IEnumerator SignInWithApple()
    {
        isAuthenticating = true;
        UpdateStatusText("Connecting to Apple ID...");
        
#if UNITY_IOS
        var loginArgs = new SignInWithApple.LoginOptions();
        loginArgs.RequestedScopes = SignInWithApple.Scope.Email | SignInWithApple.Scope.FullName;
        
        SignInWithApple.Login(loginArgs, (credential) =>
        {
            // Success callback
            HandleAppleLoginSuccess(credential);
        },
        (error) =>
        {
            // Error callback
            HandleAppleLoginError(error);
        });
#else
        // Fallback for testing in Unity Editor
        yield return new WaitForSeconds(1.5f);
        
        if (enableFallbackCredentials)
        {
            Debug.LogWarning("Sign in with Apple is only available on iOS. Using test credentials.");
            SimulateAppleLoginSuccess();
        }
        else
        {
            OnAppleLoginFailure("Invalid login");
        }
#endif
        
        yield return null;
    }
    
#if UNITY_IOS
    private void HandleAppleLoginSuccess(SignInWithApple.ICredential credential)
    {
        var appleIdCredential = credential as SignInWithApple.AppleIDCredential;
        
        if (appleIdCredential != null)
        {
            currentUserId = appleIdCredential.user;
            currentUserEmail = appleIdCredential.email ?? PlayerPrefs.GetString("AppleUserEmail", "");
            
            // Save credentials
            PlayerPrefs.SetString("AppleUserId", currentUserId);
            if (!string.IsNullOrEmpty(currentUserEmail))
            {
                PlayerPrefs.SetString("AppleUserEmail", currentUserEmail);
            }
            PlayerPrefs.Save();
            
            // Get user's name if available
            string fullName = "";
            if (appleIdCredential.fullName != null)
            {
                fullName = $"{appleIdCredential.fullName.givenName} {appleIdCredential.fullName.familyName}".Trim();
                if (!string.IsNullOrEmpty(fullName))
                {
                    PlayerPrefs.SetString("AppleUserName", fullName);
                    PlayerPrefs.Save();
                }
            }
            
            OnAppleLoginSuccessful(currentUserId, currentUserEmail, fullName);
        }
        else
        {
            HandleAppleLoginError(new SignInWithApple.Error { code = 1001, localizedDescription = "Invalid credential format" });
        }
    }
    
    private void HandleAppleLoginError(SignInWithApple.Error error)
    {
        isAuthenticating = false;
        
        string errorMessage = "Invalid login";
        
        // Handle specific error codes
        if (error.code == 1001) // User canceled
        {
            errorMessage = "Sign in cancelled by user";
        }
        
        OnAppleLoginFailure(errorMessage);
    }
#else
    private void SimulateAppleLoginSuccess()
    {
        // Simulate Apple login for testing in editor
        currentUserId = "test.apple.user." + System.Guid.NewGuid().ToString();
        currentUserEmail = "test@example.com";
        
        PlayerPrefs.SetString("AppleUserId", currentUserId);
        PlayerPrefs.SetString("AppleUserEmail", currentUserEmail);
        PlayerPrefs.SetString("AppleUserName", "Test User");
        PlayerPrefs.Save();
        
        OnAppleLoginSuccessful(currentUserId, currentUserEmail, "Test User");
    }
#endif
    
    private void AutoLoginWithSavedCredentials(string userId)
    {
        currentUserId = userId;
        currentUserEmail = PlayerPrefs.GetString("AppleUserEmail", "");
        string userName = PlayerPrefs.GetString("AppleUserName", "");
        
        OnAppleLoginSuccessful(currentUserId, currentUserEmail, userName);
    }
    
    private void OnAppleLoginSuccessful(string userId, string email, string name)
    {
        isAuthenticating = false;
        
        string displayName = !string.IsNullOrEmpty(name) ? name : email;
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "User";
        }
        
        UpdateStatusText($"Login successful! Welcome, {displayName}!");
        Debug.Log($"Apple ID authentication successful - UserID: {userId}, Email: {email}");
        
        // Invoke success event
        OnLoginSuccess?.Invoke(userId, email);
        
        // Load humanoid scene after short delay
        StartCoroutine(LoadHumanoidScene());
    }
    
    private IEnumerator LoadHumanoidScene()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"Loading scene: {humanoidSceneName}");
        SceneManager.LoadScene(humanoidSceneName);
    }
    
    private void OnAppleLoginFailure(string errorMessage)
    {
        isAuthenticating = false;
        
        UpdateStatusText(errorMessage);
        Debug.LogWarning($"Apple ID authentication failed: {errorMessage}");
        
        // Invoke failure event
        OnLoginFailed?.Invoke(errorMessage);
    }
    
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    // Public methods for session management
    public void SignOut()
    {
        ClearSavedCredentials();
        UpdateStatusText("Signed out successfully");
        Debug.Log("User signed out");
    }
    
    private void ClearSavedCredentials()
    {
        PlayerPrefs.DeleteKey("AppleUserId");
        PlayerPrefs.DeleteKey("AppleUserEmail");
        PlayerPrefs.DeleteKey("AppleUserName");
        PlayerPrefs.Save();
        
        currentUserId = null;
        currentUserEmail = null;
    }
    
    public bool IsLoggedIn()
    {
        return !string.IsNullOrEmpty(currentUserId);
    }
    
    public string GetCurrentUserId()
    {
        return currentUserId;
    }
    
    public string GetCurrentUserEmail()
    {
        return currentUserEmail;
    }
    
    private void OnDestroy()
    {
        // Clean up button listener
        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.RemoveListener(OnAppleSignInButtonClicked);
        }
    }
}
