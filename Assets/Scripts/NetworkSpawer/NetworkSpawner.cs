using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkSpawner : MonoBehaviour
{
    [Header("Network Settings")]
    public int playersRequired = 2;
    public string gameSceneName = "gameScene";
    
    [Header("UI References")]
    public GameObject statusText;
    
    private int connectedPlayers = 0;
    private bool isInitialized = false;

    void Start()
    {
        // Reset connected players count
        connectedPlayers = 0;
    }

    public void Init()
    {
        if (!isInitialized)
        {
            SetupNetworkCallbacks();
            isInitialized = true;
            Debug.Log("Network callbacks initialized");
        }
    }

    private void SetupNetworkCallbacks()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
        
        // Only handle on host/server
        if (NetworkManager.Singleton.IsHost)
        {
            connectedPlayers = (int)NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"Connected players: {connectedPlayers}/{playersRequired}");
            
            UpdateUI($"Players connected: {connectedPlayers}/{playersRequired}");
            
            if (connectedPlayers >= playersRequired)
            {
                Debug.Log("Required players connected! Loading game scene...");
                LoadGameScene();
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
        
        // Only handle on host/server
        if (NetworkManager.Singleton.IsHost)
        {
            connectedPlayers = (int)NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"Connected players: {connectedPlayers}/{playersRequired}");
            
            UpdateUI($"Players connected: {connectedPlayers}/{playersRequired}");
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("Server started");
        // Only handle on host
        if (NetworkManager.Singleton.IsHost)
        {
            connectedPlayers = 1; // Host counts as first player
            UpdateUI($"Players connected: {connectedPlayers}/{playersRequired}");
        }
    }

    private void LoadGameScene()
    {
        // Only host can load scenes
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"Host loading scene: {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }

    private void UpdateUI(string message)
    {
        if (statusText != null)
        {
            // Assuming statusText has a Text component
            var textComponent = statusText.GetComponent<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
            }
            
            // Or if using TextMeshPro
            var tmpComponent = statusText.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                tmpComponent.text = message;
            }
        }
        
        Debug.Log($"UI Update: {message}");
    }

    private void OnDestroy()
    {
        // Clean up callbacks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}