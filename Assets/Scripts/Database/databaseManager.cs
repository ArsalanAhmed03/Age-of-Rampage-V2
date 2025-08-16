using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System;
using UnityEngine.Rendering;
using Unity.Services.Lobbies.Models;



#if UNITY_EDITOR || UNITY_STANDALONE
using SFB; // StandaloneFileBrowser for PC testing
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
        public int Wins;
        public string ProfilePictureURL;
        public List<string> OwnedUnits;
        public List<int> OwnedUnitsLevels;
        public List<string> LoadOut;

        public User() { }

        public User(string username, string storedPassword, string email, int age, int gold, int level, List<string> ownedUnits, List<int> ownedUnitsLevels, string profilePictureURL = "https://res.cloudinary.com/dtl29wsay/image/upload/v1755016948/epvmfob9g36skxoiyevz.png", int wins = 0)
        {
            Username = username;
            StoredPassword = storedPassword;
            Email = email;
            Age = age;
            Gold = gold;
            Level = level;
            OwnedUnits = ownedUnits ?? new List<string>();
            OwnedUnitsLevels = ownedUnitsLevels ?? new List<int>();
            LoadOut = ownedUnits ?? new List<string>();
            ProfilePictureURL = profilePictureURL ?? "https://res.cloudinary.com/dtl29wsay/image/upload/v1755016948/epvmfob9g36skxoiyevz.png";
            Wins = wins;
        }
    }

    [System.Serializable]
    public class CloudinaryUploadResponse
    {
        public string secure_url;
        public string public_id;
        public string format;
        public int width;
        public int height;
    }

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private FirebaseUser user;
    private DependencyStatus dependencyStatus;
    private bool isProcessing = false; // Prevent multiple operations

    [Header("Cloudinary Settings")]
    public string cloudinaryCloudName = "dtl29wsay";
    public string cloudinaryUploadPreset = "profile_pictures";

    [Header("Screen Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;
    public GameObject guestLoginPanel;

    [Header("Switch Buttons")]
    public Button switchToLoginButton;
    public Button switchToSignupButton;
    public Button switchToGuestLoginButton;

    [Header("Login UI")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;

    [Header("Guest UI")]
    public Button guestLoginButton;

    [Header("Signup UI")]
    public TMP_InputField signupUsernameInput;
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupAgeInput;
    public TMP_InputField signupPasswordInput;
    public TMP_InputField signupConfirmPasswordInput;
    public Button signupButton;

    [Header("Profile Picture UI")]
    public Button selectProfilePictureButton;
    public Image profilePicturePreview;
    public TextMeshProUGUI uploadStatusText;

    [Header("User Data Display")]
    public TextMeshProUGUI currentUsernameText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentOwnedUnitsText;
    public Image currentProfilePicture;

    [Header("Messages")]
    public TextMeshProUGUI errorText;

    [Header("AutoLogin")]

    public TextMeshProUGUI AutoLoginStatus;


    private string selectedImagePath = "";
    private Texture2D selectedImageTexture;
    private User currentUserData;

    // private void Awake()
    // {
    //     FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
    //     {
    //         dependencyStatus = task.Result;
    //         if (dependencyStatus == DependencyStatus.Available)
    //         {
    //             Debug.Log("<color=purple>[databaseManager] Starting Firebase initialization...</color>");
    //             InitializeFirebase();
    //             Debug.Log("<color=red>[databaseManager] Firebase dependencies resolved and initializing...</color>");
    //             UnityEngine.WSA.Application.InvokeOnAppThread(() =>
    //             {
    //                 TryAutoLogin();
    //             }, false);
    //         }
    //         else
    //         {
    //             Debug.LogError($"Firebase dependencies not resolved: {dependencyStatus}");
    //         }
    //     });

    // }

    public void SetUpDataBase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("<color=purple>[databaseManager] Starting Firebase initialization...</color>");
                    InitializeFirebase();
                    Debug.Log("<color=red>[databaseManager] Firebase dependencies resolved and initializing...</color>");
                    TryAutoLogin();
                }
                else
                {
                    Debug.LogError($"Firebase dependencies not resolved: {dependencyStatus}");
                }
            });
    }

    private string savedEmail = "";
    private string savedPassword = "";

    private string savedUserName = "";

    public bool canAutoLogin = false;


    private void TryAutoLogin()
    {
        Debug.Log("<color=green>[databaseManager] TryAutoLogin called</color>");
        try
        {
            savedEmail = PlayerPrefs.GetString("SavedEmail", "");
            savedPassword = PlayerPrefs.GetString("SavedPassword", "");
            savedUserName = PlayerPrefs.GetString("UserName", "");


            if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
            {
                // Debug.Log("<color=blue>[databaseManager] Saved credentials found. Email: " + savedEmail + "</color>");
                // StartCoroutine(LoginAsync(savedEmail, savedPassword));\
                AutoLoginStatus.text = "User Found : " + savedUserName;
                canAutoLogin = true;
            }
            else
            {
                Debug.Log("<color=blue>[databaseManager] Saved credentials Not found</color>");
                AutoLoginStatus.text = "No User Found";

                canAutoLogin = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[databaseManager] PlayerPrefs read error: " + ex);
        }
    }

    public void LoginHelper()
    {
        if (string.IsNullOrEmpty(savedEmail) || string.IsNullOrEmpty(savedPassword))
        {
            errorText.text = "No saved credentials found.";
            return;
        }
        StartCoroutine(LoginAsync(savedEmail, savedPassword));
    }


    private void Start()
    {
        loginButton.onClick.AddListener(Login);
        signupButton.onClick.AddListener(Register);
        guestLoginButton.onClick.AddListener(GuestLogin);
        switchToLoginButton.onClick.AddListener(SwitchToLoginScreen);
        switchToSignupButton.onClick.AddListener(SwitchToSignupScreen);
        switchToGuestLoginButton.onClick.AddListener(SwitchToGuestLoginScreen);
        selectProfilePictureButton.onClick.AddListener(SelectProfilePicture);

        SwitchToLoginScreen();
        ClearUI();
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = auth.CurrentUser != user && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void SelectProfilePicture()
    {
        if (isProcessing) return; // Prevent multiple operations

#if UNITY_EDITOR || UNITY_STANDALONE
        SelectProfilePicturePC();
#elif UNITY_ANDROID || UNITY_IOS
        SelectProfilePictureMobile();
#endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void SelectProfilePicturePC()
    {
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
        };

        var paths = StandaloneFileBrowser.OpenFilePanel("Select Profile Picture", "", extensions, false);

        if (paths.Length > 0)
        {
            selectedImagePath = paths[0];
            LoadImagePreview(selectedImagePath);
            uploadStatusText.text = "Image selected for profile picture.";
        }
    }
#endif

#if UNITY_ANDROID || UNITY_IOS
    private void SelectProfilePictureMobile()
    {
        // Simply call GetImageFromGallery - it handles permissions internally
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                selectedImagePath = path;
                LoadImagePreview(selectedImagePath);
                uploadStatusText.text = "Image selected for profile picture.";
            }
            else
            {
                uploadStatusText.text = "Image selection cancelled or permission denied.";
            }
        }, "Select Profile Picture", "image/*");
    }
