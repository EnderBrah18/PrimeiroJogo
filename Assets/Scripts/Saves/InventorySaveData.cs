using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveData
{
    public List<string> itemIDs = new List<string>();
    public List<int> amounts = new List<int>();

    public InventorySaveData(Inventory inventory)
    {
        AddSlots(inventory.resourceSlots);
        AddSlots(inventory.equipmentSlots);
        AddSlots(inventory.consumableSlots);
        AddSlots(inventory.questItemSlots);
    }

    private void AddSlots(List<InventorySlot> slots)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null && slot.item != null)
            {
                itemIDs.Add(slot.item.id); // ou slot.item.name se não houver id
                amounts.Add(slot.quantity);
            }
        }
    }
}
