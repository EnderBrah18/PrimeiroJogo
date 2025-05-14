using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollectable
{
    string GetID();        // Identificador único do item
    Sprite GetIcon();      // Ícone para exibir na UI
    float GetWeight();     // Peso do item

    string GetDescription(); // Descrição do item
}

