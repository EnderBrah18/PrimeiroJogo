using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

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
            Destroy(gameObject); // Não deixa outro InventorySystem ser criado
        }

        if (Instance == null)
        {
            Debug.LogError("InventorySystem não foi inicializado corretamente.");
        }
    }

    public void AddTool(Tools tool)
    {
        // Verifica se a ferramenta já está no inventário
        var existing = toolInventory.Find(i => i.IsTool() && i.tool.toolName == tool.toolName);

        if (existing != null)
            existing.quantity++; // Se já existir, aumenta a quantidade
        else
            toolInventory.Add(new InventoryItem(tool)); // Caso contrário, adiciona como um novo item
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
    }
}


