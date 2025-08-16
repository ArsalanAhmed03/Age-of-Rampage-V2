using UnityEngine;
using UnityEngine.UI;

public class MainMenuUiManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject middlePanel;
    public GameObject loginPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button continueButton;
    public Button loginSignUpButton;

    public Button logOutButton;

    public databaseManager databaseManager; // Assign in inspector

    void Update()
    {
        continueButton.interactable = databaseManager.canAutoLogin;
        logOutButton.interactable = databaseManager.canAutoLogin;
    }

    void Start()
    {
        // Initial panel setup
        mainPanel.SetActive(true);
        middlePanel.SetActive(false);
        loginPanel.SetActive(false);

        // Button listeners
        startButton.onClick.AddListener(OnStartPressed);
        continueButton.onClick.AddListener(OnContinuePressed);
        loginSignUpButton.onClick.AddListener(OnLoginSignUpPressed);
        logOutButton.onClick.AddListener(OnLogOutPressed);

        // Set Continue button interactable based on canAutoLogin
        // continueButton.interactable = databaseManager.canAutoLogin;
    }

    void OnStartPressed()
    {
        databaseManager.SetUpDataBase();
        mainPanel.SetActive(false);
        middlePanel.SetActive(true);
        loginPanel.SetActive(false);

        // Update Continue button interactable in case canAutoLogin changed
    }

    void OnContinuePressed()
    {
        databaseManager.LoginHelper();
    }

    void OnLogOutPressed()
    {
        databaseManager.Logout();
    }

    void OnLoginSignUpPressed()
    {
        middlePanel.SetActive(false);
        loginPanel.SetActive(true);
    }
}
