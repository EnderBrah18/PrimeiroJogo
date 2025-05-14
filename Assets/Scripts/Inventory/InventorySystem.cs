using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    public UnityEvent onInventoryChanged;

    public ItemDatabase itemDatabase;
    public List<InventoryItem> toolInventory = new List<InventoryItem>();
    public List<InventoryItem> collectableInventory = new List<InventoryItem>();

    public float maxWeight = 100f;
    public float currentWeight = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogError("Uma instância do InventorySystem já existe!");
            Destroy(gameObject);
        }

        if (onInventoryChanged == null)
            onInventoryChanged = new UnityEvent();
    }

    public void AddTool(Tools tool)
    {
        // Verifica se a ferramenta já está no inventário
        var existing = toolInventory.Find(i => i.IsTool() && i.tool.toolName == tool.toolName);

        if (existing != null)
            existing.quantity++; // Se já existir, aumenta a quantidade
        else
            toolInventory.Add(new InventoryItem(tool)); // Caso contrário, adiciona como um novo item

        onInventoryChanged.Invoke(); // <- Aqui!
    }

    public void AddCollectable(ICollectable collectable)
    {

        // Obter o peso do item coletável
        float itemWeight = (collectable as CollectableObject)?.weight ?? 0f;

        // Verificar se o peso não ultrapassa o limite
        if (currentWeight + itemWeight > maxWeight)
        {
            Debug.Log("Peso máximo atingido! Não é possível carregar mais itens.");
            return;
        }

        Debug.Log($"Tentando adicionar item: {collectable.GetID()} com peso {itemWeight}kg.");

        var existing = collectableInventory.Find(i => i.IsCollectable() && i.collectable.GetID() == collectable.GetID());

        if (existing != null)
            existing.quantity++; // Se o item já estiver no inventário, aumenta a quantidade
        else
            collectableInventory.Add(new InventoryItem(collectable)); // Caso contrário, adiciona como novo item

        currentWeight += itemWeight;

        onInventoryChanged.Invoke(); // <- Aqui!
    }
}


