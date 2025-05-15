using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
            Destroy(gameObject);
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

        // Atualiza a UI aqui
        InventoryUIManager.Instance.RefreshUI(collectableInventory);

    }

    public void SwapInventoryItems(InventoryItem itemA, InventoryItem itemB)
    {
        // Procura a posição dos itens na lista correta e troca eles

        if (itemA == null && itemB == null) return;

        // Verifica se são ferramentas ou coletáveis
        bool itemAIsTool = itemA != null && itemA.IsTool();
        bool itemBIsTool = itemB != null && itemB.IsTool();

        // Para facilitar, copia listas temporárias
        var toolInv = toolInventory;
        var colInv = collectableInventory;

        // Posições dos itens
        int indexA = -1, indexB = -1;

        if (itemAIsTool)
            indexA = toolInv.IndexOf(itemA);
        else if (itemA != null)
            indexA = colInv.IndexOf(itemA);

        if (itemBIsTool)
            indexB = toolInv.IndexOf(itemB);
        else if (itemB != null)
            indexB = colInv.IndexOf(itemB);

        // Se ambos são ferramentas
        if (itemAIsTool && itemBIsTool)
        {
            if (indexA >= 0 && indexB >= 0)
            {
                toolInv[indexA] = itemB;
                toolInv[indexB] = itemA;
            }
        }
        // Se ambos são coletáveis
        else if (!itemAIsTool && !itemBIsTool)
        {
            if (indexA >= 0 && indexB >= 0)
            {
                colInv[indexA] = itemB;
                colInv[indexB] = itemA;
            }
        }
        else
        {
            // Se são diferentes tipos (tool e collectable), troca entre as listas
            if (indexA >= 0 && indexB >= 0)
            {
                if (itemAIsTool)
                {
                    toolInv[indexA] = itemB;
                    colInv[indexB] = itemA;
                }
                else
                {
                    colInv[indexA] = itemB;
                    toolInv[indexB] = itemA;
                }
            }
        }

    }
}


