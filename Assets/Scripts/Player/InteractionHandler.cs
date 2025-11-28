using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    private InteractableBase currentInteractable;

    public void Interact()
    {
        Debug.Log("Interagindo...");

        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
        else
        {
            Debug.Log("Nenhum objeto interagível próximo.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponent<InteractableBase>();

        if (interactable != null)
        {
            currentInteractable = interactable;
            Debug.Log($"Entrou no trigger de: {interactable.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<InteractableBase>() == currentInteractable)
        {
            Debug.Log($"Saiu do trigger de: {currentInteractable.name}");
            currentInteractable = null;
        }
    }
}
