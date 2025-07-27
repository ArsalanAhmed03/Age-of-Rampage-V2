using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseUpdater : MonoBehaviour
{
    public static FirebaseUpdater Instance { get; private set; }

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
            Debug.LogError("No user logged in. Cannot perform Firebase operation.");
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
}