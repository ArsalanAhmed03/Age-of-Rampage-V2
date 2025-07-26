using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.UI;

public class LocalSpawner : NetworkBehaviour
{
    [Header("Spawn Prefabs")]
    public List<NetworkObject> prefabsToSpawn;

    [Header("Side Spawn Parents")]
    public Transform leftSideParent;
    public Transform rightSideParent;

    [Header("UI Reference")]
    public Button spawnPlayersButton;
    public Button attackButton;


    private List<Transform> leftSpawns = new List<Transform>();
    private List<Transform> rightSpawns = new List<Transform>();

    // Track each client's assigned side
    private Dictionary<ulong, string> clientSides = new Dictionary<ulong, string>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        leftSpawns = GetChildren(leftSideParent);
        rightSpawns = GetChildren(rightSideParent);

        Debug.Log("NetworkSpawn: Left and right spawn points cached.");

        if (IsServer)
        {
            Debug.Log("NetworkSpawn: Assigning sides to clients (server only).");
            AssignSidesToClients();
        }

        if (spawnPlayersButton != null)
        {
            // Debug.Log("NetworkSpawn: Adding button listener (owner only).");
            spawnPlayersButton.onClick.AddListener(OnSpawnButtonClicked);
        }
        if(attackButton != null)
        {
            attackButton.onClick.AddListener(OnAttackButtonClicked);
        }
    }

    private void AssignSidesToClients()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        int leftCount = 0, rightCount = 0;
        foreach (var client in clients)
        {
            if (!clientSides.ContainsKey(client.ClientId))
            {
                string side;
                if (leftCount <= rightCount)
                {
                    side = "Left";
                    leftCount++;
                }
                else
                {
                    side = "Right";
                    rightCount++;
                }
                clientSides[client.ClientId] = side;
                Debug.Log($"AssignSidesToClients: Assigned Client {client.ClientId} to side {side}.");
            }
            else
            {
                // Count existing assignments
                if (clientSides[client.ClientId] == "Left") leftCount++;
                else rightCount++;
            }
        }
    }

    private List<Transform> GetChildren(Transform parent)
    {
        List<Transform> children = new List<Transform>();
        if (parent == null) return children;
        foreach (Transform child in parent)
            children.Add(child);
        return children;
    }

    private void OnSpawnButtonClicked()
    {
        Debug.Log("OnSpawnButtonClicked: Spawn button clicked, requesting spawn via ServerRpc.");
        // Only the owner calls this, so send a ServerRpc to request spawn
        RequestSpawnServerRpc();
    }

    private void OnAttackButtonClicked()
    {
        Debug.Log("OnAttackButtonClicked: Attack button clicked, requesting attack via ServerRpc.");
        RequestAttackServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSpawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        string side = GetClientSide(clientId);

        Debug.Log($"RequestSpawnServerRpc: Received spawn request from Client {clientId}, assigned side: {side}.");

        // Choose the correct spawn list
        List<Transform> spawnPoints = side == "Left" ? leftSpawns : rightSpawns;
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"RequestSpawnServerRpc: No spawn points available for side {side}.");
            return;
        }

        // Pick a random spawn point and remove it from the list
        int spawnIndex = Random.Range(0, spawnPoints.Count);
        Transform spawnPoint = spawnPoints[spawnIndex];
        spawnPoints.RemoveAt(spawnIndex);

        NetworkObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Count)];

        Debug.Log($"RequestSpawnServerRpc: Spawning prefab '{prefab.name}' for Client {clientId} at position {spawnPoint.position}.");

        NetworkObject spawnedObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        spawnedObj.SpawnAsPlayerObject(clientId, false);
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("RequestAttackServerRpc: Received attack request from Client.");
        ulong clientId = rpcParams.Receive.SenderClientId;
        string side = GetClientSide(clientId);
        // Find all spawned objects owned by this client
        NetworkObject[] allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (NetworkObject netObj in allNetworkObjects)
        {
            if (netObj.OwnerClientId == clientId)
            {
                PlayerAnimator animator = netObj.GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    Debug.Log($"RequestAttackServerRpc: Triggering attack animation for object {netObj.name}");
                    animator.PlayAttackClientRpc();
                }
            }
            else
            {
                PrefabStats prefabStats = netObj.GetComponent<PrefabStats>();
                if (prefabStats != null)
                {
                    Debug.Log($"RequestAttackServerRpc: Triggering attack animation for object {netObj.name} owned by another client.");
                    prefabStats.takeDamageServerRpc();
                }
            }
        }
    }


    private string GetClientSide(ulong clientId)
    {
        // If not assigned, assign now (shouldn't happen, but fallback)
        if (!clientSides.ContainsKey(clientId))
        {
            string side = Random.value < 0.5f ? "Left" : "Right";
            clientSides[clientId] = side;
            Debug.Log($"GetClientSide: Fallback assignment for Client {clientId} to side {side}.");
        }
        return clientSides[clientId];
    }
}
