using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI Elements")]
    public Image icon;
    public TextMeshProUGUI quantityText;

    public int index; // posição do slot no inventário

    //  Tornado 'protected' para herança
    protected InventoryItem currentItem;
    public InventorySystem inventorySystem;
    public ChestInventory chestInventory;

    //  Tornado 'protected' para subclasse usar
    protected GameObject dragIconObj;
    protected RectTransform dragIconRect;
    protected Canvas canvas;

    protected virtual void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public virtual void Set(InventoryItem item)
    {
        currentItem = item;
        icon.sprite = item.GetIcon();
        icon.enabled = true;
        quantityText.text = item.quantity.ToString();
        RefreshSlotUI();
    }

    public virtual void Clear()
    {
        currentItem = null;
        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";
        RefreshSlotUI();
    }

    public InventoryItem GetItem() => currentItem;

    public bool HasItem() => currentItem != null;

    public InventoryItem GetCurrentItem() => currentItem;

    public virtual void SetCurrentItem(InventoryItem item)
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

    public virtual void RefreshSlotUI()
    {
        // Pode ser sobrescrito para adicionar visuais extras em subclasses
    }

    // Evento para informar o item selecionado
    public delegate void OnItemSelectedDelegate(InventoryItem item);
    public static event OnItemSelectedDelegate OnItemSelected;

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            OnItemSelected?.Invoke(currentItem);
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

        dragIconObj = new GameObject("DragIcon");
        dragIconObj.transform.SetParent(canvas.transform, false);
        dragIconObj.transform.SetAsLastSibling();

        dragIconRect = dragIconObj.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = icon.rectTransform.sizeDelta;

        Image dragImage = dragIconObj.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false;

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

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            UpdateDragIconPosition(eventData);
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (dragIconObj != null)
        {
            Destroy(dragIconObj);
        }
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (draggedSlot == null || draggedSlot == this) return;

        InventoryItem draggedItem = draggedSlot.GetCurrentItem();
        InventoryItem thisItem = this.GetCurrentItem();

        draggedSlot.SetCurrentItem(thisItem);
        this.SetCurrentItem(draggedItem);

        if (inventorySystem != null)
        {
            inventorySystem.RecalculateWeight();
            inventorySystem.RefreshUI();
        }

        if (draggedSlot.inventorySystem != null)
        {
            draggedSlot.inventorySystem.RecalculateWeight();
            draggedSlot.inventorySystem.RefreshUI();
        }
    }

    protected float GetItemWeight(InventoryItem item)
    {
        if (item.IsCollectable())
        {
            var collectable = item.collectable as CollectableObject;
            if (collectable != null)
                return collectable.weight * item.quantity;
        }
        return 0f;
    }

    protected void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (canvas == null || dragIconRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 pos);

        dragIconRect.localPosition = pos;
    }
}
