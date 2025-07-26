using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerPvp : MonoBehaviour
{
    public static GameManagerPvp Instance;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager between scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    /// <summary>
    /// Call this when the Start button is pressed (e.g., from UI button)
    /// </summary>
    public void StartGame()
    {
        SceneManager.LoadScene("Pvp"); // Make sure the scene name matches
    }

    /// <summary>
    /// Call this to return to Main Menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Make sure the scene name matches
    }

    /// <summary>
    /// Optional: Exit game
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
