using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[System.Serializable]
public class StartingItem
{
    public ItemSO item;
    public int amount;
}

public class PlayerInventory : MonoBehaviour
{
    public Inventory inventory;
    public InventoryUI inventoryUI;

    public int maxResourceSlots = 20;
    public int maxEquipmentSlots = 10;
    public int maxConsumableSlots = 10;
    public int maxQuestItemSlots = 5;
    public float maxWeight = 100f;

    // Lista de itens iniciais configuráveis no Inspector
    public List<StartingItem> startingItems;

    void Awake()
    {
        // Create the inventory instance with separate resource and equipment slots
        inventory = new Inventory(maxResourceSlots, maxEquipmentSlots);

        foreach (var entry in startingItems)
        {
            if (entry.item != null)
                inventory.AddItem(entry.item, entry.amount);
        }

        // Connect the UI to the inventory
        if (inventoryUI != null)
            inventoryUI.Setup(inventory);
    }
}

