using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MainMenuSaveSlots : MonoBehaviour
{
    private const string AdminEmail = "adeventurerank60@gmail.com";

    public CharacterSelector characterSelector;

    [Header("Supabase Authentication")]
    [SerializeField] private string supabaseProjectUrl = "";
    [SerializeField] private string supabasePublishableKey = "";

    public static string CurrentAccessToken { get; private set; }
    public static string CurrentRefreshToken { get; private set; }
    public static string CurrentUserEmail { get; private set; }
    private string authenticatedUserId = "";

    [System.Serializable]
    private class SignupRequest
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    private class VerifySignupRequest
    {
        public string email;
        public string token;
        public string type = "signup";
    }

    [System.Serializable]
    private class AuthResponse
    {
        public string access_token;
        public string refresh_token;
        public AuthUser user;
    }

    [System.Serializable]
    private class AuthUser
    {
        public string id;
        public string email;
    }

    [System.Serializable]
    private class AuthErrorResponse
    {
        public string message;
        public string msg;
        public string error_description;
        public string error;
    }

    [Header("Button Sound Effects")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonClickSound;

    [Range(0f, 1f)]
    public float clickVolume = 0.8f;

    private bool open;
    public bool IsOpen => open || loginOpen;
    private bool loginOpen;
    private bool registerMode;
    private bool waitingForEmailCode;
    private bool authRequestInProgress;
    private bool loginComplete;
    private bool confirmOverwrite;
    private bool confirmDelete;
    private string loginEmail = "";
    private string loginPassword = "";
    private string confirmPassword = "";
    private string emailCode = "";
    private string authStatus = "";
    private CloudSaveSync cloudSaveSync;
    private Coroutine authRequest;

    private readonly List<CanvasGroup> groups =
        new List<CanvasGroup>();

    private readonly List<bool> previousInteraction =
        new List<bool>();

    private readonly List<bool> previousRaycasts =
        new List<bool>();

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle slotTitleStyle;
    private GUIStyle slotDetailStyle;
    private GUIStyle buttonStyle;
    private GUIStyle dangerButtonStyle;
    private GUIStyle confirmTitleStyle;
    private GUIStyle confirmBodyStyle;
    private GUIStyle footerStyle;
    private GUIStyle statusStyle;

    private Texture2D panelTexture;
    private Texture2D innerTexture;
    private Texture2D normalTexture;
    private Texture2D hoverTexture;
    private Texture2D selectedTexture;
    private Texture2D selectedHoverTexture;
    private Texture2D goldTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D dangerTexture;
    private Texture2D dangerHoverTexture;
    private Texture2D backdropTexture;
    private Texture2D inputTexture;
    private GUIStyle inputStyle;
    private GUIStyle fieldLabelStyle;

    private readonly Color gold =
        new Color(1f, 0.76f, 0.34f);

    private readonly Color paleGold =
        new Color(1f, 0.91f, 0.69f);

    private readonly Color muted =
        new Color(0.70f, 0.67f, 0.61f);

    void Awake()
    {
        GameSave.EnsureInstance();

        GameObject loginObject = GameObject.Find("Login");
        Button loginButton = loginObject != null
            ? loginObject.GetComponent<Button>()
            : null;

        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OpenLogin);
        }

        if (buttonAudioSource != null)
        {
            GameAudio.Route(buttonAudioSource, false);
            buttonAudioSource.volume = 1f;
        }
    }

    void PlayClickSound()
    {
        if (buttonAudioSource != null &&
            buttonClickSound != null)
        {
            buttonAudioSource.PlayOneShot(
                buttonClickSound,
                clickVolume
            );
        }
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.hideFlags = HideFlags.HideAndDontSave;

        return texture;
    }

    void CreateUIStyles()
    {
        panelTexture = MakeTexture(
            new Color(0.055f, 0.045f, 0.032f, 0.99f));

        innerTexture = MakeTexture(
            new Color(0.085f, 0.068f, 0.047f, 1f));

        normalTexture = MakeTexture(
            new Color(0.13f, 0.10f, 0.065f, 1f));

        hoverTexture = MakeTexture(
            new Color(0.24f, 0.18f, 0.095f, 1f));

        selectedTexture = MakeTexture(
            new Color(0.30f, 0.22f, 0.095f, 1f));

        selectedHoverTexture = MakeTexture(
            new Color(0.40f, 0.29f, 0.12f, 1f));

        goldTexture = MakeTexture(gold);

        buttonTexture = MakeTexture(
            new Color(0.20f, 0.15f, 0.08f, 1f));

        buttonHoverTexture = MakeTexture(
            new Color(0.38f, 0.28f, 0.12f, 1f));

        dangerTexture = MakeTexture(
            new Color(0.32f, 0.075f, 0.055f, 1f));

        dangerHoverTexture = MakeTexture(
            new Color(0.50f, 0.11f, 0.075f, 1f));

        backdropTexture = MakeTexture(
            new Color(0f, 0f, 0f, 0.76f));

        inputTexture = MakeTexture(
            new Color(0.055f, 0.045f, 0.032f, 1f));

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 30;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = paleGold;

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 15;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.wordWrap = true;
        subtitleStyle.normal.textColor = muted;

        slotTitleStyle = new GUIStyle(GUI.skin.label);
        slotTitleStyle.fontSize = 19;
        slotTitleStyle.fontStyle = FontStyle.Bold;
        slotTitleStyle.alignment = TextAnchor.MiddleLeft;
        slotTitleStyle.normal.textColor = paleGold;

        slotDetailStyle = new GUIStyle(GUI.skin.label);
        slotDetailStyle.fontSize = 12;
        slotDetailStyle.alignment = TextAnchor.MiddleLeft;
        slotDetailStyle.wordWrap = true;
        slotDetailStyle.normal.textColor = muted;

        buttonStyle = CreateButtonStyle(
            buttonTexture,
            buttonHoverTexture,
            paleGold
        );

        dangerButtonStyle = CreateButtonStyle(
            dangerTexture,
            dangerHoverTexture,
            Color.white
        );

        confirmTitleStyle = new GUIStyle(titleStyle);
        confirmTitleStyle.fontSize = 24;

        confirmBodyStyle = new GUIStyle(subtitleStyle);
        confirmBodyStyle.fontSize = 15;
        confirmBodyStyle.normal.textColor =
            new Color(0.90f, 0.86f, 0.78f);

        footerStyle = new GUIStyle(subtitleStyle);
        footerStyle.fontSize = 11;

        statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.fontSize = 10;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.alignment = TextAnchor.MiddleRight;
        statusStyle.normal.textColor = gold;

        inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = 18;
        inputStyle.padding = new RectOffset(12, 12, 8, 8);
        inputStyle.normal.background = inputTexture;
        inputStyle.focused.background = inputTexture;
        inputStyle.normal.textColor = paleGold;
        inputStyle.focused.textColor = Color.white;
        inputStyle.border = new RectOffset(2, 2, 2, 2);

        fieldLabelStyle = new GUIStyle(GUI.skin.label);
        fieldLabelStyle.fontSize = 14;
        fieldLabelStyle.fontStyle = FontStyle.Bold;
        fieldLabelStyle.normal.textColor = paleGold;
    }

    GUIStyle CreateButtonStyle(
        Texture2D normal,
        Texture2D hover,
        Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);

        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        style.padding = new RectOffset(8, 8, 6, 6);

        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = hover;
        style.focused.background = normal;

        style.normal.textColor = textColor;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = textColor;

        return style;
    }

    void DrawRect(Rect rect, Texture2D texture)
    {
        GUI.DrawTexture(rect, texture);
    }

    void DrawFrame()
    {
        DrawRect(
            new Rect(0, 0, 1280, 720),
            backdropTexture
        );

        // Main gold frame
        DrawRect(
            new Rect(222, 43, 836, 634),
            goldTexture
        );

        // Main dark panel
        DrawRect(
            new Rect(226, 47, 828, 626),
            panelTexture
        );

        DrawRect(
            new Rect(240, 61, 800, 606),
            innerTexture
        );

        // Decorative lines
        DrawRect(
            new Rect(270, 68, 740, 2),
            goldTexture
        );

        DrawRect(
            new Rect(270, 657, 740, 2),
            goldTexture
        );

        // Corner decorations
        DrawRect(
            new Rect(254, 78, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(1023, 78, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(254, 610, 3, 35),
            goldTexture
        );

        DrawRect(
            new Rect(1023, 610, 3, 35),
            goldTexture
        );
    }

    public void ShowSlots()
    {
        if (open) return;

        open = true;
        confirmOverwrite = false;
        confirmDelete = false;

        groups.Clear();
        previousInteraction.Clear();
        previousRaycasts.Clear();

        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                     FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;

            CanvasGroup group =
                canvas.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = canvas.gameObject
                    .AddComponent<CanvasGroup>();
            }

            groups.Add(group);
            previousInteraction.Add(group.interactable);
            previousRaycasts.Add(group.blocksRaycasts);

            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    public void ShowNewGame()
    {
        ShowSlots();
    }

    public void ShowContinue()
    {
        ShowSlots();
    }

    public void OpenLogin()
    {
        if (loginOpen || open) return;

        loginOpen = true;
        registerMode = false;
        waitingForEmailCode = false;
        authRequestInProgress = false;
        loginPassword = "";
        confirmPassword = "";
        emailCode = "";
        loginComplete = !string.IsNullOrWhiteSpace(CurrentAccessToken);
        authStatus = "";
        loginEmail = loginComplete ? CurrentUserEmail ?? "" : "";

        LockMenuCanvas();
    }

    void SwitchAuthMode()
    {
        if (authRequestInProgress || waitingForEmailCode || loginComplete) return;

        registerMode = !registerMode;
        confirmPassword = "";
        authStatus = "";
    }

    void SubmitLogin()
    {
        loginEmail = loginEmail.Trim();

        if (!IsValidEmail(loginEmail))
        {
            authStatus = "Enter a valid email address.";
            return;
        }

        if (string.IsNullOrEmpty(loginPassword))
        {
            authStatus = "Enter your password.";
            return;
        }

        if (!TryGetAuthEndpoint(out string endpoint)) return;

        authStatus = "Signing in...";
        authRequestInProgress = true;
        authRequest = StartCoroutine(LoginRoutine(endpoint));
    }

    IEnumerator LoginRoutine(string authEndpoint)
    {
        var payload = new SignupRequest
        {
            email = loginEmail,
            password = loginPassword
        };

        using (var request = new UnityWebRequest(
                   authEndpoint + "/token?grant_type=password",
                   UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabasePublishableKey.Trim());
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            authRequestInProgress = false;
            authRequest = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                authStatus = ReadAuthError(request.downloadHandler.text, request.error);
                yield break;
            }

            AuthResponse response = ParseAuthResponse(request.downloadHandler.text);
            if (response == null || string.IsNullOrWhiteSpace(response.access_token))
            {
                authStatus = "Supabase signed in without returning a session.";
                yield break;
            }

            CurrentAccessToken = response.access_token;
            CurrentRefreshToken = response.refresh_token;
            CurrentUserEmail = response.user != null && !string.IsNullOrWhiteSpace(response.user.email)
                ? response.user.email
                : loginEmail;
            authenticatedUserId = response.user != null ? response.user.id : "";
            loginComplete = true;
            authStatus = "Signed in successfully.";
            StartCloudSaveSync(response.access_token);
        }
    }

    void Logout()
    {
        string accessToken = CurrentAccessToken;
        string endpoint = "";
        bool canRevoke = !string.IsNullOrWhiteSpace(accessToken) &&
                         TryGetAuthEndpoint(out endpoint);

        CurrentAccessToken = null;
        CurrentRefreshToken = null;
        CurrentUserEmail = null;
        authenticatedUserId = "";
        if (CloudSaveSync.Instance != null) CloudSaveSync.Instance.EndSession();
        loginComplete = false;
        registerMode = false;
        waitingForEmailCode = false;
        loginPassword = "";
        confirmPassword = "";
        emailCode = "";
        authStatus = "You have been logged out. Log in again or register an account.";

        if (canRevoke)
        {
            StartCoroutine(LogoutRoutine(endpoint, accessToken));
        }
    }

    bool IsAdminAccount()
    {
        string accountEmail = string.IsNullOrWhiteSpace(CurrentUserEmail)
            ? loginEmail
            : CurrentUserEmail;
        return loginComplete && !string.IsNullOrWhiteSpace(CurrentAccessToken) &&
               string.Equals(accountEmail.Trim(), AdminEmail, System.StringComparison.OrdinalIgnoreCase);
    }

    void OpenAdminCardEditor()
    {
        if (!IsAdminAccount()) return;
        if (!Application.CanStreamedLevelBeLoaded("Card Data Editor"))
        {
            authStatus = "Card Data Editor scene is not included in Build Settings.";
            return;
        }

        Close();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Card Data Editor");
    }

    void StartCloudSaveSync(string accessToken)
    {
        cloudSaveSync = CloudSaveSync.EnsureInstance();
        cloudSaveSync.BeginSession(
            supabaseProjectUrl,
            supabasePublishableKey,
            accessToken,
            authenticatedUserId
        );

        authRequestInProgress = true;
        authStatus = "Checking your account save...";
        cloudSaveSync.StartInitialSync(SaveSlots.HasLocalProgress(), status =>
        {
            authStatus = status;
            authRequestInProgress = false;
        });
    }

    IEnumerator LogoutRoutine(string authEndpoint, string accessToken)
    {
        using (var request = new UnityWebRequest(
                   authEndpoint + "/logout",
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabasePublishableKey.Trim());
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return request.SendWebRequest();
        }

    }

    void LockMenuCanvas()
    {
        groups.Clear();
        previousInteraction.Clear();
        previousRaycasts.Clear();

        foreach (Canvas canvas in FindObjectsByType<Canvas>(
                     FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;

            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            groups.Add(group);
            previousInteraction.Add(group.interactable);
            previousRaycasts.Add(group.blocksRaycasts);
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    void SubmitSignup()
    {
        loginEmail = loginEmail.Trim();

        if (!IsValidEmail(loginEmail))
        {
            authStatus = "Enter a valid email address.";
            return;
        }

        if (string.IsNullOrEmpty(loginPassword))
        {
            authStatus = "Enter a password.";
            return;
        }

        if (loginPassword != confirmPassword)
        {
            authStatus = "The passwords do not match.";
            return;
        }

        if (!TryGetAuthEndpoint(out string endpoint)) return;

        authStatus = "Sending your verification code...";
        authRequestInProgress = true;
        authRequest = StartCoroutine(SignupRoutine(endpoint));
    }

    bool TryGetAuthEndpoint(out string endpoint)
    {
        endpoint = "";
        string projectUrl = supabaseProjectUrl.Trim().TrimEnd('/');

        if (!projectUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(supabasePublishableKey))
        {
            authStatus = "Set the Supabase project URL and publishable key on this component in the Inspector.";
            return false;
        }

        if (!System.Uri.TryCreate(projectUrl, System.UriKind.Absolute, out System.Uri uri) ||
            !System.Text.RegularExpressions.Regex.IsMatch(uri.Host, "^[A-Za-z0-9.-]+$"))
        {
            authStatus = "The Supabase project URL is not valid.";
            return false;
        }

        endpoint = projectUrl + "/auth/v1";
        return true;
    }

    static bool IsValidEmail(string email)
    {
        int at = email.IndexOf('@');
        int dot = email.LastIndexOf('.');
        return at > 0 && dot > at + 1 && dot < email.Length - 1;
    }

    IEnumerator SignupRoutine(string authEndpoint)
    {
        var payload = new SignupRequest
        {
            email = loginEmail,
            password = loginPassword
        };

        using (var request = new UnityWebRequest(
                   authEndpoint + "/signup",
                   UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(
                JsonUtility.ToJson(payload));
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabasePublishableKey.Trim());
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            authRequestInProgress = false;
            authRequest = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                authStatus = ReadAuthError(request.downloadHandler.text,
                    request.error);
                yield break;
            }

            AuthResponse response = ParseAuthResponse(
                request.downloadHandler.text);

            if (response != null &&
                !string.IsNullOrWhiteSpace(response.access_token))
            {
                authStatus = "Email confirmation is disabled in Supabase. Enable it to require the six-digit code.";
                yield break;
            }
        }

        loginPassword = "";
        confirmPassword = "";
        waitingForEmailCode = true;
        authStatus = "Check your email and enter the six-digit signup code.";
    }

    void VerifyEmailCode()
    {
        if (emailCode.Length != 6)
        {
            authStatus = "Enter all six digits from the email.";
            return;
        }

        if (!TryGetAuthEndpoint(out string endpoint)) return;

        authStatus = "Verifying your code...";
        authRequestInProgress = true;
        authRequest = StartCoroutine(VerifyRoutine(endpoint));
    }

    IEnumerator VerifyRoutine(string authEndpoint)
    {
        var payload = new VerifySignupRequest
        {
            email = loginEmail,
            token = emailCode
        };

        using (var request = new UnityWebRequest(
                   authEndpoint + "/verify",
                   UnityWebRequest.kHttpVerbPOST))
        {
            byte[] body = Encoding.UTF8.GetBytes(
                JsonUtility.ToJson(payload));
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("apikey", supabasePublishableKey.Trim());
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            authRequestInProgress = false;
            authRequest = null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                authStatus = ReadAuthError(request.downloadHandler.text,
                    request.error);
                yield break;
            }

            AuthResponse response = ParseAuthResponse(
                request.downloadHandler.text);

            if (response == null ||
                string.IsNullOrWhiteSpace(response.access_token))
            {
                authStatus = "Email verified, but Supabase did not return a login session.";
                yield break;
            }

            CurrentAccessToken = response.access_token;
            CurrentRefreshToken = response.refresh_token;
            CurrentUserEmail = response.user != null && !string.IsNullOrWhiteSpace(response.user.email)
                ? response.user.email
                : loginEmail;
            authenticatedUserId = response.user != null ? response.user.id : "";
            loginComplete = true;
            authStatus = "Email verified. You are now signed in.";
            StartCloudSaveSync(response.access_token);
        }
    }

    static AuthResponse ParseAuthResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonUtility.FromJson<AuthResponse>(json);
        }
        catch
        {
            return null;
        }
    }

    static string ReadAuthError(string json, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                AuthErrorResponse error =
                    JsonUtility.FromJson<AuthErrorResponse>(json);
                if (error != null)
                {
                    if (!string.IsNullOrWhiteSpace(error.message)) return error.message;
                    if (!string.IsNullOrWhiteSpace(error.msg)) return error.msg;
                    if (!string.IsNullOrWhiteSpace(error.error_description)) return error.error_description;
                    if (!string.IsNullOrWhiteSpace(error.error)) return error.error;
                }
            }
            catch
            {
                // Fall through to the request's transport error.
            }
        }

        return string.IsNullOrWhiteSpace(fallback)
            ? "Supabase authentication failed."
            : fallback;
    }

    void Close()
    {
        open = false;
        loginOpen = false;
        confirmOverwrite = false;
        confirmDelete = false;

        if (authRequest != null)
        {
            StopCoroutine(authRequest);
            authRequest = null;
        }

        authRequestInProgress = false;
        loginPassword = "";
        confirmPassword = "";

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] == null) continue;

            groups[i].interactable =
                previousInteraction[i];

            groups[i].blocksRaycasts =
                previousRaycasts[i];
        }

        groups.Clear();
        previousInteraction.Clear();
        previousRaycasts.Clear();
    }

    void OnDisable()
    {
        Close();
    }

    void Update()
    {
        if ((open || loginOpen) && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void StartNew()
    {
        int character = characterSelector != null
            ? characterSelector.SelectedCharacter
            : PlayerPrefs.GetInt("SelectedCharacter", 0);

        Close();

        PlayerPrefs.SetInt("SelectedCharacter", character);
        GameSave.EnsureInstance().NewGame();
        UnityEngine.SceneManagement.SceneManager.LoadScene("StoryScene");
    }

    void DrawSlot(int index)
    {
        var saved = SaveSlots.Read(index);

        float y = 198 + index * 94;
        bool isSelected = index == SaveSlots.Active;

        Rect slotRect = new Rect(277, y, 726, 80);

        Texture2D background = isSelected
            ? selectedTexture
            : normalTexture;

        Texture2D hoverBackground = isSelected
            ? selectedHoverTexture
            : hoverTexture;

        GUIStyle slotButton = new GUIStyle(GUI.skin.button);

        slotButton.normal.background = background;
        slotButton.hover.background = hoverBackground;
        slotButton.active.background = hoverBackground;
        slotButton.focused.background = background;
        slotButton.border = new RectOffset(2, 2, 2, 2);

        if (GUI.Button(
            slotRect,
            GUIContent.none,
            slotButton))
        {
            PlayClickSound();
            SaveSlots.Active = index;
        }

        // Left accent
        DrawRect(
            new Rect(
                slotRect.x,
                slotRect.y,
                4,
                slotRect.height),
            isSelected ? goldTexture : normalTexture
        );

        string slotName = "SLOT " + (index + 1);
        string detail;

        if (saved == null)
        {
            detail = "Empty slot - begin a new adventure.";
        }
        else
        {
            detail =
                "Scene: " + saved.scene
                + "   |   Character: " + (saved.character + 1)
                + "\nLast saved: " + saved.savedAt;
        }

        GUI.Label(
            new Rect(300, y + 8, 430, 30),
            slotName,
            slotTitleStyle
        );

        GUI.Label(
            new Rect(300, y + 36, 540, 38),
            detail,
            slotDetailStyle
        );

        GUI.Label(
            new Rect(755, y + 15, 225, 25),
            isSelected ? "◆ SELECTED" :
                (saved == null ? "EMPTY SLOT" : "SAVED"),
            statusStyle
        );
    }

    void DrawActionButton(
        Rect rect,
        string label,
        System.Action action,
        bool danger = false)
    {
        GUIStyle style = danger
            ? dangerButtonStyle
            : buttonStyle;

        if (GUI.Button(rect, label, style))
        {
            PlayClickSound();
            action();
        }
    }

    void DrawConfirmation(
        string heading,
        string message,
        string confirmLabel,
        System.Action confirmAction,
        System.Action cancelAction)
    {
        DrawRect(
            new Rect(300, 220, 680, 310),
            panelTexture
        );

        DrawRect(
            new Rect(300, 220, 680, 2),
            goldTexture
        );

        GUI.Label(
            new Rect(330, 247, 620, 48),
            heading,
            confirmTitleStyle
        );

        GUI.Label(
            new Rect(350, 305, 580, 115),
            message,
            confirmBodyStyle
        );

        DrawActionButton(
            new Rect(350, 435, 270, 52),
            confirmLabel,
            confirmAction,
            true
        );

        DrawActionButton(
            new Rect(650, 435, 270, 52),
            "CANCEL",
            cancelAction
        );
    }

    void DrawLoginOverlay()
    {
        GUI.Label(
            new Rect(270, 84, 740, 48),
            loginComplete ? "SIGNED IN" :
                (waitingForEmailCode ? "VERIFY YOUR EMAIL" :
                    (registerMode ? "CREATE YOUR ACCOUNT" : "LOG IN")),
            titleStyle
        );

        GUI.Label(
            new Rect(300, 137, 680, 32),
            waitingForEmailCode
                ? "A six-digit signup code was sent to " + loginEmail
                : (loginComplete
                    ? "You are signed in to this account."
                    : registerMode
                    ? "Create an account with your email and a password."
                    : "Enter the email and password for your existing account."),
            subtitleStyle
        );

        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && !authRequestInProgress;

        if (loginComplete)
        {
            GUI.Label(
                new Rect(350, 212, 580, 28),
                "ACCOUNT EMAIL",
                fieldLabelStyle
            );
            GUI.Label(
                new Rect(350, 242, 580, 42),
                loginEmail,
                confirmTitleStyle
            );

            GUI.Label(
                new Rect(350, 300, 580, 28),
                "USER ID",
                fieldLabelStyle
            );
            GUI.Label(
                new Rect(350, 330, 580, 38),
                string.IsNullOrWhiteSpace(authenticatedUserId)
                    ? "Available for this session"
                    : authenticatedUserId,
                subtitleStyle
            );

            DrawActionButton(
                new Rect(300, 430, 220, 52),
                "LOG OUT",
                Logout,
                true
            );

            if (IsAdminAccount())
            {
                DrawActionButton(
                    new Rect(535, 430, 250, 52),
                    "CARD EDITOR",
                    OpenAdminCardEditor
                );

                DrawActionButton(
                    new Rect(800, 430, 220, 52),
                    "CLOSE",
                    Close
                );
            }
            else
            {
                DrawActionButton(
                    new Rect(650, 430, 270, 52),
                    "CLOSE",
                    Close
                );
            }
        }
        else if (waitingForEmailCode)
        {
            GUI.Label(
                new Rect(350, 252, 580, 28),
                "SIX-DIGIT CODE",
                fieldLabelStyle
            );

            emailCode = KeepDigits(GUI.TextField(
                new Rect(350, 284, 580, 52),
                emailCode,
                6,
                inputStyle
            ));

            DrawActionButton(
                new Rect(350, 378, 270, 52),
                "VERIFY AND SIGN IN",
                VerifyEmailCode
            );

            DrawActionButton(
                new Rect(650, 378, 270, 52),
                "BACK",
                Close
            );
        }
        else
        {
            GUI.Label(new Rect(350, 190, 580, 24), "EMAIL", fieldLabelStyle);
            loginEmail = GUI.TextField(
                new Rect(350, 216, 580, 46),
                loginEmail,
                inputStyle
            );

            GUI.Label(new Rect(350, 273, 580, 24), "PASSWORD", fieldLabelStyle);
            loginPassword = GUI.PasswordField(
                new Rect(350, 299, 580, 46),
                loginPassword,
                '*',
                inputStyle
            );

            if (registerMode)
            {
                GUI.Label(new Rect(350, 356, 580, 24), "CONFIRM PASSWORD", fieldLabelStyle);
                confirmPassword = GUI.PasswordField(
                    new Rect(350, 382, 580, 46),
                    confirmPassword,
                    '*',
                    inputStyle
                );

                DrawActionButton(
                    new Rect(350, 458, 270, 52),
                    "SEND VERIFICATION CODE",
                    SubmitSignup
                );
            }
            else
            {
                DrawActionButton(
                    new Rect(350, 382, 270, 52),
                    "LOG IN",
                    SubmitLogin
                );
            }

            DrawActionButton(
                new Rect(650, registerMode ? 458 : 382, 270, 52),
                "BACK",
                Close
            );

            GUIStyle switchStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            switchStyle.normal.textColor = paleGold;
            string switchLabel = registerMode
                ? "Already have an account? Log in"
                : "Need an account? Register";
            if (GUI.Button(new Rect(350, 510, 570, 30), switchLabel, switchStyle))
            {
                PlayClickSound();
                SwitchAuthMode();
            }
        }

        GUI.enabled = oldEnabled;

        if (!string.IsNullOrWhiteSpace(authStatus))
        {
            GUI.Label(
                new Rect(290, 548, 700, 42),
                authStatus,
                subtitleStyle
            );
        }
    }

    static string KeepDigits(string value)
    {
        var digits = new StringBuilder(6);
        foreach (char character in value)
        {
            if (character < '0' || character > '9') continue;
            digits.Append(character);
            if (digits.Length == 6) break;
        }

        return digits.ToString();
    }

    void OnGUI()
    {
        if (!open && !loginOpen) return;

        if (titleStyle == null)
        {
            CreateUIStyles();
        }

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        bool oldEnabled = GUI.enabled;

        float scale = Mathf.Min(
            Screen.width / 1280f,
            Screen.height / 720f
        );

        GUI.matrix = Matrix4x4.TRS(
            new Vector3(
                (Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f,
                0f
            ),
            Quaternion.identity,
            Vector3.one * scale
        );

        DrawFrame();

        if (loginOpen)
        {
            DrawLoginOverlay();
            GUI.enabled = oldEnabled;
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
            return;
        }

        GUI.Label(
            new Rect(270, 78, 740, 52),
            "CHOOSE YOUR ADVENTURE",
            titleStyle
        );

        GUI.Label(
            new Rect(290, 132, 700, 28),
            "Select your save slot and continue your journey.",
            subtitleStyle
        );

        if (confirmDelete)
        {
            DrawConfirmation(
                "DELETE ADVENTURE?",
                "Delete Slot " + (SaveSlots.Active + 1) + "?\n\n"
                + "Its saved adventure, cards and currencies "
                + "will be permanently removed.",
                "DELETE SAVE",
                () =>
                {
                    SaveSlots.Delete(SaveSlots.Active);
                    confirmDelete = false;
                },
                () => confirmDelete = false
            );
        }
        else if (confirmOverwrite)
        {
            DrawConfirmation(
                "START A NEW ADVENTURE?",
                "Replace the adventure in Slot "
                + (SaveSlots.Active + 1) + "?\n\n"
                + "Its saved position, cards and currencies "
                + "will be reset.",
                "REPLACE AND START",
                StartNew,
                () => confirmOverwrite = false
            );
        }
        else
        {
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                DrawSlot(i);
            }

            var selected = SaveSlots.Read(SaveSlots.Active);

            GUI.enabled = selected != null
                && Application.CanStreamedLevelBeLoaded(
                    selected.scene
                );

            DrawActionButton(
                new Rect(277, 488, 166, 48),
                "▶  CONTINUE",
                () =>
                {
                    Close();
                    GameSave.EnsureInstance().ContinueGame();
                }
            );

            GUI.enabled = true;

            DrawActionButton(
                new Rect(454, 488, 166, 48),
                "+  NEW GAME",
                () =>
                {
                    if (selected == null)
                    {
                        StartNew();
                    }
                    else
                    {
                        confirmOverwrite = true;
                    }
                }
            );

            GUI.enabled = selected != null;

            DrawActionButton(
                new Rect(631, 488, 166, 48),
                "DELETE SLOT",
                () => confirmDelete = true,
                true
            );

            GUI.enabled = true;

            DrawActionButton(
                new Rect(808, 488, 195, 48),
                "←  BACK",
                Close
            );

            GUI.Label(
                new Rect(280, 555, 720, 28),
                "PROGRESS AUTOSAVES DURING EXPLORATION",
                footerStyle
            );

            GUI.Label(
                new Rect(280, 583, 720, 25),
                "Press ESC to return to the main menu.",
                footerStyle
            );
        }

        GUI.enabled = oldEnabled;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void OnDestroy()
    {
        DestroyTexture(panelTexture);
        DestroyTexture(innerTexture);
        DestroyTexture(normalTexture);
        DestroyTexture(hoverTexture);
        DestroyTexture(selectedTexture);
        DestroyTexture(selectedHoverTexture);
        DestroyTexture(goldTexture);
        DestroyTexture(buttonTexture);
        DestroyTexture(buttonHoverTexture);
        DestroyTexture(dangerTexture);
        DestroyTexture(dangerHoverTexture);
        DestroyTexture(backdropTexture);
        DestroyTexture(inputTexture);
    }

    void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }
}
