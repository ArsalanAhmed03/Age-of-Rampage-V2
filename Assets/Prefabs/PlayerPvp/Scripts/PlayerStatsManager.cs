using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Player main stats
    [SerializeField] private string playerName = "Player1";
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int currentCoins = 1000;

    // References for displaying player level and coins using TextMeshPro
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI coinsText;

    // Placeholder for future: units owned and their levels
    public List<string> OwnedUnits = new List<string>();
    public List<int> OwnedUnitsLevels = new List<int>();
    public List<string> LoadOut = new List<string>();

    // Properties for accessing and modifying stats
    public string PlayerName
    {
        get => playerName;
        set => playerName = value;
    }

    public int PlayerLevel
    {
        get => playerLevel;
        set
        {
            playerLevel = value;
            FirebaseUpdater.Instance.UpdateUserLevel(playerLevel);
            UpdateLevelUI();
        }
    }

    public int CurrentCoins
    {
        get => currentCoins;
        set
        {
            currentCoins = value;
            FirebaseUpdater.Instance.UpdateUserGold(currentCoins);
            UpdateCoinsUI();
        }
    }

    public List<string> GetOwnedUnits()
    {
        return OwnedUnits;
    }

    public void AddUnit(string unitName, int level)
    {
        OwnedUnits.Add(unitName);
        OwnedUnitsLevels.Add(level);
    }

    public void SetUnitLevel(string unitName, int level)
    {
        for (int i = 0; i < OwnedUnits.Count; i++)
        {
            if (OwnedUnits[i] == unitName)
            {
                OwnedUnitsLevels[i] = level;
                break;
            }
        }
    }

    public void AddCoins(int amount)
    {
        CurrentCoins += amount;
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            CurrentCoins -= amount;
            return true;
        }
        return false;
    }

    public void LevelUp()
    {
        PlayerLevel++;
    }

    public void SetLevel(int level)
    {
        PlayerLevel = level;
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    private void UpdateLevelUI()
    {
        if (playerLevelText != null)
            playerLevelText.text = $"Level: {playerLevel}";
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {currentCoins}";
    }

    void Start()
    {
        // Pull data from UserDataManager singleton
        if (UserDataManager.Instance != null)
        {
            playerName = UserDataManager.Instance.UserName;
            playerLevel = UserDataManager.Instance.Level;
            currentCoins = UserDataManager.Instance.Gold;

            // Set owned units from UserDataManager
            OwnedUnits = UserDataManager.Instance.OwnedUnits;
            OwnedUnitsLevels = UserDataManager.Instance.OwnedUnitsLevels;
            LoadOut = UserDataManager.Instance.LoadOut;



            // Add owned units to UnlockedUnits in SelectionScreenManager if not already present
            if (SelectionScreenManager.Instance != null)
            {
                for (int i = 0; i < OwnedUnits.Count; i++)
                {
                    string unitName = OwnedUnits[i];
                    // Check if unit is already unlocked
                    bool alreadyUnlocked = SelectionScreenManager.Instance.UnlockedUnits.Exists(
                    prefab => prefab.name == unitName
                    );
                    if (!alreadyUnlocked)
                    {
                        // Find the prefab in AvailableUnits by name
                        GameObject prefab = SelectionScreenManager.Instance.availableUnits.Find(
                            go => go.name == unitName
                        );
                        if (prefab != null)
                        {
                            UnitStats unitStats = prefab.GetComponent<UnitStats>();
                            if (unitStats != null)
                            {
                                Debug.Log($"Unlocking unit '{unitName}' at level {OwnedUnitsLevels[i]}.");
                                unitStats.currentLevel = OwnedUnitsLevels[i];
                            }
                            else
                            {
                                Debug.LogWarning($"UnitStats component not found on prefab '{unitName}'.");
                            }
                            SelectionScreenManager.Instance.UnlockedUnits.Add(prefab);
                            SelectionScreenManager.Instance.availableUnits.Remove(prefab);
                        }
                        else
                        {
                            Debug.LogWarning($"Prefab for unit '{unitName}' not found in AvailableUnits.");
                        }
                    }

                }
                // List<string> loadoutForSlots = new List<string>();
                // for (int i = 0; i < 6; i++)
                // {
                //     if (i < LoadOut.Count)
                //         loadoutForSlots.Add(LoadOut[i]);
                //     else
                //         loadoutForSlots.Add("");
                // }
                // if (SelectionScreenManager.Instance != null)
                // {
                //     Debug.Log("Assigning units to slots: " + string.Join(", ", loadoutForSlots));
                //     SelectionScreenManager.Instance.AssignUnitsToSlots(loadoutForSlots);
                // }
            }
            else
            {
                Debug.LogWarning("SelectionScreenManager.Instance is null!");
            }
        }
        else
        {
            Debug.LogWarning("UserDataManager.Instance is null! Did you forget to log in?");
        }

        // Update UI
        UpdateLevelUI();
        UpdateCoinsUI();
    }


}
