using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Controls;


public class InputDisplayHelper
{
    public static string GetDisplayString(InputAction action)
    {
        if (action == null || action.controls.Count == 0)
            return "?";

        InputControl control = action.controls[0]; // primeiro controle ativo
        int bindingIndex = action.GetBindingIndexForControl(control);

        return action.GetBindingDisplayString(bindingIndex);
    }
}
