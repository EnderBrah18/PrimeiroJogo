using Mono.Cecil;
using UnityEngine;

[System.Serializable]
public class QuestProgressEntry
{
    public string variableName;
    public int amount = 1;
}

public class QuestCollectable : MonoBehaviour
{
    [Header("Reconhecimento de Tipo de Recurso")]
    public bool countAnyResource;
    public CollectableType collectType;

    [Header("Progressos enviados ao coletar")]
    public QuestProgressEntry[] progressEntries;

    public void OnCollected(ResourceSO collectedResource)
    {
        if (collectedResource == null) return;

        // Se não for any e o tipo não bater, ignora
        if (!countAnyResource && collectedResource.type != collectType)
            return;

        int resourceAmount = collectedResource.amount; // Quantidade real do recurso coletado

        // Atualiza todas as entradas de progresso configuradas
        foreach (var entry in progressEntries)
        {
            if (string.IsNullOrEmpty(entry.variableName)) continue;

            int current = GlobalVariableSystem.Instance.GetValue(entry.variableName);
            int newValue = current + resourceAmount;
            GlobalVariableSystem.Instance.SetValue(entry.variableName,newValue);

            // Atualiza o Step corretamente
            QuestSystem.Instance.RegisterProgress(entry.variableName, newValue);

            Debug.Log($"[QuestCollectable] Variável {entry.variableName} atualizada de {current} para {newValue}");
        }
    }
}
