using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public int maxSlots;
    public float maxWeight;
    public float currentWeight;
    public List<InventorySlot> slots;

    public Inventory(int slotsAmount = 20, float weight = 100f)
    {
        maxSlots = slotsAmount;
        maxWeight = weight;
        currentWeight = 0f;
        slots = new List<InventorySlot>();

        // Inicializa slots vazios
        for (int i = 0; i < slotsAmount; i++)
        {
            slots.Add(new InventorySlot(null, 0));
        }
    }

    public bool AddItem(ItemSO item, int amount = 1)
    {
        float totalWeight = item.Weight * amount;
        if (currentWeight + totalWeight > maxWeight)
        {
            Debug.Log("Inventário cheio! Peso excedido.");
            return false;
        }

        // Se já existe, aumenta a quantidade
        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                slot.quantity += amount;
                currentWeight += totalWeight;
                return true;
            }
        }
        // Se não existe, adiciona novo slot
        if (slots.Count < maxSlots)
        {
            slots.Add(new InventorySlot(item, amount));
            currentWeight += totalWeight;
            return true;
        }

        Debug.Log("Inventário cheio! Sem espaço em slots.");
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
                    currentWeight -= maxWeight;
                }
                else
                {
                    currentWeight -= item.Weight * slots[i].quantity;
                    slots.RemoveAt(i);
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