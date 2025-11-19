using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;


public enum CharacterState
{
    IDLE,
    WALKING,
    JUMPING,
    FALLING,
    SPRINTING
}

public class Player : MonoBehaviour 
{
    public static Player Instance { get; private set; }

    public event Action OnEquipmentChanged;
    public CharacterState currentState = CharacterState.IDLE;

    public CharacterController _characterController;
    public Animator animator;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private PlayerStatsSO stats;

    #region MovementVariables
    [Header("Movimenta��o")]
    [SerializeField] private float turnSpeed = 10f;
    private float moveSpeed => stats.movement.baseMoveSpeed;
    private float jumpForce => stats.movement.baseJumpForce;
    [SerializeField] private float vSpeed = 0f;
    [SerializeField] private float gravity => stats.gravity;

    [HideInInspector]
    public bool blockMovement = false;

    public PlayerStatsSO Stats => stats;


    public float MoveSpeed
    {
        get => stats.movement.baseMoveSpeed;
        set => stats.movement.baseMoveSpeed = Mathf.Max(0, value);
    }

    public float JumpForce
    {
        get => stats.movement.baseJumpForce;
        set => stats.movement.baseJumpForce = Mathf.Max(0, value);
    }

    public float MaxStamina
    {
        get => stats.stamina.maxStamina;
        set => stats.stamina.maxStamina = Mathf.Max(0, value);
    }


    private float groundedGraceTime = 0.2f;
    private float lastGroundedTime;
    private bool isReallyGrounded => Time.time - lastGroundedTime <= groundedGraceTime;

    [Header("Dash")]
    public float dashDistance = 10f;
    public float dashCooldown = 1f;
    public float lastDashTime = -Mathf.Infinity;
    public float dashDuration = 0.2f;
    public LayerMask dashCollisionMask;

    [Header("Movimento no Ar")]
    public float airControlMultiplier = 0.65f;
    public float airAcceleration = 5f;
    public float airDrag = 2f;

    [Header("Escalada")]
    public float climbSpeed = 2f;
    public float climbCheckDistance = 1f;
    public float lateralClimbSpeed = 1f;
    public LayerMask climbableMask;

    [Header("Estamina")]
    public float currentStamina;
    private float staminaRegenRate => stats.stamina.staminaRegenRate;
    private float staminaSprintCost => stats.stamina.staminaSprintCost;
    private float staminaClimbCost => stats.stamina.staminaClimbCost;
    private float staminaJumpCost => stats.stamina.staminaJumpCost;
    private float staminaMinToSprint => stats.stamina.staminaMinToSprint;
    private float staminaMinToClimb => stats.stamina.staminaMinToClimb;

    private float sprintMultiplier => stats.stamina.sprintMultiplier;

    public float maxStamina => stats.stamina.maxStamina;

    public bool isSprinting = false;

    #endregion

    // 8 slots de equipamento
    public Equipment helmet;
    public Equipment chest;
    public Equipment legs;
    public Equipment boots;
    public Equipment gloves;
    public Equipment accessory;
    public Equipment mainHand;
    public Equipment offHand;

    public Tools equippedTool => (offHand as Tools) ?? (mainHand as Tools);
    public Weapon equippedWeapon => (offHand as Weapon) ?? (mainHand as Weapon);

    private Vector3 finalMovement;
    private Coroutine dashRoutine;
    private Vector3 currentHorizontalVelocity = Vector3.zero;
    private Vector3 velocity; //Armazenar velocidade vertical

    public PlayerInventory playerInventory;

