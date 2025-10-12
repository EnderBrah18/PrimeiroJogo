using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Container))]
[RequireComponent(typeof(Collider))]
public class ContainerInteract : MonoBehaviour
{
    [Header("Configurações de Interação")]
    public float interactionDistance = 2.5f;
    public InputAction interactAction;

    [Header("UI")]
    public ContainerUI containerUI;

    private Container container;
    private Transform player;
    private bool isPlayerNear = false;

    private void Awake()
    {
        container = GetComponent<Container>();

        // Garantir que o collider seja trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnEnable()
    {
        //  MUITO IMPORTANTE: ativar a ação manualmente
        if (interactAction != null)
            interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.Disable();
    }

    private void Start()
    {
        // Encontrar o player (pode ser melhorado com tag)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("Nenhum objeto com tag 'Player' encontrado na cena!");
    }

    private void Update()
    {
        if (!isPlayerNear || player == null)
            return;

        // Interação com tecla
        if (interactAction.WasPressedThisFrame())
        {
            Debug.Log($"[Container] Tentando abrir: {container.containerName}");
            TryOpenContainer();
        }
        
    }

    private void TryOpenContainer()
    {
        if (containerUI == null)
        {
            Debug.LogWarning("ContainerUI não atribuído no inspector!");
            return;
        }

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist <= interactionDistance)
        {
            Debug.Log($"[Container] Abrindo: {container.containerName}");
            containerUI.OpenContainerPanel(container);
        }
    }

    // Detecta quando o player entra no alcance
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log($"Player entrou na área do baú: {container.containerName}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            Debug.Log($"Player saiu da área do baú: {container.containerName}");
            containerUI.CloseContainerPanel();
        }
    }
}
