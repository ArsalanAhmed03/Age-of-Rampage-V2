using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeScreenUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;

    public TMP_Text currentCountText;
    public TMP_Text currentCSText;

    public Image unitImage;
    public Button levelUpButton;
    public Button combineButton;
    public Button closeButton;

    [Header("Stat Rows")]
    public Transform hpRow;       // 3 children: Label, Value, Next
    public Transform atkRow;
    public Transform spdRow;
    // public Transform critRow;
    public Transform critDmgRow;

    [Header("Combine Row")]

    public Transform hpRowCombine;       // 3 children: Label, Value, Next
    public Transform atkRowCombine;

    private UnitStats currentUnit;

    public bool isBuyScreen = false;



    public void Open(UnitStats unitStats, Sprite unitSprite)
    {
        Debug.Log($"Opening Upgrade Screen for {unitStats.name}");
        currentUnit = unitStats;
        unitImage.sprite = unitSprite;
        gameObject.SetActive(true);
        RefreshUI();
    }

    void RefreshUI()
    {
        UnitStatsData current = currentUnit.GetStats();
        int nextLevel = Mathf.Min(currentUnit.currentLevel + 1, currentUnit.maxLevel);

        // Simulate next level stats without leveling up
        int levelOffset = nextLevel - 1;
        StatGrowth growth = currentUnit.growth;
        UnitStatsData next = new UnitStatsData
        {
            HP = currentUnit.baseHP + (currentUnit.currentCS * 2) + growth.HPPerLevel * levelOffset,
            Attack = currentUnit.baseAttack + (currentUnit.currentCS * 1) + growth.AttackPerLevel * levelOffset,
            Speed = currentUnit.baseSpeed + growth.SpeedPerLevel * levelOffset,
            CritChance = currentUnit.baseCritChance + growth.CritChancePerLevel * levelOffset,
            CritMultiplier = currentUnit.critMultiplier + growth.CritDamagePerLevel * levelOffset,
            DodgeChance = currentUnit.baseDodgeChance + growth.DodgeChancePerLevel * levelOffset
        };

        nameText.text = currentUnit.name;
        levelText.text = $"Lvl: {currentUnit.currentLevel}/{currentUnit.maxLevel}";

        currentCountText.text = $"x{UserDataManager.Instance.GetUnitCount(currentUnit.name)}";
        currentCSText.text = $"CS: {currentUnit.currentCS} -> {currentUnit.currentCS + 1}";


        SetStatRow(hpRow, "HP", current.HP, next.HP - current.HP);
        SetStatRow(atkRow, "ATK", current.Attack, next.Attack - current.Attack);
        SetStatRow(spdRow, "Speed", current.Speed, next.Speed - current.Speed);
        // SetStatRow(critRow, "Critical", current.CritChance, next.CritChance - current.CritChance, isPercent: true);
        SetStatRow(critDmgRow, "Crit Dmg", current.CritMultiplier, next.CritMultiplier - current.CritMultiplier, isMultiplier: true);

        SetStatRow(hpRowCombine, "HP", current.HP, 2);
        SetStatRow(atkRowCombine, "ATK", current.Attack, 1);
    }

    void SetStatRow(Transform row, string label, float current, float increase, bool isPercent = false, bool isMultiplier = false)
    {
        row.GetChild(0).GetComponent<TMP_Text>().text = label;
        row.GetChild(1).GetComponent<TMP_Text>().text = isMultiplier ? $"{current:F2}x" : isPercent ? $"{current:F1}%" : Mathf.RoundToInt(current).ToString();
        if (row.childCount > 2)
        {
            row.GetChild(2).GetComponent<TMP_Text>().text = increase != 0
            ? isMultiplier ? $"+{increase:F2}x"
            : isPercent ? $"+{increase:F1}%"
            : $"+{Mathf.RoundToInt(increase)}"
            : "";
        }
    }

    public void OnLevelUp()
    {
        currentUnit.LevelUp();
        RefreshUI();
    }

    public void OnCombineUnit()
    {
        currentUnit.Combine();
        RefreshUI();
    }

    public void OnBuyUnit()
    {
        SelectionScreenManager.Instance.BuyUnit(currentUnit.name);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (isBuyScreen)
        {
            levelUpButton?.gameObject.SetActive(false);
            combineButton?.gameObject.SetActive(false);
        }
        else
        {
            levelUpButton?.onClick.AddListener(OnLevelUp);
            combineButton?.onClick.AddListener(OnCombineUnit);

        }
        closeButton?.onClick.AddListener(Close);
    }
}
