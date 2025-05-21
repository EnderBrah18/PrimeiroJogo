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
    public string itemName;
    public Sprite icon;
    public string description;
    public float weight = 1f;

    public virtual string GetID() => itemName;
    public virtual Sprite GetIcon() => icon;
    public virtual float GetWeight() => weight;
    public virtual string GetDescription() => description;

    public virtual bool CanBeCollected(Tools currentTool)
    {
        // Implementação padrão (pode ser true, false ou alguma lógica genérica)
        return true;
    }

    public virtual void StartCollect(Tools equippedTool)
    {
        Debug.LogWarning("StartCollect should be overridden in derived class.");
    }
}

