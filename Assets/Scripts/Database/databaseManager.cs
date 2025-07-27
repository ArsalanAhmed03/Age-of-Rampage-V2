using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;

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
        public List<string> OwnedUnits;
        public List<int> OwnedUnitsLevels;

        public User() { }

        public User(string username, string storedPassword, string email, int age, int gold, int level, List<string> ownedUnits, List<int> ownedUnitsLevels)
        {
            Username = username;
            StoredPassword = storedPassword;
            Email = email;
            Age = age;
            Gold = gold;
            Level = level;
            OwnedUnits = ownedUnits;
            OwnedUnitsLevels = ownedUnitsLevels;
        }
    }


    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private FirebaseUser user;
    private DependencyStatus dependencyStatus;

    [Header("Screen Panels")]
    public GameObject loginPanel;
    public GameObject signupPanel;

    [Header("Switch Buttons")]
    public Button switchToLoginButton;
    public Button switchToSignupButton;

    [Header("Login UI")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;

    [Header("Signup UI")]
    public TMP_InputField signupUsernameInput;
    public TMP_InputField signupEmailInput;
    public TMP_InputField signupAgeInput;
    public TMP_InputField signupPasswordInput;
    public TMP_InputField signupConfirmPasswordInput;
    public Button signupButton;

    [Header("User Data Display")]
    public TextMeshProUGUI currentUsernameText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentOwnedUnitsText;

    [Header("Messages")]
    public TextMeshProUGUI errorText;

    private void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Firebase dependencies not resolved: {dependencyStatus}");
            }
        });
    }

    private void Start()
    {
        loginButton.onClick.AddListener(Login);
        signupButton.onClick.AddListener(Register);
        switchToLoginButton.onClick.AddListener(SwitchToLoginScreen);
        switchToSignupButton.onClick.AddListener(SwitchToSignupScreen);

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

    public void Login()
    {
        StartCoroutine(LoginAsync(loginEmailInput.text, loginPasswordInput.text));
    }

    private System.Collections.IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError("Login failed: " + loginTask.Exception);
            errorText.text = "Login Failed!";
        }
        else
        {
            user = loginTask.Result.User;
            Debug.Log($"Login successful: {user.Email}, UID: {user.UserId}");

            FetchUserData(user.UserId);
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(
            signupUsernameInput.text,
            signupEmailInput.text,
            signupPasswordInput.text,
            signupConfirmPasswordInput.text,
            signupAgeInput.text));
    }

    private System.Collections.IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword, string age)
    {
        if (name == "" || email == "" || age == "" || password != confirmPassword)
        {
            errorText.text = "Invalid input fields.";
            yield break;
        }

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError("Signup failed: " + registerTask.Exception);
            errorText.text = "Signup Failed!";
        }
        else
        {
            user = registerTask.Result.User;
            Debug.Log($"Registered successfully. UID: {user.UserId}");

            // Update display name
            var profile = new UserProfile { DisplayName = name };
            var profileTask = user.UpdateUserProfileAsync(profile);
            yield return new WaitUntil(() => profileTask.IsCompleted);

            if (profileTask.Exception != null)
            {
                Debug.LogError("Profile update failed: " + profileTask.Exception);
                errorText.text = "Profile Update Failed!";
                yield break;
            }

            // Set user data in DB using UID
            List<string> defaultUnits = new List<string> { "CraneRon", "KenDuong", "Ronny-V", "TrevorAdam" };
            List<int> defaultUnitsLevels = new List<int> { 1, 1, 1, 1 };

            User newUser = new User(name, password, email, int.Parse(age), 100, 1, defaultUnits, defaultUnitsLevels);

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
                SwitchToLoginScreen();
            }
        }
    }

    private void FetchUserData(string uid)
    {
        dbRef.Child("users").Child(uid).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to fetch user data: " + task.Exception);
                errorText.text = "Failed to load user data.";
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogWarning("User data not found for UID: " + uid);
                errorText.text = "User data not found.";
                return;
            }

            string json = snapshot.GetRawJsonValue();
            User fetchedUser = JsonUtility.FromJson<User>(json);
            UpdateUIWithUser(fetchedUser);
        });
    }

    private void UpdateUIWithUser(User user)
    {
        currentUsernameText.text = "Username: " + user.Username;
        currentLevelText.text = "Level: " + user.Level;
        currentGoldText.text = "Gold: " + user.Gold;
        currentOwnedUnitsText.text = "Units: " + string.Join(", ", user.OwnedUnits);
        errorText.text = "Welcome back, " + user.Username + "!";
        Debug.Log($"User data loaded: {user.Username}, Level: {user.Level}, Gold: {user.Gold}");
        UserDataManager.Instance.UserName = user.Username;
        UserDataManager.Instance.Gold = user.Gold;
        UserDataManager.Instance.Level = user.Level;
        UserDataManager.Instance.OwnedUnits = user.OwnedUnits;
        UserDataManager.Instance.OwnedUnitsLevels = user.OwnedUnitsLevels;

        UnityEngine.SceneManagement.SceneManager.LoadScene("Pvp");
    }

    private void ClearUI()
    {
        currentUsernameText.text = "Username: N/A";
        currentLevelText.text = "Level: N/A";
        currentGoldText.text = "Gold: N/A";
        currentOwnedUnitsText.text = "Units: N/A";
        errorText.text = "Please Login or Sign Up.";
    }

    private void SwitchToLoginScreen()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        errorText.text = "Please enter your login credentials.";
    }

    private void SwitchToSignupScreen()
    {
        signupPanel.SetActive(true);
        loginPanel.SetActive(false);
        errorText.text = "Please fill in your signup details.";
    }
}
