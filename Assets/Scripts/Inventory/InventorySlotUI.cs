using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Dados do slot")]
    public InventorySlot slotData;          // referência ao slot real
    public Image icon;                      // ícone do item
    public TextMeshProUGUI amountText;      // quantidade do item

    [Header("Prefabs")]
    public GameObject placeholderPrefab;    // prefab simplificado só com moldura/fundo

    private CanvasGroup canvasGroup;
    private GameObject placeholder;         // objeto que mantém o espaço
    private GameObject dragIcon;            // ícone que segue o mouse
    private Transform parentAfterDrag;      // parent original do slot
    private ChestUI chestUI;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(InventorySlot data, ChestUI chestUIReference = null)
    {
        slotData = data;
        chestUI = chestUIReference;

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotData == null || slotData.item == null)
        {
            Debug.LogWarning("Cannot drag an empty slot.");
            return;
        }

        if (icon == null)
        {
            Debug.LogError("Icon is not assigned in the InventorySlotUI.");
            return;
        }

        if (dragIcon != null)
        {
            Debug.LogWarning("DragIcon already exists. Preventing duplicate creation.");
            return;
        }

        parentAfterDrag = transform.parent;

        // Create the drag icon
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(GetRootCanvas().transform, true); // Attach to the root canvas
        dragIcon.transform.SetAsLastSibling(); // Ensure it's rendered on top of other UI elements

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = icon.sprite;
        dragImage.raycastTarget = false;

        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).sizeDelta;
        rt.pivot = new Vector2(0.5f, 0.5f);
        dragIcon.transform.position = transform.position;

        // Hide the original slot visuals
        icon.enabled = false;
        amountText.gameObject.SetActive(false);
        canvasGroup.blocksRaycasts = false; // Disable raycasts for the slot while dragging
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null; // Reset the reference
        }

        // Restore the original slot visuals
        if (slotData != null && slotData.item != null)
        {
            icon.sprite = slotData.item.icon; // Restore the item's icon
            icon.enabled = true;
            amountText.text = slotData.quantity > 1 ? slotData.quantity.ToString() : "";
            amountText.gameObject.SetActive(true);
        }
        else
        {
            icon.sprite = null; // Clear the icon if the slot is empty
            icon.enabled = false;
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }

        canvasGroup.blocksRaycasts = true; // Re-enable raycasts for the slot
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI otherSlotUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (otherSlotUI == null || otherSlotUI == this)
            return;

        // Transfer player <-> chest
        bool isChestToPlayer = chestUI != null && otherSlotUI.chestUI == null;
        bool isPlayerToChest = chestUI == null && otherSlotUI.chestUI != null;

        if (isChestToPlayer)
        {
            chestUI.TransferToPlayer(otherSlotUI.slotData, otherSlotUI.slotData.quantity);
        }
        else if (isPlayerToChest)
        {
            chestUI.TransferToChest(otherSlotUI.slotData, otherSlotUI.slotData.quantity);
        }
        else
        {
            // Simple swap
            ItemSO tempItem = otherSlotUI.slotData.item;
            int tempQty = otherSlotUI.slotData.quantity;

            otherSlotUI.slotData.item = slotData.item;
            otherSlotUI.slotData.quantity = slotData.quantity;

            slotData.item = tempItem;
            slotData.quantity = tempQty;
        }

        // Update UI
        otherSlotUI.Setup(otherSlotUI.slotData, otherSlotUI.chestUI);
        Setup(slotData, chestUI);
    }

    private Canvas GetRootCanvas()
    {
        return GetComponentInParent<Canvas>();
    }
}

