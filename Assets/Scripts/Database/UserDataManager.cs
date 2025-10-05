using System.Collections.Generic;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;

    public string UserName;

    public int gold;
    public int level;
    public int wins;
    public List<string> OwnedUnits = new List<string>();
    public List<int> OwnedUnitsLevels = new List<int>();
    public List<int> OwnedUnitsCS = new List<int>();

    public List<int> OwnedUnitsCounts = new List<int>();
    public List<string> LoadOut = new List<string>();
    public string ProfilePictureURL;
    public bool isLoggedIn = false;

    public int Gold
    {
        get => gold;
        set
        {
            gold = value;
            if (isLoggedIn)
            {
                FirebaseUpdater.Instance.UpdateUserGold(gold);
                PlayerStatsManager.Instance?.UpdateCoinsUI();
            }
        }
    }

    public int Level
    {
        get => level;
        set
        {
            level = value;
            if (isLoggedIn)
            {
                FirebaseUpdater.Instance.UpdateUserLevel(level);
                PlayerStatsManager.Instance?.UpdateLevelUI();
            }
        }
    }

    public int Wins
    {
        get => wins;
        set
        {
            wins = value;
            if (isLoggedIn)
            {
                FirebaseUpdater.Instance.UpdateUserWins(wins);
            }
        }
    }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
    }

    public int GetUnitIndexByName(string unitName)
    {
        return OwnedUnits.IndexOf(unitName);
    }

    public int GetUnitLevelByName(string unitName)
    {
        int index = GetUnitIndexByName(unitName);
        if (index != -1 && index < OwnedUnitsLevels.Count)
        {
            return OwnedUnitsLevels[index];
        }
        return 0; // Unit not found or level not available
    }


}
