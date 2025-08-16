using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using Firebase.Extensions; // make sure this is at the top
using UnityEngine.Networking;


public class TournamentManager : MonoBehaviour
{
    public static TournamentManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsTournamentActive = false;

    public void StartTournament()
    {
        IsTournamentActive = true;
    }

    public void EndTournament()
    {
        IsTournamentActive = false;
        BackToStartScreen();
    }

    [Header("Screen References")]
    public GameObject startScreen;
    public GameObject tournamentScreen;

    [Header("UI")]

    [SerializeField] private Button tournamentButton;
    [SerializeField] private TextMeshProUGUI timerText;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    private DateTime nextStartTimeUtc;
    private DateTime nextEndTimeUtc;
    private bool tournamentActive = false;

    [Header("Leaderboard UI")]
    [SerializeField] private Transform leaderboardContent; // Parent for entries
    [SerializeField] private GameObject leaderboardEntryPrefab; // Prefab
    [SerializeField] private TextMeshProUGUI playerCountText; // Text showing count

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        // Example: Friday 00:00 UTC and Tuesday 00:00 UTC
        CalculateNextTournamentTimes();

        tournamentButton.onClick.AddListener(OnTournamentButton);
        InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
    }

    public void OpenTournamentScreen()
    {
        startScreen.SetActive(false);
        tournamentScreen.SetActive(true);

        LoadLeaderboard();
    }

    // Optional back button
    public void BackToStartScreen()
    {
        tournamentScreen.SetActive(false);
        startScreen.SetActive(true);

        foreach (Transform child in leaderboardContent)
            Destroy(child.gameObject);

    }

    void CalculateNextTournamentTimes()
    {
        DateTime utcNow = DateTime.UtcNow;

        // All possible start days
        DateTime nextTuesday = GetUtcDateFor(DayOfWeek.Tuesday);
        DateTime nextFriday = GetUtcDateFor(DayOfWeek.Friday);

        // If today is Tuesday or Friday, check if we’re in the active 24h window
        bool isTournamentDay = (utcNow.Date == nextTuesday.Date || utcNow.Date == nextFriday.Date);
        if (isTournamentDay)
        {
            DateTime startTime = utcNow.Date; // midnight UTC today
            DateTime endTime = startTime.AddHours(24);

            if (utcNow >= startTime && utcNow < endTime)
            {
                // Tournament is active right now
                tournamentActive = true;
                nextStartTimeUtc = startTime;
                nextEndTimeUtc = endTime;
                return;
            }
        }

        // Not currently active → find the next start
        if (utcNow < nextTuesday)
        {
            nextStartTimeUtc = nextTuesday;
        }
        else if (utcNow < nextFriday)
        {
            nextStartTimeUtc = nextFriday;
        }
        else
        {
            // Past Friday this week → next Tuesday
            nextStartTimeUtc = nextTuesday.AddDays(7);
        }

        nextEndTimeUtc = nextStartTimeUtc.AddHours(24);
        tournamentActive = false;
    }

    DateTime GetUtcDateFor(DayOfWeek day)
    {
        DateTime now = DateTime.UtcNow;
        int daysUntil = ((int)day - (int)now.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 0; // Keep today if we're calculating for today
        return now.Date.AddDays(daysUntil);
    }

    void UpdateTimer()
    {
        DateTime utcNow = DateTime.UtcNow;
        if (tournamentActive)
        {
            TimeSpan remaining = nextEndTimeUtc - utcNow;
            timerText.text = $"Ends in: {remaining:hh\\:mm\\:ss}";
        }
        else
        {
            TimeSpan untilStart = nextStartTimeUtc - utcNow;
            timerText.text = $"Starts in: {untilStart:hh\\:mm\\:ss}";
        }
    }


    void OnTournamentButton()
    {
        if (!tournamentActive)
        {
            Debug.Log("Tournament not active yet!");
            return;
        }

        JoinTournamentRoom();
    }

    // void JoinTournamentRoom()
    // {
    //     OpenTournamentScreen();
    //     string uid = auth.CurrentUser.UserId;

    //     // Calculate total unit level from PlayerStatsManager.OwnedUnitsLevels
    //     int totalUnitLevel = 0;
    //     foreach (int level in PlayerStatsManager.Instance.OwnedUnitsLevels)
    //     {
    //         totalUnitLevel += level;
    //     }

    //     // Check if already assigned to a room
    //     dbRef.Child("TournamentRooms").GetValueAsync().ContinueWith(task =>
    //     {
    //         if (task.IsFaulted) { Debug.LogError(task.Exception); return; }
    //         DataSnapshot snap = task.Result;

    //         string myRoomId = "";
    //         foreach (var room in snap.Children)
    //         {
    //             if (room.Child("Players").HasChild(uid))
    //             {
    //                 myRoomId = room.Key;
    //                 break;
    //             }
    //         }

    //         if (string.IsNullOrEmpty(myRoomId))
    //         {
    //             // Assign to a room with < 50 players and similar total level
    //             foreach (var room in snap.Children)
    //             {
    //                 int count = (int)room.Child("Players").ChildrenCount;
    //                 int avgLevel = int.Parse(room.Child("AvgLevel").Value.ToString());

    //                 if (count < 50 && Mathf.Abs(avgLevel - totalUnitLevel) <= 10)
    //                 {
    //                     myRoomId = room.Key;
    //                     break;
    //                 }
    //             }

    //             if (string.IsNullOrEmpty(myRoomId))
    //             {
    //                 // Create a new room
    //                 myRoomId = dbRef.Child("TournamentRooms").Push().Key;
    //                 dbRef.Child("TournamentRooms").Child(myRoomId).Child("AvgLevel").SetValueAsync(totalUnitLevel);
    //             }

    //             // Add player entry with both level and initial damage
    //             dbRef.Child("TournamentRooms").Child(myRoomId).Child("Players").Child(uid)
    //                 .Child("TotalLevel").SetValueAsync(totalUnitLevel);
    //             dbRef.Child("TournamentRooms").Child(myRoomId).Child("Players").Child(uid)
    //                 .Child("Damage").SetValueAsync(0);
    //         }

    //         PlayerPrefs.SetString("TournamentRoomID", myRoomId);
    //         Debug.Log("TournamentRoomID: " + PlayerPrefs.GetString("TournamentRoomID", ""));
    //     });
    // }


    void JoinTournamentRoom()
    {
        string uid = auth.CurrentUser.UserId;

        // Calculate total unit level from PlayerStatsManager.OwnedUnitsLevels
        int totalUnitLevel = 0;
        foreach (int level in PlayerStatsManager.Instance.OwnedUnitsLevels)
        {
            totalUnitLevel += level;
        }

        // Check if already assigned to a room
        dbRef.Child("TournamentRooms").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError(task.Exception); return; }
            DataSnapshot snap = task.Result;

            string myRoomId = "";
            foreach (var room in snap.Children)
            {
                if (room.Child("Players").HasChild(uid))
                {
                    myRoomId = room.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(myRoomId))
            {
                // Assign to a room with < 50 players and similar total level
                foreach (var room in snap.Children)
                {
                    int count = (int)room.Child("Players").ChildrenCount;
                    int avgLevel = int.Parse(room.Child("AvgLevel").Value.ToString());

                    if (count < 50 && Mathf.Abs(avgLevel - totalUnitLevel) <= 10)
                    {
                        myRoomId = room.Key;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(myRoomId))
                {
                    // Create a new room
                    myRoomId = dbRef.Child("TournamentRooms").Push().Key;
                    dbRef.Child("TournamentRooms").Child(myRoomId).Child("AvgLevel").SetValueAsync(totalUnitLevel);
                }

                // Add player entry with both level and initial damage
                dbRef.Child("TournamentRooms").Child(myRoomId).Child("Players").Child(uid)
                    .Child("TotalLevel").SetValueAsync(totalUnitLevel);
                dbRef.Child("TournamentRooms").Child(myRoomId).Child("Players").Child(uid)
                    .Child("Damage").SetValueAsync(0);
            }

            PlayerPrefs.SetString("TournamentRoomID", myRoomId);
            PlayerPrefs.Save(); // make sure it’s committed
            Debug.Log("TournamentRoomID: " + PlayerPrefs.GetString("TournamentRoomID", ""));
            OpenTournamentScreen();

        });
    }

    public void AddTournamentDamage(int sessionDamage)
    {
        string uid = auth.CurrentUser.UserId;
        Debug.Log($"Tournament UID: {uid}");
        string myRoomId = PlayerPrefs.GetString("TournamentRoomID", "");

        if (string.IsNullOrEmpty(myRoomId))
        {
            Debug.LogError("Player is not in a tournament room!");
            return;
        }

        // Path: TournamentRooms/{roomId}/Players/{uid}/Damage
        DatabaseReference damageRef = dbRef.Child("TournamentRooms").Child(myRoomId).Child("Players").Child(uid).Child("Damage");

        damageRef.RunTransaction(mutableData =>
        {
            int currentDamage = 0;

            if (mutableData.Value != null)
            {
                currentDamage = Convert.ToInt32(mutableData.Value);
            }

            // Add this session's damage
            mutableData.Value = currentDamage + sessionDamage;
            return TransactionResult.Success(mutableData);

        }).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to update tournament damage: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                Debug.Log("Successfully updated damage by +" + sessionDamage);
            }
        });
    }


    public void LoadLeaderboard()
    {
        string roomId = PlayerPrefs.GetString("TournamentRoomID", "");
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("No TournamentRoomID found in PlayerPrefs");
            return;
        }

        dbRef.Child("TournamentRooms").Child(roomId).Child("Players").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError(task.Exception); return; }
            DataSnapshot snap = task.Result;

            List<PlayerLeaderboardData> players = new List<PlayerLeaderboardData>();
            foreach (var player in snap.Children)
            {
                string uid = player.Key;
                int totalLevel = player.Child("TotalLevel").Exists ? int.Parse(player.Child("TotalLevel").Value.ToString()) : 0;
                int damage = player.Child("Damage").Exists ? int.Parse(player.Child("Damage").Value.ToString()) : 0;

                players.Add(new PlayerLeaderboardData(uid, "Loading...", null, totalLevel, damage));
            }

            // Sort by damage descending
            players.Sort((a, b) => b.Damage.CompareTo(a.Damage));

            // Clear old leaderboard entries
            foreach (Transform child in leaderboardContent)
                Destroy(child.gameObject);

            // Create UI entries
            foreach (var p in players)
            {
                GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);

                Image profileImg = entry.transform.GetChild(0).GetComponent<Image>();
                TextMeshProUGUI nameTxt = entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI dmgTxt = entry.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

                nameTxt.text = "Loading...";
                dmgTxt.text = $"Damage Done: {p.Damage:N0}";
                profileImg.sprite = GetDefaultProfile();

                // Fetch user info async
                dbRef.Child("users").Child(p.UID).GetValueAsync().ContinueWithOnMainThread(userTask =>
                {
                    if (userTask.IsFaulted || !userTask.IsCompleted) return;

                    DataSnapshot userSnap = userTask.Result;
                    string username = userSnap.Child("Username").Exists ? userSnap.Child("Username").Value.ToString() : "Unknown";
                    string picUrl = userSnap.Child("ProfilePictureURL").Exists ? userSnap.Child("ProfilePictureURL").Value.ToString() : "";

                    nameTxt.text = username;

                    if (!string.IsNullOrEmpty(picUrl))
                        StartCoroutine(LoadProfilePicture(picUrl, profileImg));
                });
            }

            // Update count
            playerCountText.text = "Players: " + players.Count + "/50";
        });
    }

    private System.Collections.IEnumerator LoadProfilePicture(string url, Image targetImg)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Failed to load profile picture: " + request.error);
            yield break;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(request);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        targetImg.sprite = sprite;
    }


    private Sprite GetDefaultProfile()
    {
        return null; // TODO: load real profile pics later
    }

    private class PlayerLeaderboardData
    {
        public string UID;
        public string Name;
        public Sprite ProfileSprite;
        public int TotalLevel;
        public int Damage;

        public PlayerLeaderboardData(string uid, string name, Sprite profileSprite, int totalLevel, int damage)
        {
            UID = uid;
            Name = name;
            ProfileSprite = profileSprite;
            TotalLevel = totalLevel;
            Damage = damage;
        }
    }



}
