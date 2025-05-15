using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using static UnityEditor.Progress;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image icon;
    public TextMeshProUGUI quantityText;
    public int index; // posição do slot no inventário

    private InventoryItem currentItem;

    private GameObject dragIconObj;
    private RectTransform dragIconRect;
    private Canvas canvas;

    private void Awake()
    {
        // Pega o canvas pai para posicionar o ícone de arraste
        canvas = GetComponentInParent<Canvas>();
    }

    public void Set(InventoryItem item)
    {
        currentItem = item;
        icon.sprite = item.GetIcon();
        icon.enabled = true;
        quantityText.text = item.quantity.ToString();
        RefreshSlotUI();
    }

    public void Clear()
    {
        currentItem = null;
        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";
        RefreshSlotUI();
    }

    public InventoryItem GetItem()
    {
        return currentItem;
    }

    public bool HasItem()
    {
        return currentItem != null;
    }

    public InventoryItem GetCurrentItem()
    {
        return currentItem;
    }

    public void SetCurrentItem(InventoryItem item)
    {
        currentItem = item;
        if (item != null)
        {
            icon.sprite = item.GetIcon();
            icon.enabled = true;
            quantityText.text = item.quantity.ToString();
        }
        else
        {
            Clear();
        }
    }

    public void RefreshSlotUI()
    {
        if (currentItem != null)
        {
            // Atualiza ícone, quantidade, etc.
            // Exemplo: iconImage.sprite = item.icon;
            // quantidadeText.text = item.quantity.ToString();
        }
        else
        {
            // Limpa slot visualmente
        }
    }

    // Evento para informar o item selecionado
    public delegate void OnItemSelectedDelegate(InventoryItem item);
    public static event OnItemSelectedDelegate OnItemSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnItemSelected?.Invoke(currentItem);
        }
    }

    // --- Drag and Drop ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // Cria o objeto ícone que segue o mouse
        dragIconObj = new GameObject("DragIcon");
        dragIconObj.transform.SetParent(canvas.transform, false);
        dragIconObj.transform.SetAsLastSibling();

        dragIconRect = dragIconObj.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = icon.rectTransform.sizeDelta;

        Image dragImage = dragIconObj.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false; // para não bloquear o raycast

        // Texto da quantidade
        GameObject textObj = new GameObject("QuantityText");
        textObj.transform.SetParent(dragIconObj.transform, false);
        TextMeshProUGUI dragText = textObj.AddComponent<TextMeshProUGUI>();
        dragText.text = quantityText.text;
        dragText.fontSize = quantityText.fontSize;
        dragText.color = quantityText.color;
        dragText.alignment = TextAlignmentOptions.BottomRight;
        dragText.raycastTarget = false;
        dragText.rectTransform.anchorMin = new Vector2(0, 0);
        dragText.rectTransform.anchorMax = new Vector2(1, 1);
        dragText.rectTransform.offsetMin = Vector2.zero;
        dragText.rectTransform.offsetMax = Vector2.zero;

        UpdateDragIconPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            UpdateDragIconPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            Destroy(dragIconObj);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();

        if (draggedSlot == null) return;

        InventoryItem draggedItem = draggedSlot.GetCurrentItem();
        InventoryItem thisItem = this.GetCurrentItem();

        // Trocar os itens entre os slots
        this.SetCurrentItem(draggedItem);
        draggedSlot.SetCurrentItem(thisItem);
    }

    private void UpdateDragIconPosition(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos);
        dragIconRect.localPosition = pos;
    }
}
