using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableResource : CollectableObject
{
    public ResourceSO resourceData;

    private bool isBeingCollected = false;

    private Inventory playerInventory;
    private InventoryUI inventoryUI;


        private void Start()
    {
        var playerInvHolder = FindFirstObjectByType<PlayerInventory>();
        if (playerInvHolder != null)
            playerInventory = playerInvHolder.inventory;
        else
            Debug.LogWarning("PlayerInventory não encontrado na cena. playerInventory ficará nulo.", this);

        inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI == null)
            Debug.LogWarning("InventoryUI não encontrado na cena.", this);
    }

    public override void StartCollect(Tools equippedTool)
    {
        if (isBeingCollected || resourceData == null) return;

        // Verifica se pode coletar com a ferramenta atual (CanBeCollected já trata currentTool == null)
        if (!CanBeCollected(equippedTool)) return;

        float finalTime = resourceData.baseCollectTime;

        // Detecta se estamos sem ferramenta equipada (ou enum NONE)
        bool noTool = (equippedTool == null || equippedTool.toolType == ToolType.None);

        // Se não há ferramenta e o recurso requer uma ferramenta específica, aborta (defesa extra)
        if (noTool && resourceData.requiredToolType != ToolType.None)
        {
            Debug.LogWarning($"StartCollect: tentativa de coletar {name} sem ferramenta quando é exigida. Abortando.", this);
            return;
        }

        // Valores padrão quando não há ferramenta: nível 0, raridade comum, stat modifier 0
        int toolLevel = noTool ? 0 : equippedTool.level;
        Rarity toolRarity = noTool ? Rarity.Common : equippedTool.rarity;
        float statModifier = noTool ? 0f : GetStatModifierValue(equippedTool, "CollectSpeed");

        // Calcula diferença de nível (pode resultar negativo, só aplica se > 0)
        int levelDiff = toolLevel - resourceData.requiredToolLevel;
        if (levelDiff > 0)
            finalTime -= levelDiff * 2f;

        finalTime -= GetRarityReduction(toolRarity);
        finalTime -= statModifier;
        finalTime = Mathf.Max(0.3f, finalTime);

        StartCoroutine(CollectDelay(finalTime));
    }

    private IEnumerator CollectDelay(float time)
    {
        isBeingCollected = true;
        yield return new WaitForSeconds(time);

        if (playerInventory != null && resourceData != null)
        {
            bool added = playerInventory.AddItem(resourceData, 1);
            if (added)
            {
                Debug.Log($"{resourceData.resourceName} coletado e adicionado ao inventário!");
            }
            else
            {
                Debug.Log("Inventário cheio ou peso excedido!");
                // Aqui você pode decidir: descartar no chão, criar um drop físico, etc.
            }
        }
        else
        {
            if (playerInventory == null)
                Debug.LogWarning("CollectDelay: playerInventory é nulo — item não foi adicionado.");
            if (resourceData == null)
                Debug.LogWarning("CollectDelay: resourceData é nulo.");
            // Se quiser criar um drop no chão quando não tiver inventário, faça aqui.
        }

        Destroy(gameObject);
    }

    public override bool CanBeCollected(Tools currentTool)
    {
        if (resourceData.requiredToolType == ToolType.None)
            return true;

        if (currentTool == null)
            return false;

        return currentTool.toolType == resourceData.requiredToolType && currentTool.level >= resourceData.requiredToolLevel;
    }

    private float GetRarityReduction(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => 0.5f,
            Rarity.Rare => 1.5f,
            Rarity.Epic => 2f,
            Rarity.Legendary => 3f,
            Rarity.Mythic => 5f,
            _ => 0f,
        };
    }

    private float GetStatModifierValue(Tools tool, string statName)
    {
        if (tool == null) return 0f;
        if (tool.statusModifiers == null || tool.statusModifiers.Count == 0) return 0f;

        float total = 0f;
        foreach (var mod in tool.statusModifiers)
        {
            if (mod == null) continue;
            if (mod.statName == statName)
                total += mod.value;
        }
        return total;
    }

}
