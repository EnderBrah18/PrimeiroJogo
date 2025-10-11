using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    public int maxSlots;
    public float maxWeight;
    public float currentWeight;

    public List<InventorySlot> slots;
    public List<InventorySlot> resourceSlots;
    public List<InventorySlot> equipmentSlots;
    public List<InventorySlot> consumableSlots;
    public List<InventorySlot> questItemSlots;

    // Evento que será disparado quando o inventário mudar
    public event Action<ItemType> OnInventoryChanged;

    public Inventory(int resourceSlotsAmount = 20, int equipmentSlotsAmount = 10, int consumableSlotsAmount = 10, int questSlotsAmount = 5, float weight = 100f, bool isChest = false)
    {
        maxWeight = weight;
        currentWeight = 0f;

        if (isChest)
        {
            maxSlots = resourceSlotsAmount + equipmentSlotsAmount + consumableSlotsAmount + questSlotsAmount;
            slots = new List<InventorySlot>(maxSlots);
            for (int i = 0; i < maxSlots; i++)
                slots.Add(new InventorySlot(null, 0));
        }
        else
        {
            resourceSlots = CreateSlots(resourceSlotsAmount);
            equipmentSlots = CreateSlots(equipmentSlotsAmount);
            consumableSlots = CreateSlots(consumableSlotsAmount);
            questItemSlots = CreateSlots(questSlotsAmount);
        }
    }

    private List<InventorySlot> CreateSlots(int amount)
    {
        var list = new List<InventorySlot>(amount);
        for (int i = 0; i < amount; i++)
            list.Add(new InventorySlot(null, 0));
        return list;
    }

    private List<InventorySlot> GetTargetSlots(ItemSO item)
    {
        if (slots != null) return slots;

        switch (item.itemType)
        {
            case ItemType.Resource: return resourceSlots;
            case ItemType.Equipment: return equipmentSlots;
            case ItemType.Consumable: return consumableSlots;
            case ItemType.QuestItem: return questItemSlots;
            default: return null;
        }
    }

    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null) return false;

        float totalWeight = item.Weight * amount;
        if (currentWeight + totalWeight > maxWeight) return false;

        List<InventorySlot> targetSlots = GetTargetSlots(item);
        if (targetSlots == null) return false;

        // Empilha
        foreach (var slot in targetSlots)
        {
            if (slot == null) continue;
            if (slot.item == item)
            {
                slot.quantity += amount;
                currentWeight += totalWeight;

                OnInventoryChanged?.Invoke(item.itemType); // Dispara evento
                return true;
            }
        }

        // Coloca em slot vazio
        foreach (var slot in targetSlots)
        {
            if (slot == null) continue;
            if (slot.item == null)
            {
                slot.item = item;
                slot.quantity = amount;
                currentWeight += totalWeight;

                OnInventoryChanged?.Invoke(item.itemType); // Dispara evento
                return true;
            }
        }

        return false;
    }

    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        List<InventorySlot> targetSlots = GetTargetSlots(item);
        if (targetSlots == null) return false;

        for (int i = 0; i < targetSlots.Count; i++)
        {
            var slot = targetSlots[i];
            if (slot == null || slot.item != item) continue;

            if (slot.quantity > amount)
            {
                slot.quantity -= amount;
                currentWeight -= item.Weight * amount;
            }
            else
            {
                currentWeight -= item.Weight * slot.quantity;
                slot.item = null;
                slot.quantity = 0;
            }

            OnInventoryChanged?.Invoke(item.itemType); // Dispara evento
            return true;
        }

        return false;
    }

    public float GetCurrentWeight()
    {
        return currentWeight;
    }


}