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
    //public ToolType requiredTool;
    public float collectTime = 0f;

    private bool isBeingCollected = false;

    public void StartCollect()
    {
        if (!isBeingCollected)
        {
            if (collectTime <= 0f)
                Collect();
            else
                StartCoroutine(CollectWithDelay());
        }
    }

    IEnumerator CollectWithDelay()
    {
        isBeingCollected = true;
        // Adicione barra de progresso ou animação aqui

        float timer = 0f;
        while (timer < collectTime)
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
