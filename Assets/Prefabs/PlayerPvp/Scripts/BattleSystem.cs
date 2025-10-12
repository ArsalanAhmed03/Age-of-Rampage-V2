using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

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

    public UnityEngine.UI.Button resumeButton;

    public GameObject PauseMenu;

    public GameObject FilmButton;

    public bool TournamentMode = false;

    public UnityEngine.UI.Button fastForwardButton;



    public GameObject messageStart;
    public GameObject messageEnd;
    public GameObject messagePrefab;

    private Transform[] playerFrontSpawns;
    private Transform[] playerBackSpawns;
    private Transform[] enemyFrontSpawns;
    private Transform[] enemyBackSpawns;

    public List<UnitCombatHandler> playerFrontline = new List<UnitCombatHandler>();
    public List<UnitCombatHandler> playerBackline = new List<UnitCombatHandler>();
    public List<UnitCombatHandler> enemyFrontline = new List<UnitCombatHandler>();
    public List<UnitCombatHandler> enemyBackline = new List<UnitCombatHandler>();

    public List<GameObject> enemyFrontPrefabs;
    public List<GameObject> enemyBackPrefabs;

    [Header("End Screens")]
    public GameObject wonScreen;
    public GameObject lostScreen;

    public float playerHealthTotal = 0f;
    public float enemyHealthTotal = 0f;

    public float playerHealthCurrent = 0f;
    public float enemyHealthCurrent = 0f;

    public Transform worldCanvas; // Canvas where animation will play (world-space or overlay)
    public RectTransform filmButtonUI; // Assign the Film button's RectTransform in inspector
    public GameObject filmSpritePrefab; // Assign a small film sprite prefab (UI Image or world sprite)

    public bool fightEnded = true;
    private int currentTurnIndex = 0;
    private List<UnitCombatHandler> turnQueue = new List<UnitCombatHandler>();

    private void Awake()
    {
        Instance = this;
        pauseButton.onClick.AddListener(PauseGame);
        resumeButton.onClick.AddListener(ResumeGame);
        // fastForwardButton.onClick.AddListener(ToogleSpeedUp);
        // fastForwardButton.onClick.RemoveAllListeners();
        fastForwardButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var trigger = fastForwardButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();

        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener((data) => SpeedUp());
        trigger.triggers.Add(pointerDown);

        var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp
        };
        pointerUp.callback.AddListener((data) => SpeedDown());
        trigger.triggers.Add(pointerUp);

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
            int index = UserDataManager.Instance.OwnedUnits.IndexOf(unitPrefab.name);

            if (!TournamentMode)
            {
                if (index >= 0 && index < UserDataManager.Instance.OwnedUnitAbilities.Count)
                    handler.specialAbility = UserDataManager.Instance.OwnedUnitAbilities[index];
                else
                {
                    handler.specialAbility = "Cleptomaniac";
                }

                int randomValue = Random.Range(1, 6);
                if (handler.specialAbility.ToLower() == "staminaexpert")
                {
                    handler.extraHealthFromAbility = (int)(handler.unitStats.GetStats().HP * (randomValue * 5 / 100f));
                }
                playerHealthTotal += handler.unitStats.GetStats().HP + handler.extraHealthFromAbility;
                handler.IsFrontline = true;

                playerFrontline.Add(handler);

                handler.specialAbility = "None";
            }
            else
            {
                handler.specialAbility = "None";
                playerHealthTotal += handler.unitStats.GetStats().HP;
                handler.IsFrontline = true;

                playerFrontline.Add(handler);
            }
        }

        // Spawn Player Backline
        for (int i = 3; i < 6; i++)
        {
            GameObject unitPrefab = LoadoutData.selectedUnits[i];
            if (unitPrefab == null) continue;

            GameObject go = Instantiate(unitPrefab, playerBackSpawns[i - 3].position, Quaternion.identity);
            UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();

            int index = UserDataManager.Instance.OwnedUnits.IndexOf(unitPrefab.name);
            if (!TournamentMode)
            {
                if (index >= 0 && index < UserDataManager.Instance.OwnedUnitAbilities.Count)
                    handler.specialAbility = UserDataManager.Instance.OwnedUnitAbilities[index];
                else
                {
                    handler.specialAbility = "Cleptomaniac";
                }

                int randomValue = Random.Range(1, 6);
                if (handler.specialAbility.ToLower() == "staminaexpert")
                {
                    handler.extraHealthFromAbility = (int)(handler.unitStats.GetStats().HP * (randomValue * 5 / 100f));
                }
                playerHealthTotal += handler.unitStats.GetStats().HP + handler.extraHealthFromAbility;
                handler.IsFrontline = false;

                playerBackline.Add(handler);

                handler.specialAbility = "None";
            }
            else
            {
                handler.specialAbility = "None";
                playerHealthTotal += handler.unitStats.GetStats().HP;
                handler.IsFrontline = false;

                playerBackline.Add(handler);
            }
        }

        if (TournamentMode)
        {
            GameObject unitPrefab = enemyFrontPrefabs[0];
            if (unitPrefab != null)
            {
                GameObject go = Instantiate(unitPrefab, enemyFrontSpawns[0].position, Quaternion.identity);
                UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
                enemyHealthTotal += handler.unitStats.GetStats().HP;
                handler.IsFrontline = true;
                enemyFrontline.Add(handler);
                handler.specialAbility = "None";

            }
        }
        else
        {

            // Spawn Enemy Frontline
            for (int i = 0; i < enemyFrontPrefabs.Count && i < enemyFrontSpawns.Length; i++)
            {
                GameObject unitPrefab = enemyFrontPrefabs[i];
                if (unitPrefab == null) continue;
                GameObject go = Instantiate(unitPrefab, enemyFrontSpawns[i].position, Quaternion.identity);
                UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
                enemyHealthTotal += handler.unitStats.GetStats().HP;
                handler.IsFrontline = true;
                enemyFrontline.Add(handler);
                FilmCaptureTarget capture = go.GetComponent<FilmCaptureTarget>();
                Collider2D col = go.GetComponent<Collider2D>();
                if (col == null)
                    col = go.AddComponent<BoxCollider2D>();

                if (capture == null)
                {
                    capture = go.AddComponent<FilmCaptureTarget>();
                    Debug.Log("Added FilmCaptureTarget component to enemy unit.");
                }


                if (capture != null)
                {
                    capture.unitHandler = handler;
                    capture.filmButtonUI = filmButtonUI;
                    capture.filmSpritePrefab = filmSpritePrefab;
                    capture.worldCanvas = worldCanvas;
                }


                // int index = UserDataManager.Instance.OwnedUnits.IndexOf(unitPrefab.name);
                // if (index >= 0 && index < UserDataManager.Instance.OwnedUnitAbilities.Count)
                //     handler.specialAbility = UserDataManager.Instance.OwnedUnitAbilities[index];
                // else
                // {
                //     handler.specialAbility = "Cleptomaniac";
                // }
                handler.specialAbility = "None";

            }

            // Spawn Enemy Backline
            for (int i = 0; i < enemyBackPrefabs.Count && i < enemyBackSpawns.Length; i++)
            {
                GameObject unitPrefab = enemyBackPrefabs[i];
                if (unitPrefab == null) continue;
                GameObject go = Instantiate(unitPrefab, enemyBackSpawns[i].position, Quaternion.identity);
                UnitCombatHandler handler = go.GetComponent<UnitCombatHandler>();
                enemyHealthTotal += handler.unitStats.GetStats().HP;
                handler.IsFrontline = false;
                enemyBackline.Add(handler);

                FilmCaptureTarget capture = go.GetComponent<FilmCaptureTarget>();
                Collider2D col = go.GetComponent<Collider2D>();
                if (col == null)
                    col = go.AddComponent<BoxCollider2D>();

                if (capture == null)
                {
                    capture = go.AddComponent<FilmCaptureTarget>();
                    Debug.Log("Added FilmCaptureTarget component to enemy unit.");
                }

                if (capture != null)
                {
                    capture.unitHandler = handler;
                    capture.filmButtonUI = filmButtonUI;
                    capture.filmSpritePrefab = filmSpritePrefab;
                    capture.worldCanvas = worldCanvas;
                }
                // int index = UserDataManager.Instance.OwnedUnits.IndexOf(unitPrefab.name);
                // if (index >= 0 && index < UserDataManager.Instance.OwnedUnitAbilities.Count)
                //     handler.specialAbility = UserDataManager.Instance.OwnedUnitAbilities[index];
                // else
                // {
                //     handler.specialAbility = "Cleptomaniac";
                // }
                handler.specialAbility = "None";
            }
        }

        playerHealthCurrent = playerHealthTotal;
        enemyHealthCurrent = enemyHealthTotal;

        StartBattle();
    }

    private Transform[] GetChildren(Transform parent)
    {
        if (parent == null || parent.childCount == 0) return new Transform[0];
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

        if (TournamentMode)
        {
            TournamentManager.Instance.StartTournament();
        }
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


        if (!TournamentMode)
        {
            // Enemy Back: +3, +2, +1
            for (int i = 0; i < enemyBackline.Count; i++)
                bonusSpeed[enemyBackline[i]] = 3 - i;
        }

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

    private void Update()
    {
        if (fightEnded || TournamentMode) return;

        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("UserDataManager instance is null.");
            return;
        }

        if (UserDataManager.Instance.Films < 1)
        {
            FilmButton.SetActive(false);
        }
        else
        {
            FilmButton.SetActive(true);
        }


    }

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


        if (unit.specialAbility.ToLower() == "flashwave" && unit.unitStats.GetStats().HP <= 0)
        {
            int randomValue = Random.Range(1, 6);
            int chance = Random.Range(1, 101);
            if (randomValue <= chance)
            {
                ShowFloatingText($"Flash Wave Active!", messageStart.transform);

                foreach (var unitCheck in playerFrontline)
                {
                    string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                    FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                    ShowFloatingText($"Captured {capturedUnitName}!", messageStart.transform);
                }
                foreach (var unitCheck in playerBackline)
                {
                    string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                    FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                    ShowFloatingText($"Captured {capturedUnitName}!", messageStart.transform);
                }
                foreach (var unitCheck in enemyFrontline)
                {
                    string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                    FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                    ShowFloatingText($"Captured {capturedUnitName}!", messageStart.transform);
                }
                foreach (var unitCheck in enemyBackline)
                {
                    string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                    FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                    ShowFloatingText($"Captured {capturedUnitName}!", messageStart.transform);
                }
            }
        }

        if (playerFrontline.Count + playerBackline.Count == 0)
        {
            Debug.Log("Enemy team wins!");
            if (!TournamentMode)
            {
                if (UserDataManager.Instance.Level > 1)
                {
                    UserDataManager.Instance.Level--;
                }

                // Update enemy level when they win
                UpdateEnemyStats(1, 1, 1);
            }

            lostScreen.SetActive(true);
            fightEnded = true;
            return;
        }
        if (enemyFrontline.Count + enemyBackline.Count == 0)
        {
            foreach (var unitCheck in playerFrontline)
            {
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "cleptomaniac" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = Random.Range(1, 6);
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        UserDataManager.Instance.Films += 1;
                    }
                }
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "piggybank" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = 10;
                    int goldCount = Random.Range(1, 6);
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        UserDataManager.Instance.Gold += goldCount;
                    }
                }
            }
            foreach (var unitCheck in playerBackline)
            {
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "cleptomaniac" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = Random.Range(1, 6);
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        UserDataManager.Instance.Films += 1;
                    }
                }
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "piggybank" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = 10;
                    int goldCount = Random.Range(1, 6);
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        UserDataManager.Instance.Gold += goldCount;
                    }
                }
            }

            Debug.Log("Player team wins!");
            if (!TournamentMode)
            {
                UserDataManager.Instance.Level += 1;
                UserDataManager.Instance.Wins += 1;
                UserDataManager.Instance.Gold += 1;

                // Update enemy level when they lose
                UpdateEnemyStats(-1, 0, 0);
            }

            wonScreen.SetActive(true);
            fightEnded = true;
            return;
        }

        if (currentTurnIndex >= turnQueue.Count)
            currentTurnIndex = 0;
    }

    // Method to update the enemy's level based on battle outcome
    private void UpdateEnemyStats(int levelChange, int winChange, int goldChange)
    {
        Debug.Log($"UpdateEnemyStats called with levelChange: {levelChange}, winChange: {winChange}, gold: {goldChange}");

        // Find the EnemyScreenManager in the scene
        EnemyScreenManager enemyManager = FindFirstObjectByType<EnemyScreenManager>();

        if (enemyManager != null)
        {
            Debug.Log($"Found EnemyScreenManager. HasSelectedOpponent: {enemyManager.HasSelectedOpponent}");
            if (enemyManager.HasSelectedOpponent)
            {
                enemyManager.UpdateSelectedOpponentInfo(levelChange, winChange, goldChange);
                // enemyManager.UpdateSelectedOpponentLosses(newLosses);
            }
            else
            {
                Debug.LogWarning("EnemyScreenManager found but no selected opponent to update stats for");
            }
        }
        else
        {
            Debug.LogError("EnemyScreenManager not found in scene");
        }
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

    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
        PauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        PauseMenu.SetActive(false);
        isPaused = false;
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

    public void SpeedUp()
    {
        Time.timeScale = 1.5f;
        isSpeedingUp = true;
    }

    public void SpeedDown()
    {
        Time.timeScale = 1f;
        isSpeedingUp = false;
    }



    public void ResetBattleScene()
    {

        FirebaseUpdater.Instance.GetAllOpponents();
        if (TournamentMode)
        {
            TournamentManager.Instance.EndTournament();
            TournamentManager.Instance.AddTournamentDamage((int)(enemyHealthTotal - enemyHealthCurrent));
        }
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

        EnemyScreenManager enemyManager = FindFirstObjectByType<EnemyScreenManager>();
        if (enemyManager != null)
        {
            enemyManager.ClearSelectedOpponent();
            enemyManager.ShowArenaStartScreen();
        }
        SelectionScreenManager.Instance.OnBattleEnded();
    }

    public void ShowFloatingText(string message, Transform start, float speed = 1f, float riseHeight = 100f)
    {
        if (messagePrefab == null || worldCanvas == null)
        {
            Debug.LogWarning("FloatingTextPrefab or worldCanvas not assigned!");
            return;
        }

        // Spawn prefab
        GameObject textObj = Instantiate(messagePrefab, worldCanvas);
        textObj.SetActive(true);

        // Get text component
        TMP_Text tmpText = textObj.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
            tmpText.text = message;
        else
        {
            Text legacyText = textObj.GetComponentInChildren<Text>();
            if (legacyText != null)
                legacyText.text = message;
        }

        // Set initial position
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.position = start.position;

        // Compute upward end position
        Vector3 startPos = rect.position;
        Vector3 endPos = startPos + Vector3.up * riseHeight;

        StartCoroutine(MoveAndFadeFloatingText(rect, textObj, startPos, endPos, speed));
    }

    private IEnumerator MoveAndFadeFloatingText(RectTransform rect, GameObject textObj, Vector3 startPos, Vector3 endPos, float speed)
    {
        float duration = 1.2f / speed;
        float t = 0f;

        CanvasGroup group = textObj.GetComponent<CanvasGroup>();
        if (group == null)
            group = textObj.AddComponent<CanvasGroup>();

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            // Smooth upward motion
            rect.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, progress));

            // Fade out near the end
            group.alpha = 1f - Mathf.Pow(progress, 2f);

            yield return null;
        }

        Destroy(textObj);
    }




}


