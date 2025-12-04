using UnityEngine;

public class QuestCraft : MonoBehaviour
{
    [Header("Variável da Quest")]
    public string variableName; // Ex: "itens_crafteados"
    public int amount = 1;

    [Header("Item específico para a quest (opcional)")]
    public ItemSO requiredItem; // se vazio, qualquer item conta

    // Chamado quando o jogador crafta o item
    public void OnCrafted(ItemSO craftedItem)
    {
        if (requiredItem != null && craftedItem != requiredItem)
            return; // não é o item da quest


        int current = GlobalVariableSystem.Instance.GetValue(variableName);
        GlobalVariableSystem.Instance.SetValue(variableName, current + amount);

        QuestSystem.Instance.CheckQuestProgress(variableName, current + amount);
    }
}
