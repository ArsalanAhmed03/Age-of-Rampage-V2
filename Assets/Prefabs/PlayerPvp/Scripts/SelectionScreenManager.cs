using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionScreenManager : MonoBehaviour
{
    public static SelectionScreenManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [Header("UI Panels")]
    public GameObject loadoutScreen;
    public GameObject battleScreen;
    public GameObject tournamentBattleScreen;


    [Header("Unit Selection")]
    public Transform unitSelectionGrid;
    public Transform shopGrid;
    public GameObject unitButtonPrefab;
    public GameObject unitShopButtonPrefab;

    public List<GameObject> availableUnits;
    public List<GameObject> UnlockedUnits;

    [Header("Loadout Display")]
    public TMP_Text frontlineCountText;
    public TMP_Text backlineCountText;

    [Header("Upgrade Screen")]
    public UpgradeScreenUI UpgradeScreenUI;

    [Header("Buy Screen")]
    public UpgradeScreenUI BuyScreenUI;

    [SerializeField] private Button loadLoadoutButton;
    [SerializeField] private Button saveLoadoutButton;


    private void Start()
    {
        CreateUnitButtons();
        LoadoutData.selectedFrontline.Clear();
        LoadoutData.selectedBackline.Clear();
        saveLoadoutButton.onClick.AddListener(() =>
        {
            OnSaveLoadoutBtnClicked();
        });

        loadLoadoutButton.onClick.AddListener(() =>
        {
            OnLoadUpdateBtnClicked();
        });

        // PopulateUnitButtons();
        UpdateCountText();

        for (int i = 0; i < 6; i++)
        {
            GameObject slotObj = loadoutSlotsParent.GetChild(i).gameObject;
            DropSlot drop = slotObj.GetComponent<DropSlot>();
            if (drop == null)
                drop = slotObj.AddComponent<DropSlot>();
            drop.slotIndex = i;
        }
    }

    public void OnLoadUpdateBtnClicked()
    {
        List<string> loadoutForSlots = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            if (i < PlayerStatsManager.Instance.LoadOut.Count)
                loadoutForSlots.Add(PlayerStatsManager.Instance.LoadOut[i]);
            else
                loadoutForSlots.Add("");
        }
        for (int i = 0; i < loadoutSlotsParent.childCount; i++)
        {
            Transform slot = loadoutSlotsParent.GetChild(i);
            DropSlot dropSlot = slot.GetComponent<DropSlot>();
            if (dropSlot != null)
            {
                dropSlot.RemoveUnit();
            }
        }
        Debug.Log("////////////////");
        AssignUnitsToSlots(loadoutForSlots);
    }

    public void OnSaveLoadoutBtnClicked()
    {
        if (FirebaseUpdater.Instance != null)
        {
            List<string> unitNames = new List<string>();
            for (int i = 0; i < LoadoutData.selectedUnits.Length; i++)
            {
                if (LoadoutData.selectedUnits[i] != null)
                {
                    unitNames.Add(LoadoutData.selectedUnits[i].name);
                }
                else
                {
                    unitNames.Add(string.Empty);
                }
            }
            FirebaseUpdater.Instance.UpdateLoadout(unitNames);

            // Also update PlayerStatsManager.LoadOut
            if (PlayerStatsManager.Instance != null)
            {
                PlayerStatsManager.Instance.LoadOut = new List<string>(unitNames);
            }
        }
        else
        {
            Debug.LogWarning("FirebaseUpdater.Instance is null!");
        }
    }

    [Header("Loadout Slots")]
    public Transform loadoutSlotsParent;
    void CreateUnitButtons()
    {

        foreach (GameObject unit in availableUnits)
        {
            GameObject btn = Instantiate(unitShopButtonPrefab, shopGrid);
            Sprite unitSprite = unit.GetComponent<SpriteRenderer>()?.sprite;
            UnitStats unitStats = unit.GetComponent<UnitStats>();


            // List<Transform> btnImageChildren = new List<Transform>();
            if (btn != null)
            {
                foreach (Transform child in btn.transform)
                {
                    if (child.CompareTag("ShopItemSprite"))
                    {
                        if (child != null && unitSprite != null)
                        {
                            child.GetComponent<Image>().sprite = unitSprite;
                        }
                    }
                    else if (child.CompareTag("NameText"))
                    {
                        TMP_Text nameText = child.GetComponent<TMP_Text>();
                        if (nameText != null)
                        {
                            nameText.text = unit.name;
                        }
                    }
                    else if (child.CompareTag("BuyButton"))
                    {
                        Button btnComponent = child.GetComponent<Button>();
                        if (btnComponent != null)
                        {
                            GameObject unitCopy = unit; // Capture for closure
                            btnComponent.onClick.AddListener(() => BuyUnit(unitCopy.name));
                        }

                        TMP_Text priceText = null;
                        if (child.childCount > 0)
                        {
                            priceText = child.GetChild(0).GetComponent<TMP_Text>();
                        }
                        if (priceText != null)
                        {
                            if (unitStats != null)
                            {
                                priceText.text = unitStats.price.ToString();
                            }
                        }
                    }
                }
            }

            UnitClickHandler clickHandler = btn.GetComponent<UnitClickHandler>();
            if (clickHandler != null)
            {
                clickHandler.unitStats = unitStats;
                clickHandler.unitSprite = unitSprite;
                clickHandler.UpgradeScreenUI = BuyScreenUI;
            }
        }

        foreach (GameObject unit in UnlockedUnits)
        {
            GameObject btn = Instantiate(unitButtonPrefab, unitSelectionGrid);

            Image btnImage = btn.GetComponent<Image>();
            Sprite unitSprite = unit.GetComponent<SpriteRenderer>()?.sprite;
            if (btnImage != null && unitSprite != null)
            {
                btnImage.sprite = unitSprite;
            }

            DraggableUnit drag = btn.GetComponent<DraggableUnit>();
            if (drag != null)
            {
                drag.SetPrefab(unit);
            }

            UnitClickHandler clickHandler = btn.GetComponent<UnitClickHandler>();
            if (clickHandler != null)
            {
                clickHandler.unitStats = unit.GetComponent<UnitStats>();
                clickHandler.unitSprite = unitSprite;
                clickHandler.UpgradeScreenUI = UpgradeScreenUI;
            }
        }
    }

    public void AssignUnitsToSlots(List<string> unitNames)
    {
        Debug.Log($"Assigning {unitNames.Count} units to slots: {string.Join(", ", unitNames)}");
        for (int i = 0; i < Mathf.Min(unitNames.Count, loadoutSlotsParent.childCount); i++)
        {
            string unitName = unitNames[i];
            Transform slotTransform = loadoutSlotsParent.GetChild(i);
            DropSlot dropSlot = slotTransform.GetComponent<DropSlot>();
            if (dropSlot == null) continue;

            if (!string.IsNullOrEmpty(unitName))
            {
                GameObject unitPrefab = UnlockedUnits.Find(u => u.name == unitName);
                if (unitPrefab != null)
                {
                    Debug.Log($"Assigning unit '{unitName}' to slot {i}");
                    dropSlot.AssignUnitToSlot(unitPrefab);
                    // Remove the unit's button from the unitSelectionGrid
                    foreach (Transform child in unitSelectionGrid)
                    {
                        Debug.Log($"Checking child: {child.name}");
                        DraggableUnit drag = child.GetComponent<DraggableUnit>();
                        if (drag != null && drag.unitPrefab.name == unitName)
                        {
                            Destroy(child.gameObject);
                            break;
                        }
                    }

                }
                else
                {
                    // dropSlot.RemoveUnit();
                }
            }
            else
            {
                // dropSlot.RemoveUnit();
            }
        }
    }
    public void BuyUnit(string name)
    {
        GameObject unitToBuy = availableUnits.Find(u => u.name == name);

        UnitStats unitStats = unitToBuy?.GetComponent<UnitStats>();
        if (unitStats == null) return;

        if (!PlayerStatsManager.Instance.SpendCoins(unitStats.price))
        {
            Debug.LogWarning("Not enough coins to buy this unit.");
            return;
        }

        Debug.Log($"Buying unit: {name}");
        FirebaseUpdater.Instance.AddUnit(unitToBuy.name);

        if (unitToBuy != null)
        {
            availableUnits.Remove(unitToBuy);
            UnlockedUnits.Add(unitToBuy);

            // Remove the bought unit's button from the shop grid
            foreach (Transform child in shopGrid)
            {
                UnitClickHandler clickHandler = child.GetComponent<UnitClickHandler>();
                if (clickHandler != null && clickHandler.unitStats != null && child.gameObject.activeSelf)
                {
                    if (clickHandler.unitStats.gameObject.name == name)
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
            }

            // Create button for unlocked unit and add to selection grid
            GameObject btn = Instantiate(unitButtonPrefab, unitSelectionGrid);

            Image btnImage = btn.GetComponent<Image>();
            Sprite unitSprite = unitToBuy.GetComponent<SpriteRenderer>()?.sprite;
            if (btnImage != null && unitSprite != null)
            {
                btnImage.sprite = unitSprite;
            }

            DraggableUnit drag = btn.GetComponent<DraggableUnit>();
            if (drag != null)
            {
                drag.SetPrefab(unitToBuy);
            }

            UnitClickHandler clickHandlerNew = btn.GetComponent<UnitClickHandler>();
            if (clickHandlerNew != null)
            {
                clickHandlerNew.unitStats = unitToBuy.GetComponent<UnitStats>();
                clickHandlerNew.unitSprite = unitSprite;
                clickHandlerNew.UpgradeScreenUI = UpgradeScreenUI;
            }
        }
    }

    void OnUnitSelected(GameObject unitPrefab)
    {
        Debug.Log($"Selected Unit: {unitPrefab.name}");
        // UpdateCountText();
    }

    void UpdateCountText()
    {
        if (frontlineCountText)
            frontlineCountText.text = $"Frontline: {LoadoutData.selectedFrontline.Count}/3";
        if (backlineCountText)
            backlineCountText.text = $"Backline: {LoadoutData.selectedBackline.Count}/3";
    }

    public BattleSystem battleSystem;

    public void OnStartBattleClicked()
    {
        int frontCount = 0, backCount = 0;

        for (int i = 0; i < 3; i++)
            if (LoadoutData.selectedUnits[i] != null) frontCount++;

        for (int i = 3; i < 6; i++)
            if (LoadoutData.selectedUnits[i] != null) backCount++;

        // Allow battle if either all frontline or all backline or all six are present
        if (!(frontCount == 3 || backCount == 3))
        {
            Debug.LogWarning("Please assign either 3 front units, 3 back units, or all 6 units before starting the battle.");
            return;
        }

        // Hide Loadout UI, Show Battle Screen
        loadoutScreen.SetActive(false);
        battleScreen.SetActive(true);

        // Pass control to BattleSystem
        // BattleSystem bs = FindFirstObjectByType<BattleSystem>();
        battleSystem.InitializeBattle();
    }

    public BattleSystem tournamentBattleHandler;

    public void OnTournamentBattleClicked()
    {
        int frontCount = 0, backCount = 0;

        for (int i = 0; i < 3; i++)
            if (LoadoutData.selectedUnits[i] != null) frontCount++;

        for (int i = 3; i < 6; i++)
            if (LoadoutData.selectedUnits[i] != null) backCount++;

        // Allow battle if either all frontline or all backline or all six are present
        if (!(frontCount == 3 || backCount == 3))
        {
            Debug.LogWarning("Please assign either 3 front units, 3 back units, or all 6 units before starting the battle.");
            return;
        }
        
        // Hide Loadout UI, Show Tournament Battle Screen
        loadoutScreen.SetActive(false);
        tournamentBattleScreen.SetActive(true);

        // Pass control to BattleSystem
        // BattleSystem bs = FindFirstObjectByType<BattleSystem>();
        tournamentBattleHandler.InitializeBattle();
    }

    public void OnBattleEnded()
    {
        // Show Loadout UI, Hide Battle Screen
        loadoutScreen.SetActive(true);
        battleScreen.SetActive(false);

        // Clear the 6 slots of loadoutSlotsParent
        for (int i = 0; i < loadoutSlotsParent.childCount; i++)
        {
            Transform slot = loadoutSlotsParent.GetChild(i);
            DropSlot dropSlot = slot.GetComponent<DropSlot>();
            if (dropSlot != null)
            {
                dropSlot.RemoveUnit();
            }
        }

        // Remove all unit buttons from the selection grid
        foreach (Transform child in unitSelectionGrid)
        {
            Destroy(child.gameObject);
        }


        // Optionally clear selected units
        LoadoutData.selectedFrontline.Clear();
        LoadoutData.selectedBackline.Clear();
        for (int i = 0; i < LoadoutData.selectedUnits.Length; i++)
        {
            LoadoutData.selectedUnits[i] = null;
        }

        // Recreate unit buttons for unlocked units
        foreach (GameObject unit in UnlockedUnits)
        {
            GameObject btn = Instantiate(unitButtonPrefab, unitSelectionGrid);

            Image btnImage = btn.GetComponent<Image>();
            Sprite unitSprite = unit.GetComponent<SpriteRenderer>()?.sprite;
            if (btnImage != null && unitSprite != null)
            {
                btnImage.sprite = unitSprite;
            }

            DraggableUnit drag = btn.GetComponent<DraggableUnit>();
            if (drag != null)
            {
                drag.SetPrefab(unit);
            }

            UnitClickHandler clickHandler = btn.GetComponent<UnitClickHandler>();
            if (clickHandler != null)
            {
                clickHandler.unitStats = unit.GetComponent<UnitStats>();
                clickHandler.unitSprite = unitSprite;
                clickHandler.UpgradeScreenUI = UpgradeScreenUI;
            }
        }

        UIScreenSlider.Instance.GoToScreen(1);
        UpdateCountText();
    }

    public void ReAddUnitToGrid(GameObject unit)
    {
        Debug.Log($"Re-adding unit to grid: {unit.name}");
        if (unit == null) return;

        GameObject btn = Instantiate(unitButtonPrefab, unitSelectionGrid);

        Image btnImage = btn.GetComponent<Image>();
        Sprite unitSprite = unit.GetComponent<SpriteRenderer>()?.sprite;
        if (btnImage != null && unitSprite != null)
        {
            btnImage.sprite = unitSprite;
        }

        DraggableUnit drag = btn.GetComponent<DraggableUnit>();
        if (drag != null)
        {
            drag.SetPrefab(unit);
        }
    }

}
