using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class InventoryDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static InventoryDragHandler draggedItem;

    public Image icon;
    private Transform originalParent;

    public void OnBeginDrag(PointerEventData eventData)
    {
        draggedItem = this;
        originalParent = transform.parent;
        transform.SetParent(transform.root); // Tira da hierarquia do layout
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        draggedItem = null;
    }
}
