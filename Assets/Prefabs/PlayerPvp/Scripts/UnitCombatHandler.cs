using System.Collections;
using UnityEngine;
using TMPro;

public class UnitCombatHandler : MonoBehaviour
{
    public bool IsFrontline = true;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("UI")]
    public TMP_Text healthText;
    public TMP_Text speedText;

    [Header("Body Rotation")]
    public GameObject bodyRotate;

    public BattleSystem battleSystem;
    public BattleSystem battleSystemTournament;
    private UnitCombatHandler currentTarget;
    public UnitStats unitStats;
    private int finalDamage;
    private bool hasAttacked = false;

    public bool isLookingTowardsRight = false;
    public Vector3 originalPosition;
    public float attackDistance = 0.8f;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        // battleSystem = BattleSystem.Instance;
        unitStats = GetComponent<UnitStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (unitStats == null)
            Debug.LogError($"UnitStats missing on {name}");

        if (battleSystem == null)
        {
            GameObject bsObj = GameObject.FindGameObjectWithTag("BattleSystem");
            if (bsObj != null)
            {
                battleSystem = bsObj.GetComponent<BattleSystem>();
                if (battleSystem == null)
                    Debug.LogError("BattleSystem component not found on object with tag 'BattleSystem'.");
            }
            else
            {
                Debug.LogError("No GameObject found with tag 'BattleSystem'.");
            }
        }

        if (battleSystemTournament == null)
        {
            GameObject bsObj = GameObject.FindGameObjectWithTag("BattleSystemTournament");
            if (bsObj != null)
            {
                battleSystemTournament = bsObj.GetComponent<BattleSystem>();
                if (battleSystemTournament == null)
                    Debug.LogError("BattleSystemTournament component not found on object with tag 'BattleSystemTournament'.");
            }
            else
            {
                Debug.LogError("No GameObject found with tag 'BattleSystemTournament'.");
            }
        }

        // Flip sprite based on position
        if (spriteRenderer != null)
        {
            bool isTowardsRight = transform.position.x > 0;
            spriteRenderer.flipX = isLookingTowardsRight == isTowardsRight;
        }

        if (bodyRotate != null)
        {
            Debug.Log($"Setting body rotation for {name}");
            bool isTowardsRight = transform.position.x > 0;
            bodyRotate.transform.localScale = (isLookingTowardsRight == isTowardsRight) ? new Vector3(-1, bodyRotate.transform.localScale.y, bodyRotate.transform.localScale.z) : new Vector3(1, bodyRotate.transform.localScale.y, bodyRotate.transform.localScale.z);   
        }

        unitStats.ResetHP(); // Ensure currentHP is set at spawn
        UpdateHealthText();
        UpdateSpeedText();
    }

    private void Start()
    {
        UpdateHealthText();
        UpdateSpeedText();
    }

    public void StartTurn()
    {
        StartAttack();
    }

    int originalOrder = 0;
    public void StartAttack()
    {

        // Debug.Log($"{gameObject.name} is starting an attack!");
        originalOrder = spriteRenderer.sortingOrder;
        spriteRenderer.sortingOrder = 10;

        if (TournamentManager.Instance.IsTournamentActive)
        {
            currentTarget = battleSystemTournament.PickTarget(this);
        }
        else
        {
            currentTarget = battleSystem.PickTarget(this);
        }
        if (currentTarget == null)
        {
            Debug.Log("No valid target found!");
            return;
        }

        finalDamage = CalculateDamage();
        hasAttacked = false;

        StartCoroutine(MoveToTarget(currentTarget));
    }

    private int CalculateDamage()
    {
        UnitStatsData stats = unitStats.GetStats();
        float critRoll = Random.Range(0f, 100f);
        bool isCrit = critRoll <= stats.CritChance;

        int damage = isCrit
            ? Mathf.RoundToInt(stats.Attack * stats.CritMultiplier)
            : stats.Attack;

        // Debug.Log($"Attack Damage: {stats.Attack}, Critical Hit: {isCrit}, Damage: {damage}");

        if (isCrit)
            Debug.Log($"{gameObject.name} landed a CRITICAL HIT! Damage: {damage}");

        return damage;
    }

    public void TakeDamage(int incomingDamage, UnitCombatHandler attacker)
    {
        float dodgeRoll = Random.Range(0f, 100f);
        float dodgeChance = unitStats.GetStats().DodgeChance;

        if (dodgeRoll <= dodgeChance)
        {
            Debug.Log($"{gameObject.name} DODGED the attack from {attacker.gameObject.name}!");
            return;
        }

        int actualDamage = unitStats.currentHP <= incomingDamage ? unitStats.currentHP : incomingDamage;
        unitStats.currentHP -= actualDamage;
        // if (unitStats.currentHP < 0) unitStats.currentHP = 0;

        if (TournamentManager.Instance.IsTournamentActive)
        {
            battleSystemTournament.UpdateHealthBars(actualDamage);
        }
        else
        {
            battleSystem.UpdateHealthBars(actualDamage);
        }

        UpdateHealthText();

        // Debug.Log($"{gameObject.name} took {incomingDamage} damage from {attacker.gameObject.name}. Remaining HP: {unitStats.currentHP}");

        if (unitStats.currentHP <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        if (TournamentManager.Instance.IsTournamentActive)
        {
            battleSystemTournament.RemoveUnit(this);
        }
        else
        {
            battleSystem.RemoveUnit(this);
        }
        Destroy(gameObject);
    }

    public void EndAttack()
    {
        animator.SetBool("isAttacking", false);
        StartCoroutine(ReturnToStart());
    }

    public void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = $"HP: {unitStats.currentHP}/{unitStats.MaxHP}";
    }

    public void UpdateSpeedText()
    {
        if (speedText != null)
            speedText.text = $"Speed: {unitStats.GetStats().Speed}";
    }

    IEnumerator MoveToTarget(UnitCombatHandler target)
    {
        animator.SetFloat("xVelocity", 1);
        Vector3 targetPos = target.transform.position + Vector3.left * 1f * Mathf.Sign(transform.position.x);

        while (Vector3.Distance(transform.position, targetPos) > attackDistance)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 10f);
            yield return null;
        }

        if (!hasAttacked)
        {
            currentTarget.TakeDamage(finalDamage, this);
            hasAttacked = true;
            animator.SetBool("isAttacking", true);
        }
    }

    IEnumerator ReturnToStart()
    {
        animator.SetFloat("xVelocity", 0);

        while (Vector3.Distance(transform.position, originalPosition) > 0f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, Time.deltaTime * 10f);
            yield return null;
        }
        spriteRenderer.sortingOrder = originalOrder;

        if (TournamentManager.Instance.IsTournamentActive)
        {
            if (!battleSystemTournament.fightEnded)
            {
                battleSystemTournament.NextTurn();
            }
        }
        else
        {
            if (!battleSystem.fightEnded)
            {
                battleSystem.NextTurn();
            }
        }
    }
}
