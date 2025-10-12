using UnityEngine;
using UnityEngine.EventSystems;
public class ClickTest : MonoBehaviour, IPointerClickHandler
{
    private void OnMouseDown()
    {
        Debug.Log($"Clicked on: {gameObject.name}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"📸 Clicked or tapped on: {gameObject.name}");
    }
}
