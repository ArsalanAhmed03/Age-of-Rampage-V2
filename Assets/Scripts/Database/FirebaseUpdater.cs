using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.AI;

public class FirebaseUpdater : MonoBehaviour
{
    public static FirebaseUpdater Instance { get; private set; }

    // Event to notify when opponents data is ready
    public System.Action<List<OpponentData>> OnOpponentsDataReady;
    public System.Action<List<OpponentData>> OnLeaderBoardDataReady;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private FirebaseUser user;

    private void Awake()
    {
        // Initialize Firebase - should check if Firebase is ready first
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }

    private void InitializeFirebase()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        // Listen for auth state changes instead of checking once
        auth.StateChanged += OnAuthStateChanged;
        OnAuthStateChanged(this, null);
    }

    private void OnAuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("User not logged in!");
        }
        else
        {
            Debug.Log("User logged in: " + user.UserId);
            GetAllOpponents(); // Fetch opponents when user is logged in
            StartOpponentPolling();

            // CancelInvoke(nameof(GetAllOpponents)); // prevent duplicates
            // InvokeRepeating(nameof(GetAllOpponents), 20f, 20f);
        }

    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }

    public void UpdateUserGold(int newGold)
    {
        if (!IsUserValid()) return;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Gold", newGold }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("User gold updated successfully!");
                // Update local data to keep in sync
                if (UserDataManager.Instance != null)
                    UserDataManager.Instance.Gold = newGold;
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update gold: " + task.Exception?.GetBaseException());
            }
        });
    }

    public void UpdateUserLevel(int newLevel)
    {
        if (!IsUserValid()) return;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Level", newLevel }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("User level updated successfully!");
                // Update local data to keep in sync
                if (UserDataManager.Instance != null)
                    UserDataManager.Instance.Level = newLevel;
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update level: " + task.Exception?.GetBaseException());
            }
        });
    }

    public void UpdateUserWins(int newWins)
    {
        if (!IsUserValid()) return;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Wins", newWins }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("User wins updated successfully!");
                // Update local data to keep in sync
                if (UserDataManager.Instance != null)
                    UserDataManager.Instance.Wins = newWins;
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update wins: " + task.Exception?.GetBaseException());
            }
        });
    }

    public void UpdateOwnedUnits(List<string> ownedUnits, List<int> unitLevels, List<int> ownedUnitsCounts)
    {
        if (!IsUserValid()) return;

        // Validate input lists
        if (ownedUnits == null || unitLevels == null || ownedUnitsCounts == null)
        {
            Debug.LogError("Owned units, unit levels, or unit counts list is null");
            return;
        }

        if (ownedUnits.Count != unitLevels.Count || ownedUnits.Count != ownedUnitsCounts.Count)
        {
            Debug.LogError("Owned units, unit levels, and unit counts lists must have the same length");
            return;
        }

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "OwnedUnits", ownedUnits },
            { "OwnedUnitsLevels", unitLevels },
            { "OwnedUnitsCounts", ownedUnitsCounts }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Owned units updated successfully!");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update owned units: " + task.Exception?.GetBaseException());
            }
        });
    }

    public void AddUnit(string unitName, int level = 1)
    {
        if (!IsUserValid()) return;

        // Validate input
        if (string.IsNullOrEmpty(unitName))
        {
            Debug.LogError("Unit name cannot be null or empty");
            return;
        }

        if (level < 1)
        {
            Debug.LogError("Unit level must be at least 1");
            return;
        }

        // Check if UserDataManager exists
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return;
        }

        // Check if unit already exists
        if (UserDataManager.Instance.OwnedUnits.Contains(unitName))
        {
            Debug.LogWarning($"Unit {unitName} already owned");
            UserDataManager.Instance.OwnedUnitsCounts[UserDataManager.Instance.GetUnitIndexByName(unitName)]++;
            UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
            return;
        }

        // Modify local copy
        UserDataManager.Instance.OwnedUnits.Add(unitName);
        UserDataManager.Instance.OwnedUnitsLevels.Add(level);
        UserDataManager.Instance.OwnedUnitsCounts.Add(1);

        // Push to database
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
    }

    public void UpdateUnitLevel(string unitName, int newLevel)
    {
        if (!IsUserValid()) return;

        int index = UserDataManager.Instance.GetUnitIndexByName(unitName);

        // Validate input
        if (newLevel < 1)
        {
            Debug.LogError("Unit level must be at least 1");
            return;
        }

        // Check if UserDataManager exists
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return;
        }

        if (index < 0 || index >= UserDataManager.Instance.OwnedUnitsLevels.Count)
        {
            Debug.LogError($"Invalid unit index: {index}. Valid range: 0-{UserDataManager.Instance.OwnedUnitsLevels.Count - 1}");
            return;
        }

        UserDataManager.Instance.OwnedUnitsLevels[index] = newLevel;
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
    }

    public void UpdateUnitCS(string unitName, int newCS)
    {
        if (!IsUserValid()) return;

        int index = UserDataManager.Instance.GetUnitIndexByName(unitName);

        // Validate input
        if (newCS < 1)
        {
            Debug.LogError("Unit CS must be at least 1");
            return;
        }

        // Check if UserDataManager exists
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return;
        }

        if (index < 0 || index >= UserDataManager.Instance.OwnedUnitsCounts.Count)
        {
            Debug.LogError($"Invalid unit index: {index}. Valid range: 0-{UserDataManager.Instance.OwnedUnitsCounts.Count - 1}");
            return;
        }

        UserDataManager.Instance.OwnedUnitsCounts[index] = newCS;
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
    }

    public void UpdateUnitCount(string unitName, int newCount)
    {
        if (!IsUserValid()) return;

        int index = UserDataManager.Instance.GetUnitIndexByName(unitName);

        // Validate input
        if (newCount < 1)
        {
            Debug.LogError("Unit count must be at least 1");
            return;
        }

        // Check if UserDataManager exists
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return;
        }

        if (index < 0 || index >= UserDataManager.Instance.OwnedUnitsCounts.Count)
        {
            Debug.LogError($"Invalid unit index: {index}. Valid range: 0-{UserDataManager.Instance.OwnedUnitsCounts.Count - 1}");
            return;
        }

        UserDataManager.Instance.OwnedUnitsCounts[index] = newCount;
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
    }

    // Helper method to check if user is valid
    private bool IsUserValid()
    {
        if (user == null)
        {
            // Debug.LogError("No user logged in. Cannot perform Firebase operation.");
            return false;
        }
        return true;
    }

    // Method to remove a unit
    public void RemoveUnit(string unitName)
    {
        if (!IsUserValid()) return;

        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return;
        }

        int index = UserDataManager.Instance.OwnedUnits.IndexOf(unitName);
        if (index == -1)
        {
            Debug.LogWarning($"Unit {unitName} not found in owned units");
            return;
        }

        UserDataManager.Instance.OwnedUnits.RemoveAt(index);
        UserDataManager.Instance.OwnedUnitsLevels.RemoveAt(index);
        UserDataManager.Instance.OwnedUnitsCounts.RemoveAt(index);

        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts);
    }

    public void UpdateLoadout(List<string> loadout)
    {
        if (!IsUserValid()) return;

        if (loadout == null)
        {
            Debug.LogError("Loadout list is null");
            return;
        }

        // Save the list as-is, including empty strings
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "LoadOut", loadout }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Loadout updated successfully!");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update loadout: " + task.Exception?.GetBaseException());
            }
        });
    }

    [System.Serializable]
    public class OpponentData
    {
        public string userId;
        public string username;
        public int level;
        public int gold;
        public List<string> loadout;
        public List<int> loadoutLevels;
        public string profilePictureUrl;

        public int wins;

        public OpponentData(string userId, string username, int level, List<string> loadout, List<int> loadoutLevels, string profilePictureUrl, int wins, int gold)
        {
            this.userId = userId;
            this.username = username;
            this.level = level;
            this.loadout = loadout ?? new List<string>();
            this.loadoutLevels = loadoutLevels ?? new List<int>();
            this.profilePictureUrl = profilePictureUrl;
            this.wins = wins;
            this.gold = gold;
        }
    }

    public List<OpponentData> opponents = new List<OpponentData>();

    public List<OpponentData> leaderBoardList = new List<OpponentData>();

    public void StartOpponentPolling()
    {
        CancelInvoke(nameof(GetAllOpponents));
        InvokeRepeating(nameof(GetAllOpponents), 0f, 20f);
    }

    public void StopOpponentPolling()
    {
        CancelInvoke(nameof(GetAllOpponents));
    }

    bool poolingStarted = false;

    public void GetAllOpponents()
    {
        // if (!IsUserValid()) return;

        if (poolingStarted) return;

        poolingStarted = true;

        opponents.Clear();
        leaderBoardList.Clear();

        Debug.Log("<color=red>Fetching all opponents from Firebase...</color>");

        dbRef.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to fetch users: " + task.Exception?.GetBaseException());
                return;
            }

            if (task.IsCanceled)
            {
                Debug.LogError("Fetch users task was canceled");
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogWarning("No users found in database");
                return;
            }


            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {

                string userId = userSnapshot.Key;

                // Skip the current logged-in user
                if (UserDataManager.Instance.isLoggedIn && userId == user.UserId)
                    continue;

                try
                {
                    // Get user data
                    string username = userSnapshot.Child("Username").Value?.ToString() ?? "Unknown";

                    int level = 1;
                    int wins = 1;
                    int gold = 0;

                    if (userSnapshot.Child("Level").Value != null)
                    {
                        if (int.TryParse(userSnapshot.Child("Level").Value.ToString(), out int parsedLevel))
                        {
                            level = parsedLevel;
                        }
                    }

                    if (userSnapshot.Child("Wins").Value != null)
                    {
                        if (int.TryParse(userSnapshot.Child("Wins").Value.ToString(), out int parsedWins))
                        {
                            wins = parsedWins;
                        }
                    }

                    if (userSnapshot.Child("Gold").Value != null)
                    {
                        if (int.TryParse(userSnapshot.Child("Gold").Value.ToString(), out int parsedGold))
                        {
                            gold = parsedGold;
                        }
                    }

                    // Get loadout
                    List<string> loadout = new List<string>();
                    DataSnapshot loadoutSnapshot = userSnapshot.Child("LoadOut");
                    if (loadoutSnapshot.Exists)
                    {
                        foreach (DataSnapshot loadoutItem in loadoutSnapshot.Children)
                        {
                            string unitName = loadoutItem.Value?.ToString() ?? "";
                            loadout.Add(unitName);
                        }
                    }

                    // Get loadout levels by matching with owned units
                    List<int> loadoutLevels = new List<int>();
                    List<string> ownedUnits = new List<string>();
                    List<int> ownedUnitsLevels = new List<int>();
                    List<int> ownedUnitsCounts = new List<int>();

                    // Get owned units
                    DataSnapshot ownedUnitsSnapshot = userSnapshot.Child("OwnedUnits");
                    if (ownedUnitsSnapshot.Exists)
                    {
                        foreach (DataSnapshot unitItem in ownedUnitsSnapshot.Children)
                        {
                            string unitName = unitItem.Value?.ToString() ?? "";
                            ownedUnits.Add(unitName);
                        }
                    }

                    // Get owned units levels
                    DataSnapshot ownedUnitsLevelsSnapshot = userSnapshot.Child("OwnedUnitsLevels");
                    if (ownedUnitsLevelsSnapshot.Exists)
                    {
                        foreach (DataSnapshot levelItem in ownedUnitsLevelsSnapshot.Children)
                        {
                            if (int.TryParse(levelItem.Value?.ToString(), out int unitLevel))
                            {
                                ownedUnitsLevels.Add(unitLevel);
                            }
                            else
                            {
                                ownedUnitsLevels.Add(1); // Default level
                            }
                        }
                    }

                    DataSnapshot ownedUnitsCountsSnapshot = userSnapshot.Child("OwnedUnitsCounts");
                    if (ownedUnitsCountsSnapshot.Exists)
                    {
                        foreach (DataSnapshot countItem in ownedUnitsCountsSnapshot.Children)
                        {
                            if (int.TryParse(countItem.Value?.ToString(), out int unitCount))
                            {
                                ownedUnitsCounts.Add(unitCount);
                            }
                            else
                            {
                                ownedUnitsCounts.Add(0);
                            }
                        }
                    }

                    // Match loadout units with their levels
                    for (int i = 0; i < loadout.Count; i++)
                    {
                        string loadoutUnit = loadout[i];
                        int unitIndex = ownedUnits.IndexOf(loadoutUnit);

                        if (unitIndex >= 0 && unitIndex < ownedUnitsLevels.Count)
                        {
                            loadoutLevels.Add(ownedUnitsLevels[unitIndex]);
                        }
                        else
                        {
                            loadoutLevels.Add(1); // Default level if unit not found
                        }
                    }

                    // Get profile picture URL
                    string profilePictureUrl = userSnapshot.Child("ProfilePictureURL").Value?.ToString() ?? "";

                    OpponentData opponent = new OpponentData(userId, username, level, loadout, loadoutLevels, profilePictureUrl, wins, gold);

                    opponents.Add(opponent);

                    // leaderBoardList.Add(opponent);

                    Debug.Log($"Found opponent: {username} (Level {level}) - Loadout: {string.Join(", ", loadout)} - Levels: {string.Join(", ", loadoutLevels)} - Gold: {gold}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing user data for {userId}: {e.Message}");
                }
            }

            poolingStarted = false;
            OnOpponentsDataReady?.Invoke(opponents);

            // Debug.Log($"Total opponents found: {leaderBoardList.Count}");

            // Notify listeners that opponents data is ready
            // OnOpponentsDataReady?.Invoke(opponents);

            // leaderBoardList.Sort((a, b) => b.level.CompareTo(a.level));

            // if (leaderBoardList.Count > 10)
            // {
            //     leaderBoardList = leaderBoardList.GetRange(0, 10);
            // }


            // OnLeaderBoardDataReady?.Invoke(leaderBoardList);

            // Data is now available in the 'opponents' list
            // You can process this data as needed
        });
    }

    public void Logout()
    {
        Debug.Log("Logging out...");
        auth.SignOut();

        PlayerPrefs.DeleteKey("SavedEmail");
        PlayerPrefs.DeleteKey("SavedPassword");
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.Save();

        UnityEngine.SceneManagement.SceneManager.LoadScene("SignUp");
    }
}