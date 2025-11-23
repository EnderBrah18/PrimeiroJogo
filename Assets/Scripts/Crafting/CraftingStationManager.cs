using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class CraftingStationManager : MonoBehaviour
{
    public static CraftingStationManager Instance { get; private set; }

    public static event Action OnCraftingManagerReady;

    [Header("UI Principal da Estação")]
    public GameObject CraftingUI;
    private ThirdPersonCamera cameraScript;

    [Header("Referência da Estação Ativa")]
    private CraftingStation currentStation;

    [Header("Input")]
    public InputActionReference interactAction;
    private InteractionHandler interactionHandler;

    void Awake()
    {
        Instance = this;
        OnCraftingManagerReady?.Invoke();
        cameraScript = FindAnyObjectByType<ThirdPersonCamera>();
    }

    void Start()
    {
        CloseStationUI();
    }

    void Update()
    {
        if (interactAction.action.WasPressedThisFrame())
            interactionHandler?.Interact();
    }

    public void RegisterInteractionHandler(InteractionHandler handler)
    {
        interactionHandler = handler;
    }

    // =================================================================
    // Abrir / Fechar Estação
    // =================================================================
    public void OpenStationUI(CraftingStation station)
    {
        // Se a estação já está aberta, feche a UI
        if (currentStation == station && CraftingUIManager.IsCraftingOpen)
        {
            CloseStationUI();
            return;
        }

        // Fecha qualquer UI aberta de outra estação
        CloseStationUI();

        // Abre a estação atual
        currentStation = station;

        CraftingUI.SetActive(true);
        CraftingUIManager.IsCraftingOpen = true;

        Player.Instance?.SetMovementBlocked(true);
        cameraScript?.HandleInventoryToggled(true);
    }

    public void CloseStationUI()
    {
        CraftingUI.SetActive(false);
        CraftingUIManager.IsCraftingOpen = false;

        Player.Instance?.SetMovementBlocked(false);
        cameraScript?.HandleInventoryToggled(false);

        currentStation = null;
    }

    // =================================================================
    // Trigger de Detecção de Estação
    // =================================================================
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        var station = GetComponent<CraftingStation>();
        if (station != null)
        {
            currentStation = station;
            Debug.Log("Player entrou na crafting station: " + station.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (currentStation != null)
        {
            Debug.Log("Player saiu da crafting station: " + currentStation.name);
            currentStation = null;
        }
    }

    // =================================================================
    // Simples controle de UI
    // =================================================================
    public static class CraftingUIManager
    {
        public static bool IsCraftingOpen = false;
    }
}
