using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class Container : MonoBehaviour
{
    [Header("Configurações do Container")]
    public string containerName = "Baú";
    public int maxResourceSlots = 20;
    public int maxEquipmentSlots = 10;
    public int maxConsumableSlots = 10;
    public int maxQuestItemSlots = 5;
    public float maxWeight = 200f;

    [Header("Inventário Interno")]
    public Inventory inventory;

    public List<StartingItem> startingItems;

    void Awake()
    {
        // Cria o inventário interno
        inventory = new Inventory(maxResourceSlots, maxEquipmentSlots, maxConsumableSlots, maxQuestItemSlots, maxWeight, true);
    }

    void Start()
    {
        // Adiciona os itens iniciais
        foreach (var entry in startingItems)
        {
            if (entry.item != null && entry.amount > 0)
                inventory.AddItem(entry.item, entry.amount);
        }

    }

    // Métodos auxiliares
    public bool AddItem(ItemSO item, int amount = 1) => inventory.AddItem(item, amount);
    public bool RemoveItem(ItemSO item, int amount = 1) => inventory.RemoveItem(item, amount);
}
