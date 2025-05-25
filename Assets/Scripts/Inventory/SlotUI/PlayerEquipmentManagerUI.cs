using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerEquipmentManagerUI : MonoBehaviour
{
    public Player player; // Referência ao script do jogador

    [Header("Containers com os slots de equipamento")]
    public Transform equipmentSlotContainerLeft;
    public Transform equipmentSlotContainerRight;

    void Start()
    {
        SetupSlots(equipmentSlotContainerLeft);
        SetupSlots(equipmentSlotContainerRight);
    }

    void SetupSlots(Transform container)
    {
        foreach (Transform child in container)
        {
            var slotUI = child.GetComponent<PlayerEquipmentSlotUI>();
            if (slotUI == null)
                slotUI = child.gameObject.AddComponent<PlayerEquipmentSlotUI>();

            // Definir tipo com base no nome
            string name = child.name.ToLower();

            if (name.Contains("head")) slotUI.slotType = EquipmentType.Head;
            else if (name.Contains("chest")) slotUI.slotType = EquipmentType.Chest;
            else if (name.Contains("legs")) slotUI.slotType = EquipmentType.Legs;
            else if (name.Contains("feet") || name.Contains("boots")) slotUI.slotType = EquipmentType.Feet;
            else if (name.Contains("hands") || name.Contains("gloves")) slotUI.slotType = EquipmentType.Gloves;
            else if (name.Contains("accessory")) slotUI.slotType = EquipmentType.Accessory;
            else if (name.Contains("main")) slotUI.slotType = EquipmentType.MainHand;
            else if (name.Contains("off")) slotUI.slotType = EquipmentType.OffHand;
            else
            {
                Debug.LogWarning($"Slot '{child.name}' não reconhecido. Verifique o nome do GameObject.");
            }

            // Conectar referências
            slotUI.player = player;
            slotUI.slotText = child.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
