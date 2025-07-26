using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableUnit : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject unitPrefab;

    private GameObject dragPreview;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetPrefab(GameObject prefab)
    {
        unitPrefab = prefab;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (unitPrefab == null)
        {
            Debug.LogError("unitPrefab not assigned!");
            return;
        }

        // Create drag preview
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(canvas.transform, false);
        dragPreview.transform.SetAsLastSibling();

        Image image = dragPreview.AddComponent<Image>();
        image.sprite = unitPrefab.GetComponent<SpriteRenderer>()?.sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.SetNativeSize();

        RectTransform rt = dragPreview.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80); // Adjust as needed
        dragPreview.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview != null)
            dragPreview.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag called");
        if (dragPreview != null)
        {
            Debug.Log("Destroying dragPreview");
            Destroy(dragPreview);
        }
    }

    public void DestroyDragPreview()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }
}
