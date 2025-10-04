using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;

    public GameObject slotPrefab;

    // Panels para cada tipo de item
    public GameObject recursosPanel;
    public GameObject equipamentosPanel;
    public GameObject consumiveisPanel;
    public GameObject questItemsPanel;

    // Parent transforms para cada tipo de slot
    public Transform recursosSlotsParent;
    public Transform equipamentosSlotsParent;
    public Transform consumiveisSlotsParent;
    public Transform questItemsSlotsParent;
    public Transform chestSlotsParent; // um GridLayout só para o baú


    // Listas de GameObjects criados dinamicamente
    public List<GameObject> chestSlots = new List<GameObject>();
    private List<GameObject> recursosSlots = new List<GameObject>();
    private List<GameObject> equipamentosSlots = new List<GameObject>();
    private List<GameObject> consumiveisSlots = new List<GameObject>();
    private List<GameObject> questItemsSlots = new List<GameObject>();

    public TextMeshProUGUI pesoText;

    void Start()
    {
        if (inventory != null) // Only open if already configured
            ShowRecursos();
        else
        {
            Debug.LogError("Inventory is not set! Call Setup to initialize it.");
        }
    }

    public void Setup(Inventory inv)
    {
        if (inv == null)
        {
            Debug.LogError("Setup called with a null Inventory.");
            return;
        }

        inventory = inv;
        ShowRecursos(); // Show resources by default
    }

    public void ShowRecursos()
    {
        SetActivePanel(recursosPanel);
        UpdateUIForType(ItemType.Resource);
    }

    /// <summary>
    /// Mostra painel de equipamentos
    /// </summary>
    public void ShowEquipamentos()
    {
        SetActivePanel(equipamentosPanel);
        UpdateUIForType(ItemType.Equipment);
    }

    public void ShowConsumiveis()
    {
        SetActivePanel(consumiveisPanel);
        UpdateUIForType(ItemType.Consumable);
    }

    public void ShowQuestItems()
    {
        SetActivePanel(questItemsPanel);
        UpdateUIForType(ItemType.QuestItem);
    }

    /// <summary>
    /// Ativa apenas o painel selecionado
    /// </summary>
    private void SetActivePanel(GameObject activePanel)
    {
        recursosPanel.SetActive(false);
        equipamentosPanel.SetActive(false);
        consumiveisPanel.SetActive(false);
        questItemsPanel.SetActive(false);

        if (activePanel != null)
            activePanel.SetActive(true);
    }

    /// Atualiza slots de um tipo específico de item
    /// </summary>
    /// 

    public void UpdateChestUI()
    {
        // Limpa os slots antigos do baú
        foreach (var slot in chestSlots) Destroy(slot);
        chestSlots.Clear();

        if (inventory == null || inventory.slots.Count == 0)
        {
            Debug.LogWarning("Chest inventory is empty!");
            return;
        }

        // Cria os slots do baú
        for (int i = 0; i < inventory.slots.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, chestSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(inventory.slots[i]); // funciona mesmo se slot vazio
            chestSlots.Add(slotGO);
        }

        UpdatePesoUI();
    }

    public void UpdateUIForType(ItemType type)
    {
        if (inventory == null)
        {
            Debug.LogError("Inventory is null in UpdateUIForType");
            return;
        }

        List<InventorySlot> targetSlots = GetSlotsForType(type);
        if (targetSlots == null) return;

        // Seleciona a lista de GameObjects correspondente para limpeza
        List<GameObject> slotObjects = GetSlotObjectsList(type);
        foreach (var obj in slotObjects) Destroy(obj);
        slotObjects.Clear();

        // Cria os slots
        Transform parent = GetParentForType(type);
        foreach (var invSlot in targetSlots)
        {
            if (invSlot == null) continue;

            GameObject slotGO = Instantiate(slotPrefab, parent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot);
            slotObjects.Add(slotGO);
        }

        UpdatePesoUI();
    }

    /// <summary>
    /// Retorna a lista de InventorySlot correspondente ao tipo
    /// </summary>
    private List<InventorySlot> GetSlotsForType(ItemType type)
    {
        if (inventory.slots != null) return inventory.slots; // Baú usa lista unificada

        switch (type)
        {
            case ItemType.Resource: return inventory.resourceSlots;
            case ItemType.Equipment: return inventory.equipmentSlots;
            case ItemType.Consumable: return inventory.consumableSlots;
            case ItemType.QuestItem: return inventory.questItemSlots;
            default: return null;
        }
    }

    /// <summary>
    /// Retorna a lista de GameObjects correspondente ao tipo
    /// </summary>
    private List<GameObject> GetSlotObjectsList(ItemType type)
    {
        switch (type)
        {
            case ItemType.Resource: return recursosSlots;
            case ItemType.Equipment: return equipamentosSlots;
            case ItemType.Consumable: return consumiveisSlots;
            case ItemType.QuestItem: return questItemsSlots;
            default: return null;
        }
    }

    /// <summary>
    /// Retorna o parent Transform correspondente ao tipo
    /// </summary>
    private Transform GetParentForType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Resource: return recursosSlotsParent;
            case ItemType.Equipment: return equipamentosSlotsParent;
            case ItemType.Consumable: return consumiveisSlotsParent;
            case ItemType.QuestItem: return questItemsSlotsParent;
            default: return null;
        }
    }

    /// <summary>
    /// Atualiza o texto de peso
    /// </summary>
    void UpdatePesoUI()
    {
        if (pesoText != null && inventory != null)
        {
            pesoText.text = $"Peso: {inventory.GetCurrentWeight():0.0} / {inventory.maxWeight}";
        }
    }

    
}
