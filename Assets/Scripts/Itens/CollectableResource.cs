using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableResource : CollectableObject
{
    public ResourceSO resourceData;

    private bool isBeingCollected = false;

    private void Awake()
    {
        if (resourceData != null)
        {
            itemName = resourceData.resourceName;
            icon = resourceData.icon;
            description = resourceData.description;
            weight = resourceData.weight;
        }
    }

    public override void StartCollect(Tools equippedTool)
    {
        if (isBeingCollected || resourceData == null) return;
        if (!CanBeCollected(equippedTool)) return;

        float finalTime = resourceData.baseCollectTime;

        int levelDiff = equippedTool.level - resourceData.requiredToolLevel;
        if (levelDiff > 0)
            finalTime -= levelDiff * 2f;

        finalTime -= GetRarityReduction(equippedTool.rarity);
        finalTime -= GetStatModifierValue(equippedTool, "CollectSpeed");
        finalTime = Mathf.Max(0.3f, finalTime);

        if (!InventorySystem.Instance.TryAddToSlot(this)) return;

        Debug.Log($"Coletando recurso: {resourceData.resourceName}, tempo: {finalTime:F2}");
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
        float total = 0f;
        foreach (var mod in tool.statusModifiers)
        {
            if (mod.statName == statName)
                total += mod.value;
        }
        return total;
    }

    public override string GetID() => resourceData != null ? resourceData.resourceName : itemName;
    public override Sprite GetIcon() => resourceData != null ? resourceData.icon : icon;
    public override string GetDescription() => resourceData != null ? resourceData.description : description;
    public override float GetWeight() => resourceData != null ? resourceData.weight : weight;
}
