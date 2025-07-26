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


    private void Start()
    {
        CreateUnitButtons();
        LoadoutData.selectedFrontline.Clear();
        LoadoutData.selectedBackline.Clear();
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
        BattleSystem bs = FindFirstObjectByType<BattleSystem>();
        bs.InitializeBattle();
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
