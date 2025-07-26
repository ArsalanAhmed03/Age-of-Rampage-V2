using UnityEngine;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("UI Windows")]
    public GameObject LoginWindow;
    public GameObject SignUpWindow;

    [Header("Switch Buttons")]
    public Button LoginButton;
    public Button SignUpButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoginButton.onClick.AddListener(ShowLogin);
        SignUpButton.onClick.AddListener(ShowSignUp);
        ShowLogin(); // Show login by default
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ShowLogin()
    {
        LoginWindow.SetActive(true);
        SignUpWindow.SetActive(false);
    }

    private void ShowSignUp()
    {
        LoginWindow.SetActive(false);
        SignUpWindow.SetActive(true);
    }
}
