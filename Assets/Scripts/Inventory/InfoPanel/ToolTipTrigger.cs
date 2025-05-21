using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string content;
    public GameObject tooltipObject;
    public TextMeshProUGUI tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltipObject.SetActive(true);
        tooltipText.text = content;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipObject.SetActive(false);
    }
}
