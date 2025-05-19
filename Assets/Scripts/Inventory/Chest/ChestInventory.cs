using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInventory : MonoBehaviour
{
    public Transform chestSlotsParent;
    public GameObject slotPrefab;
    public int chestSlotCount = 20;

    private List<InventorySlotUI> chestSlotList = new List<InventorySlotUI>();

    private void Awake()
    {
        SetupChestSlots();
    }

    private void SetupChestSlots()
    {
        for (int i = 0; i < chestSlotCount; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, chestSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Clear();
            chestSlotList.Add(slotUI);
        }
    }

    public List<InventorySlotUI> GetSlots() => chestSlotList;
}
