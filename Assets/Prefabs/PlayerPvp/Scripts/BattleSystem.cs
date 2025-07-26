using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    public static BattleSystem Instance;

    [Header("Spawn Parents")]
    public Transform playerFrontSpawnParent;
    public Transform playerBackSpawnParent;
    public Transform enemyFrontSpawnParent;
    public Transform enemyBackSpawnParent;

    [Header("UI References")]
    public UnityEngine.UI.Image playerHealth;
    public UnityEngine.UI.Image enemyHealth;

    [Header("Battle Controls")]
    public UnityEngine.UI.Button pauseButton;
    public UnityEngine.UI.Button fastForwardButton;


    private Transform[] playerFrontSpawns;
    private Transform[] playerBackSpawns;
    private Transform[] enemyFrontSpawns;
    private Transform[] enemyBackSpawns;

    private List<UnitCombatHandler> playerFrontline = new List<UnitCombatHandler>();
    private List<UnitCombatHandler> playerBackline = new List<UnitCombatHandler>();
    private List<UnitCombatHandler> enemyFrontline = new List<UnitCombatHandler>();
    private List<UnitCombatHandler> enemyBackline = new List<UnitCombatHandler>();

    public List<GameObject> enemyFrontPrefabs;
    public List<GameObject> enemyBackPrefabs;

    [Header("End Screens")]
    public GameObject wonScreen;
    public GameObject lostScreen;

    public float playerHealthTotal = 0f;
    public float enemyHealthTotal = 0f;

    public float playerHealthCurrent = 0f;
    public float enemyHealthCurrent = 0f;

    public bool fightEnded = false;
    private int currentTurnIndex = 0;
    private List<UnitCombatHandler> turnQueue = new List<UnitCombatHandler>();

    private void Awake()
    {
        Instance = this;
        pauseButton.onClick.AddListener(TooglePause);
        fastForwardButton.onClick.AddListener(ToogleSpeedUp);
    }

    public void InitializeBattle()
    {
        fightEnded = false;
        playerFrontSpawns = GetChildren(playerFrontSpawnParent);
        playerBackSpawns = GetChildren(playerBackSpawnParent);
        enemyFrontSpawns = GetChildren(enemyFrontSpawnParent);
        enemyBackSpawns = GetChildren(enemyBackSpawnParent);

        // Spawn Player Frontline
        for (int i = 0; i < 3; i++)
        {
            GameObject unitPrefab = LoadoutData.selectedUnits[i];
            if (unitPrefab == null) continue;

            GameObject go = Instantiate(unitPrefab, playerFrontSpawns[i].position, Quaternion.identity);
            UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
            playerHealthTotal += handler.unitStats.GetStats().HP;
            handler.IsFrontline = true;
            playerFrontline.Add(handler);
        }

        // Spawn Player Backline
        for (int i = 3; i < 6; i++)
        {
            GameObject unitPrefab = LoadoutData.selectedUnits[i];
            if (unitPrefab == null) continue;

            GameObject go = Instantiate(unitPrefab, playerBackSpawns[i - 3].position, Quaternion.identity);
            UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
            playerHealthTotal += handler.unitStats.GetStats().HP;
            handler.IsFrontline = false;
            playerBackline.Add(handler);
        }

        // Spawn Enemy Frontline
        for (int i = 0; i < enemyFrontPrefabs.Count && i < enemyFrontSpawns.Length; i++)
        {
            GameObject go = Instantiate(enemyFrontPrefabs[i], enemyFrontSpawns[i].position, Quaternion.identity);
            UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
            enemyHealthTotal += handler.unitStats.GetStats().HP;
            handler.IsFrontline = true;
            enemyFrontline.Add(handler);
        }

        // Spawn Enemy Backline
        for (int i = 0; i < enemyBackPrefabs.Count && i < enemyBackSpawns.Length; i++)
        {
            GameObject go = Instantiate(enemyBackPrefabs[i], enemyBackSpawns[i].position, Quaternion.identity);
            UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
            enemyHealthTotal += handler.unitStats.GetStats().HP;
            handler.IsFrontline = false;
            enemyBackline.Add(handler);
        }

        playerHealthCurrent = playerHealthTotal;
        enemyHealthCurrent = enemyHealthTotal;

        StartBattle();
    }

    private Transform[] GetChildren(Transform parent)
    {
        Transform[] children = new Transform[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
        {
            children[i] = parent.GetChild(i);
        }
        return children;
    }

    public void StartBattle()
    {
        turnQueue.Clear();
        turnQueue.AddRange(playerFrontline);
        turnQueue.AddRange(playerBackline);
        turnQueue.AddRange(enemyFrontline);
        turnQueue.AddRange(enemyBackline);

        // Assign bonus speed based on placement
        Dictionary<UnitCombatHandler, int> bonusSpeed = new Dictionary<UnitCombatHandler, int>();

        // Player Front: +6, +5, +4
        for (int i = 0; i < playerFrontline.Count; i++)
            bonusSpeed[playerFrontline[i]] = 6 - i;

        // Player Back: +3, +2, +1
        for (int i = 0; i < playerBackline.Count; i++)
            bonusSpeed[playerBackline[i]] = 3 - i;

        // Enemy Front: +6, +5, +4
        for (int i = 0; i < enemyFrontline.Count; i++)
            bonusSpeed[enemyFrontline[i]] = 6 - i;

        // Enemy Back: +3, +2, +1
        for (int i = 0; i < enemyBackline.Count; i++)
            bonusSpeed[enemyBackline[i]] = 3 - i;

        // Shuffle for randomness among same speed+bonus
        for (int i = turnQueue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = turnQueue[i];
            turnQueue[i] = turnQueue[j];
            turnQueue[j] = temp;
        }

        // Sort by (base speed + bonus), descending
        turnQueue.Sort((a, b) =>
        {
            int aSpeed = a.unitStats.GetStats().Speed + (bonusSpeed.ContainsKey(a) ? bonusSpeed[a] : 0);
            int bSpeed = b.unitStats.GetStats().Speed + (bonusSpeed.ContainsKey(b) ? bonusSpeed[b] : 0);
            return bSpeed.CompareTo(aSpeed);
        });

        currentTurnIndex = 0;
        NextTurn();
    }

    public void NextTurn()
    {
        if (turnQueue.Count == 0) return;

        UnitCombatHandler unit = turnQueue[currentTurnIndex];
        currentTurnIndex = (currentTurnIndex + 1) % turnQueue.Count;
        unit.originalPosition = unit.transform.position;
        unit.StartTurn();
    }

    public UnitCombatHandler PickTarget(UnitCombatHandler attacker)
    {
        List<UnitCombatHandler> enemiesFront;
        List<UnitCombatHandler> enemiesBack;

        if (playerFrontline.Contains(attacker) || playerBackline.Contains(attacker))
        {
            lastAttackerPlayer = true;
            enemiesFront = enemyFrontline;
            enemiesBack = enemyBackline;
        }
        else
        {
            lastAttackerPlayer = false;
            enemiesFront = playerFrontline;
            enemiesBack = playerBackline;
        }

        List<UnitCombatHandler> validFront = enemiesFront.FindAll(u => u.unitStats.GetStats().HP > 0);
        List<UnitCombatHandler> validBack = enemiesBack.FindAll(u => u.unitStats.GetStats().HP > 0);

        if (validFront.Count > 0)
            return validFront[Random.Range(0, validFront.Count)];
        if (validBack.Count > 0)
            return validBack[Random.Range(0, validBack.Count)];

        return null;
    }

    bool lastAttackerPlayer = true;

    public void UpdateHealthBars(int damage)
    {
        if (lastAttackerPlayer)
        {
            enemyHealthCurrent -= damage;
            if (enemyHealthCurrent <= 0)
            {
                enemyHealthCurrent = 0;
            }

            enemyHealth.fillAmount = enemyHealthCurrent / enemyHealthTotal;

        }
        else
        {
            playerHealthCurrent -= damage;
            if (playerHealthCurrent <= 0)
            {
                playerHealthCurrent = 0;
            }

            playerHealth.fillAmount = playerHealthCurrent / playerHealthTotal;

        }
    }

    public void RemoveUnit(UnitCombatHandler unit)
    {
        turnQueue.Remove(unit);
        playerFrontline.Remove(unit);
        playerBackline.Remove(unit);
        enemyFrontline.Remove(unit);
        enemyBackline.Remove(unit);

        if (playerFrontline.Count + playerBackline.Count == 0)
        {
            Debug.Log("Enemy team wins!");
            PlayerStatsManager.Instance.PlayerLevel--;
            lostScreen.SetActive(true);
            fightEnded = true;
            return;
        }
        if (enemyFrontline.Count + enemyBackline.Count == 0)
        {
            Debug.Log("Player team wins!");
            PlayerStatsManager.Instance.PlayerLevel++;
            wonScreen.SetActive(true);
            fightEnded = true;
            return;
        }

        if (currentTurnIndex >= turnQueue.Count)
            currentTurnIndex = 0;
    }


    bool isPaused = false;
    bool isSpeedingUp = false;
    public void TooglePause()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
        else
        {
            Time.timeScale = 0f;
            isPaused = true;
        }

    }

    public void ToogleSpeedUp()
    {
        if (isSpeedingUp)
        {
            Time.timeScale = 1f;
            isSpeedingUp = false;
        }
        else
        {
            Time.timeScale = 1.5f;
            isSpeedingUp = true;
        }
    }

    public void ResetBattleScene()
    {
        fightEnded = false;
        // Destroy all player units
        foreach (var unit in playerFrontline)
        {
            if (unit != null)
                Destroy(unit.gameObject);
        }
        foreach (var unit in playerBackline)
        {
            if (unit != null)
                Destroy(unit.gameObject);
        }

        // Destroy all enemy units
        foreach (var unit in enemyFrontline)
        {
            if (unit != null)
                Destroy(unit.gameObject);
        }
        foreach (var unit in enemyBackline)
        {
            if (unit != null)
                Destroy(unit.gameObject);
        }

        // Clear all lists
        playerFrontline.Clear();
        playerBackline.Clear();
        enemyFrontline.Clear();
        enemyBackline.Clear();
        turnQueue.Clear();

        // Reset health values
        playerHealthTotal = 0f;
        enemyHealthTotal = 0f;
        playerHealthCurrent = 0f;
        enemyHealthCurrent = 0f;
        playerHealth.fillAmount = 1f;
        enemyHealth.fillAmount = 1f;

        // Disable end screens
        if (wonScreen != null)
            wonScreen.SetActive(false);
        if (lostScreen != null)
            lostScreen.SetActive(false);
        
        SelectionScreenManager.Instance.OnBattleEnded();
    }


}


