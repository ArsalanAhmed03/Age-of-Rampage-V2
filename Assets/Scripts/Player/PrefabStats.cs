using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PrefabStats : NetworkBehaviour
{
    [Header("Prefab Stats")]
    [SerializeField] private int Prefab_Health = 100;
    [SerializeField] private int Prefab_Level = 1;
    [SerializeField] private float Attack = 10f;
    [SerializeField] private int Critical_Chance = 10; //in percentage
    [SerializeField] private float Critical_Damage = 1.5f; //1.5 times the normal damage
    [SerializeField] private int Dodge_Change = 1;
    [SerializeField] private int Speed = 1;


    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Level = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text levelText;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Health.Value = Prefab_Health;
            Level.Value = Prefab_Level;
        }

        Critical_Damage *= Attack;

        Health.OnValueChanged += HandleHealthChanged;
        Level.OnValueChanged += HandleLevelChanged;

        UpdateHealthText(Health.Value);
        UpdateLevelText(Level.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Health.OnValueChanged -= HandleHealthChanged;
        Level.OnValueChanged -= HandleLevelChanged;
    }

    private void HandleHealthChanged(int previous, int current)
    {
        Debug.Log($"Health changed from {previous} to {current}");
        UpdateHealthText(current);
    }

    private void HandleLevelChanged(int previous, int current)
    {
        Debug.Log($"Level changed from {previous} to {current}");
        UpdateLevelText(current);
    }

    private void UpdateHealthText(int value)
    {
        if (healthText != null)
            healthText.text = $"Health: {value}";
    }

    private void UpdateLevelText(int value)
    {
        if (levelText != null)
            levelText.text = $"Level: {value}";
    }

    [ServerRpc (RequireOwnership = false)]
    public void takeDamageServerRpc()
    {
        Health.Value -= 2;
        if (Health.Value < 0) Health.Value = 0;
        Debug.Log("Damage taken");
    }

    [ServerRpc (RequireOwnership = false)]
    public void SetHealthServerRpc(int value)
    {
        Health.Value = value;
    }

    // Example: Change level on the server
    [ServerRpc (RequireOwnership = false)]
    public void SetLevelServerRpc(int value)
    {
        Level.Value = value;
    }
}
