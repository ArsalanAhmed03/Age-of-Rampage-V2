using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;


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
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update wins: " + task.Exception?.GetBaseException());
            }
        });
    }


    public void UpdateUserFilms(int newFilms)
    {
        if (!IsUserValid()) return;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Films", newFilms }
        };

        dbRef.Child("users").Child(user.UserId).UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("User films updated successfully!");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to update films: " + task.Exception?.GetBaseException());
            }
        });
    }

    public void UpdateOwnedUnits(List<string> ownedUnits, List<int> unitLevels, List<int> ownedUnitsCounts, List<int> ownedUnitsCS, List<string> ownedUnitAbilities)
    {
        if (!IsUserValid()) return;

        // Validate input lists
        if (ownedUnits == null || unitLevels == null || ownedUnitsCounts == null || ownedUnitsCS == null || ownedUnitAbilities == null)
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
            { "OwnedUnitsCounts", ownedUnitsCounts },
            { "OwnedUnitsCS", ownedUnitsCS },
            { "OwnedUnitAbilities", ownedUnitAbilities }
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
            UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts, UserDataManager.Instance.OwnedUnitsCS, UserDataManager.Instance.OwnedUnitAbilities);
            return;
        }

        // Modify local copy
        UserDataManager.Instance.OwnedUnits.Add(unitName);
        UserDataManager.Instance.OwnedUnitsLevels.Add(level);
        UserDataManager.Instance.OwnedUnitsCounts.Add(1);
        UserDataManager.Instance.OwnedUnitsCS.Add(0); // Default CS value
        string randomAbility = "";

        var allAbilities = Enum.GetNames(typeof(AbilityTypes.Ability));
        randomAbility = allAbilities[UnityEngine.Random.Range(0, allAbilities.Length)];

        UserDataManager.Instance.OwnedUnitAbilities.Add(randomAbility);



        // Push to database
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts, UserDataManager.Instance.OwnedUnitsCS, UserDataManager.Instance.OwnedUnitAbilities);
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
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts, UserDataManager.Instance.OwnedUnitsCS, UserDataManager.Instance.OwnedUnitAbilities);
    }

    public bool IncrementUnitCS(string unitName)
    {
        if (!IsUserValid()) return false;

        int index = UserDataManager.Instance.GetUnitIndexByName(unitName);

        if (UserDataManager.Instance.OwnedUnitsCounts[index] < 2)
        {
            Debug.LogError("Unit count must be at least 2");
            return false;
        }

        // Check if UserDataManager exists
        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager.Instance is null");
            return false;
        }

        if (index < 0 || index >= UserDataManager.Instance.OwnedUnitsCounts.Count)
        {
            Debug.LogError($"Invalid unit index: {index}. Valid range: 0-{UserDataManager.Instance.OwnedUnitsCounts.Count - 1}");
            return false;
        }

        UserDataManager.Instance.OwnedUnitsCounts[index] = UserDataManager.Instance.OwnedUnitsCounts[index] - 1;
        UserDataManager.Instance.OwnedUnitsCS[index] = UserDataManager.Instance.OwnedUnitsCS[index] + 1;
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts, UserDataManager.Instance.OwnedUnitsCS, UserDataManager.Instance.OwnedUnitAbilities);

        foreach (Transform child in SelectionScreenManager.Instance.unitSelectionGrid)
        {
            DraggableUnit drag = child.GetComponent<DraggableUnit>();
            if (drag != null && drag.unitPrefab != null && drag.unitPrefab.name == unitName)
            {
                TMP_Text countText = child.GetComponentInChildren<TMP_Text>();
                int index1 = UserDataManager.Instance.OwnedUnits.FindIndex(u => u == unitName);
                if (countText != null && index1 >= 0 && index1 < UserDataManager.Instance.OwnedUnitsCounts.Count)
                {
                    countText.text = $"x{UserDataManager.Instance.OwnedUnitsCounts[index]}";
                }
                break;
            }
        }

        return true;
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

        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels, UserDataManager.Instance.OwnedUnitsCounts, UserDataManager.Instance.OwnedUnitsCS, UserDataManager.Instance.OwnedUnitAbilities);
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

        public List<int> loadoutCS = new List<int>();
        public string profilePictureUrl;

        public int wins;

        public OpponentData(string userId, string username, int level, List<string> loadout, List<int> loadoutLevels, List<int> loadoutCS, string profilePictureUrl, int wins, int gold)
        {
            this.userId = userId;
            this.username = username;
            this.level = level;
            this.loadout = loadout ?? new List<string>();
            this.loadoutLevels = loadoutLevels ?? new List<int>();
            this.loadoutCS = loadoutCS ?? new List<int>();
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

    // NOTE: You'll need to replace 'dbRef', 'user', and 'UserDataManager.Instance' 
    // with your actual Firebase/User variables and logic, as they are not
    // provided in the snippet.

    public void GetAllOpponents()
    {
        // if (!IsUserValid()) return;

        if (poolingStarted) return;

        poolingStarted = true;

        opponents.Clear();
        leaderBoardList.Clear(); // Clear the leader board list too

        Debug.Log("<color=red>Fetching all users from Firebase for opponents and leaderboard...</color>");

        // The logic for fetching the data remains the same as it fetches ALL users
        dbRef.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to fetch users: " + task.Exception?.GetBaseException());
                poolingStarted = false; // Important: reset pooling flag on failure
                return;
            }

            if (task.IsCanceled)
            {
                Debug.LogError("Fetch users task was canceled");
                poolingStarted = false; // Important: reset pooling flag on cancellation
                return;
            }

            DataSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogWarning("No users found in database");
                poolingStarted = false; // Important: reset pooling flag on no data
                return;
            }


            foreach (DataSnapshot userSnapshot in snapshot.Children)
            {

                string userId = userSnapshot.Key;

                // Read username and wins first, as they are needed for both lists
                string username = userSnapshot.Child("Username").Value?.ToString() ?? "Unknown";
                int wins = 0; // Default wins to 0 for safety
                if (userSnapshot.Child("Wins").Value != null && int.TryParse(userSnapshot.Child("Wins").Value.ToString(), out int parsedWins))
                {
                    wins = parsedWins;
                }

                string profilePictureUrl = userSnapshot.Child("ProfilePictureURL").Value?.ToString() ?? "";


                // --- Leaderboard Data Collection ---
                // For the leaderboard, we only need userId, username, and wins. 
                // We pass null/default values for the rest.
                OpponentData leaderboardEntry = new OpponentData(
                    userId: userId,
                    username: username,
                    level: 0, // Not needed for leaderboard, setting to 0
                    loadout: null, // Not needed, setting to null
                    loadoutLevels: null, // Not needed, setting to null
                    loadoutCS: null, // Not needed, setting to null
                    profilePictureUrl: profilePictureUrl, // Not needed, setting to null
                    wins: wins,
                    gold: 0 // Not needed, setting to 0
                );
                leaderBoardList.Add(leaderboardEntry);


                // --- Opponent Data Collection ---
                // Skip the current logged-in user for the 'opponents' list
                if (UserDataManager.Instance.isLoggedIn && userId == user.UserId)
                    continue;

                try
                {
                    // Continue parsing all the other opponent-specific data (level, gold, loadout, etc.)
                    // This section remains largely the same as your original code.

                    int level = 1;
                    int gold = 0;

                    if (userSnapshot.Child("Level").Value != null)
                    {
                        if (int.TryParse(userSnapshot.Child("Level").Value.ToString(), out int parsedLevel))
                        {
                            level = parsedLevel;
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
                    List<int> loadoutCS = new List<int>();
                    List<int> loadoutCounts = new List<int>();
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
                    DataSnapshot ownedUnitsCSnapshot = userSnapshot.Child("OwnedUnitsCS");
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

                    if (ownedUnitsCSnapshot.Exists)
                    {
                        foreach (DataSnapshot csItem in ownedUnitsCSnapshot.Children)
                        {
                            if (int.TryParse(csItem.Value?.ToString(), out int unitCS))
                            {
                                ownedUnitsCounts.Add(unitCS);
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
                            loadoutCS.Add(ownedUnitsCounts[unitIndex]);
                            loadoutCounts.Add(ownedUnitsCounts[unitIndex]);
                        }
                        else
                        {
                            loadoutLevels.Add(1); // Default level if unit not found
                            loadoutCS.Add(0); // Default CS if unit not found
                            loadoutCounts.Add(1); // Default count if unit not found
                        }
                    }

                    OpponentData opponent = new OpponentData(userId, username, level, loadout, loadoutLevels, loadoutCS, profilePictureUrl, wins, gold);

                    opponents.Add(opponent); // Add to opponents list

                    Debug.Log($"Found opponent: {username} (Level {level}) - Loadout: {string.Join(", ", loadout)} - Levels: {string.Join(", ", loadoutLevels)} - Gold: {gold}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing opponent data for {userId}: {e.Message}");
                }
            }

            poolingStarted = false;

            // --- Post-Processing and Invocation ---

            // Notify listeners that opponents data is ready
            OnOpponentsDataReady?.Invoke(opponents);

            // Sort the leaderboard list by wins (descending)
            leaderBoardList.Sort((a, b) => b.wins.CompareTo(a.wins));

            // Optional: Limit the leaderboard size (e.g., to top 10)
            // if (leaderBoardList.Count > 10)
            // {
            //     leaderBoardList = leaderBoardList.GetRange(0, 10);
            // }

            // Notify listeners that leaderboard data is ready
            OnLeaderBoardDataReady?.Invoke(leaderBoardList);

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