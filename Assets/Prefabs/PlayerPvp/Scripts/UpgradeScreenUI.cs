using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeScreenUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Image unitImage;
    public Button levelUpButton;
    public Button closeButton;

    [Header("Stat Rows")]
    public Transform hpRow;       // 3 children: Label, Value, Next
    public Transform atkRow;
    public Transform spdRow;
    // public Transform critRow;
    public Transform critDmgRow;

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
            HP = currentUnit.baseHP + growth.HPPerLevel * levelOffset,
            Attack = currentUnit.baseAttack + growth.AttackPerLevel * levelOffset,
            Speed = currentUnit.baseSpeed + growth.SpeedPerLevel * levelOffset,
            CritChance = currentUnit.baseCritChance + growth.CritChancePerLevel * levelOffset,
            CritMultiplier = currentUnit.critMultiplier + growth.CritDamagePerLevel * levelOffset,
            DodgeChance = currentUnit.baseDodgeChance + growth.DodgeChancePerLevel * levelOffset
        };

        nameText.text = currentUnit.name;
        levelText.text = $"Level: {currentUnit.currentLevel}/{currentUnit.maxLevel}";

        SetStatRow(hpRow, "Health", current.HP, next.HP - current.HP);
        SetStatRow(atkRow, "Attack", current.Attack, next.Attack - current.Attack);
        SetStatRow(spdRow, "Speed", current.Speed, next.Speed - current.Speed);
        // SetStatRow(critRow, "Critical", current.CritChance, next.CritChance - current.CritChance, isPercent: true);
        SetStatRow(critDmgRow, "Crit Dmg", current.CritMultiplier, next.CritMultiplier - current.CritMultiplier, isMultiplier: true);
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
            levelUpButton.gameObject.SetActive(false);
        }
        else
        {
            levelUpButton.onClick.AddListener(OnLevelUp);
        }
        closeButton.onClick.AddListener(Close);
    }
}
