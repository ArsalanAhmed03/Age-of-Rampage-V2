using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private TMPro.TextMeshProUGUI playerLevelText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;

    // Placeholder for future: units owned and their levels
    private Dictionary<string, int> unitLevels = new Dictionary<string, int>();

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
            UpdateLevelUI();
        }
    }

    public int CurrentCoins
    {
        get => currentCoins;
        set
        {
            currentCoins = value;
            UpdateCoinsUI();
        }
    }

    // Methods to modify coins
    public void AddCoins(int amount)
    {
        CurrentCoins += amount;
        UpdateCoinsUI();

    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            CurrentCoins -= amount;
            // UpdateCoinsUI();
            return true;
        }
        return false;
    }

    // Methods to modify player level
    public void LevelUp()
    {
        PlayerLevel++;
        // UpdateLevelUI();
    }

    public void SetLevel(int level)
    {
        PlayerLevel = level;
        // UpdateLevelUI();
    }

    // Methods to modify player name
    public void SetPlayerName(string name)
    {
        playerName = name;
    }

    // Placeholder for future unit management
    public Dictionary<string, int> GetUnitLevels()
    {
        return unitLevels;
    }

    public void AddUnit(string unitName, int level)
    {
        unitLevels[unitName] = level;
    }

    public void SetUnitLevel(string unitName, int level)
    {
        if (unitLevels.ContainsKey(unitName))
        {
            unitLevels[unitName] = level;
        }
    }

    // Update UI methods
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

    // In future, fetch stats from Firebase in Start()
    void Start()
    {
        UpdateLevelUI();
        UpdateCoinsUI();
        // TODO: Fetch stats from Firebase Realtime Database
    }
}