    [HideInInspector] public float totalAttack;
    [HideInInspector] public float totalDefense;
    [HideInInspector] public float totalSpeed;
    [HideInInspector] public float totalStamina;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (stats != null)
            SaveSystem.Instance.RegisterSOSavable(stats);
    }

    private void Start()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager não encontrado na cena. Certifique-se de ter um InputManager ativo.");
            return;
        }
        InputManager.Instance.OnInteractPerformed += OnInteractPerformed;


        // inicializações anteriores
        currentStamina = maxStamina;
        if (_characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
            vSpeed = -2f;
            currentState = CharacterState.IDLE;
        }
        else currentState = CharacterState.FALLING;

        if (playerInventory != null)
        {
            InventoryUI inventoryUI = UnityEngine.Object.FindFirstObjectByType<InventoryUI>(); // Updated to use FindFirstObjectByType
            if (inventoryUI != null)
            {
                inventoryUI.Setup(playerInventory.inventory, this); // Pass both Inventory and Player references
            }
            else
            {
                Debug.LogError("InventoryUI not found in the scene.");
            }
            playerInventory.inventory.GetCurrentWeight();
        }


        // Apply stats modifiers from equipped items
        ApplyEquipmentStats();

        // Notify listeners
        OnEquipmentChanged?.Invoke();
        UpdateStats();
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInteractPerformed -= OnInteractPerformed;
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        // se você quer usar TryCollect() igual antes
        CollectableManager.Instance.TryCollect();
    }


    private void Update()
    {
        if (blockMovement)
        {
            // Para animações
            animator.SetBool("isWalking", false);
            animator.SetBool("isIdle", true);

            // Não processa movimento
            return;
        }


            finalMovement = Vector3.zero;

        #region CollectUpdate

        if (InputManager.Instance.WasInteractPressedThisFrame())
        {
            CollectableManager.Instance.TryCollect();
        }

        #endregion

        if (_characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        // switch states (mantém seus handlers)
        switch (currentState)
        {
            case CharacterState.IDLE:
                HandleJump();
                break;
            case CharacterState.WALKING:
                HandleMovement();
                HandleJump();
                break;
            case CharacterState.JUMPING:
                HandleMovement();
                HandleJump();
                break;
            case CharacterState.FALLING:
                HandleMovement();
                HandleGravity();
                break;
            case CharacterState.SPRINTING:
                HandleMovement();
                HandleJump();
                HandleGravity();
                break;
                

        }

        // animações...
        animator.SetBool("isIdle", currentState == CharacterState.IDLE);
        animator.SetBool("isWalking", currentState == CharacterState.WALKING);
        animator.SetBool("isJumping", currentState == CharacterState.JUMPING);
        animator.SetBool("isFalling", currentState == CharacterState.FALLING);

        Vector3 verticalVelocity = Vector3.up * vSpeed;
        Vector3 totalVelocity = currentHorizontalVelocity + verticalVelocity;
        _characterController.Move(totalVelocity * Time.deltaTime);

        UpdateState();
        UpdateStamina();
    }

    void UpdateState()
    { 
        Vector2 input = InputManager.Instance.GetMoveVector();

            if (!isReallyGrounded)
            {
                currentState = vSpeed > 0 ? CharacterState.JUMPING : CharacterState.FALLING;
            }
            else if (input.magnitude > 0.1f)
            {
                currentState = CharacterState.WALKING;
            }
            else
            {
                currentState = CharacterState.IDLE;
            }
    }

    #region Movement
    void HandleMovement()
    {
        Vector2 input = InputManager.Instance.GetMoveVector();

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (_characterController.isGrounded)
        {
            float speed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
            currentHorizontalVelocity = moveDirection * speed;
        }
        else
        {
            Vector3 targetVelocity = moveDirection * moveSpeed * airControlMultiplier;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, airAcceleration * Time.deltaTime);

            if (moveDirection == Vector3.zero)
            {
                currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, airDrag * Time.deltaTime);
            }
        }

    }

    void HandleJump()
    {
        if (isReallyGrounded && InputManager.Instance.WasJumpPressedThisFrame() && currentStamina >= staminaJumpCost)
        {
            vSpeed = jumpForce;
            currentState = CharacterState.JUMPING;
            lastGroundedTime = -1;
            currentStamina -= staminaJumpCost;
        }

        if (!isReallyGrounded)
        {
            vSpeed += gravity * Time.deltaTime;
        }
    }

    void HandleGravity()
    {
        if (_characterController.isGrounded && vSpeed < 0)
        {
            vSpeed = -2f;
        }
        else
        {
            vSpeed += gravity * Time.deltaTime;
        }
    }
    #endregion

    void UpdateStamina()
    {
        var input = InputManager.Instance;
        if (input == null) return;

        bool sprintPressed = input.IsSprintPressed();
        bool canSprint = currentState == CharacterState.WALKING && currentStamina > staminaMinToSprint;

        // Corrida
        isSprinting = sprintPressed && canSprint;

        if (isSprinting)
        {
            DrainStamina(staminaSprintCost * Time.deltaTime);
            currentState = CharacterState.SPRINTING;
        }
        else
        {
            // Regenera stamina quando não está correndo
            RegenStamina(staminaRegenRate * Time.deltaTime);

            // volta o estado caso não esteja correndo
            if (currentState == CharacterState.SPRINTING)
                currentState = CharacterState.WALKING;
        }

        // Garante que não ultrapasse limites
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    private void DrainStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0) currentStamina = 0;
    }

    private void RegenStamina(float amount)
    {
        currentStamina += amount;
        if (currentStamina > maxStamina) currentStamina = maxStamina;
    }


    #region Equipament Manager

    public bool EquipItem(ItemSO item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Equipamento] Tentando equipar um item nulo!");
            return false;
        }

        if (item is not Equipment equipment)
        {
            Debug.Log($"[Equipamento] {item.itemName} não é um item equipável.");
            return false;
        }

        Equipment previous = GetEquipped(equipment.equipmentType);
        if (previous == equipment)
        {
            Debug.Log($"[Equipamento] {equipment.itemName} já está equipado.");
            return false;
        }

        bool removed = playerInventory.inventory.RemoveItem(equipment);
        if (!removed) Debug.LogWarning("Falha ao remover do inventário");

        // Equipar
        switch (equipment.equipmentType)
        {
            case EquipmentType.Head: helmet = equipment; break;
            case EquipmentType.Chest: chest = equipment; break;
            case EquipmentType.Legs: legs = equipment; break;
            case EquipmentType.Feet: boots = equipment; break;
            case EquipmentType.Gloves: gloves = equipment; break;
            case EquipmentType.Accessory: accessory = equipment; break;
            case EquipmentType.MainHand: mainHand = equipment; break;
            case EquipmentType.OffHand: offHand = equipment; break;
            default:
                Debug.LogWarning($"[Equipamento] Tipo de equipamento não reconhecido: {equipment.equipmentType}");
                return false;
        }

        Debug.Log($"[Equipamento] {equipment.itemName} equipado em {equipment.equipmentType}");

        // Atualiza o peso
        playerInventory.inventory.GetCurrentWeight();
        // Dispara evento para atualizar UI ou atributos
        OnEquipmentChanged?.Invoke();
        UpdateStats(); // <-- aqui

        return true;
    }

    public Equipment Unequip(EquipmentType type)
    {
        Equipment removed = GetEquipped(type);
        if (removed == null) return null;

        switch (type)
        {
            case EquipmentType.Head: helmet = null; break;
            case EquipmentType.Chest: chest = null; break;
            case EquipmentType.Legs: legs = null; break;
            case EquipmentType.Feet: boots = null; break;
            case EquipmentType.Gloves: gloves = null; break;
            case EquipmentType.Accessory: accessory = null; break;
            case EquipmentType.MainHand: mainHand = null; break;
            case EquipmentType.OffHand: offHand = null; break;
        }

        Debug.Log($"[Equipamento] Desequipado: {type}");

        playerInventory.inventory.AddItem(removed);

        playerInventory.inventory.GetCurrentWeight();
        OnEquipmentChanged?.Invoke();
        UpdateStats(); // <-- aqui

        return removed;
    }



    public Equipment GetEquipped(EquipmentType type)
    {
        return type switch
        {
            EquipmentType.Head => helmet,
            EquipmentType.Chest => chest,
            EquipmentType.Legs => legs,
            EquipmentType.Feet => boots,
            EquipmentType.Gloves => gloves,
            EquipmentType.Accessory => accessory,
            EquipmentType.MainHand => mainHand,
            EquipmentType.OffHand => offHand,
            _ => null
        };
    }
    #endregion

    #region Equip Attributes & Weight

    /// <summary>
    /// Retorna o peso total de todos os equipamentos atualmente equipados.
    /// </summary>
    public float GetEquippedWeight()
    {
        float total = 0f;
        Equipment[] equippedItems =
        {
        helmet, chest, legs, boots, gloves, accessory, mainHand, offHand
    };

        foreach (var item in equippedItems)
        {
            if (item != null)
                total += item.Weight;
        }

        return total;
    }

    public void ApplyEquipmentStats()
    {

        // List of all equipped items
        Equipment[] equippedItems = { helmet, chest, legs, boots, gloves, accessory, mainHand, offHand };

        // Apply modifiers from each equipped item
        foreach (var item in equippedItems)
        {
            if (item != null)
            {
                stats.stamina.maxStamina += item.bonusStamina;
                //Adicionar outros atributos conforme necessário
            }
        }

        // Notify listeners that equipment stats have changed
        OnEquipmentChanged?.Invoke();
    }

    public void UpdateStats()
    {;

        // Lista de equipamentos equipados
        Equipment[] equippedItems =
        {
        helmet, chest, legs, boots, gloves, accessory, mainHand, offHand
    };

        // Aplica os StatModifiers de cada equipamento
        foreach (var item in equippedItems)
        {
            if (item == null) continue;

            foreach (var modifier in item.statModifiers)
            {
                switch (modifier.statName)
                {
                    case "Speed": stats.movement.baseMoveSpeed += modifier.value; break;
                    case "Stamina": stats.stamina.maxStamina += modifier.value; break;
                    default: Debug.LogWarning($"Stat desconhecido: {modifier.statName}"); break;
                }
            }
        }
    }


    #endregion

    public void SetMovementBlocked(bool blocked)
    {
        blockMovement = blocked;

        if (blocked)
        {
            currentHorizontalVelocity = Vector3.zero;
            vSpeed = 0f;
        }
    }
}


