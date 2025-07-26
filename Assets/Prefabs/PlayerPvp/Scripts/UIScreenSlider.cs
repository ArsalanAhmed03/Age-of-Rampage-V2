using UnityEngine;

public class UIScreenSlider : MonoBehaviour
{
    public static UIScreenSlider Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [Header("References")]
    public RectTransform screenContainer;
    private RectTransform[] screens;

    [Header("Settings")]
    public float slideSpeed = 10f;

    private Vector2 targetPos;
    private float screenWidth;


    void Start()
    {
        // Get current screen width based on parent canvas size
        screenWidth = GetComponent<RectTransform>().rect.width;
        screens = new RectTransform[screenContainer.childCount];

        for (int i = 0; i < screenContainer.childCount - 1; i++)
        {
            screens[i] = screenContainer.GetChild(i) as RectTransform;
        }

        for (int i = 0; i < screens.Length - 2; i++)
        {
            RectTransform screen = screens[i];
            screen.anchoredPosition = new Vector2(i * screenWidth, 0);
            screen.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, screenWidth);
        }

        RectTransform buyScreentemp = screenContainer.GetChild(screenContainer.childCount - 2) as RectTransform;
        buyScreentemp.anchoredPosition = new Vector2(0 * screenWidth, 0);

        RectTransform selectionScreentemp = screenContainer.GetChild(screenContainer.childCount - 1) as RectTransform;
        selectionScreentemp.anchoredPosition = new Vector2(1 * screenWidth, 0);

        targetPos = screenContainer.anchoredPosition;
    }

    void Update()
    {
        // Smooth slide animation
        screenContainer.anchoredPosition = Vector2.Lerp(screenContainer.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
    }

    public void GoToScreen(int index)
    {
        // Set new target position
        targetPos = new Vector2(-index * screenWidth, 0);
    }
}
