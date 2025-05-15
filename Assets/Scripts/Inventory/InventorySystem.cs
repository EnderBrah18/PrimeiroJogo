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

    private List<InventorySlotUI> slotList = new List<InventorySlotUI>();

    public float maxWeight = 100f;
    public float currentWeight = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupSlots();
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
            slotUI.Clear();  // Começa vazio
            slotList.Add(slotUI);
        }
    }

    public void AddTool(Tools tool)
    {
        // Procura slot que já contenha essa ferramenta
        InventorySlotUI existingSlot = slotList.Find(s => s.HasItem() && s.GetItem().IsTool() && s.GetItem().tool.toolName == tool.toolName);

        if (existingSlot != null)
        {
            existingSlot.GetItem().quantity++;
        }
        else
        {
            InventorySlotUI emptySlot = slotList.Find(s => !s.HasItem());
            if (emptySlot != null)
            {
                emptySlot.Set(new InventoryItem(tool));
            }
            else
            {
                Debug.Log("Inventário cheio!");
                return;
            }
        }

        RefreshUI();
    }

    public void AddCollectable(ICollectable collectable)
    {
        float itemWeight = (collectable as CollectableObject)?.weight ?? 0f;

        if (currentWeight + itemWeight > maxWeight)
        {
            Debug.Log("Peso máximo atingido! Não é possível carregar mais itens.");
            return;
        }

        // Procura slot que já contenha esse coletável
        InventorySlotUI existingSlot = slotList.Find(s => s.HasItem() && s.GetItem().IsCollectable() && s.GetItem().collectable.GetID() == collectable.GetID());

        if (existingSlot != null)
        {
            existingSlot.GetItem().quantity++;
        }
        else
        {
            InventorySlotUI emptySlot = slotList.Find(s => !s.HasItem());
            if (emptySlot != null)
            {
                emptySlot.Set(new InventoryItem(collectable));
            }
            else
            {
                Debug.Log("Inventário cheio!");
                return;
            }
        }

        currentWeight += itemWeight;
        RefreshUI();
    }

    public void RefreshUI()
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].index = i;
            slotList[i].RefreshSlotUI();
        }
    }
}


