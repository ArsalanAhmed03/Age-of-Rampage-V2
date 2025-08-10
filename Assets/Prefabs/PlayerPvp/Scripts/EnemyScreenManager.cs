using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyScreenManager : MonoBehaviour
{
    [Header("Screen References")]
    public GameObject ArenaStartScreen;
    public GameObject ArenaOpponentsScreen;
    public GameObject SelectedEnemyMenu;

    [Header("Enemy Selection UI")]
    public Transform EnemySlotParents;
    public Transform LeaderBoardSlotParents;

    [Header("Selected Enemy Details")]
    public TextMeshProUGUI EnemyName;
    public TextMeshProUGUI EnemyLevel;
    public Transform EnemyLoadParent;

    [Header("Unit Images Database")]
    public SelectionScreenManager selectionScreenManager; // Reference to get unit prefabs

    // Store the opponents data locally
    private List<FirebaseUpdater.OpponentData> currentOpponents = new List<FirebaseUpdater.OpponentData>();
    private List<FirebaseUpdater.OpponentData> currentLeaderBoard = new List<FirebaseUpdater.OpponentData>();
    private int selectedOpponentIndex = -1; // Track the currently selected opponent

    // Cache frequently accessed components
    private Button[] opponentSlotButtons;
    private Image[] opponentSlotImages;
    private TextMeshProUGUI[] opponentSlotTexts;
    private bool isOpponentDataLoaded = false;
    private bool isLeaderBoardDataLoaded = false;

    // Public properties for external access
    public List<FirebaseUpdater.OpponentData> CurrentOpponents => currentOpponents;
    public int SelectedOpponentIndex => selectedOpponentIndex;
    public bool HasSelectedOpponent => selectedOpponentIndex >= 0 && selectedOpponentIndex < currentOpponents.Count;

    // Method to update the selected opponent's level
    public void UpdateSelectedOpponentLevel(int levelChange)
    {
        Debug.Log($"UpdateSelectedOpponentLevel called with levelChange: {levelChange}");
        Debug.Log($"HasSelectedOpponent: {HasSelectedOpponent}, selectedOpponentIndex: {selectedOpponentIndex}, currentOpponents.Count: {currentOpponents.Count}");

        if (!HasSelectedOpponent)
        {
            Debug.LogWarning("No valid selected opponent to update level for");
            return;
        }

        FirebaseUpdater.OpponentData selectedOpponent = currentOpponents[selectedOpponentIndex];
        int oldLevel = selectedOpponent.level;
        int newLevel = Mathf.Max(1, selectedOpponent.level + levelChange);

        Debug.Log($"Updating {selectedOpponent.username} level from {oldLevel} to {newLevel}");

        // Update locally first
        selectedOpponent.level = newLevel;

        // Update UI if the selected enemy menu is visible
        if (SelectedEnemyMenu != null && SelectedEnemyMenu.activeInHierarchy)
        {
            if (EnemyLevel != null)
            {
                EnemyLevel.text = "Level " + newLevel.ToString();
            }
        }

        // Update in Firebase database
        UpdateOpponentLevelInDatabase(selectedOpponent.userId, newLevel);

        Debug.Log($"Successfully updated {selectedOpponent.username}'s level to {newLevel}");
    }

    private void UpdateOpponentLevelInDatabase(string userId, int newLevel)
    {
        if (FirebaseUpdater.Instance == null)
        {
            Debug.LogError("FirebaseUpdater.Instance is null, cannot update opponent level in database");
            return;
        }

        // Update the opponent's level in Firebase database
        StartCoroutine(UpdateUserLevelCoroutine(userId, newLevel));
    }

    private System.Collections.IEnumerator UpdateUserLevelCoroutine(string userId, int newLevel)
    {
        // Get Firebase database reference
        Firebase.Database.DatabaseReference dbRef = Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference;

        var updateTask = dbRef.Child("users").Child(userId).Child("Level").SetValueAsync(newLevel);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.IsCompletedSuccessfully)
        {
            Debug.Log($"Successfully updated user {userId} level to {newLevel} in database");
        }
        else if (updateTask.IsFaulted)
        {
            Debug.LogError($"Failed to update user {userId} level in database: {updateTask.Exception?.GetBaseException()}");
        }
    }

    private void Start()
    {
        // Subscribe to Firebase opponents data event
        if (FirebaseUpdater.Instance != null)
        {
            FirebaseUpdater.Instance.OnOpponentsDataReady += OnOpponentsDataReceived;
            FirebaseUpdater.Instance.OnLeaderBoardDataReady += OnLeaderBoardDataReceived;

        }

        // Set up initial state
        ShowArenaStartScreen();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (FirebaseUpdater.Instance != null)
        {
            FirebaseUpdater.Instance.OnOpponentsDataReady -= OnOpponentsDataReceived;
        }
    }

    public void ShowArenaStartScreen()
    {
        SetScreenStates(true, false, false);

        // Clear opponent data when going back to start screen
        ClearOpponentData();
    }

    public void ShowOpponentsScreen()
    {
        SetScreenStates(false, true, false);

        // Load opponents from Firebase only if not already loaded
        if (!isOpponentDataLoaded)
        {
            LoadOpponents();
        }
    }

    private void SetScreenStates(bool arenaStart, bool arenaOpponents, bool selectedEnemy)
    {
        if (ArenaStartScreen != null)
            ArenaStartScreen.SetActive(arenaStart);

        if (ArenaOpponentsScreen != null)
            ArenaOpponentsScreen.SetActive(arenaOpponents);

        if (SelectedEnemyMenu != null)
            SelectedEnemyMenu.SetActive(selectedEnemy);

        // Clear selected enemy UI data when hiding the menu, but keep the opponent reference
        if (!selectedEnemy && selectedOpponentIndex != -1)
        {
            ClearSelectedEnemyData(); // Only clear UI, not the selected opponent index
        }
    }

    public void LoadOpponents()
    {
        if (FirebaseUpdater.Instance != null)
        {
            // Simply call GetAllOpponents - the result will come through the event
            FirebaseUpdater.Instance.GetAllOpponents(6);
        }
        else
        {
            Debug.LogError("FirebaseUpdater.Instance is null");
        }
    }

    public void SetOpponentsData(List<FirebaseUpdater.OpponentData> opponents)
    {
        currentOpponents = opponents;
        isOpponentDataLoaded = true;
        CacheOpponentSlotComponents();
        UpdateOpponentSlots();
    }

    public void SetLeaderBoardData(List<FirebaseUpdater.OpponentData> leaderBoardList)
    {
        currentLeaderBoard = leaderBoardList;
        isLeaderBoardDataLoaded = true;
        CacheLeaderBoardSlotComponents();
        UpdateLeaderBoardSlots();
    }

    private void CacheOpponentSlotComponents()
    {
        if (EnemySlotParents == null) return;

        int slotCount = Mathf.Min(EnemySlotParents.childCount, 6);
        opponentSlotButtons = new Button[slotCount];
        opponentSlotImages = new Image[slotCount];
        opponentSlotTexts = new TextMeshProUGUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            Transform slotTransform = EnemySlotParents.GetChild(i);
            opponentSlotImages[i] = slotTransform.GetComponent<Image>();
            opponentSlotTexts[i] = slotTransform.GetComponentInChildren<TextMeshProUGUI>();

            // Get or add button component
            opponentSlotButtons[i] = slotTransform.GetComponent<Button>();
            if (opponentSlotButtons[i] == null)
            {
                opponentSlotButtons[i] = slotTransform.gameObject.AddComponent<Button>();
            }
        }
    }

    // Cache leaderboard slot components: profile image, name, level, wins
    private Button[] leaderBoardSlotButtons;
    private Image[] leaderBoardSlotImages;
    private TextMeshProUGUI[] leaderBoardSlotNameTexts;
    private TextMeshProUGUI[] leaderBoardSlotLevelTexts;
    private TextMeshProUGUI[] leaderBoardSlotWinsTexts;

    private void CacheLeaderBoardSlotComponents()
    {
        if (LeaderBoardSlotParents == null) return;

        int slotCount = Mathf.Min(LeaderBoardSlotParents.childCount, 10);
        leaderBoardSlotButtons = new Button[slotCount];
        leaderBoardSlotImages = new Image[slotCount];
        leaderBoardSlotNameTexts = new TextMeshProUGUI[slotCount];
        leaderBoardSlotLevelTexts = new TextMeshProUGUI[slotCount];
        leaderBoardSlotWinsTexts = new TextMeshProUGUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            Transform slotTransform = LeaderBoardSlotParents.GetChild(i);

            // Profile picture
            leaderBoardSlotImages[i] = slotTransform.GetComponent<Image>();

            // Find texts by name or order (assumes children: [Image], [NameText], [LevelText], [WinsText])
            TextMeshProUGUI[] texts = slotTransform.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                leaderBoardSlotNameTexts[i] = texts[0];
                leaderBoardSlotLevelTexts[i] = texts[1];
                leaderBoardSlotWinsTexts[i] = texts[2];
            }
            else
            {
                // Fallback: assign nulls
                leaderBoardSlotNameTexts[i] = null;
                leaderBoardSlotLevelTexts[i] = null;
                leaderBoardSlotWinsTexts[i] = null;
            }

            // Get or add button component
            leaderBoardSlotButtons[i] = slotTransform.GetComponent<Button>();
            if (leaderBoardSlotButtons[i] == null)
            {
                leaderBoardSlotButtons[i] = slotTransform.gameObject.AddComponent<Button>();
            }
        }
    }

    private void UpdateOpponentSlots()
    {
        if (EnemySlotParents == null || opponentSlotButtons == null)
        {
            Debug.LogError("EnemySlotParents or cached components are null");
            return;
        }

        // Update slots with cached components for better performance
        for (int i = 0; i < opponentSlotButtons.Length; i++)
        {
            if (i < currentOpponents.Count)
            {
                FirebaseUpdater.OpponentData opponent = currentOpponents[i];

                // Set username text
                if (opponentSlotTexts[i] != null)
                {
                    opponentSlotTexts[i].text = opponent.username;
                }

                // Set first unit image (only if opponent has units)
                // if (opponentSlotImages[i] != null && opponent.loadout.Count > 0 && !string.IsNullOrEmpty(opponent.loadout[0]))
                // {
                //     Sprite unitSprite = GetUnitSprite(opponent.loadout[0]);
                //     if (unitSprite != null)
                //     {
                //         opponentSlotImages[i].sprite = unitSprite;
                //     }
                // }

                if (opponentSlotImages[i] != null && !string.IsNullOrEmpty(opponent.profilePictureUrl))
                {
                    Debug.Log($"Loading profile picture for {opponent.username}");
                    StartCoroutine(LoadProfilePicture(opponent.profilePictureUrl, opponentSlotImages[i]));
                }

                // Enable the slot
                opponentSlotButtons[i].gameObject.SetActive(true);

                // Set up click event (remove previous listeners for safety)
                opponentSlotButtons[i].onClick.RemoveAllListeners();
                int index = i; // Capture for closure
                opponentSlotButtons[i].onClick.AddListener(() => ShowSelectedEnemy(index));
            }
            else
            {
                // Hide and clear empty slots
                opponentSlotButtons[i].gameObject.SetActive(false);
                if (opponentSlotImages[i] != null) opponentSlotImages[i].sprite = null;
                if (opponentSlotTexts[i] != null) opponentSlotTexts[i].text = "";
            }
        }
    }

    private void UpdateLeaderBoardSlots()
    {
        if (LeaderBoardSlotParents == null || leaderBoardSlotButtons == null)
        {
            Debug.LogError("LeaderBoardSlotParents or cached components are null");
            return;
        }

        // Update slots with cached components for better performance
        for (int i = 0; i < leaderBoardSlotButtons.Length; i++)
        {
            if (i < currentLeaderBoard.Count)
            {
                FirebaseUpdater.OpponentData leaderBoardEntry = currentLeaderBoard[i];

                // Set username text
                if (leaderBoardSlotNameTexts[i] != null)
                {
                    leaderBoardSlotNameTexts[i].text = leaderBoardEntry.username;
                }

                // Set level text
                if (leaderBoardSlotLevelTexts[i] != null)
                {
                    leaderBoardSlotLevelTexts[i].text = "Level " + leaderBoardEntry.level.ToString();
                }

                // Set wins text
                if (leaderBoardSlotWinsTexts[i] != null)
                {
                    leaderBoardSlotWinsTexts[i].text = leaderBoardEntry.wins.ToString();
                }

                // Enable the slot
                leaderBoardSlotButtons[i].gameObject.SetActive(true);

                // Set up click event (remove previous listeners for safety)
                leaderBoardSlotButtons[i].onClick.RemoveAllListeners();
                // int index = i; // Capture for closure
                // leaderBoardSlotButtons[i].onClick.AddListener(() => ShowSelectedLeaderBoardEntry(index));
            }
            else
            {
                // Hide and clear empty slots
                leaderBoardSlotButtons[i].gameObject.SetActive(false);
                if (leaderBoardSlotNameTexts[i] != null) leaderBoardSlotNameTexts[i].text = "";
                if (leaderBoardSlotLevelTexts[i] != null) leaderBoardSlotLevelTexts[i].text = "";
                if (leaderBoardSlotWinsTexts[i] != null) leaderBoardSlotWinsTexts[i].text = "";
            }
        }
    }

    private System.Collections.IEnumerator LoadProfilePicture(string imageUrl, Image targetImage)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogWarning("Failed to load profile picture: " + request.error);
            }
        }
    }
    public void ShowSelectedEnemy(int opponentIndex)
    {
        if (opponentIndex < 0 || opponentIndex >= currentOpponents.Count)
        {
            Debug.LogError($"Invalid opponent index: {opponentIndex}");
            return;
        }

        // Store the selected opponent index
        selectedOpponentIndex = opponentIndex;
        FirebaseUpdater.OpponentData selectedOpponent = currentOpponents[opponentIndex];

        // Show the selected enemy menu
        if (SelectedEnemyMenu != null)
            SelectedEnemyMenu.SetActive(true);

        // Populate enemy details
        PopulateSelectedEnemyData(selectedOpponent);
    }

    private void PopulateSelectedEnemyData(FirebaseUpdater.OpponentData opponent)
    {
        // Set enemy name
        if (EnemyName != null)
        {
            EnemyName.text = opponent.username;
        }

        // Set enemy level
        if (EnemyLevel != null)
        {
            EnemyLevel.text = "Level " + opponent.level.ToString();
        }

        // Set enemy loadout
        if (EnemyLoadParent != null)
        {
            UpdateEnemyLoadout(opponent);
        }
    }

    private void UpdateEnemyLoadout(FirebaseUpdater.OpponentData opponent)
    {
        if (EnemyLoadParent == null)
        {
            Debug.LogError("EnemyLoadParent is null");
            return;
        }

        // Update each loadout slot (should be 6 children)
        for (int i = 0; i < EnemyLoadParent.childCount && i < 6; i++)
        {
            Transform loadoutSlot = EnemyLoadParent.GetChild(i);
            Image unitImage = loadoutSlot.GetComponent<Image>();
            TextMeshProUGUI unitLevelText = loadoutSlot.GetComponentInChildren<TextMeshProUGUI>();

            if (i < opponent.loadout.Count && !string.IsNullOrEmpty(opponent.loadout[i]))
            {
                string unitName = opponent.loadout[i];
                int unitLevel = i < opponent.loadoutLevels.Count ? opponent.loadoutLevels[i] : 1;

                // Set unit image
                if (unitImage != null)
                {
                    Sprite unitSprite = GetUnitSprite(unitName);
                    if (unitSprite != null)
                    {
                        unitImage.sprite = unitSprite;
                        unitImage.color = Color.white; // Make sure it's visible
                    }
                }

                // Set unit level text
                if (unitLevelText != null)
                {
                    unitLevelText.text = unitLevel.ToString();
                }

                // Enable the slot
                loadoutSlot.gameObject.SetActive(true);
            }
            else
            {
                // Hide empty slots or set to default
                if (unitImage != null)
                {
                    unitImage.sprite = null;
                    unitImage.color = new Color(1, 1, 1, 0.3f); // Make it semi-transparent
                }

                if (unitLevelText != null)
                {
                    unitLevelText.text = "";
                }

                loadoutSlot.gameObject.SetActive(true); // Keep active but empty
            }
        }
    }

    private Sprite GetUnitSprite(string unitName)
    {
        if (selectionScreenManager != null)
        {
            // First check in availableUnits
            foreach (GameObject unitPrefab in selectionScreenManager.availableUnits)
            {
                if (unitPrefab != null && unitPrefab.name == unitName)
                {
                    Image unitImage = unitPrefab.GetComponent<Image>();
                    if (unitImage != null && unitImage.sprite != null)
                    {
                        return unitImage.sprite;
                    }

                    // If no Image component, try SpriteRenderer
                    SpriteRenderer spriteRenderer = unitPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        return spriteRenderer.sprite;
                    }
                }
            }

            // Then check in UnlockedUnits
            foreach (GameObject unitPrefab in selectionScreenManager.UnlockedUnits)
            {
                if (unitPrefab != null && unitPrefab.name == unitName)
                {
                    Image unitImage = unitPrefab.GetComponent<Image>();
                    if (unitImage != null && unitImage.sprite != null)
                    {
                        return unitImage.sprite;
                    }

                    // If no Image component, try SpriteRenderer
                    SpriteRenderer spriteRenderer = unitPrefab.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        return spriteRenderer.sprite;
                    }
                }
            }

            Debug.LogWarning($"Unit sprite not found for: {unitName}");
            return null;
        }
        else
        {
            Debug.LogWarning("SelectionScreenManager is not assigned");
            return null;
        }
    }

    public void CloseSelectedEnemyMenu()
    {
        if (SelectedEnemyMenu != null)
            SelectedEnemyMenu.SetActive(false);

        // Clear UI data when menu is closed, but keep the opponent reference for battle
        ClearSelectedEnemyData();
    }

    public void HideAndClearSelectedEnemyMenu()
    {
        CloseSelectedEnemyMenu(); // Reuse the close method for consistency
    }

    private void ClearSelectedEnemyData()
    {
        // Clear enemy name
        if (EnemyName != null)
        {
            EnemyName.text = "";
        }

        // Clear enemy level
        if (EnemyLevel != null)
        {
            EnemyLevel.text = "";
        }

        // Clear enemy loadout efficiently
        ClearEnemyLoadoutDisplay();
    }

    // Public method to manually clear the selected opponent when needed
    public void ClearSelectedOpponent()
    {
        // Reset selected opponent index
        selectedOpponentIndex = -1;

        // Also clear the UI data
        ClearSelectedEnemyData();
    }

    private void ClearEnemyLoadoutDisplay()
    {
        if (EnemyLoadParent == null) return;

        // Clear each loadout slot efficiently
        for (int i = 0; i < EnemyLoadParent.childCount; i++)
        {
            Transform loadoutSlot = EnemyLoadParent.GetChild(i);
            Image unitImage = loadoutSlot.GetComponent<Image>();
            TextMeshProUGUI unitLevelText = loadoutSlot.GetComponentInChildren<TextMeshProUGUI>();

            // Clear unit image
            if (unitImage != null)
            {
                unitImage.sprite = null;
                unitImage.color = new Color(1, 1, 1, 0.3f); // Semi-transparent
            }

            // Clear unit level text
            if (unitLevelText != null)
            {
                unitLevelText.text = "";
            }
        }
    }

    private void ClearOpponentData()
    {
        currentOpponents.Clear();
        isOpponentDataLoaded = false;
        selectedOpponentIndex = -1; // Clear selected opponent when going back to start

        // Clear cached components
        opponentSlotButtons = null;
        opponentSlotImages = null;
        opponentSlotTexts = null;

        // Clear opponent slots display
        if (EnemySlotParents != null)
        {
            for (int i = 0; i < EnemySlotParents.childCount; i++)
            {
                Transform slot = EnemySlotParents.GetChild(i);
                Image slotImage = slot.GetComponent<Image>();
                TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();

                if (slotImage != null) slotImage.sprite = null;
                if (slotText != null) slotText.text = "";
                slot.gameObject.SetActive(false);
            }
        }
    }

    public void BackToArenaStart()
    {
        ShowArenaStartScreen();
    }

    public void SetEnemyLoadoutForBattle()
    {
        if (selectedOpponentIndex < 0 || selectedOpponentIndex >= currentOpponents.Count)
        {
            Debug.LogError($"No valid opponent selected. Selected index: {selectedOpponentIndex}");
            return;
        }

        FirebaseUpdater.OpponentData selectedOpponent = currentOpponents[selectedOpponentIndex];

        // Get reference to BattleSystem
        BattleSystem battleSystem = FindFirstObjectByType<BattleSystem>();
        if (battleSystem == null)
        {
            Debug.LogError("BattleSystem not found in scene");
            return;
        }

        // Clear existing enemy prefab lists
        battleSystem.enemyFrontPrefabs.Clear();
        battleSystem.enemyBackPrefabs.Clear();

        // Set up front line (first 3 units)
        for (int i = 0; i < 3; i++)
        {
            GameObject unitPrefab = null;

            if (i < selectedOpponent.loadout.Count && !string.IsNullOrEmpty(selectedOpponent.loadout[i]))
            {
                string unitName = selectedOpponent.loadout[i];
                unitPrefab = FindUnitPrefab(unitName);

                if (unitPrefab != null)
                {
                    // Set the unit level if we found the prefab
                    UnitStats unitStats = unitPrefab.GetComponent<UnitStats>();
                    if (unitStats != null && i < selectedOpponent.loadoutLevels.Count)
                    {
                        unitStats.currentLevel = selectedOpponent.loadoutLevels[i];
                    }
                }
            }

            battleSystem.enemyFrontPrefabs.Add(unitPrefab); // Add null if empty slot
        }

        // Set up back line (next 3 units)
        for (int i = 3; i < 6; i++)
        {
            GameObject unitPrefab = null;

            if (i < selectedOpponent.loadout.Count && !string.IsNullOrEmpty(selectedOpponent.loadout[i]))
            {
                string unitName = selectedOpponent.loadout[i];
                unitPrefab = FindUnitPrefab(unitName);

                if (unitPrefab != null)
                {
                    // Set the unit level if we found the prefab
                    UnitStats unitStats = unitPrefab.GetComponent<UnitStats>();
                    if (unitStats != null && i < selectedOpponent.loadoutLevels.Count)
                    {
                        unitStats.currentLevel = selectedOpponent.loadoutLevels[i];
                    }
                }
            }

            battleSystem.enemyBackPrefabs.Add(unitPrefab); // Add null if empty slot
        }

        // Don't clear selected opponent here - keep it for level updates after battle
        // BackToArenaStart(); // Removed this to preserve selected opponent for level updates

        SelectionScreenManager.Instance.OnStartBattleClicked();
        Debug.Log($"Set enemy loadout for {selectedOpponent.username}: Front={battleSystem.enemyFrontPrefabs.Count}, Back={battleSystem.enemyBackPrefabs.Count}");
        Debug.Log($"Selected opponent preserved for level updates: {selectedOpponent.username} (Index: {selectedOpponentIndex})");
    }

    private GameObject FindUnitPrefab(string unitName)
    {
        if (selectionScreenManager == null) return null;

        // Search in availableUnits
        foreach (GameObject unitPrefab in selectionScreenManager.availableUnits)
        {
            if (unitPrefab != null && unitPrefab.name == unitName)
            {
                return unitPrefab;
            }
        }

        // Search in UnlockedUnits
        foreach (GameObject unitPrefab in selectionScreenManager.UnlockedUnits)
        {
            if (unitPrefab != null && unitPrefab.name == unitName)
            {
                return unitPrefab;
            }
        }

        Debug.LogWarning($"Unit prefab not found for: {unitName}");
        return null;
    }

    // Public method to be called from FirebaseUpdater when opponents data is ready
    public void OnOpponentsDataReceived(List<FirebaseUpdater.OpponentData> opponents)
    {
        SetOpponentsData(opponents);
    }

    public void OnLeaderBoardDataReceived(List<FirebaseUpdater.OpponentData> leaderBoardList)
    {
        SetLeaderBoardData(leaderBoardList);
    }
}
