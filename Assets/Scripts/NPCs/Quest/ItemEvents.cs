using UnityEngine;
using UnityEngine.Events;


/// Evento genérico para qualquer coleta de recurso.
/// Outros scripts (como QuestCollectable) podem se inscrever neste evento.

public class ItemEvents : MonoBehaviour
{
    public static ItemEvents Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    
    /// Evento chamado quando um item é coletado.
    
    public UnityEvent<ResourceSO> OnItemCollected = new UnityEvent<ResourceSO>();

    
    /// Método público para disparar o evento
    
    public void ItemCollected(ResourceSO collectedResource)
    {
        if (collectedResource == null) return;

        OnItemCollected.Invoke(collectedResource);
    }
}
