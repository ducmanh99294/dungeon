// LoginUI.cs — gắn vào LoginPanel
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("Forms")]
    public GameObject loginForm;
    public GameObject registerForm;
    public GameObject loadingPanel;

    [Header("Login Form")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public Button btnLoginSubmit;    // "Login" button
    public Button btnGoToRegister;   // "Register" button trong LoginForm

    [Header("Register Form")]
    public TMP_InputField registerUsername;
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public Button btnRegisterSubmit; // "Register" button
    public Button btnGoToLogin;      // "Login" button trong RegisterForm

    [Header("Google Login")]
    public Button btnContinueWithGoogle;

    [Header("Feedback")]
    public TMP_Text errorText;
    public TMP_Text loadingText;

    [Header("Settings")]
    public string serverUrl = "http://localhost:3000";

    void Start()
    {
        // Login form buttons
        btnLoginSubmit.onClick.AddListener(OnLoginSubmit);
        btnGoToRegister.onClick.AddListener(ShowRegisterForm);

        // Register form buttons
        btnRegisterSubmit.onClick.AddListener(OnRegisterSubmit);
        btnGoToLogin.onClick.AddListener(ShowLoginForm);

        // Google
        if (btnContinueWithGoogle != null)
            btnContinueWithGoogle.onClick.AddListener(OnGoogleLogin);

        // Password ẩn ký tự
        loginPassword.contentType = TMP_InputField.ContentType.Password;
        registerPassword.contentType = TMP_InputField.ContentType.Password;

        // Mặc định hiện Login
        ShowLoginForm();
        HideError();
        loadingPanel.SetActive(false);
    }

    // ── SWITCH FORMS ────────────────────────────────────────────────────────

    void ShowLoginForm()
    {
        loginForm.SetActive(true);
        registerForm.SetActive(false);
        HideError();
    }

    void ShowRegisterForm()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(true);
        HideError();
    }

    // ── LOGIN ───────────────────────────────────────────────────────────────

    void OnLoginSubmit()
    {
        string username = loginUsername.text.Trim();
        string password = loginPassword.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng điền đầy đủ thông tin!");
            return;
        }

        StartCoroutine(LoginRequest(username, password));
    }

    IEnumerator LoginRequest(string username, string password)
    {
        ShowLoading("Đang đăng nhập...");

        var body = JsonUtility.ToJson(new LoginBody
        {
            username = username,
            password = password,
        });

        using var req = new UnityWebRequest(
            $"{serverUrl}/api/auth/login", "POST"
        );
        req.uploadHandler = new UploadHandlerRaw(
            System.Text.Encoding.UTF8.GetBytes(body)
        );
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        HideLoading();

        if (req.result != UnityWebRequest.Result.Success)
        {
            var err = JsonUtility.FromJson<ErrorResponse>(req.downloadHandler.text);
            ShowError(err?.message ?? "Đăng nhập thất bại!");
            yield break;
        }

        var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
        OnAuthSuccess(res);
    }

    // ── REGISTER ────────────────────────────────────────────────────────────

    void OnRegisterSubmit()
    {
        string username = registerUsername.text.Trim();
        string email = registerEmail.text.Trim();
        string password = registerPassword.text;

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng điền đầy đủ thông tin!");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự!");
            return;
        }

        StartCoroutine(RegisterRequest(username, email, password));
    }

    IEnumerator RegisterRequest(string username, string email, string password)
    {
        ShowLoading("Đang tạo tài khoản...");

        var body = JsonUtility.ToJson(new RegisterBody
        {
            username = username,
            email = email,
            password = password,
        });

        using var req = new UnityWebRequest(
            $"{serverUrl}/api/auth/register", "POST"
        );
        req.uploadHandler = new UploadHandlerRaw(
            System.Text.Encoding.UTF8.GetBytes(body)
        );
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        HideLoading();

        if (req.result != UnityWebRequest.Result.Success)
        {
            var err = JsonUtility.FromJson<ErrorResponse>(req.downloadHandler.text);
            ShowError(err?.message ?? "Đăng ký thất bại!");
            yield break;
        }

        // Đăng ký xong → tự đăng nhập luôn
        var res = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
        OnAuthSuccess(res);
    }

    // ── GOOGLE LOGIN ────────────────────────────────────────────────────────

    void OnGoogleLogin()
    {
        // Mở browser Google OAuth
        Application.OpenURL($"{serverUrl}/api/auth/google");
        // TODO: handle callback sau khi Google auth xong
        Debug.Log("[Login] Google login opened");
    }

    // ── ON AUTH SUCCESS ─────────────────────────────────────────────────────

    void OnAuthSuccess(AuthResponse res)
    {
        Debug.Log($"[Login] Thành công: {res.username}");

        // Lưu token
        PlayerPrefs.SetString("token", res.token);
        PlayerPrefs.SetString("playerId", res.playerId);
        PlayerPrefs.SetString("username", res.username);
        PlayerPrefs.Save();

        // Kết nối socket
        NetworkManager.Instance?.Connect(res.token, res.playerId);

        // Load MainMap
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        ShowLoading("Đang vào game...");
        yield return new WaitForSeconds(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMap");
    }

    // ── UI HELPERS ──────────────────────────────────────────────────────────

    void ShowError(string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.enabled = true;
    }

    void HideError()
    {
        if (errorText == null) return;
        errorText.enabled = false;
    }

    void ShowLoading(string msg = "Loading...")
    {
        loadingPanel.SetActive(true);
        if (loadingText != null) loadingText.text = msg;
        btnLoginSubmit.interactable = false;
        btnRegisterSubmit.interactable = false;
    }

    void HideLoading()
    {
        loadingPanel.SetActive(false);
        btnLoginSubmit.interactable = true;
        btnRegisterSubmit.interactable = true;
    }
}

// ── Data Classes ─────────────────────────────────────────────────────────────

[System.Serializable] class LoginBody { public string username; public string password; }
[System.Serializable] class RegisterBody { public string username; public string email; public string password; }
[System.Serializable] class AuthResponse { public string token; public string playerId; public string username; }
[System.Serializable] class ErrorResponse { public string message; }