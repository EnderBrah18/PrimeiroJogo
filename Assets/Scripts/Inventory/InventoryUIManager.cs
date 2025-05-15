using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance;

    public Transform slotsParent; // Painel com os 20 slots
    public GameObject slotPrefab; // Prefab do Slot com ícone e quantidade
    public List<InventorySlotUI> slotList = new List<InventorySlotUI>();
    public int maxSlots = 20;
    [SerializeField] private GameObject inventoryPanel;

    private void Awake()
    {
        inventoryPanel.SetActive(true);
        Instance = this;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotList.Add(slotUI);
        }

        RefreshUI(new List<InventoryItem>());

        inventoryPanel.SetActive(false);
    }

    public void RefreshUI(List<InventoryItem> inventory)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].index = i; // IMPORTANTE para drag & drop

            if (i < inventory.Count)
                slotList[i].Set(inventory[i]);
            else
                slotList[i].Clear();
        }
    }
}
