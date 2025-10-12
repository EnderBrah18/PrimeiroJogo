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

public class PlayerInventory : MonoBehaviour
{
    public Inventory inventory;
    public InventoryUI inventoryUI;

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

        // Adiciona os itens iniciais configurados no Inspector
        foreach (var entry in startingItems)
        {
            if (entry.item != null)
                inventory.AddItem(entry.item, entry.amount);
        }

        // Conecta o inventário à interface de usuário
        if (inventoryUI != null)
        {
            inventoryUI.Setup(inventory, player);
        }
        else
        {
            Debug.LogWarning("InventoryUI is not assigned in PlayerInventory.");
        }
    }
}

