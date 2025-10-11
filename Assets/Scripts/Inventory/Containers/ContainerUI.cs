using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ContainerUI : MonoBehaviour
{
    [Header("Container UI Elements")]
    public TextMeshProUGUI containerNameText;
    public TextMeshProUGUI weightText;
    public GameObject slotPrefab;
    public GameObject containerPanel;
    public Transform containerParent;

    private Container container;
    private Dictionary<ItemType, List<InventorySlotUI>> slotObjectsByType = new Dictionary<ItemType, List<InventorySlotUI>>();
    private InventorySlotUI selectedSlot;


    public void OpenContainerPanel(Container container)
    {
        this.container = container;
        SetupContainerUI();

        if (containerPanel != null)
            containerPanel.SetActive(true);
    }

    private void SetupContainerUI()
    {
        if (containerNameText != null)
            containerNameText.text = container.containerName;

        container.inventory.OnInventoryChanged += HandleInventoryChanged;

        UpdateUIForAllTypes();
    }

    private void HandleInventoryChanged(ItemType type)
    {
        UpdateUIForType(type);
    }

    private void UpdateUIForAllTypes()
    {
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            UpdateUIForType(type);
        }
    }

    private void UpdateUIForType(ItemType type)
    {
        if (container == null || container.inventory == null) return;

        if (!slotObjectsByType.ContainsKey(type))
            slotObjectsByType[type] = new List<InventorySlotUI>();

        foreach (var slotUI in slotObjectsByType[type])
            Destroy(slotUI.gameObject);

        slotObjectsByType[type].Clear();

        List<InventorySlot> slots = GetSlotsForType(type);
        if (slots == null) return;

        foreach (var invSlot in slots)
        {
            if (invSlot == null) continue;

            GameObject slotGO = Instantiate(slotPrefab, containerParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot);

            slotObjectsByType[type].Add(slotUI);

            // Debug: imprimir slot criado
            Debug.Log($"Slot criado: {invSlot.item?.itemName ?? "Vazio"} (Quantidade: {invSlot.quantity})");
        }

        UpdateWeightUI();
    }

    private List<InventorySlot> GetSlotsForType(ItemType type)
    {
        if (container.inventory.slots != null) return container.inventory.slots; // Baú usa lista unificada

        switch (type)
        {
            case ItemType.Resource: return container.inventory.resourceSlots;
            case ItemType.Equipment: return container.inventory.equipmentSlots;
            case ItemType.Consumable: return container.inventory.consumableSlots;
            case ItemType.QuestItem: return container.inventory.questItemSlots;
            default: return null;
        }
    }

    private void UpdateWeightUI()
    {
        if (weightText != null && container != null && container.inventory != null)
            weightText.text = $"Peso: {container.inventory.GetCurrentWeight():0.0} / {container.inventory.maxWeight}";
    }
}
