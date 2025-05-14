using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollectableType
{
    Flor,
    Minerio,
    Arvore
}

public class CollectableObject : MonoBehaviour, ICollectable
{
    [SerializeField] private InventoryUIManager inventoryUIManager;

    public string itemName;
    public CollectableType type;
    public ToolType requiredToolType = ToolType.None;
    public int requiredToolLevel = 0;
    public float baseCollectTime = 0f;
    public Sprite icon;
    public string description;

    private InventorySystem inventory;
    public float weight = 1f;

    public string GetID() => itemName;
    public Sprite GetIcon() => icon;
    public float GetWeight() => weight;
    public string GetDescription() => description;

    private bool isBeingCollected = false;


    private void Start()
    {
        inventory = FindObjectOfType<InventorySystem>();
    }

    public bool CanBeCollected(Tools currentTool)
    {
        if (requiredToolType == ToolType.None)
            return true;

        if (currentTool == null)
            return false;

        return currentTool.toolType == requiredToolType && currentTool.level >= requiredToolLevel;
    }

    public void StartCollect(Tools equippedTool)
    {
        if (!isBeingCollected && equippedTool != null)
        {
            // Verifica se o tipo da ferramenta é compatível
            if (equippedTool.toolType != requiredToolType) return;

            float finalTime = baseCollectTime;

            // Se o nível da ferramenta for maior, diminui o tempo
            if (equippedTool.level > requiredToolLevel)
            {
                int diff = equippedTool.level - requiredToolLevel;
                finalTime -= diff * 2f; // Reduz 0.5 segundos por nível extra
                finalTime = Mathf.Max(0.5f, finalTime); // Tempo mínimo de segurança
            }

            // Verificar o peso antes de coletar
            float itemWeight = (this as ICollectable).GetWeight();
            if (InventorySystem.Instance.currentWeight + itemWeight > InventorySystem.Instance.maxWeight)
            {
                Debug.Log("Inventário cheio, não é possível coletar o item!");
                return; // Não começa a coleta se o peso máximo for ultrapassado
            }

            Debug.Log($"Tempo de coleta final: {finalTime} segundos");

            if (finalTime <= 0f)
                Collect();
            else
                StartCoroutine(CollectWithDelay(finalTime));
        }
    }

    IEnumerator CollectWithDelay(float time)
    {
        isBeingCollected = true;
        // Adicione barra de progresso ou animação aqui

        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        Collect();
    }

    void Collect()
    {
        // Verificar se o peso máximo foi atingido antes de destruir o objeto
        float itemWeight = (this as ICollectable).GetWeight();
        if (InventorySystem.Instance.currentWeight + itemWeight > InventorySystem.Instance.maxWeight)
        {
            Debug.Log("O limite de peso foi atingido. O item não será adicionado ao inventário.");
            return; // Não destruir o objeto ou adicionar ao inventário
        }

        Debug.Log($"Você coletou: {itemName} ({type})");
        InventorySystem.Instance.AddCollectable(this); // Adiciona o item ao inventário
                                                       
        if (inventoryUIManager.gameObject.activeInHierarchy)
            inventoryUIManager.UpdateUI(); // Atualiza a UI apenas se estiver ativa
        Destroy(gameObject); // Destrói o objeto coletado
    }
}
