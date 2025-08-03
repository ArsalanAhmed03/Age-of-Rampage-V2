using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseUpdater : MonoBehaviour
{
    public static FirebaseUpdater Instance { get; private set; }

    // Event to notify when opponents data is ready
    public System.Action<List<OpponentData>> OnOpponentsDataReady;

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

    public void UpdateOwnedUnits(List<string> ownedUnits, List<int> unitLevels)
    {
        if (!IsUserValid()) return;

        // Validate input lists
        if (ownedUnits == null || unitLevels == null)
        {
            Debug.LogError("Owned units or unit levels list is null");
            return;
        }

        if (ownedUnits.Count != unitLevels.Count)
        {
            Debug.LogError("Owned units and unit levels lists must have the same length");
            return;
        }

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "OwnedUnits", ownedUnits },
            { "OwnedUnitsLevels", unitLevels }
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
            return;
        }

        // Modify local copy
        UserDataManager.Instance.OwnedUnits.Add(unitName);
        UserDataManager.Instance.OwnedUnitsLevels.Add(level);

        // Push to database
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels);
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
        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels);
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

        UpdateOwnedUnits(UserDataManager.Instance.OwnedUnits, UserDataManager.Instance.OwnedUnitsLevels);
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
        public List<string> loadout;
        public List<int> loadoutLevels;
        public string profilePictureUrl;

        public OpponentData(string userId, string username, int level, List<string> loadout, List<int> loadoutLevels, string profilePictureUrl)
        {
            this.userId = userId;
            this.username = username;
            this.level = level;
            this.loadout = loadout ?? new List<string>();
            this.loadoutLevels = loadoutLevels ?? new List<int>();
            this.profilePictureUrl = profilePictureUrl;
        }
    }

    public List<OpponentData> opponents = new List<OpponentData>();

    public void GetAllOpponents(int maxResults = 6)
    {
        if (!IsUserValid()) return;

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
                // Check if we've reached the maximum number of results
                if (opponents.Count >= maxResults)
                    break;

                string userId = userSnapshot.Key;

                // Skip the current logged-in user
                if (UserDataManager.Instance.isLoggedIn && userId == user.UserId)
                    continue;

                try
                {
                    // Get user data
                    string username = userSnapshot.Child("Username").Value?.ToString() ?? "Unknown";
                    int level = 1;

                    if (userSnapshot.Child("Level").Value != null)
                    {
                        if (int.TryParse(userSnapshot.Child("Level").Value.ToString(), out int parsedLevel))
                        {
                            level = parsedLevel;
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

                    OpponentData opponent = new OpponentData(userId, username, level, loadout, loadoutLevels, profilePictureUrl);
                    opponents.Add(opponent);

                    Debug.Log($"Found opponent: {username} (Level {level}) - Loadout: {string.Join(", ", loadout)} - Levels: {string.Join(", ", loadoutLevels)}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing user data for {userId}: {e.Message}");
                }
            }

            Debug.Log($"Total opponents found: {opponents.Count} (limited to {maxResults} max results)");

            // Notify listeners that opponents data is ready
            OnOpponentsDataReady?.Invoke(opponents);

            // Data is now available in the 'opponents' list
            // You can process this data as needed
        });
    }
}