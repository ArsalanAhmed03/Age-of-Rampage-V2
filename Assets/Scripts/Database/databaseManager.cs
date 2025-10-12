using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.IO;
using System;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_EDITOR || UNITY_STANDALONE
using SFB; // StandaloneFileBrowser
#endif

public class databaseManager : MonoBehaviour
{
    [System.Serializable]

    public class User
    {
        public string Username;
        public string StoredPassword;
        public string Email;
        public int Age;
        public int Gold;
        public int Level;
        public int Films;
        public int Wins;
        public string ProfilePictureURL;
        public List<string> OwnedUnits;
        public List<int> OwnedUnitsLevels;
        public List<int> OwnedUnitsCS;
        public List<int> OwnedUnitsCounts;
        public List<string> LoadOut;
        public List<string> OwnedUnitAbilities;

        public User() { }

        public User(string username, string storedPassword, string email, int age, int gold, int level, int films,
            List<string> ownedUnits, List<int> ownedUnitsLevels, List<int> ownedUnitsCS, List<int> ownedUnitsCounts, List<string> ownedUnitAbilities, string profilePictureURL, int wins = 0)
        {
            Username = username;
            StoredPassword = storedPassword;
            Email = email;
            Age = age;
            Gold = gold;
            Level = level;
            Films = films;
            OwnedUnits = ownedUnits ?? new List<string>();
            OwnedUnitsLevels = ownedUnitsLevels ?? new List<int>();
            OwnedUnitsCS = ownedUnitsCS ?? new List<int>();
            OwnedUnitsCounts = ownedUnitsCounts ?? new List<int>();
            OwnedUnitAbilities = ownedUnitAbilities ?? new List<string>();
            LoadOut = ownedUnits ?? new List<string>();
            ProfilePictureURL = string.IsNullOrEmpty(profilePictureURL)
                ? "https://res.cloudinary.com/dtl29wsay/image/upload/v1755016948/epvmfob9g36skxoiyevz.png"
                : profilePictureURL;
            Wins = wins;
        }
    }

    [System.Serializable]
    public class CloudinaryUploadResponse
    {
        public string secure_url;
    }
    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private FirebaseUser user;
    private DependencyStatus dependencyStatus;
    private bool isProcessing = false;

    [Header("Cloudinary Settings")]
    public string cloudinaryCloudName = "dtl29wsay";
    public string cloudinaryUploadPreset = "profile_pictures";

    [Header("UI Panels")]
    public GameObject loginPanel, signupPanel, guestLoginPanel;
    [Header("Buttons")]
    public Button loginButton, signupButton, guestLoginButton, switchToLoginButton, switchToSignupButton, switchToGuestLoginButton, selectProfilePictureButton;
    [Header("Login Fields")]
    public TMP_InputField loginEmailInput, loginPasswordInput;
    [Header("Signup Fields")]
    public TMP_InputField signupUsernameInput, signupEmailInput, signupAgeInput, signupPasswordInput, signupConfirmPasswordInput;
    [Header("Profile Picture UI")]
    public Image profilePicturePreview; public TextMeshProUGUI uploadStatusText;
    [Header("User Data Display")]
    public TextMeshProUGUI currentUsernameText, currentLevelText, currentGoldText, currentOwnedUnitsText; public Image currentProfilePicture;
    [Header("Messages")]
    public TextMeshProUGUI errorText, AutoLoginStatus;

    private string selectedImagePath = "";
    private Texture2D selectedImageTexture;
    private string savedEmail = "", savedPassword = "", savedUserName = "";
    public bool canAutoLogin = false;

    private void Start()
    {
        loginButton.onClick.AddListener(() => _ = Login());
        signupButton.onClick.AddListener(() => _ = Register());
        guestLoginButton.onClick.AddListener(GuestLogin);
        switchToLoginButton.onClick.AddListener(SwitchToLoginScreen);
        switchToSignupButton.onClick.AddListener(SwitchToSignupScreen);
        switchToGuestLoginButton.onClick.AddListener(SwitchToGuestLoginScreen);
        selectProfilePictureButton.onClick.AddListener(SelectProfilePicture);

        SwitchToLoginScreen();
        ClearUI();
    }

