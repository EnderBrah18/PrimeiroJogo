using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollectableType
{
    Flor,
    Minerio,
    Arvore
}

public class CollectableObject : MonoBehaviour
{
    public string itemName;
    public CollectableType type;
    public ToolType requiredToolType = ToolType.None;
    public int requiredToolLevel = 0;
    public float baseCollectTime = 0f;

    private bool isBeingCollected = false;

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
                finalTime = Mathf.Max(0.5f, finalTime); // tempo mínimo de segurança
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
        Debug.Log($"Você coletou: {itemName} ({type})");
        // Aqui você pode: dar item ao inventário, spawnar partículas, tocar som, etc
        Destroy(gameObject);
    }
}
