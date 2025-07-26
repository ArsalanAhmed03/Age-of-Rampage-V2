using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    private Sprite SourceImage;
    private Image iconImage;
    public Button removeButton;
    private GameObject assignedPrefab;
    private SelectionScreenManager manager;
    public Image AreanaSlotImage;

    private void Start()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            SourceImage = iconImage.sprite;
        }

        if (removeButton == null)
        {
            removeButton = GetComponentInChildren<Button>(true);
        }

        if (manager == null)
        {
            manager = FindFirstObjectByType<SelectionScreenManager>();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableUnit dragged = eventData.pointerDrag?.GetComponent<DraggableUnit>();
        if (dragged == null) return;

        assignedPrefab = dragged.unitPrefab;
        LoadoutData.selectedUnits[slotIndex] = assignedPrefab;

        // Set icon
        Sprite unitSprite = assignedPrefab.GetComponent<SpriteRenderer>()?.sprite;
        if (iconImage != null && unitSprite != null)
        {
            iconImage.sprite = unitSprite;
            iconImage.color = Color.white;
            AreanaSlotImage.sprite = unitSprite;
            AreanaSlotImage.color = Color.white;
        }



        if (removeButton != null)
            removeButton.gameObject.SetActive(true);

        // Setup Remove logic
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() =>
        {
            RemoveUnit();
        });

        // Destroy dragged button
        dragged.DestroyDragPreview();
        Destroy(dragged.gameObject);
    }

    public void RemoveUnit()
    {
        if (assignedPrefab == null) return;

        LoadoutData.selectedUnits[slotIndex] = null;

        iconImage.sprite = SourceImage;
        AreanaSlotImage.sprite = SourceImage;
        iconImage.color = new Color32(0x6D, 0x6D, 0x6D, 0xFF);
        AreanaSlotImage.color = new Color32(0x6D, 0x6D, 0x6D, 0xFF);


        manager.ReAddUnitToGrid(assignedPrefab);

        assignedPrefab = null;
    }
}

