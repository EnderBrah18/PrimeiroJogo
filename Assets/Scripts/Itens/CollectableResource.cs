using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableResource : CollectableObject
{
    public CollectableType type;
    public ToolType requiredToolType = ToolType.None;
    public int requiredToolLevel = 0;
    public float baseCollectTime = 0f;

    private bool isBeingCollected = false;

    public override void StartCollect(Tools equippedTool)
    {
        if (isBeingCollected) return;

        if (!CanBeCollected(equippedTool)) return;

        float finalTime = baseCollectTime;

        int levelDiff = equippedTool.level - requiredToolLevel;
        if (levelDiff > 0)
            finalTime -= levelDiff * 2f;

        finalTime -= GetRarityReduction(equippedTool.rarity);
        finalTime -= GetStatModifierValue(equippedTool, "CollectSpeed");
        finalTime = Mathf.Max(0.3f, finalTime);

        if (!InventorySystem.Instance.TryAddToSlot(this)) return;

        Debug.Log($"Coletando recurso: {itemName}, tempo: {finalTime:F2}");
        InventorySystem.Instance.AddCollectable(this);

        StartCoroutine(CollectDelay(finalTime));
    }

    private IEnumerator CollectDelay(float time)
    {
        isBeingCollected = true;
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

    public override bool CanBeCollected(Tools currentTool)
    {
        // Lógica específica para recursos
        // Exemplo:
        if (requiredToolType == ToolType.None)
            return true;
        if (currentTool == null)
            return false;
        return currentTool.toolType == requiredToolType && currentTool.level >= requiredToolLevel;
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
        float total = 0f;
        foreach (var mod in tool.statusModifiers)
        {
            if (mod.statName == statName)
                total += mod.value;
        }
        return total;
    }
}
