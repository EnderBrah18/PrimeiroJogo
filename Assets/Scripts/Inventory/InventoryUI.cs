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
    }

    public void Setup(Inventory inv)
    {
        inventory = inv;
        ShowRecursos(); // mostra recursos por padrão
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

    void UpdateRecursosUI()
    {
        foreach (var slot in recursosSlots) Destroy(slot);
        recursosSlots.Clear();

        foreach (InventorySlot invSlot in inventory.slots)
        {
            if (invSlot == null || invSlot.item == null) continue;
            if (invSlot.item.itemType != ItemType.Resource) continue;

            GameObject slotGO = Instantiate(slotPrefab, recursosSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot);

            recursosSlots.Add(slotGO);
        }

        UpdatePesoUI();
    }

    void UpdateEquipamentosUI()
    {
        foreach (var slot in equipamentosSlots) Destroy(slot);
        equipamentosSlots.Clear();

        foreach (InventorySlot invSlot in inventory.slots)
        {
            if (invSlot == null || invSlot.item == null) continue;
            if (invSlot.item.itemType != ItemType.Equipment) continue;

            GameObject slotGO = Instantiate(slotPrefab, equipamentosSlotsParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Setup(invSlot);

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
