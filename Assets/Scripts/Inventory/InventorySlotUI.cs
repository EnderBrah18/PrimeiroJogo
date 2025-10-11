using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot Data")]
    public InventorySlot slotData;          // Reference to the actual slot
    public Image icon;                      // Item icon
    public TextMeshProUGUI amountText;      // Item quantity text

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private InventoryUI parentUI;
    private bool isPlayerInventory;        // Flag to determine if this slot belongs to the player inventory

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(InventorySlot data, InventoryUI parent = null, bool isPlayer = false)
    {
        slotData = data;
        parentUI = parent;
        isPlayerInventory = isPlayer;

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


