using UnityEngine;
using UnityEngine.UI;

public class UnitButtonToggle : MonoBehaviour
{
    private bool isSelected = false;
    private GameObject selectionBorder;

    void Awake()
    {
        // Find child by name
        selectionBorder = transform.Find("SelectionBorder")?.gameObject;
        if (selectionBorder != null)
            selectionBorder.SetActive(false);

        // Hook up click
        GetComponent<Button>().onClick.AddListener(ToggleSelection);
    }

    void ToggleSelection()
    {
        isSelected = !isSelected;

        if (selectionBorder != null)
            selectionBorder.SetActive(isSelected);
    }

    public bool IsSelected() => isSelected;

    public void SetSelected(bool state)
    {
        isSelected = state;
        if (selectionBorder != null)
            selectionBorder.SetActive(isSelected);
    }
}
