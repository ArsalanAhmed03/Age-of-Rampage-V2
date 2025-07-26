// using UnityEngine;
// using System.Threading.Tasks;
// using Firebase.Database;
// using Firebase.Extensions;
// using UnityEngine.UI;

// public class databaseManager : MonoBehaviour
// {
//     public class User
//     {
//         public string Name;
//         public int Gold;
//         public int Level;

//         public User() { }

//         public User(string name, int gold, int level)
//         {
//             Name = name;
//             Gold = gold;
//             Level = level;
//         }
//     }

//     private DatabaseReference dbRef;

//     public Text UserNameText;
//     public Text LevelText;
//     public Text GoldText;
//     public Text ErrorText;
//     public InputField UserNameInputField;
//     public Button LoginButton;
//     public Button SignupButton;

//     private void Awake()
//     {
//         dbRef = FirebaseDatabase.DefaultInstance.RootReference;
//         LoginButton.onClick.AddListener(OnLoginClicked);
//         SignupButton.onClick.AddListener(OnSignupClicked);
//     }

//     public void CreateUser(string userId, string userName)
//     {
//         User user = new User(userName, 0, 1);
//         string json = JsonUtility.ToJson(user);
//         dbRef.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
//             {
//                 ErrorText.text = "Signup successful!";
//                 FetchUser(userId, UpdateUIWithUser);
//             }
//             else
//             {
//                 ErrorText.text = "Signup failed!";
//             }
//         });
//     }

//     public void UpdateUser(string userId, User user)
//     {
//         string json = JsonUtility.ToJson(user);
//         dbRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
//     }

//     public void FetchUser(string userId, System.Action<User> onFetched)
//     {
//         dbRef.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
//         {
//             if (task.IsCompleted && task.Result.Exists)
//             {
//                 string json = task.Result.GetRawJsonValue();
//                 User user = JsonUtility.FromJson<User>(json);
//                 onFetched?.Invoke(user);
//             }
//             else
//             {
//                 ErrorText.text = "User not found!";
//                 onFetched?.Invoke(null);
//             }
//         });
//     }

//     private void UpdateUIWithUser(User user)
//     {
//         if (user != null)
//         {
//             UserNameText.text = user.Name;
//             LevelText.text = $"Level: {user.Level}";
//             GoldText.text = $"Gold: {user.Gold}";
//             ErrorText.text = "";
//         }
//     }

//     private void OnLoginClicked()
//     {
//         string userId = UserNameInputField.text.Trim();
//         if (string.IsNullOrEmpty(userId))
//         {
//             ErrorText.text = "Please enter a username.";
//             return;
//         }
//         FetchUser(userId, user =>
//         {
//             if (user != null)
//             {
//                 ErrorText.text = "Login successful!";
//                 UpdateUIWithUser(user);
//             }
//             else
//             {
//                 ErrorText.text = "User does not exist!";
//             }
//         });
//     }

//     private void OnSignupClicked()
//     {
//         string userId = UserNameInputField.text.Trim();
//         if (string.IsNullOrEmpty(userId))
//         {
//             ErrorText.text = "Please enter a username.";
//             return;
//         }
//         // Check if user already exists
//         FetchUser(userId, user =>
//         {
//             if (user != null)
//             {
//                 ErrorText.text = "User already exists!";
//             }
//             else
//             {
//                 CreateUser(userId, userId);
//             }
//         });
//     }
// }
