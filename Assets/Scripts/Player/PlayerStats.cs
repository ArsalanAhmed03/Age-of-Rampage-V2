using UnityEngine;
using Unity.Netcode;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Points = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Health.OnValueChanged += OnHealthChanged;
        Points.OnValueChanged += OnPointsChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Health.OnValueChanged -= OnHealthChanged;
        Points.OnValueChanged -= OnPointsChanged;
    }

    private void OnHealthChanged(int previous, int current)
    {
        Debug.Log($"Health changed from {previous} to {current}");
    }

    private void OnPointsChanged(int previous, int current)
    {
        Debug.Log($"Points changed from {previous} to {current}");
    }
}