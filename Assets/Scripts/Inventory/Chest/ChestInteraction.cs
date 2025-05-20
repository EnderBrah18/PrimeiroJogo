using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChestInteraction : MonoBehaviour
{

    [Header("Referências")]
    public GameObject chestCanvas;               // Canvas com as UIs do baú e inventário do jogador
    public KeyCode interactionKey = KeyCode.E;  // Tecla para abrir/fechar o baú
    public GameObject inventoryUI;

    [SerializeField] private Transform painelRecursos;
    [SerializeField] private Transform painelEquipamentos;

    // Índices dos filhos que você quer mostrar
    private int[] filhosParaExibirRecursos = { 0 };
    private int[] filhosParaExibirEquipamentos = { 0 }; // nenhum por exemplo

    public bool playerInRange = false;
    private bool isOpen = false;

    private void Start()
    {
        if (chestCanvas != null)
            chestCanvas.SetActive(false);
    }

    public void ToggleChestUI()
    {
        isOpen = !isOpen;
        if (chestCanvas != null)
            chestCanvas.SetActive(isOpen);

        if(inventoryUI != null)
            inventoryUI.SetActive(isOpen);
            

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None; // Libera o cursor
            Cursor.visible = true;
            Time.timeScale = 0f; // Pausa o jogo
            ControlarFilhosDoInventario(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Trava o cursor no centro
            Cursor.visible = false;
            Time.timeScale = 1f; // Retoma o jogo
            ControlarFilhosDoInventario(false);
        }

        // Se quiser, bloqueie o movimento do jogador aqui quando o baú estiver aberto.
        // Exemplo:
        // PlayerController.Instance.SetMovementEnabled(!isOpen);
    }

    private void ControlarFilhosDoInventario(bool mostrar)
    {
        // Painel de Recursos
        for (int i = 0; i < painelRecursos.childCount; i++)
        {
            bool deveMostrar = filhosParaExibirRecursos.Contains(i);
            painelRecursos.GetChild(i).gameObject.SetActive(mostrar && deveMostrar);
        }

        // Painel de Equipamentos
        for (int i = 0; i < painelEquipamentos.childCount; i++)
        {
            bool deveMostrar = filhosParaExibirEquipamentos.Contains(i);
            painelEquipamentos.GetChild(i).gameObject.SetActive(mostrar && deveMostrar);
        }
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
