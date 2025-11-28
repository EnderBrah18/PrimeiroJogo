using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionInput : MonoBehaviour
{
    public InputActionReference interactAction;
    private InteractionHandler interactionHandler;

    private void Awake()
    {
        interactionHandler = GetComponent<InteractionHandler>();
    }

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    private void Update()
    {
        if (interactAction.action.WasPressedThisFrame())
        {
            interactionHandler?.Interact();
        }
    }
}
