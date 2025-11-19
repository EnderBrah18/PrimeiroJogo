using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindMenu : MonoBehaviour
{
    [Serializable]
    public class ActionButtonReference
    {
        public string actionName;  // Nome da ação
        public Button button;      // Botão já existente na UI
        public TMP_Text label;     // Texto mostrado no botão
    }

    [SerializeField] private List<ActionButtonReference> actionsToRebind;

    private void Start()
    {
        foreach (var actionRef in actionsToRebind)
        {
            var action = InputManager.Instance.GetAction(actionRef.actionName);
            if (action == null) continue;

            int bindingIndex = FindFirstNonComposite(action);
            if (bindingIndex < 0) continue;

            // Atualiza o texto inicial
            actionRef.label.text = action.GetBindingDisplayString(bindingIndex);

            // Configura o botão
            actionRef.button.onClick.AddListener(() =>
            {
                actionRef.button.interactable = false;
                InputManager.Instance.StartRebind(action.name, bindingIndex, success =>
                {
                    actionRef.button.interactable = true;
                    actionRef.label.text = action.GetBindingDisplayString(bindingIndex);
                });
            });
        }
    }

    private int FindFirstNonComposite(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
            if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                return i;
        return -1;
    }
}