#endif

    private void LoadImagePreview(string imagePath)
    {
        // Move exception handling outside of any potential yield-returning methods
        if (!File.Exists(imagePath))
        {
            Debug.LogError("Image file does not exist: " + imagePath);
            uploadStatusText.text = "Error: Image file not found.";
            return;
        }

        byte[] imageData = File.ReadAllBytes(imagePath);
        selectedImageTexture = new Texture2D(2, 2);

        if (!selectedImageTexture.LoadImage(imageData))
        {
            Debug.LogError("Failed to load image data");
            uploadStatusText.text = "Error: Failed to load image.";
            return;
        }

        if (selectedImageTexture.width > 512 || selectedImageTexture.height > 512)
        {
            selectedImageTexture = ResizeTexture(selectedImageTexture, 512, 512);
        }

        Sprite previewSprite = Sprite.Create(selectedImageTexture,
            new Rect(0, 0, selectedImageTexture.width, selectedImageTexture.height),
            new Vector2(0.5f, 0.5f));

        profilePicturePreview.sprite = previewSprite;
        profilePicturePreview.gameObject.SetActive(true);
    }

    private Texture2D ResizeTexture(Texture2D originalTexture, int maxWidth, int maxHeight)
    {
        float aspectRatio = (float)originalTexture.width / originalTexture.height;
        int newWidth, newHeight;

        if (originalTexture.width > originalTexture.height)
        {
            newWidth = Mathf.Min(maxWidth, originalTexture.width);
            newHeight = Mathf.RoundToInt(newWidth / aspectRatio);
        }
        else
        {
            newHeight = Mathf.Min(maxHeight, originalTexture.height);
            newWidth = Mathf.RoundToInt(newHeight * aspectRatio);
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(originalTexture, renderTexture);

        Texture2D resizedTexture = new Texture2D(newWidth, newHeight);
        RenderTexture.active = renderTexture;
        resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resizedTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        return resizedTexture;
    }

    private IEnumerator UploadImageToCloudinary()
    {
        uploadStatusText.text = "Uploading profile picture...";

        // Move validation outside of try-catch with yield
        if (selectedImageTexture == null)
        {
            uploadStatusText.text = "No image selected for upload.";
            selectedImagePath = "";
            yield break;
        }

        byte[] imageData = selectedImageTexture.EncodeToPNG();
        WWWForm form = new WWWForm();

        form.AddField("upload_preset", cloudinaryUploadPreset);
        form.AddBinaryData("file", imageData, "profile_picture.png", "image/png");

        string uploadUrl = $"https://api.cloudinary.com/v1_1/{cloudinaryCloudName}/image/upload";

        using (UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();

            // Handle response after yield
            if (request.result == UnityWebRequest.Result.Success)
            {
                HandleUploadSuccess(request.downloadHandler.text);
            }
            else
            {
                HandleUploadError(request.error);
            }
        }
    }

    private void HandleUploadSuccess(string responseText)
    {
        CloudinaryUploadResponse response = JsonUtility.FromJson<CloudinaryUploadResponse>(responseText);

        if (!string.IsNullOrEmpty(response.secure_url))
        {
            selectedImagePath = response.secure_url;
            uploadStatusText.text = "Profile picture uploaded successfully!";
        }
        else
        {
            uploadStatusText.text = "Upload failed: Invalid response";
            selectedImagePath = "";
        }
    }

    private void HandleUploadError(string error)
    {
        uploadStatusText.text = "Upload failed: " + error;
        selectedImagePath = "";
        Debug.LogError("Cloudinary upload error: " + error);
    }

    private IEnumerator LoadProfilePictureFromURL(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) yield break;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();

            // Handle response after yield
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite profileSprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));

                if (currentProfilePicture != null)
                {
                    currentProfilePicture.sprite = profileSprite;
                    currentProfilePicture.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Failed to load profile picture: " + request.error);
            }
        }
    }

    public void GuestLogin()
    {
        SetButtonsInteractable(false);
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null!");
            return;
        }
        User guestUser = new User(
            "GuestUser",
            "guest_password",
            "<guest_email>",
            18, // example guest age
            500,
            1,
            new() { "CraneRon", "KenDuong", "Ronny-V" },
            new() { 1, 1, 1 },
            "https://res.cloudinary.com/dtl29wsay/image/upload/v1755016948/epvmfob9g36skxoiyevz.png"
        );
        UpdateUserDataManager(guestUser, false);
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    public void Login()
    {
        if (isProcessing)
        {
            Debug.Log("Login already in progress...");
            return;
        }

        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            errorText.text = "Please enter email and password.";
            return;
        }

        StartCoroutine(LoginAsync(loginEmailInput.text, loginPasswordInput.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        isProcessing = true;
        SetButtonsInteractable(false);
        errorText.text = "Logging in...";

        // Perform the async operation first
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        // Handle the result after yield
        if (loginTask.Exception != null)
        {
            HandleLoginError(loginTask.Exception);
        }
        else
        {
            HandleLoginSuccess(loginTask.Result.User);
            yield return StartCoroutine(FetchUserDataCoroutine(user.UserId));
        }

        // Cleanup
        isProcessing = false;
        SetButtonsInteractable(true);
    }

    private void HandleLoginError(System.AggregateException exception)
    {
        Debug.LogError("Login failed: " + exception);
        errorText.text = "Login Failed: " + GetFirebaseErrorMessage(exception);
    }

    private void HandleLoginSuccess(FirebaseUser loggedInUser)
    {
        user = loggedInUser;
        Debug.Log($"Login successful: {user.Email}, UID: {user.UserId}");
        errorText.text = "Login successful! Loading data...";

        PlayerPrefs.SetString("SavedEmail", loginEmailInput.text);
        PlayerPrefs.SetString("SavedPassword", loginPasswordInput.text);
        PlayerPrefs.SetString("UserName", user.DisplayName ?? user.Email);
        PlayerPrefs.Save();
    }

    public void Register()
    {
        if (isProcessing)
        {
            Debug.Log("Registration already in progress...");
            return;
        }

        StartCoroutine(RegisterAsync(
            signupUsernameInput.text,
            signupEmailInput.text,
            signupPasswordInput.text,
            signupConfirmPasswordInput.text,
            signupAgeInput.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword, string age)
    {
        isProcessing = true;
        SetButtonsInteractable(false);

        // Validate input first
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(age) || password != confirmPassword)
        {
            errorText.text = "Invalid input fields.";
            isProcessing = false;
            SetButtonsInteractable(true);
            yield break;
        }

        // Upload profile picture first if selected
        string profilePictureURL = "";
        if (selectedImageTexture != null)
        {
            uploadStatusText.text = "Uploading profile picture...";
            yield return StartCoroutine(UploadImageToCloudinary());
            profilePictureURL = selectedImagePath;

            if (string.IsNullOrEmpty(profilePictureURL))
            {
                errorText.text = "Profile picture upload failed. Please try again.";
                isProcessing = false;
                SetButtonsInteractable(true);
                yield break;
            }
        }

        // Create user account
        uploadStatusText.text = "Creating account...";
        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            HandleRegistrationError(registerTask.Exception);
            isProcessing = false;
            SetButtonsInteractable(true);
            yield break;
        }

        user = registerTask.Result.User;
        Debug.Log($"Registered successfully. UID: {user.UserId}");

        // Update user profile
        var profile = new UserProfile { DisplayName = name };
        var profileTask = user.UpdateUserProfileAsync(profile);
        yield return new WaitUntil(() => profileTask.IsCompleted);

        if (profileTask.Exception != null)
        {
            Debug.LogError("Profile update failed: " + profileTask.Exception);
            errorText.text = "Profile Update Failed!";
            isProcessing = false;
            SetButtonsInteractable(true);
            yield break;
        }

        // Save user data to database
        yield return StartCoroutine(SaveUserDataToDatabase(name, password, email, age, profilePictureURL));

        isProcessing = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator SaveUserDataToDatabase(string name, string password, string email, string age, string profilePictureURL)
    {
        List<string> defaultUnits = new List<string> { "CraneRon", "KenDuong", "Ronny-V" };
        List<int> defaultUnitsLevels = new List<int> { 1, 1, 1 };
        int defaultWins = 0;

        User newUser = new User(name, password, email, int.Parse(age), 100, 1, defaultUnits, defaultUnitsLevels, profilePictureURL, defaultWins);

        string json = JsonUtility.ToJson(newUser);
        var dbTask = dbRef.Child("users").Child(user.UserId).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogError("Database write failed: " + dbTask.Exception);
            errorText.text = "Database Save Failed!";
        }
        else
        {
            Debug.Log("User data saved successfully!");
            uploadStatusText.text = "Account created successfully!";

            PlayerPrefs.SetString("SavedEmail", email);
            PlayerPrefs.SetString("SavedPassword", password);
            PlayerPrefs.SetString("UserName", name);
            PlayerPrefs.Save();

            // Clear selected image after successful signup
            ClearSelectedImage();
            yield return new WaitForSeconds(1f);
            SwitchToLoginScreen();
        }
    }

    private void HandleRegistrationError(System.AggregateException exception)
    {
        Debug.LogError("Signup failed: " + exception);
        errorText.text = "Signup Failed: " + GetFirebaseErrorMessage(exception);
    }

    private void ClearSelectedImage()
    {
        selectedImagePath = "";
        selectedImageTexture = null;
        if (profilePicturePreview != null)
        {
            profilePicturePreview.gameObject.SetActive(false);
        }
    }

    private IEnumerator FetchUserDataCoroutine(string uid)
    {
        var fetchTask = dbRef.Child("users").Child(uid).GetValueAsync();
        yield return new WaitUntil(() => fetchTask.IsCompleted);

        if (fetchTask.IsFaulted || fetchTask.IsCanceled)
        {
            Debug.LogError("Failed to fetch user data: " + fetchTask.Exception);
            errorText.text = "Failed to load user data.";
            yield break;
        }

        DataSnapshot snapshot = fetchTask.Result;
        if (!snapshot.Exists)
        {
            Debug.LogWarning("User data not found for UID: " + uid);
            errorText.text = "User data not found.";
            yield break;
        }

        // Parse and handle user data after yield
        HandleFetchedUserData(snapshot);
    }

    private void HandleFetchedUserData(DataSnapshot snapshot)
    {
        string json = snapshot.GetRawJsonValue();
        User fetchedUser = JsonUtility.FromJson<User>(json);

        if (fetchedUser == null)
        {
            Debug.LogError("Failed to parse user data");
            errorText.text = "Error loading user data.";
            return;
        }

        currentUserData = fetchedUser;
        UpdateUIWithUser(fetchedUser);
    }

    private void UpdateUIWithUser(User user)
    {
        currentUsernameText.text = "Username: " + user.Username;
        currentLevelText.text = "Level: " + user.Level;
        currentGoldText.text = "Gold: " + user.Gold;
        currentOwnedUnitsText.text = "Units: " + string.Join(", ", user.OwnedUnits ?? new List<string>());
        errorText.text = "Welcome back, " + user.Username + "!";

        if (!string.IsNullOrEmpty(user.ProfilePictureURL))
        {
            StartCoroutine(LoadProfilePictureFromURL(user.ProfilePictureURL));
        }

        // Update UserDataManager
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null!");
            return;
        }

        UpdateUserDataManager(user);
        StartCoroutine(LoadGameSceneAfterDelay());
    }

    private void UpdateUserDataManager(User user, bool isLoggedIn = true)
    {
        UserDataManager.Instance.UserName = user.Username;
        UserDataManager.Instance.Gold = user.Gold;
        UserDataManager.Instance.Level = user.Level;
        UserDataManager.Instance.OwnedUnits = user.OwnedUnits ?? new List<string> { "CraneRon", "KenDuong", "Ronny-V" };
        UserDataManager.Instance.OwnedUnitsLevels = user.OwnedUnitsLevels ?? new List<int> { 1, 1, 1 };
        UserDataManager.Instance.LoadOut = user.LoadOut ?? new List<string> { "CraneRon", "KenDuong", "Ronny-V" };
        UserDataManager.Instance.ProfilePictureURL = user.ProfilePictureURL ?? "";
        UserDataManager.Instance.Wins = user.Wins;
        UserDataManager.Instance.isLoggedIn = isLoggedIn;
    }

    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Pvp");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (signupButton != null) signupButton.interactable = interactable;
        if (switchToLoginButton != null) switchToLoginButton.interactable = interactable;
        if (switchToSignupButton != null) switchToSignupButton.interactable = interactable;
        if (selectProfilePictureButton != null) selectProfilePictureButton.interactable = interactable;
        if (switchToGuestLoginButton != null) switchToGuestLoginButton.interactable = interactable;
        if (guestLoginButton != null) guestLoginButton.interactable = interactable;
    }

    private string GetFirebaseErrorMessage(System.AggregateException exception)
    {
        if (exception.InnerException is FirebaseException firebaseEx)
        {
            switch (firebaseEx.ErrorCode)
            {
                case (int)AuthError.InvalidEmail:
                    return "Invalid email address.";
                case (int)AuthError.WrongPassword:
                    return "Wrong password.";
                case (int)AuthError.UserNotFound:
                    return "User not found.";
                case (int)AuthError.EmailAlreadyInUse:
                    return "Email already in use.";
                case (int)AuthError.WeakPassword:
                    return "Password is too weak.";
                default:
                    return "Authentication error.";
            }
        }
        return "Unknown error occurred.";
    }

    private void ClearUI()
    {
        currentUsernameText.text = "Username: N/A";
        currentLevelText.text = "Level: N/A";
        currentGoldText.text = "Gold: N/A";
        currentOwnedUnitsText.text = "Units: N/A";
        errorText.text = "Please Login or Sign Up.";
        uploadStatusText.text = "";

        if (currentProfilePicture != null)
        {
            currentProfilePicture.gameObject.SetActive(false);
        }

        if (profilePicturePreview != null)
        {
            profilePicturePreview.gameObject.SetActive(false);
        }

        selectedImagePath = "";
        selectedImageTexture = null;
    }

    private void SwitchToLoginScreen()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        guestLoginPanel.SetActive(false);
        errorText.text = "Please enter your login credentials.";
        ClearInputFields();
    }

    private void SwitchToSignupScreen()
    {
        signupPanel.SetActive(true);
        loginPanel.SetActive(false);
        guestLoginPanel.SetActive(false);
        errorText.text = "Please fill in your signup details.";

        ClearSelectedImage();
        uploadStatusText.text = "";
        ClearInputFields();
    }

    private void SwitchToGuestLoginScreen()
    {
        guestLoginPanel.SetActive(true);
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        errorText.text = "Press Start.";
        ClearInputFields();
    }

    private void ClearInputFields()
    {
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (signupUsernameInput != null) signupUsernameInput.text = "";
        if (signupEmailInput != null) signupEmailInput.text = "";
        if (signupAgeInput != null) signupAgeInput.text = "";
        if (signupPasswordInput != null) signupPasswordInput.text = "";
        if (signupConfirmPasswordInput != null) signupConfirmPasswordInput.text = "";
    }

    public void Logout()
    {
        Debug.Log("Logging out...");
        auth.SignOut();

        AutoLoginStatus.text = "No User Found";
        canAutoLogin = false;

        PlayerPrefs.DeleteKey("SavedEmail");
        PlayerPrefs.DeleteKey("SavedPassword");
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.Save();

        ClearUI();
        SwitchToLoginScreen();
    }
}