using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindMenu : MonoBehaviour
{
    [Serializable]
    public class ActionButtonPrefab
    {
        public string actionName;       // Nome da ação no Input System
        public GameObject buttonPrefab; // Prefab do botão com TMP_Text
    }

    [SerializeField] private Transform contentParent; // Pai para os botões
    [SerializeField] private List<ActionButtonPrefab> actionsToRebind;

    private void Start()
    {
        foreach (var actionData in actionsToRebind)
        {
            var action = InputManager.Instance.GetAction(actionData.actionName);
            if (action == null) continue;

            if (action.controls.Count > 0 && action.controls[0].device is Keyboard && action.bindings[0].isComposite)
            {
                // É um composite (como Move)
                CreateCompositeButtons(action, actionData.buttonPrefab);
            }
            else
            {
                // Ação simples
                CreateSimpleButton(action, actionData.buttonPrefab);
            }
        }
    }

    private void CreateSimpleButton(InputAction action, GameObject prefab)
    {
        var buttonGO = Instantiate(prefab, contentParent);
        var text = buttonGO.GetComponentInChildren<TMP_Text>();
        var button = buttonGO.GetComponent<Button>();

        int bindingIndex = FindFirstNonComposite(action);
        text.text = action.GetBindingDisplayString(bindingIndex);

        button.onClick.AddListener(() =>
        {
            button.interactable = false;
            InputManager.Instance.StartRebind(action.name, bindingIndex, success =>
            {
                button.interactable = true;
                text.text = action.GetBindingDisplayString(bindingIndex);
            });
        });
    }

    private void CreateCompositeButtons(InputAction action, GameObject prefab)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (!action.bindings[i].isPartOfComposite || action.bindings[i].isComposite) continue;

            var buttonGO = Instantiate(prefab, contentParent);
            var text = buttonGO.GetComponentInChildren<TMP_Text>();
            var button = buttonGO.GetComponent<Button>();

            string displayName = $"{action.name} {action.bindings[i].name}";
            text.text = $"{displayName}: {action.GetBindingDisplayString(i)}";

            int bindingIndex = i;
            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                InputManager.Instance.StartRebind(action.name, bindingIndex, success =>
                {
                    button.interactable = true;
                    text.text = $"{displayName}: {action.GetBindingDisplayString(bindingIndex)}";
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
