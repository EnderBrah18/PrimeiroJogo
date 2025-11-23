using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    private InteractableBase currentInteractable;

    void OnEnable()
    {
        ShopManager.OnShopManagerReady += RegisterWhenReady;
    }

    void OnDisable()
    {
        ShopManager.OnShopManagerReady -= RegisterWhenReady;
    }

    void Start()
    {
        TryRegister();
    }

    private void TryRegister()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterInteractionHandler(this);
            Debug.Log("InteractionHandler registrado no ShopManager.");
        }
    }

    private void RegisterWhenReady()
    {
        TryRegister();
    }

    public void Interact()
    {
        if (currentInteractable != null)
        {
            Debug.Log("Interagindo com: " + currentInteractable.name);
            currentInteractable.Interact();
        }
        else
        {
            Debug.Log("Interação ignorada: nenhum objeto interagível próximo.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // pega qualquer objeto que herde de InteractableBase
        var interactable = other.GetComponent<InteractableBase>();

        if (interactable != null)
        {
            currentInteractable = interactable;
            Debug.Log($"Player ENTROU em área de interação: {interactable.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponent<InteractableBase>();

        if (interactable != null && interactable == currentInteractable)
        {
            Debug.Log($"Player SAIU da área de interação: {currentInteractable.name}");
            currentInteractable = null;
        }
    }
}
