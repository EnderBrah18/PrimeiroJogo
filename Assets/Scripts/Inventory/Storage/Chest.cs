using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public Inventory chestInventory;
    public int chestSize = 20;

    private void Awake()
    {
        chestInventory = new Inventory();
        chestInventory.maxSlots = chestSize;

        for (int i = 0; i < chestSize; i++)
        {
            chestInventory.slots.Add(new InventorySlot());
        }
    }
}
