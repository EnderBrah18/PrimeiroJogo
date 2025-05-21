using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [Header("UI")]
    public Transform slotsParent;       // Painel com os slots
    public GameObject slotPrefab;       // Prefab do slot
    public int maxSlots = 20;
    [SerializeField] private GameObject inventoryPanel;

    public List<InventorySlotUI> slotList = new List<InventorySlotUI>();
    public List<EquipmentSlotUI> equipmentSlotList = new List<EquipmentSlotUI>(); // slots equipamento

    [Header("UI - Equipamentos")]
    public Transform equipmentSlotsParent; // Novo painel com os slots de equipamento
    public GameObject equipmentSlotPrefab;
    public int maxEquipmentSlots = 10;


    [Header("WeightUI")]
    [SerializeField] private InventoryWeighUI inventoryWeighUI;
    public float maxWeight = 100f;
    public float currentWeight = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupSlots();
            SetupEquipmentSlots();       // Novos slots de equipamento

            RefreshUI();
            inventoryPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Uma instância do InventorySystem já existe!");
            Destroy(gameObject);
        }
    }

    private void SetupSlots()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Clear();
            slotList.Add(slotUI);
            slotUI.inventorySystem = this;
            slotUI.chestInventory = null;
        }
    }

    private void SetupEquipmentSlots()
    {
        for (int i = 0; i < maxEquipmentSlots; i++)
        {
            GameObject slotGO = Instantiate(equipmentSlotPrefab, equipmentSlotsParent);
            EquipmentSlotUI slotUI = slotGO.GetComponent<EquipmentSlotUI>();  // pega a subclasse
            slotUI.Clear();
            equipmentSlotList.Add(slotUI);
            slotUI.inventorySystem = this;
            slotUI.chestInventory = null;
        }
    }

    
    public void AddEquipment(Equipment equipment)
    {
        InventorySlotUI existingSlot = equipmentSlotList.Find(s =>
            s.HasItem() && s.GetItem().IsEquipment() && s.GetItem().equipment.equipmentName == equipment.equipmentName);

        if (existingSlot != null)
        {
            existingSlot.GetItem().quantity++;
        }
        else
        {
            InventorySlotUI emptySlot = equipmentSlotList.Find(s => !s.HasItem());
            if (emptySlot != null)
            {
                emptySlot.Set(new InventoryItem(equipment));
            }
            else
            {
                Debug.Log("Inventário de Equipamentos cheio!");
                return;
            }
        }

        RefreshUI();
    }

    public void AddCollectable(ICollectable collectable)
    {
        float itemWeight = (collectable as CollectableObject)?.weight ?? 0f;


        currentWeight += itemWeight;
        RefreshUI();
    }

    public void RemoveItem(InventoryItem item)
    {
        if (item == null) return;

        float itemWeight = 0f;

        if (item.IsCollectable())
        {
            var collectable = item.collectable as CollectableObject;
            if (collectable != null)
            {
                itemWeight = collectable.weight;
            }
        }

        currentWeight -= itemWeight;
        if (currentWeight < 0f) currentWeight = 0f;

        InventorySlotUI slot = slotList.Find(s => s.HasItem() && s.GetItem() == item);
        if (slot == null)
            slot = slotList.Find(s => s.HasItem() && s.GetItem() == item);

        if (slot != null)
            slot.Clear();

        RefreshUI();
    }

    public void RefreshUI()
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].index = i;
            slotList[i].RefreshSlotUI();
        }

        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].index = i;
            slotList[i].RefreshSlotUI();
        }

        if (inventoryWeighUI.weightText != null)
        {
            inventoryWeighUI.weightText.text = $"Peso: {currentWeight}/{maxWeight}";
        }

        Debug.Log($"RefreshUI - Peso Atual: {currentWeight} / {maxWeight}");
    }

    public bool TryAddToSlot(ICollectable collectable)
    {
        Debug.Log($"TryAddToSlot chamado para coletável {collectable.GetID()} - Stack possível? {slotList.Exists(s => s.HasItem() && s.GetItem().collectable.GetID() == collectable.GetID())}");

        // Procura slot com o mesmo item (empilhável)
        InventorySlotUI existingSlot = slotList.Find(s =>
            s.HasItem() &&
            s.GetItem().IsCollectable() &&
            s.GetItem().collectable.GetID() == collectable.GetID());

        if (existingSlot != null)
        {
            existingSlot.GetItem().quantity++;
            return true;
        }

        // Caso não haja stack, procura slot vazio
        InventorySlotUI emptySlot = slotList.Find(s => !s.HasItem());
        if (emptySlot != null)
        {
            emptySlot.Set(new InventoryItem(collectable));
            return true;
        }

        Debug.Log("Inventário cheio!");
        return false;
    }

    public void RecalculateWeight()
    {
        float newWeight = 0f;

        foreach (var slot in slotList)
        {
            if (slot.HasItem())
            {
                InventoryItem item = slot.GetItem();
                if (item.IsCollectable())
                {
                    var collectable = item.collectable as CollectableObject;
                    if (collectable != null)
                    {
                        newWeight += collectable.weight * item.quantity;
                    }
                }
                else if (item.IsEquipment())
                {
                    // Se quiser contar peso das ferramentas, faça aqui (exemplo):
                    // newWeight += item.tool.weight * item.quantity;
                }
            }
        }

        foreach (var slot in slotList)
        {
            if (slot.HasItem())
            {
                InventoryItem item = slot.GetItem();
                if (item.IsEquipment())
                {
                    // newWeight += item.tool.weight * item.quantity; // se necessário
                }
            }
        }

        currentWeight = newWeight;
    }
}


