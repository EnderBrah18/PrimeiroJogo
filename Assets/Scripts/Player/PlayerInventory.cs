using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[System.Serializable]
public class StartingItem
{
    public ItemSO item;
    public int amount;
}

public class PlayerInventory : MonoBehaviour, ISavable
{
    public Inventory inventory;
    public InventoryUI inventoryUI;

    public string SaveKey => "PlayerInventory";

    public int maxResourceSlots = 20;
    public int maxEquipmentSlots = 10;
    public int maxConsumableSlots = 10;
    public int maxQuestItemSlots = 5;
    public float maxWeight = 100f;

    // Lista de itens iniciais configuráveis no Inspector
    public List<StartingItem> startingItems;

    void Awake()
    {
        // Obtém referência ao Player
        Player player = GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("Player component is missing in PlayerInventory.");
            return;
        }

        // Cria o inventário com referência ao Player
        inventory = new Inventory(
            player,
            maxResourceSlots,
            maxEquipmentSlots,
            maxConsumableSlots,
            maxQuestItemSlots,
            maxWeight,
            false // não é um baú
        );

        foreach (var entry in startingItems)
        {
            if (entry.item != null)
                inventory.AddItem(entry.item, entry.amount);
        }

        inventoryUI?.Setup(inventory, player);
    }

    private void OnEnable()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.RegisterSavable(this);
        else
            StartCoroutine(WaitAndRegister());
    }

    private IEnumerator WaitAndRegister()
    {
        yield return new WaitUntil(() => SaveSystem.Instance != null);
        SaveSystem.Instance.RegisterSavable(this);
    }

    private void OnDisable()
    {
        SaveSystem.Instance.UnregisterSavable(this);
    }

    public string SaveData()
    {
        Debug.Log("Salvando inventário do jogador: " + inventory);
        InventorySaveData data = new InventorySaveData(inventory);
        return JsonUtility.ToJson(data);
    }

    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);


        for (int i = 0; i < data.itemIDs.Count; i++)
        {
            ItemSO item = ItemLoader.GetItemByID(data.itemIDs[i]);
            if (item != null)
                inventory.AddItem(item, data.amounts[i]);
            else
                Debug.LogWarning("Item do save não encontrado: " + data.itemIDs[i]);
        }

        inventoryUI?.Setup(inventory, GetComponent<Player>());
    }

    public string GetSaveKey() => SaveKey;

}
    

