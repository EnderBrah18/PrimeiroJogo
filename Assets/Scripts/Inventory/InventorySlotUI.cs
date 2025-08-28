using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot Data")]
    public InventorySlot slotData;          // Reference to the actual slot
    public Image icon;                      // Item icon
    public TextMeshProUGUI amountText;      // Item quantity text

    private ChestUI chestUI;
    private InventoryUI inventoryUI;

    public void Setup(InventorySlot data, ChestUI chestUIReference = null, InventoryUI inventoryUIReference = null)
    {
        slotData = data;
        chestUI = chestUIReference;
        inventoryUI = inventoryUIReference;

        if (data != null && data.item != null)
        {
            icon.sprite = data.item.icon;
            icon.enabled = true;
            amountText.text = data.quantity > 1 ? data.quantity.ToString() : "";
            amountText.gameObject.SetActive(true);
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }
    }
}


