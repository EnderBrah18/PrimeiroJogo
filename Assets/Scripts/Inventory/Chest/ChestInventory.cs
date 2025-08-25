using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChestInventory : MonoBehaviour
{
    public Transform chestSlotsParent;
    public GameObject slotPrefab;
    public int chestSlotCount = 20;

    [Header("Referências")]
    public InventorySystem playerInventory;   // seu script que gerencia o inventário do player
    public InventorySlotUI playerDragHandler; // script que mostra item arrastando

    private List<InventorySlotUI> chestSlotList = new List<InventorySlotUI>();

    private void Awake()
    {
        if (chestSlotsParent == null)
            chestSlotsParent = transform.Find("ChestSlotsParent");

        SetupChestSlots();
    }

    private void SetupChestSlots()
    {
        chestSlotList.Clear();

        for (int i = 0; i < chestSlotCount; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, chestSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Clear();
            chestSlotList.Add(slotUI);
            slotUI.chestInventory = this;
            slotUI.inventorySystem = null;
        }
    }

    public List<InventorySlotUI> GetSlots() => chestSlotList;

    public bool AddItem(InventoryItem item)
    {
        InventorySlotUI existingSlot = chestSlotList.Find(s =>
            s.HasItem() &&
            s.GetCurrentItem().IsCollectable() &&
            s.GetCurrentItem().collectable.GetID() == item.collectable.GetID());

        if (existingSlot != null)
        {
            existingSlot.GetCurrentItem().quantity += item.quantity;
            existingSlot.RefreshSlotUI();
            return true;
        }

        InventorySlotUI emptySlot = chestSlotList.Find(s => !s.HasItem());
        if (emptySlot != null)
        {
            emptySlot.Set(item);
            return true;
        }

        Debug.Log("Baú cheio!");
        return false;
    }

    public void RemoveItem(InventoryItem item)
    {
        InventorySlotUI slot = chestSlotList.Find(s => s.HasItem() && s.GetCurrentItem() == item);
        if (slot != null)
        {
            slot.Clear();
        }
    }

    public void RefreshUI()
    {
        foreach (var slot in chestSlotList)
        {
            slot.RefreshSlotUI();
        }
    }

    public void TakeItemFromChest(InventorySlotUI chestSlot)
    {
        if (chestSlot == null || !chestSlot.HasItem()) return;

        InventoryItem item = chestSlot.GetCurrentItem();

        // Remove do baú
        chestSlot.Clear();

        // Adiciona ao inventário do player
        if (item.IsEquipment())
        {
            InventorySystem.Instance.AddEquipment(item.equipment);
        }
        else if (item.IsCollectable())
        {
            InventorySystem.Instance.AddCollectable(item.collectable);
        }

        InventorySystem.Instance.RefreshUI();

        // Encontrar o slot do player que recebeu o item
        InventorySlotUI playerSlot = InventorySystem.Instance.GetSlotWithItem(item); // você precisará criar esse método se não existir

        // Iniciar drag manualmente
        if (playerSlot != null)
        {
            // Simula o OnBeginDrag
            playerSlot.OnBeginDrag(new PointerEventData(EventSystem.current));
        }
    }
}
