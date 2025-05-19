using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInteraction : MonoBehaviour
{

    [Header("Referências")]
    public GameObject chestCanvas;               // Canvas com as UIs do baú e inventário do jogador
    public KeyCode interactionKey = KeyCode.E;  // Tecla para abrir/fechar o baú

    private bool playerInRange = false;
    private bool isOpen = false;

    private void Start()
    {
        if (chestCanvas != null)
            chestCanvas.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            ToggleChestUI();
        }
    }

    private void ToggleChestUI()
    {
        isOpen = !isOpen;
        if (chestCanvas != null)
            chestCanvas.SetActive(isOpen);

        // Se quiser, bloqueie o movimento do jogador aqui quando o baú estiver aberto.
        // Exemplo:
        // PlayerController.Instance.SetMovementEnabled(!isOpen);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // Aqui pode exibir um texto tipo "Pressione E para abrir"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (isOpen)
            {
                ToggleChestUI();  // Fecha o baú se o jogador sair do alcance
            }
            // Esconder texto de instrução aqui se houver
        }
    }
}
