using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput; // arraste no inspector (opcional)

    // Ações expostas (leitura ou subscribe)
    private InputAction _move;
    private InputAction _jump;
    private InputAction _dash;
    private InputAction _sprint;
    private InputAction _interact;
    private InputAction _climb;
    private InputAction _attack;
    private InputAction _aim;   // novo: botão direito / aim
    private InputAction _lock;  // novo: lock-on toggle

    // Eventos para actions do tipo "performed"
    public event Action<InputAction.CallbackContext> OnJumpPerformed;
    public event Action<InputAction.CallbackContext> OnDashPerformed;
    public event Action<InputAction.CallbackContext> OnInteractPerformed;
    public event Action<InputAction.CallbackContext> OnClimbPerformed;
    public event Action<InputAction.CallbackContext> OnAttackPerformed;

    // Guardar delegates para conseguir unsub later
    private Action<InputAction.CallbackContext> _jumpForwarder;
    private Action<InputAction.CallbackContext> _dashForwarder;
    private Action<InputAction.CallbackContext> _interactForwarder;
    private Action<InputAction.CallbackContext> _climbForwarder;
    private Action<InputAction.CallbackContext> _attackForwarder;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("InputManager: PlayerInput não encontrado. Arraste um PlayerInput no inspector ou coloque o componente neste GameObject.");
            enabled = false;
            return;
        }

        var actions = playerInput.actions;
        actions.Enable();

        _move = actions.FindAction("Move");
        _jump = actions.FindAction("Jump");
        _dash = actions.FindAction("Dash");
        _sprint = actions.FindAction("Sprint");
        _interact = actions.FindAction("Interact");
        _climb = actions.FindAction("Climb");
        _attack = actions.FindAction("Attack");
        _aim = actions.FindAction("Aim");   // procura ação "Aim" no PlayerInput
        _lock = actions.FindAction("Lock"); // procura ação "Lock" no PlayerInput

        // criar forwarders (delegates estáveis) para unsubscribing
        _jumpForwarder = ctx => OnJumpPerformed?.Invoke(ctx);
        _dashForwarder = ctx => OnDashPerformed?.Invoke(ctx);
        _interactForwarder = ctx => OnInteractPerformed?.Invoke(ctx);
        _climbForwarder = ctx => OnClimbPerformed?.Invoke(ctx);
        _attackForwarder = ctx => OnAttackPerformed?.Invoke(ctx);

        if (_jump != null) _jump.performed += _jumpForwarder;
        if (_dash != null) _dash.performed += _dashForwarder;
        if (_interact != null) _interact.performed += _interactForwarder;
        if (_climb != null) _climb.performed += _climbForwarder;
        if (_attack != null) _attack.performed += _attackForwarder;
    }

    private void OnDestroy()
    {
        // cleanup - importante para evitar lambdas perdidas
        if (_jump != null && _jumpForwarder != null) _jump.performed -= _jumpForwarder;
        if (_dash != null && _dashForwarder != null) _dash.performed -= _dashForwarder;
        if (_interact != null && _interactForwarder != null) _interact.performed -= _interactForwarder;
        if (_climb != null && _climbForwarder != null) _climb.performed -= _climbForwarder;
        if (_attack != null && _attackForwarder != null)
            _attack.performed -= _attackForwarder;
    }

    public InputAction GetAction(string actionName)
    {
        if (playerInput == null) return null;
        return playerInput.actions.FindAction(actionName, true);
    }

    // Helpers de leitura
    public Vector2 GetMoveVector() => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
    public bool WasJumpPressedThisFrame() => _jump != null && _jump.WasPressedThisFrame();
    public bool WasInteractPressedThisFrame() => _interact != null && _interact.WasPressedThisFrame();
    public bool WasClimbPressedThisFrame() => _climb != null && _climb.WasPressedThisFrame();
    public bool IsSprintPressed() => _sprint != null && _sprint.ReadValue<float>() > 0.5f;

    // NOVOS HELPERS para combate (aim/lock)
    public bool IsAimPressed() => _aim != null && _aim.ReadValue<float>() > 0.5f;
    public bool WasLockPressedThisFrame() => _lock != null && _lock.WasPressedThisFrame();

    // Expor a InputAction cru (se alguém quiser subscrever diretamente)
    public InputAction MoveAction => _move;
    public InputAction JumpAction => _jump;
    public InputAction DashAction => _dash;
    public InputAction SprintAction => _sprint;
    public InputAction InteractAction => _interact;
    public InputAction ClimbAction => _climb;
    public InputAction AttackAction => _attack;
    public InputAction AimAction => _aim;
    public InputAction LockAction => _lock;

    // ... (restante do arquivo permanece igual)
    private const string REBIND_PREFS_KEY = "rebinds_json_v1";

    /// <summary>
    /// Inicia o rebind de uma action específica.
    /// </summary>
    /// <param name="actionName">Nome da action, ex: "Jump"</param>
    /// <param name="bindingIndex">-1 para auto encontrar primeiro binding não composite</param>
    /// <param name="onComplete">Callback quando terminar (true = sucesso, false = cancelado)</param>
    public RebindingOperation StartRebind(string actionName, int bindingIndex = -1, Action<bool> onComplete = null)
    {
        var action = GetAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"InputManager: ação '{actionName}' não encontrada para rebind.");
            onComplete?.Invoke(false);
            return null;
        }

        // Auto escolher bindingIndex
        if (bindingIndex < 0)
        {
            bindingIndex = FindFirstNonCompositeBinding(action);
            if (bindingIndex < 0)
            {
                Debug.LogWarning($"InputManager: não há bindings válidos para '{actionName}'");
                onComplete?.Invoke(false);
                return null;
            }
        }

        // DESABILITA a action antes de rebinding
        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                op.Dispose();
                action.Enable();             // REHABILITA ao finalizar
                SaveBindingOverrides();      // salva automaticamente
                onComplete?.Invoke(true);
            })
            .OnCancel(op =>
            {
                op.Dispose();
                action.Enable();             // REHABILITA se cancelar
                onComplete?.Invoke(false);
            });

        rebind.Start();
        return rebind;
    }

    /// <summary>
    /// Encontra o primeiro binding não-composite/parte
    /// </summary>
    private int FindFirstNonCompositeBinding(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
            if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                return i;
        return -1;
    }

    /// <summary>
    /// Salva todos os overrides em PlayerPrefs
    /// </summary>
    public void SaveBindingOverrides()
    {
        if (playerInput == null) return;
        string json = playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(REBIND_PREFS_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("Bindings salvos");
    }

    /// <summary>
    /// Carrega os overrides de PlayerPrefs
    /// </summary>
    public void LoadBindingOverrides()
    {
        if (playerInput == null) return;
        if (!PlayerPrefs.HasKey(REBIND_PREFS_KEY)) return;

        string json = PlayerPrefs.GetString(REBIND_PREFS_KEY);
        if (!string.IsNullOrEmpty(json))
        {
            playerInput.actions.LoadBindingOverridesFromJson(json);
            Debug.Log("Bindings carregados");
        }
    }

    /// <summary>
    /// Reseta todos os bindings para default
    /// </summary>
    public void ResetBindings()
    {
        if (playerInput == null) return;
        playerInput.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(REBIND_PREFS_KEY);
        Debug.Log("Bindings resetados para default");
    }
}