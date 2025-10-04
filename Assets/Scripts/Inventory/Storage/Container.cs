// 04/10/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class Container : MonoBehaviour
{
    [Header("Container Settings")]
    public string containerName = "Container";
    public int maxSlots = 20;
    public float maxWeight = 100f;

    [Header("Allowed Item Types")]
    public bool allowResources = true;
    public bool allowEquipment = true;
    public bool allowConsumables = true;
    public bool allowQuestItems = true;

    [Header("UI")]
    public GameObject containerUIPrefab; // Prefab for the container UI
    private GameObject containerUIInstance;
    private ContainerUI containerUIScript;

    public Inventory containerInventory;

    private void Awake()
    {
        // Initialize the container inventory
        containerInventory = new Inventory(maxSlots, maxWeight);

        // Optionally, pre-fill the container with items for testing
        Debug.Log($"{containerName} initialized with {maxSlots} slots and max weight {maxWeight}.");
    }

    public bool CanAcceptItem(ItemSO item)
    {
        // Check if the item type is allowed in this container
        switch (item.itemType)
        {
            case ItemType.Resource: return allowResources;
            case ItemType.Equipment: return allowEquipment;
            case ItemType.Consumable: return allowConsumables;
            case ItemType.QuestItem: return allowQuestItems;
            default: return false;
        }
    }

    public void OpenContainerUI(Inventory playerInventory)
    {
        if (containerUIPrefab == null)
        {
            Debug.LogError("Container UI Prefab is not assigned.");
            return;
        }

        if (containerUIInstance == null)
        {
            containerUIInstance = Instantiate(containerUIPrefab);
            containerUIScript = containerUIInstance.GetComponent<ContainerUI>();
            containerUIScript.Setup(this, playerInventory);
        }
        else
        {
            containerUIInstance.SetActive(true);
            // Atualiza o inventário caso já exista
            containerUIScript.Setup(this, playerInventory);
        }
    }

    public void CloseContainerUI()
    {
        if (containerUIScript != null)
        {
            containerUIScript.CloseUI();
        }
    }
}

