using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.GridLayoutGroup;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    public enum InventoryOwnerType
    {
        Player,
        Chest,
        NPC
    }

    [Header("Slot Data")]
    public InventorySlot slotData;          // Reference to the actual slot
    public Image icon;                      // Item icon
    public TextMeshProUGUI amountText;      // Item quantity text

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private InventoryUI parentUI;

    public InventoryOwnerType ownerType = InventoryOwnerType.Player;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(InventorySlot data, InventoryUI parent = null, InventoryOwnerType owner = InventoryOwnerType.Player)
    {

        slotData = data;
        parentUI = parent;
        ownerType = owner;

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotData == null || slotData.item == null || parentUI == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            parentUI.ShowItemActions(slotData, transform.position, ownerType);
        }
    }

}


