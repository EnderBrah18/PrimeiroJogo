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


    public void OpenContainerPanel(Container container)
    {
        this.container = container;

        if (container == null || container.inventory == null)
        {
            Debug.LogWarning("[ContainerUI] Container ou inventário inválido!");
            return;
        }

        SetupContainerUI();

        if (containerPanel != null)
            containerPanel.SetActive(true);

        Debug.Log($"[ContainerUI] Painel aberto para: {container.containerName}");
    }

    public void CloseContainerPanel()
    {
        if (container != null && container.inventory != null)
            container.inventory.OnInventoryChanged -= HandleInventoryChanged;

        foreach (var slotList in slotObjectsByType.Values)
        {
            foreach (var slotUI in slotList)
                if (slotUI != null)
                    Destroy(slotUI.gameObject);
        }

        slotObjectsByType.Clear();

        if (containerPanel != null)
            containerPanel.SetActive(false);

        Debug.Log("[ContainerUI] Painel fechado.");

        container = null;
    }

    private void SetupContainerUI()
    {
        if (containerNameText != null)
            containerNameText.text = container.containerName;

        container.inventory.OnInventoryChanged += HandleInventoryChanged;

        // Se for baú (isChest = true), atualiza tudo de uma vez
        if (container.inventory.slots != null)
        {
            Debug.Log($"[ContainerUI] Configurando interface unificada ({container.containerName})");
            UpdateUIUnified();
        }
        else
        {
            UpdateUIForAllTypes();
        }

        UpdateWeightUI();
    }

    private void HandleInventoryChanged(ItemType type)
    {
        if (container.inventory.slots != null)
            UpdateUIUnified();
        else
            UpdateUIForType(type);
    }

    //  Modo baú — lista única
    private void UpdateUIUnified()
    {
        foreach (Transform child in containerParent)
            Destroy(child.gameObject);

        if (container.inventory.slots == null) return;

        foreach (var invSlot in container.inventory.slots)
        {
            GameObject slotGO = Instantiate(slotPrefab, containerParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot);

            Debug.Log($"[ContainerUI] Slot criado: {invSlot.item?.itemName ?? "Vazio"} (x{invSlot.quantity})");
        }

        UpdateWeightUI();
    }

    // Modo normal — separado por tipo
    private void UpdateUIForAllTypes()
    {
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
            UpdateUIForType(type);
    }

    private void UpdateUIForType(ItemType type)
    {
        if (container == null || container.inventory == null) return;

        if (!slotObjectsByType.ContainsKey(type))
            slotObjectsByType[type] = new List<InventorySlotUI>();

        foreach (var slotUI in slotObjectsByType[type])
            if (slotUI != null)
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

            Debug.Log($"[ContainerUI] Slot criado ({type}): {invSlot.item?.itemName ?? "Vazio"} x{invSlot.quantity}");
        }

        UpdateWeightUI();
    }

    private List<InventorySlot> GetSlotsForType(ItemType type)
    {
        // Se for baú, todos os tipos compartilham a mesma lista
        if (container.inventory.slots != null)
            return container.inventory.slots;

        // Caso contrário, usa as listas separadas
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
