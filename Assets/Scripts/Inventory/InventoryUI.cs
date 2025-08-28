using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private Inventory inventory; 

    public GameObject slotPrefab;
    public Transform recursosSlotsParent;
    public Transform equipamentosSlotsParent;

    private List<GameObject> recursosSlots = new List<GameObject>();
    private List<GameObject> equipamentosSlots = new List<GameObject>();

    public GameObject recursosPanel;
    public GameObject equipamentosPanel;

    public TextMeshProUGUI pesoText;

    void Start()
    {
        if (inventory != null) // só abre se já foi configurado
            ShowRecursos();

        else
        {
            Debug.LogError("Inventory is not set! Call Setup to initialize it.");
        }
    }



    public void Setup(Inventory inv)
    {
        if (inv == null)
        {
            Debug.LogError("Setup called with a null Inventory.");
            return;
        }

        inventory = inv;
        ShowRecursos(); // Show resources by default
    }

    public void ShowRecursos()
    {

        Debug.Log("Recursos painel aberto!");
        recursosPanel.SetActive(true);
        equipamentosPanel.SetActive(false);
        UpdateRecursosUI();
    }

    public void ShowEquipamentos()
    {

        Debug.Log("Equipamentos painel aberto!");
        recursosPanel.SetActive(false);
        equipamentosPanel.SetActive(true);
        UpdateEquipamentosUI();
    }

    public void UpdateRecursosUI(ChestUI chestUIReference = null)
    {
        // Clear existing resource slots
        foreach (var slot in recursosSlots) Destroy(slot);
        recursosSlots.Clear();

        // Determine the target slots to display
        List<InventorySlot> targetSlots = inventory.slots ?? inventory.resourceSlots;

        if (targetSlots == null)
        {
            Debug.LogError("Target slots are null in UpdateRecursosUI.");
            return;
        }

        // Create slots for the resource inventory
        foreach (InventorySlot invSlot in targetSlots)
        {
            GameObject slotGO = Instantiate(slotPrefab, recursosSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot, chestUIReference);
            recursosSlots.Add(slotGO);
        }

        UpdatePesoUI();
    }

    public void UpdateEquipamentosUI()
    {
        // Clear existing equipment slots
        foreach (var slot in equipamentosSlots) Destroy(slot);
        equipamentosSlots.Clear();

        // Determine the target slots to display
        List<InventorySlot> targetSlots = inventory.slots ?? inventory.equipmentSlots;

        if (targetSlots == null)
        {
            Debug.LogError("Target slots are null in UpdateEquipamentosUI.");
            return;
        }

        // Create slots for the equipment inventory
        foreach (InventorySlot invSlot in targetSlots)
        {
            GameObject slotGO = Instantiate(slotPrefab, equipamentosSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot, null);
            equipamentosSlots.Add(slotGO);
        }

        UpdatePesoUI();
    }

    void UpdatePesoUI()
    {
        if (pesoText != null && inventory != null)
        {
            pesoText.text = $"Peso: {inventory.GetCurrentWeight():0.0} / {inventory.maxWeight}";
        }
    }
}
