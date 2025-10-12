using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class FilmCaptureTarget : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public UnitCombatHandler unitHandler;
    public RectTransform filmButtonUI; // Assign in inspector
    public GameObject filmSpritePrefab; // Assign in inspector
    public Transform worldCanvas; // Canvas for animation

    private bool isBeingCaptured = false;

    private void Awake()
    {
        if (unitHandler == null)
            unitHandler = GetComponent<UnitCombatHandler>();
    }

    private void OnMouseDown()
    {
        Debug.Log("FilmCaptureTarget clicked.");
        // Only allow during active battles
        if (BattleSystem.Instance == null || BattleSystem.Instance.fightEnded)
            return;

        // Check film count
        if (UserDataManager.Instance.Films <= 0)
        {
            Debug.Log("Not enough Films to capture.");
            return;
        }

        if (isBeingCaptured) return;
        isBeingCaptured = true;

        // Start visual + logic
        StartCoroutine(AnimateFilmAndCapture());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("📸 FilmCaptureTarget clicked or tapped.");
        // Only allow during active battles
        if (BattleSystem.Instance == null || BattleSystem.Instance.fightEnded)
            return;

        // Check film count
        if (UserDataManager.Instance.Films <= 0)
        {
            Debug.Log("Not enough Films to capture.");
            return;
        }

        if (isBeingCaptured) return;
        isBeingCaptured = true;

        // Start visual + logic
        StartCoroutine(AnimateFilmAndCapture());
    }

    private IEnumerator AnimateFilmAndCapture()
    {
        // Create animated film sprite
        GameObject filmObj = Instantiate(filmSpritePrefab, worldCanvas);
        RectTransform filmRect = filmObj.GetComponent<RectTransform>();

        Vector3 startPos = filmButtonUI.position;
        Vector3 endPos = Camera.main.WorldToScreenPoint(transform.position);

        filmRect.position = startPos;

        float duration = 0.6f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            filmRect.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, progress));
            yield return null;
        }

        Destroy(filmObj);
        yield return new WaitForSeconds(0.1f);

        AttemptCapture();
        isBeingCaptured = false;
    }

    private void AttemptCapture()
    {
        UserDataManager.Instance.Films -= 1;

        float missingHealthPercent = 1f - (unitHandler.unitStats.currentHP / (float)unitHandler.unitStats.MaxHP);
        float roll = Random.value;

        Debug.Log($"Attempting capture: chance={missingHealthPercent * 100f:F1}%, roll={roll * 100f:F1}%");

        if (roll <= missingHealthPercent)
        {
            string unitName = unitHandler.name.Replace("(Clone)", "").Trim();
            FirebaseUpdater.Instance.AddUnit(unitName);
            Debug.Log($"Capture success! Added {unitName} (Level 1) to collection.");
            BattleSystem.Instance.ShowFloatingText($"Captured {unitName}!", BattleSystem.Instance.messageStart.transform);


            foreach (var unitCheck in BattleSystem.Instance.playerFrontline)
            {
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "photobomb" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = Random.Range(1, 6) * 5;
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                        FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                        BattleSystem.Instance.ShowFloatingText($"Captured {capturedUnitName}!", BattleSystem.Instance.messageStart.transform);
                    }
                }
            }
            foreach (var unitCheck in BattleSystem.Instance.playerBackline)
            {
                if (unitCheck != null && unitCheck.specialAbility.ToLower() == "photobomb" && unitCheck.unitStats.GetStats().HP > 0)
                {
                    int randomNum = Random.Range(1, 6) * 5;
                    int chances = Random.Range(1, 101);
                    if (randomNum <= chances)
                    {
                        string capturedUnitName = unitCheck.name.Replace("(Clone)", "").Trim();
                        FirebaseUpdater.Instance.AddUnit(capturedUnitName);
                        BattleSystem.Instance.ShowFloatingText($"Captured {capturedUnitName}!", BattleSystem.Instance.messageStart.transform);
                    }
                }
            }
        }
        else
        {
            Debug.Log("Capture failed.");
            BattleSystem.Instance.ShowFloatingText("Capture failed!", BattleSystem.Instance.messageStart.transform);
        }


    }
}
