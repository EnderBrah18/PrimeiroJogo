using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image icon;
    public TextMeshProUGUI quantityText;
    public bool isToolSlot; // Adicione isso

    private InventoryUIManager inventoryUI;

    private InventoryItem currentItem;
    private System.Action<InventoryItem> onClickCallback;

    public void SetInventoryUI(InventoryUIManager ui)
    {
        inventoryUI = ui;  // Guarda essa referência para usar no clique
    }

    public void SetItem(InventoryItem item, System.Action<InventoryItem> onClick = null)
    {
        currentItem = item;
        icon.sprite = item.collectable?.GetIcon();
        icon.enabled = true;
        quantityText.text = item.quantity.ToString();

        onClickCallback = onClick;
    }

    public InventoryItem GetItem()
    {
        return currentItem;
    }

    public void Clear()
    {
        icon.sprite = null;
        quantityText.text = "";
        currentItem = null;
    }

    public void OnClick()
    {
        if (currentItem != null)
            onClickCallback?.Invoke(currentItem);
    }

    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false; // Opcional: esconde o ícone
        quantityText.text = "";
        currentItem = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && currentItem != null)
        {
            inventoryUI?.OnItemSelected(currentItem); // Chama o método que ativa o Details Panel
        }
    }
}
