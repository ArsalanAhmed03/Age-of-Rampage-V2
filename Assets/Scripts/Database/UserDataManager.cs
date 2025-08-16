using System.Collections.Generic;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;

    public string UserName;
    public int Gold;
    public int Level;
    public List<string> OwnedUnits = new List<string>();
    public List<int> OwnedUnitsLevels = new List<int>();
    public List<string> LoadOut = new List<string>();
    public string ProfilePictureURL;
    public int Wins;
    public bool isLoggedIn = false;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
    }

    public int GetUnitIndexByName(string unitName)
    {
        return OwnedUnits.IndexOf(unitName);
    }
}
