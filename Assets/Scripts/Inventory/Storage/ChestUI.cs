using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestUI : MonoBehaviour
{
    public InventoryManager inventoryManager; // Referência ao InventoryManager
    public GameObject chestCanvas;
    public InventoryUI chestInventoryUI;
    public InventoryUI playerInventoryUI;
    public PlayerInventory playerInventory;

    public Transform chestSlotsParent;
    public GameObject slotPrefab;

    private Chest currentChest;

    public void OpenChest(Chest chest, Inventory playerInventory)
{
    if (playerInventory == null)
    {
        Debug.LogError("Player inventory is null in OpenChest.");
        return;
    }

    if (chest == null || chest.chestInventory == null)
    {
        Debug.LogError("Chest or chest inventory is null in OpenChest.");
        return;
    }

    currentChest = chest;
    chestCanvas.SetActive(true);
    inventoryManager.ToggleInventory();

    // Update UI with the chest's inventory
    chestInventoryUI.Setup(currentChest.chestInventory); // Ensure chestInventoryUI is properly set up
    chestInventoryUI.ShowRecursos(); // Show resources by default

    playerInventoryUI.Setup(playerInventory); // Ensure playerInventoryUI is properly set up
    playerInventoryUI.UpdateRecursosUI(this); // Pass the ChestUI reference

        UpdateChestSlots();

        Debug.Log("Chest opened!");
}

    public void CloseChest()
    {
        currentChest = null;
        chestCanvas.SetActive(false);
        inventoryManager.ToggleInventory();
    }

    public void TransferToChest(InventorySlot slot, int amount)
    {
        if (currentChest == null || slot == null || amount <= 0) return;

        bool isEquipment = slot.item.itemType == ItemType.Equipment; // Check the itemType

        // Add item to the chest inventory
        if (currentChest.chestInventory.AddItem(slot.item, amount, isEquipment))
        {
            // Remove item from the player inventory
            playerInventory.inventory.RemoveItem(slot.item, amount, isEquipment);

            // Update both UIs
            UpdateChestSlots();
            playerInventoryUI.UpdateRecursosUI(this);
        }
        else
        {
            Debug.Log("Failed to transfer item to chest. Chest might be full.");
        }
    }

    public void TransferToPlayer(InventorySlot slot, int amount)
    {
        if (currentChest == null || slot == null || amount <= 0) return;

        bool isEquipment = slot.item.itemType == ItemType.Equipment; // Check the itemType

        // Add item to the player inventory
        if (playerInventory.inventory.AddItem(slot.item, amount, isEquipment))
        {
            // Remove item from the chest inventory
            currentChest.chestInventory.RemoveItem(slot.item, amount, isEquipment);

            // Update both UIs
            UpdateChestSlots();
            playerInventoryUI.UpdateRecursosUI(this);
        }
        else
        {
            Debug.Log("Failed to transfer item to player. Inventory might be full.");
        }
    }

    private void UpdateChestSlots()
    {
        // Clear existing slots in the chest UI
        foreach (Transform child in chestSlotsParent)
        {
            Destroy(child.gameObject);
        }

        // Create slots for the chest inventory
        foreach (InventorySlot invSlot in currentChest.chestInventory.slots)
        {
            GameObject slotGO = Instantiate(slotPrefab, chestSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot, this); // Pass the ChestUI reference
        }
    }
}
