using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInventory : MonoBehaviour
{
    public Transform chestSlotsParent;
    public GameObject slotPrefab;
    public int chestSlotCount = 20;

    private List<InventorySlotUI> chestSlotList = new List<InventorySlotUI>();

    private void Awake()
    {
        SetupChestSlots();
    }

    private void SetupChestSlots()
    {
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
        // Procura slot vazio ou slot com mesmo item para empilhar
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
}
