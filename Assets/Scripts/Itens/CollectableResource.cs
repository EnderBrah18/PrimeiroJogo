using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CollectableResource : CollectableObject
{
    public ResourceSO resourceData;

    [Header("UI de Coleta")]
    public Canvas worldCanvas;
    public Image progressImage;

    private bool isBeingCollected = false;
    private bool blocked = false;

    private Inventory playerInventory;
    private InventoryUI inventoryUI;

    private Tween progressTween;




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

        blocked = !blocked;

        Player.Instance?.SetMovementBlocked(blocked);
        Player.Instance?.SetAttackBlocked(blocked);

        // Ativa a UI de coleta
        if (worldCanvas != null)
            worldCanvas.enabled = true;

        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(true);

        if (progressImage != null)
        {
            progressImage.fillAmount = 0f;

            // Cancela tween anterior, se existir
            progressTween?.Kill();

            // Cria animação do DOTween
            progressTween = progressImage
                .DOFillAmount(1f, finalTime)
                .SetEase(Ease.Linear);
        }

        StartCoroutine(CollectDelay(finalTime));


    }

    private IEnumerator CollectDelay(float time)
    {
        isBeingCollected = true;
        yield return new WaitForSeconds(time);

        if (playerInventory != null && resourceData != null)
        {
            bool added = playerInventory.AddItem(resourceData, resourceData.amount);
            if (added)
            {
                Debug.Log($"{resourceData.resourceName} coletado e adicionado ao inventário!");
                QuestCollectable qc = GetComponent<QuestCollectable>();
                if (qc != null)
                    qc.OnCollected(resourceData);
            }
            else
            {
                Debug.Log("Inventário cheio ou peso excedido!");

                isBeingCollected = false; // <- permite tentar coletar novamente

                HideProgressUI();

                yield break; //  BLOQUEIA A DESTRUIÇÃO DO OBJETO
            }
        }
        else
        {
            if (playerInventory == null)
                Debug.LogWarning("CollectDelay: playerInventory é nulo — item não foi adicionado.");
            if (resourceData == null)
                Debug.LogWarning("CollectDelay: resourceData é nulo.");

            isBeingCollected = false; // <- permite tentar coletar novamente

            HideProgressUI();

            yield break; //  BLOQUEIA A DESTRUIÇÃO DO OBJETO
        }

        FloatingTextManager.Instance.CreateText($"+{resourceData.amount} " + resourceData.resourceName, transform.position, Color.yellow);

        Destroy(gameObject);

        HideProgressUI();

        UnblockPlayer();
    }

    public override bool CanBeCollected(Tools currentTool)
    {
        if (resourceData.requiredToolType == ToolType.None)
            return true;

        if (currentTool == null)
            return false;

        return currentTool.toolType == resourceData.requiredToolType && currentTool.level >= resourceData.requiredToolLevel;
    }

    private void UnblockPlayer()
    {
        blocked = !blocked;

        Player.Instance?.SetMovementBlocked(blocked);
        Player.Instance?.SetAttackBlocked(blocked);
    }

    private void HideProgressUI()
    {
        if (worldCanvas != null)
            worldCanvas.enabled = false;

        progressTween?.Kill();
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
