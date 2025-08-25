using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int maxSlots = 20;
    public List<InventorySlot> slots = new List<InventorySlot>();

    public bool AddItem(ItemSO item, int amount = 1)
    {
        // Se já existe, aumenta a quantidade
        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                slot.quantity += amount;
                return true;
            }
        }
        // Se não existe, adiciona novo slot
        if (slots.Count < maxSlots)
        {
            slots.Add(new InventorySlot(item, amount));
            return true;
        }
        return false;
    }

    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item == item)
            {
                if (slots[i].quantity > amount)
                {
                    slots[i].quantity -= amount;
                }
                else
                {
                    slots.RemoveAt(i);
                }
                return true;
            }
        }
        return false;
    }
}