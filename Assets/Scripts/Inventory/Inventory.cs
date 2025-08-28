using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public int maxSlots;
    public float maxWeight;
    public float currentWeight;

    public List<InventorySlot> slots; // General list for all slots
    public List<InventorySlot> resourceSlots; // Separate list for resources
    public List<InventorySlot> equipmentSlots; // Separate list for equipment

    public Inventory(int resourceSlotsAmount = 20, int equipmentSlotsAmount = 10, float weight = 100f, bool isChest = false)
    {
        maxWeight = weight;
        currentWeight = 0f;

        if (isChest)
        {
            // Unified list for Chest Inventory
            maxSlots = resourceSlotsAmount + equipmentSlotsAmount;
            slots = new List<InventorySlot>();

            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new InventorySlot(null, 0));
            }
        }
        else
        {
            // Separate lists for Player Inventory
            maxSlots = resourceSlotsAmount + equipmentSlotsAmount;

            resourceSlots = new List<InventorySlot>();
            equipmentSlots = new List<InventorySlot>();

            for (int i = 0; i < resourceSlotsAmount; i++)
            {
                resourceSlots.Add(new InventorySlot(null, 0));
            }

            for (int i = 0; i < equipmentSlotsAmount; i++)
            {
                equipmentSlots.Add(new InventorySlot(null, 0));
            }
        }
    }

    public bool AddItem(ItemSO item, int amount = 1, bool isEquipment = false)
    {
        float totalWeight = item.Weight * amount;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Inventory full! Exceeded weight.");
            return false;
        }

        List<InventorySlot> targetSlots;

        if (slots != null)
        {
            // Unified Chest Inventory
            targetSlots = slots;
        }
        else
        {
            // Separate Player Inventory
            targetSlots = isEquipment ? equipmentSlots : resourceSlots;
        }

        // Try to stack in an existing slot
        for (int i = 0; i < targetSlots.Count; i++)
        {
            if (targetSlots[i].item == item)
            {
                targetSlots[i].quantity += amount;
                currentWeight += totalWeight;
                Debug.Log($"Item stacked in slot {i}. Quantity: {targetSlots[i].quantity}");
                return true;
            }
        }

        // Find an empty slot
        for (int i = 0; i < targetSlots.Count; i++)
        {
            if (targetSlots[i].item == null)
            {
                targetSlots[i].item = item;
                targetSlots[i].quantity = amount;
                currentWeight += totalWeight;
                Debug.Log($"Item added to empty slot {i}. Quantity: {targetSlots[i].quantity}");
                return true;
            }
        }

        Debug.Log("Inventory full! No empty slots.");
        return false;
    }

    public bool RemoveItem(ItemSO item, int amount = 1, bool isEquipment = false)
    {
        List<InventorySlot> targetSlots;

        if (slots != null)
        {
            // Unified Chest Inventory
            targetSlots = slots;
        }
        else
        {
            // Separate Player Inventory
            targetSlots = isEquipment ? equipmentSlots : resourceSlots;
        }

        for (int i = 0; i < targetSlots.Count; i++)
        {
            if (targetSlots[i].item == item)
            {
                if (targetSlots[i].quantity > amount)
                {
                    targetSlots[i].quantity -= amount;
                    currentWeight -= item.Weight * amount;
                    Debug.Log($"Item removed from slot {i}. Remaining Quantity: {targetSlots[i].quantity}");
                }
                else
                {
                    currentWeight -= item.Weight * targetSlots[i].quantity;
                    targetSlots[i].item = null;
                    targetSlots[i].quantity = 0;
                    Debug.Log($"Slot {i} is now empty.");
                }
                return true;
            }
        }
        return false;
    }

    public float GetCurrentWeight()
    {
        return currentWeight;
    }
}