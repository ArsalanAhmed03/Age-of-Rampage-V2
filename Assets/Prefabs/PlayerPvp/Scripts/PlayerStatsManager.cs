using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using Unity.VisualScripting;

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
    [SerializeField] private string profilePictureURL = ""; // Add this field

    // References for displaying player level and coins using TextMeshPro
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI PlayerNameText;
    [SerializeField] private Image profilePicture;

    [SerializeField] Button LogoutButton;

    // Placeholder for future: units owned and their levels
    public List<string> OwnedUnits = new List<string>();
    public List<int> OwnedUnitsLevels = new List<int>();
    public List<string> LoadOut = new List<string>();

    private void Logout()
    {
        FirebaseUpdater.Instance.Logout();
    }

    // Properties for accessing and modifying stats
    public string PlayerName
    {
        get => playerName;
        set
        {
            playerName = value;
            UpdatePlayerNameUI(); // Update UI when name changes
        }
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

    public string ProfilePictureURL
    {
        get => profilePictureURL;
        set
        {
            profilePictureURL = value; // Fixed: was causing infinite recursion
            if (profilePicture != null && !string.IsNullOrEmpty(value))
            {
                StartCoroutine(LoadProfilePictureFromURL(value));
            }
        }
    }

    private IEnumerator LoadProfilePictureFromURL(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) yield break;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite profileSprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));

                if (profilePicture != null)
                {
                    profilePicture.sprite = profileSprite;
                    profilePicture.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Failed to load profile picture: " + request.error);
            }
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
        PlayerName = name; // Use the property instead of direct field assignment
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

    private void UpdatePlayerNameUI()
    {
        if (PlayerNameText != null)
            PlayerNameText.text = playerName;
    }

    void Start()
    {

        // Pull data from UserDataManager singleton
        if (UserDataManager.Instance != null)
        {
            // Use properties to ensure UI updates
            LogoutButton.onClick.AddListener(Logout);
            PlayerName = UserDataManager.Instance.UserName;
            PlayerLevel = UserDataManager.Instance.Level;
            CurrentCoins = UserDataManager.Instance.Gold;
            ProfilePictureURL = UserDataManager.Instance.ProfilePictureURL;

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

        // Force UI updates (backup in case properties didn't trigger)
        UpdateLevelUI();
        UpdateCoinsUI();
        UpdatePlayerNameUI();

        // Load profile picture if available
        if (!string.IsNullOrEmpty(ProfilePictureURL) && profilePicture != null)
        {
            StartCoroutine(LoadProfilePictureFromURL(ProfilePictureURL));
        }
    }

    public void RefreshAllUI()
    {
        UpdateLevelUI();
        UpdateCoinsUI();
        UpdatePlayerNameUI();

        if (!string.IsNullOrEmpty(ProfilePictureURL) && profilePicture != null)
        {
            StartCoroutine(LoadProfilePictureFromURL(ProfilePictureURL));
        }
    }
}