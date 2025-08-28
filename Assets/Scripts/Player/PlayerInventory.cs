using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public Inventory inventory;
    public InventoryUI inventoryUI;

    public int maxResourceSlots = 20;
    public int maxEquipmentSlots = 10;
    public float maxWeight = 100f;

    void Awake()
    {
        // Create the inventory instance with separate resource and equipment slots
        inventory = new Inventory(maxResourceSlots, maxEquipmentSlots, maxWeight, false);

        // Connect the UI to the inventory
        if (inventoryUI != null)
            inventoryUI.Setup(inventory);
    }
}

