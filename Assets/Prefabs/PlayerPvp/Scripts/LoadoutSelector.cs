// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections.Generic;
// using UnityEngine.SceneManagement;

// public class LoadoutSelector : MonoBehaviour
// {
//     public List<string> availableUnitNames; // Prefab names
//     public int maxFront = 3;
//     public int maxBack = 3;

//     public Transform frontPanel;
//     public Transform backPanel;
//     public GameObject unitButtonPrefab;

//     private List<string> selectedFrontline = new List<string>();
//     private List<string> selectedBackline = new List<string>();

//     void Start()
//     {
//         foreach (string unitName in availableUnitNames)
//         {
//             GameObject buttonGO = Instantiate(unitButtonPrefab, transform);
//             buttonGO.GetComponentInChildren<Text>().text = unitName;

//             Button btn = buttonGO.GetComponent<Button>();
//             btn.onClick.AddListener(() => OnUnitSelected(unitName));
//         }
//     }

//     void OnUnitSelected(string unitName)
//     {
//         if (selectedFrontline.Count < maxFront)
//         {
//             selectedFrontline.Add(unitName);
//             Debug.Log($"Added {unitName} to Frontline");
//         }
//         else if (selectedBackline.Count < maxBack)
//         {
//             selectedBackline.Add(unitName);
//             Debug.Log($"Added {unitName} to Backline");
//         }
//         else
//         {
//             Debug.Log("Loadout Full!");
//         }
//     }

//     public void OnStartBattleClicked()
//     {
//         LoadoutData.SelectedFrontline = new List<string>(selectedFrontline);
//         LoadoutData.SelectedBackline = new List<string>(selectedBackline);

//         SceneManager.LoadScene("BattleScene");
//     }
// }
