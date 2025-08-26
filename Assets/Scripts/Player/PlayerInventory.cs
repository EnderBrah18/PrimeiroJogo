using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public Inventory inventory;

    void Awake()
    {
        inventory = new Inventory(20, 100f); // slots e peso do player
    }
}
