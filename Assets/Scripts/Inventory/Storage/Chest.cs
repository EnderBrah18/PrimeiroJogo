using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public Inventory chestInventory;
    public int slotCount = 30; // Number of slots in the chest

    private void Awake()
    {
        // Initialize the chest inventory with a unified list of slots
        chestInventory = new Inventory(slotCount, 0, 100f, true);
    }
}