    public void SetUpDataBase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
                TryAutoLogin();
            }
            else Debug.LogError("Firebase dependencies not resolved: " + dependencyStatus);
        });
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void TryAutoLogin()
    {
        savedEmail = PlayerPrefs.GetString("SavedEmail", "");
        savedPassword = PlayerPrefs.GetString("SavedPassword", "");
        savedUserName = PlayerPrefs.GetString("UserName", "");

        if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
        {
            AutoLoginStatus.text = "User Found: " + savedUserName;
            canAutoLogin = true;
        }
        else
        {
            AutoLoginStatus.text = "No User Found";
            canAutoLogin = false;
        }
    }

    // ---------------- LOGIN ----------------
    public async Task Login()
    {
        if (isProcessing) return;
        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            errorText.text = "Please enter email and password.";
            return;
        }

        isProcessing = true; SetButtonsInteractable(false);
        bool success = await TryLoginWithRetry(loginEmailInput.text, loginPasswordInput.text);
        if (!success) errorText.text = "Login failed after retry.";
        isProcessing = false; SetButtonsInteractable(true);
    }

    private async Task<bool> TryLoginWithRetry(string email, string password)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
                var completed = await Task.WhenAny(loginTask, Task.Delay(10000));
                if (completed != loginTask) throw new TimeoutException("Login timed out.");
                user = loginTask.Result.User;

                HandleLoginSuccess(user);
                await FetchUserData(user.UserId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Login attempt {attempt} failed: {ex}");
                errorText.text = $"Login Failed (Attempt {attempt})";
                if (attempt == 2) return false;
                await Task.Delay(1000);
            }
        }
        return false;
    }

    private void HandleLoginSuccess(FirebaseUser loggedInUser)
    {
        errorText.text = "Login successful!";
        PlayerPrefs.SetString("SavedEmail", loginEmailInput.text);
        PlayerPrefs.SetString("SavedPassword", loginPasswordInput.text);
        PlayerPrefs.SetString("UserName", loggedInUser.DisplayName ?? loggedInUser.Email);
        PlayerPrefs.Save();
    }

    public void LoginHelper()
    {
        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
        {
            errorText.text = "No saved credentials found.";
            return;
        }
        TryLoginWithRetry(savedEmail, savedPassword).ContinueWithOnMainThread(task =>
        {
            if (!task.Result) errorText.text = "Auto-login failed.";
        });
    }

    // ---------------- REGISTER ----------------
    public async Task Register()
    {
        if (isProcessing) return;
        if (string.IsNullOrEmpty(signupUsernameInput.text) || string.IsNullOrEmpty(signupEmailInput.text) ||
            string.IsNullOrEmpty(signupAgeInput.text) || signupPasswordInput.text != signupConfirmPasswordInput.text)
        {
            errorText.text = "Invalid signup details.";
            return;
        }

        isProcessing = true; SetButtonsInteractable(false);
        bool success = await TryRegisterWithRetry();
        if (!success) errorText.text = "Registration failed after retry.";
        isProcessing = false; SetButtonsInteractable(true);
    }

    private async Task<bool> TryRegisterWithRetry()
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                // Upload to Cloudinary first (if image is selected)
                string profilePictureURL = await UploadImageToCloudinaryAsync();
                if (string.IsNullOrEmpty(profilePictureURL))
                {
                    // fallback to default if upload failed
                    profilePictureURL = "https://res.cloudinary.com/dtl29wsay/image/upload/v1755416199/xnofhbrurygsaotspe1c.png";
                }

                // Create Firebase account
                var registerTask = auth.CreateUserWithEmailAndPasswordAsync(signupEmailInput.text, signupPasswordInput.text);
                var completed = await Task.WhenAny(registerTask, Task.Delay(10000));
                if (completed != registerTask) throw new TimeoutException("Registration timed out.");

                user = registerTask.Result.User;

                // Update Firebase Auth profile
                await user.UpdateUserProfileAsync(new UserProfile { DisplayName = signupUsernameInput.text });

                // Save user data to Realtime DB with Cloudinary URL
                await SaveUserDataToDatabase(
                    signupUsernameInput.text,
                    signupPasswordInput.text,
                    signupEmailInput.text,
                    signupAgeInput.text,
                    profilePictureURL
                );

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration attempt {attempt} failed: {ex}");
                errorText.text = $"Signup Failed (Attempt {attempt})";

                if (attempt == 2) return false; // after 2 attempts, fail
                await Task.Delay(1000); // retry delay
            }
        }
        return false;
    }


    private async Task SaveUserDataToDatabase(string name, string password, string email, string age, string profilePictureURL)
    {

        var allAbilities = Enum.GetNames(typeof(AbilityTypes.Ability));
        string randomAbility1 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];
        string randomAbility2 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];
        string randomAbility3 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];

        var selectedAbilities = new List<string>
        {
            randomAbility1,
            randomAbility2,
            randomAbility3
        };

        var newUser = new User(name, password, email, int.Parse(age), 100, 1, 1,
            new List<string> { "CraneRon", "KenDuong", "Ronny-V" },
            new List<int> { 1, 1, 1 },
            new List<int> { 1, 1, 1 },
            new List<int> { 1, 1, 1 },
            selectedAbilities,
            profilePictureURL);

        string json = JsonUtility.ToJson(newUser);
        await dbRef.Child("users").Child(user.UserId).SetRawJsonValueAsync(json);

        PlayerPrefs.SetString("SavedEmail", email);
        PlayerPrefs.SetString("SavedPassword", password);
        PlayerPrefs.SetString("UserName", name);
        PlayerPrefs.Save();

        errorText.text = "Account created!";
        SwitchToLoginScreen();
    }

    // ---------------- FETCH DATA ----------------
    private async Task FetchUserData(string uid)
    {
        var dbTask = dbRef.Child("users").Child(uid).GetValueAsync();
        var completed = await Task.WhenAny(dbTask, Task.Delay(10000));
        if (completed != dbTask) { errorText.text = "Data fetch timed out."; return; }

        DataSnapshot snapshot = dbTask.Result;
        if (!snapshot.Exists) { errorText.text = "User data not found."; return; }

        User fetchedUser = JsonUtility.FromJson<User>(snapshot.GetRawJsonValue());
        UpdateUIWithUser(fetchedUser);
    }

    private void UpdateUIWithUser(User user)
    {
        currentUsernameText.text = "Username: " + user.Username;
        currentLevelText.text = "Level: " + user.Level;
        currentGoldText.text = "Gold: " + user.Gold;
        currentOwnedUnitsText.text = "Units: " + string.Join(", ", user.OwnedUnits ?? new List<string>());
        errorText.text = "Welcome, " + user.Username + "!";

        if (UserDataManager.Instance != null)
            UpdateUserDataManager(user);
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    private void UpdateUserDataManager(User user, bool isLoggedIn = true)
    {
        UserDataManager.Instance.UserName = user.Username;
        UserDataManager.Instance.Gold = user.Gold;
        UserDataManager.Instance.Films = user.Films;
        UserDataManager.Instance.Level = user.Level;
        UserDataManager.Instance.OwnedUnits = user.OwnedUnits ?? new List<string> { "CraneRon", "KenDuong", "Ronny-V" };
        UserDataManager.Instance.OwnedUnitsLevels = user.OwnedUnitsLevels ?? new List<int> { 1, 1, 1 };
        UserDataManager.Instance.OwnedUnitAbilities = user.OwnedUnitAbilities ?? new List<string> { "Cleptomaniac", "PiggyBank", "StaminaExpert" };
        UserDataManager.Instance.OwnedUnitsCS = user.OwnedUnitsCS ?? new List<int> { 1, 1, 1 };
        UserDataManager.Instance.OwnedUnitsCounts = user.OwnedUnitsCounts ?? new List<int> { 1, 1, 1 };
        UserDataManager.Instance.LoadOut = user.LoadOut ?? new List<string> { "CraneRon", "KenDuong", "Ronny-V" };
        UserDataManager.Instance.ProfilePictureURL = user.ProfilePictureURL ?? "";
        UserDataManager.Instance.Wins = user.Wins;
        UserDataManager.Instance.isLoggedIn = isLoggedIn;
    }

    // ---------------- GUEST LOGIN ----------------
    public void GuestLogin()
    {
        if (isProcessing) return;
        isProcessing = true; SetButtonsInteractable(false);
        var allAbilities = Enum.GetNames(typeof(AbilityTypes.Ability));
        string randomAbility1 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];
        string randomAbility2 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];
        string randomAbility3 = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];

        var selectedAbilities = new List<string>
        {
            randomAbility1,
            randomAbility2,
            randomAbility3
        };


        User guestUser = new User("GuestUser", "guest_password", "<guest_email>", 18, 500, 1, 1,
            new List<string> { "CraneRon", "KenDuong", "Ronny-V" },
            new List<int> { 1, 1, 1 },
            new List<int> { 1, 1, 1 },
            new List<int> { 1, 1, 1 },
            selectedAbilities,
            "https://res.cloudinary.com/dtl29wsay/image/upload/v1755016948/epvmfob9g36skxoiyevz.png");

        UpdateUIWithUser(guestUser);
        if (UserDataManager.Instance != null) UpdateUserDataManager(guestUser, false);

        PlayerPrefs.DeleteKey("SavedEmail");
        PlayerPrefs.DeleteKey("SavedPassword");
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.Save();

        AutoLoginStatus.text = "Guest Session"; canAutoLogin = false;
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    // ---------------- HELPERS ----------------
    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        isProcessing = false; SetButtonsInteractable(true);
        SceneManager.LoadScene("Pvp");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        loginButton.interactable = signupButton.interactable = guestLoginButton.interactable =
            switchToLoginButton.interactable = switchToSignupButton.interactable = switchToGuestLoginButton.interactable =
            selectProfilePictureButton.interactable = interactable;
    }

    private void SwitchToLoginScreen() { loginPanel.SetActive(true); signupPanel.SetActive(false); guestLoginPanel.SetActive(false); ClearInputFields(); }
    private void SwitchToSignupScreen() { signupPanel.SetActive(true); loginPanel.SetActive(false); guestLoginPanel.SetActive(false); ClearInputFields(); }
    private void SwitchToGuestLoginScreen() { guestLoginPanel.SetActive(true); loginPanel.SetActive(false); signupPanel.SetActive(false); ClearInputFields(); }

    private void ClearUI()
    {
        currentUsernameText.text = "Username: N/A";
        currentLevelText.text = "Level: N/A";
        currentGoldText.text = "Gold: N/A";
        currentOwnedUnitsText.text = "Units: N/A";
        errorText.text = "Please Login or Sign Up.";
        uploadStatusText.text = "";
        currentProfilePicture.gameObject.SetActive(false);
        profilePicturePreview.gameObject.SetActive(false);
        selectedImagePath = ""; selectedImageTexture = null;
    }

    private void ClearInputFields()
    {
        loginEmailInput.text = loginPasswordInput.text = signupUsernameInput.text =
            signupEmailInput.text = signupAgeInput.text = signupPasswordInput.text =
            signupConfirmPasswordInput.text = "";
    }

    public void Logout()
    {
        auth.SignOut();
        AutoLoginStatus.text = "No User Found"; canAutoLogin = false;
        PlayerPrefs.DeleteKey("SavedEmail"); PlayerPrefs.DeleteKey("SavedPassword"); PlayerPrefs.DeleteKey("UserName"); PlayerPrefs.Save();
        ClearUI(); SwitchToLoginScreen();
    }

    // ---------------- PROFILE PICTURE HANDLING ----------------
    public void SelectProfilePicture()
    {
        if (isProcessing) return;
#if UNITY_EDITOR || UNITY_STANDALONE
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Select Profile Picture", "", extensions, false);
        if (paths.Length > 0) { selectedImagePath = paths[0]; LoadImagePreview(selectedImagePath); }
#elif UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery((path) => { if (path != null) { selectedImagePath = path; LoadImagePreview(selectedImagePath); } }, "Select Profile Picture", "image/*");
#endif
    }

    private async Task<string> UploadImageToCloudinaryAsync()
    {
        if (selectedImageTexture == null)
            return ""; // fallback later to default image

        byte[] imageData = selectedImageTexture.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddField("upload_preset", cloudinaryUploadPreset);
        form.AddBinaryData("file", imageData, "profile_picture.png", "image/png");

        string uploadUrl = $"https://api.cloudinary.com/v1_1/{cloudinaryCloudName}/image/upload";

        using (UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form))
        {
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                CloudinaryUploadResponse response = JsonUtility.FromJson<CloudinaryUploadResponse>(request.downloadHandler.text);
                return response.secure_url ?? "";
            }
            else
            {
                Debug.LogError("Cloudinary upload failed: " + request.error);
                return "";
            }
        }
    }



    private void LoadImagePreview(string path)
    {
        if (!File.Exists(path)) return;
        byte[] imageData = File.ReadAllBytes(path);
        selectedImageTexture = new Texture2D(2, 2);
        selectedImageTexture.LoadImage(imageData);
        if (selectedImageTexture.width > 512 || selectedImageTexture.height > 512)
            selectedImageTexture = ResizeTexture(selectedImageTexture, 512, 512);

        profilePicturePreview.sprite = Sprite.Create(selectedImageTexture, new Rect(0, 0, selectedImageTexture.width, selectedImageTexture.height), new Vector2(0.5f, 0.5f));
        profilePicturePreview.gameObject.SetActive(true);
    }

    private Texture2D ResizeTexture(Texture2D original, int maxWidth, int maxHeight)
    {
        float aspect = (float)original.width / original.height;
        int w = Mathf.Min(maxWidth, original.width);
        int h = Mathf.RoundToInt(w / aspect);
        RenderTexture rt = RenderTexture.GetTemporary(w, h);
        Graphics.Blit(original, rt);
        Texture2D resized = new Texture2D(w, h);
        RenderTexture.active = rt;
        resized.ReadPixels(new Rect(0, 0, w, h), 0, 0); resized.Apply();
        RenderTexture.active = null; RenderTexture.ReleaseTemporary(rt);
        return resized;
    }
}
