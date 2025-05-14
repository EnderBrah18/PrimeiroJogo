using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI quantityText;
    public bool isToolSlot; // Adicione isso
    public TextMeshProUGUI weightText;

    private InventoryItem currentItem;
    private System.Action<InventoryItem> onClickCallback;

    public void SetItem(InventoryItem item, System.Action<InventoryItem> onClick = null)
    {
        currentItem = item;
        icon.sprite = item.collectable?.GetIcon();
        icon.enabled = true;
        quantityText.text = item.quantity.ToString();

        // Exibir o peso do item
        weightText.text = $"Peso: {item.TotalWeight}kg";

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
}
