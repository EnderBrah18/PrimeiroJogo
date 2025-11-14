using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

[RequireComponent(typeof(UniqueID))]
public class ChestSavable : SavableEntity
{
    [System.Serializable]
    public class ChestData
    {
        public List<string> itemIDs = new List<string>();
        public List<int> amounts = new List<int>();
        public string SaveKey => "ChestInventory";
    }

    [SerializeField] private Container container; // referencia ao Inventory do baú
    [SerializeField] private ContainerUI containerUI; // se tiver UI específica
    private GameObject ContainerManager;

    private Inventory chestInventory => container != null ? container.inventory : null;

    protected override void Awake()
    {
        if (container == null)
        {
            container = GetComponent<Container>();
            containerUI = ContainerManager.GetComponent<ContainerUI>();
            if (container == null)
                Debug.LogError("ChestSavable precisa ter referência ao Container!");
        }

        if (containerUI == null)
        {
            ContainerManager = GameObject.Find("ContainerManager");
            containerUI = ContainerManager.GetComponent<ContainerUI>();
        }

        
    }

    private void Start()
    {
        // Registra no SaveSystem apenas quando o container e inventário estiverem prontos
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.RegisterSavable(this);
    }

    private void OnEnable()
    {
        if (SaveSystem.Instance != null && container != null)
            SaveSystem.Instance.RegisterSavable(this);
    }

    private void OnDisable()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.UnregisterSavable(this);
    }

    public override string SaveData()
    {
        if (chestInventory == null) return null;

        var d = new ChestData();

        void AddSlots(List<InventorySlot> slots)
        {
            if (slots == null) return;

            foreach (var slot in slots)
            {
                if (slot?.item != null)
                {
                    if (string.IsNullOrEmpty(slot.item.id))
                        Debug.LogWarning($"Item {slot.item.itemName} sem ID definido!");
                    else
                    {
                        d.itemIDs.Add(slot.item.id);
                        d.amounts.Add(slot.quantity);
                        Debug.Log($"[ChestSavable] Salvando item: {slot.item.itemName} x{slot.quantity} (ID: {slot.item.id})");
                    }
                }
            }
        }

        // Unifica todos os slots do inventário
        if (chestInventory.slots != null) AddSlots(chestInventory.slots);
        if (chestInventory.resourceSlots != null) AddSlots(chestInventory.resourceSlots);
        if (chestInventory.equipmentSlots != null) AddSlots(chestInventory.equipmentSlots);
        if (chestInventory.consumableSlots != null) AddSlots(chestInventory.consumableSlots);
        if (chestInventory.questItemSlots != null) AddSlots(chestInventory.questItemSlots);

        return JsonUtility.ToJson(d);
    }

    public override void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json) || chestInventory == null) return;

        var d = JsonUtility.FromJson<ChestData>(json);

        // Limpa todos os slots
        chestInventory.slots?.ForEach(s => { s.item = null; s.quantity = 0; });
        chestInventory.resourceSlots?.ForEach(s => { s.item = null; s.quantity = 0; });
        chestInventory.equipmentSlots?.ForEach(s => { s.item = null; s.quantity = 0; });
        chestInventory.consumableSlots?.ForEach(s => { s.item = null; s.quantity = 0; });
        chestInventory.questItemSlots?.ForEach(s => { s.item = null; s.quantity = 0; });

        // Carrega os itens salvos
        for (int i = 0; i < d.itemIDs.Count; i++)
        {
            var item = ItemLoader.GetItemByID(d.itemIDs[i]);
            if (item != null)
                chestInventory.AddItem(item, d.amounts[i]);
            else
                Debug.LogWarning($"ChestSavable: item do save não encontrado: {d.itemIDs[i]}");
        }

        // Atualiza UI
        containerUI?.Setup(container);
    }

}
