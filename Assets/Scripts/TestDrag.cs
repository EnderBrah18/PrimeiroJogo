using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TestDrag : MonoBehaviour, IBeginDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log(" OnBeginDrag chamado!");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(" OnEndDrag chamado!");
    }
}
