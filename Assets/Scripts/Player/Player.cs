using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using DG.Tweening;
using UnityEngine.UI;

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

    [Header("UI Bars")]
    public Image healthBar;
    public Image staminaBar;
    public TMP_Text staminaText;
    public TMP_Text healthText;

    [Header("CombatSystem")]
    public bool isLockedOn = false;
    public Transform lockTarget;

    public bool rightClickAim = false;   // mira com botão direito
    public bool autoTurnOnAttack = true; // vira quando atacar
    public bool isAttacking = false;
    public float attackCooldown => stats.combat.attackCooldown;
    public float attackDamage => stats.combat.attackDamage;
    public float attackRange => stats.combat.attackRange;
    public float attackKnockback => stats.combat.attackKnockback;
    public float attackStaminaCost => stats.combat.attackStaminaCost;
    public int maxHealth => stats.combat.maxHealth;
    public int currentHealth;
    public int HealthRegenRate => stats.combat.HealthRegenRate;
    public float HealthRegenDelay => stats.combat.HealthRegenDelay;
    public float defense => stats.combat.defense;
    public float HealthRegenInterval = 0.5f;

    private float lastAttackTime = -Mathf.Infinity;
    private Coroutine regenCoroutine;

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

    // coroutine para resetar isAttacking
    private Coroutine attackResetRoutine;

    private bool attackBlocked = false;

    // parâmetros de hit
    [Header("Attack Hit Settings")]
    public float attackHitRadius = 0.6f;
    public LayerMask attackLayerMask = ~0; // por padrão atinge tudo; ajuste no inspector para camada de inimigos

    private void Awake()
    {
        Debug.Log("PLAYER AWAKE");

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
        // subscribe para ataque
        InputManager.Instance.OnAttackPerformed += OnAttackPerformed;

        // fallback para cameraTransform se não estiver atribuído
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // inicializações anteriores
        currentStamina = maxStamina;
        currentHealth = maxHealth;
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
        {
            InputManager.Instance.OnInteractPerformed -= OnInteractPerformed;
            InputManager.Instance.OnAttackPerformed -= OnAttackPerformed;
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        // chama o método de ataque quando a action "Attack" for performed
        Attack();
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

        // Atualiza estado de mira com botão direito (segurar) - informativo, para UI etc.
        rightClickAim = InputManager.Instance != null && InputManager.Instance.IsAimPressed();

        // Toggle lock-on quando o botão de lock é pressionado
        if (InputManager.Instance != null && InputManager.Instance.WasLockPressedThisFrame())
        {
            TryToggleLockFromCrosshair();
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
        animator.SetBool("isSprinting", currentState == CharacterState.SPRINTING);
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

        Vector3 cameraForward = Vector3.zero;
        Vector3 cameraRight = Vector3.zero;
        if (cameraTransform != null)
        {
            cameraForward = cameraTransform.forward;
            cameraRight = cameraTransform.right;
        }
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

        // ROTATION: só alteramos a rotação se NÃO estivermos em ataque (para não sobrescrever a rotação definida no Attack)
        bool aimPressedNow = InputManager.Instance != null && InputManager.Instance.IsAimPressed();

        if (!(isAttacking && autoTurnOnAttack))
        {
            if (isLockedOn && lockTarget != null)
            {
                Vector3 lookDir = lockTarget.position - transform.position;
                lookDir.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
            else if (aimPressedNow)
            {
                // Vira para onde a câmera está apontando
                Vector3 camForward = cameraForward;
                camForward.y = 0;

                if (camForward.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(camForward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }
            }
            else if (moveDirection != Vector3.zero)
            {
                // Movimento normal
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
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
    #endregion

    void HandleJump()
    {
        if (isReallyGrounded && InputManager.Instance.WasJumpPressedThisFrame() && currentStamina >= staminaJumpCost)
        {
            vSpeed = jumpForce;
            currentState = CharacterState.JUMPING;
            lastGroundedTime = -1;
            currentStamina -= staminaJumpCost;
            UpdateStaminaUI();
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
        UpdateStaminaUI();
        if (currentStamina < 0) currentStamina = 0;
    }

    private void RegenStamina(float amount)
    {
        currentStamina += amount;
        UpdateStaminaUI();
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
    {
        ;

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

    public void SetAttackBlocked(bool blocked)
    {
        attackBlocked = blocked;

        if (blocked)
        {
            // Cancela ataque atual
            if (isAttacking)
            {
                isAttacking = false;

                if (attackResetRoutine != null)
                {
                    StopCoroutine(attackResetRoutine);
                    attackResetRoutine = null;
                }
            }
        }
    }

    #region Combat System
    public void ToggleLock(Transform target)
    {
        if (isLockedOn)
        {
            isLockedOn = false;
            lockTarget = null;
            return;
        }

        if (target != null)
        {
            isLockedOn = true;
            lockTarget = target;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();

        StartHealthRegen();
        if (currentHealth <= 0)
            Die();
    }

    private void StartHealthRegen()
    {
        // Se já está regenerando, reinicia
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        regenCoroutine = StartCoroutine(RegenHealthRoutine());
    }

    private void RegenHealth(float amount)
    {
        currentHealth += HealthRegenRate;
        FloatingTextManager.Instance.CreateText("+" + HealthRegenRate, transform.position, Color.green);
        UpdateHealthUI();
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    private IEnumerator RegenHealthRoutine()
    {
        // espera o delay antes de começar a regenerar
        yield return new WaitForSeconds(HealthRegenDelay);

        // enquanto não estiver cheio
        while (currentHealth < maxHealth)
        {
            RegenHealth(HealthRegenRate);
            yield return new WaitForSeconds(HealthRegenInterval);
        }

        regenCoroutine = null; // terminou
    }

    private void Die()
    {
        Debug.Log($"morreu!");
        //lógica de morte do jogador aqui

    }

    public void Attack()
    {

        if (attackBlocked)
            return;

        // Verifica stamina
        if (currentStamina < attackStaminaCost)
        {
            // opcional: reproduzir som de falta de stamina
            return;
        }

        // Evita spam
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        isAttacking = true;

        // consome stamina
        DrainStamina(attackStaminaCost);

        HandleAttackRotation();

        // detectar e aplicar dano imediatamente (pode sincronizar com animação se quiser)
        PerformAttackHit();

        // iniciar reset para isAttacking (ideal: reset via AnimationEvent ao terminar animação)
        if (attackResetRoutine != null) StopCoroutine(attackResetRoutine);
        attackResetRoutine = StartCoroutine(ResetAttackState(Mathf.Clamp(attackCooldown * 0.5f, 0.1f, attackCooldown)));

        animator.SetTrigger("attack");
    }

    IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
        attackResetRoutine = null;
    }

    void PerformAttackHit()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 dir = transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, attackHitRadius, dir, attackRange, attackLayerMask, QueryTriggerInteraction.Ignore);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            Vector3 pushDir = (hit.collider.transform.position - transform.position);
            pushDir.y = 0;
            if (pushDir.sqrMagnitude == 0) pushDir = dir;

            // 1️⃣ Tenta achar NPC
            NPC npc = hit.collider.GetComponentInParent<NPC>();
            if (npc != null)
            {
                //npc.ReceiveDamage(Mathf.RoundToInt(attackDamage));

                if (!npc.isInvincible)
                {
                    FloatingTextManager.Instance.CreateText(
                        "-" + attackDamage + " dealt to " + npc.npcName,
                        transform.position,
                        Color.red
                    );

                    ApplyKnockback(npc.gameObject, pushDir);
                }
                continue;
            }

            // 2️⃣ Se não achou NPC, tenta achar Enemy
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            enemy.TakeDamage(Mathf.RoundToInt(attackDamage));

            FloatingTextManager.Instance.CreateText(
                "-" + attackDamage.ToString() + " dealt to " + enemy.enemyName,
                transform.position,
                Color.red
            );

            ApplyKnockback(enemy.gameObject, pushDir);
        }
    }

    void ApplyKnockback(GameObject target, Vector3 pushDir)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(pushDir.normalized * attackKnockback, ForceMode.Impulse);
            return;
        }

        var enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.ApplyKnockback(pushDir, attackKnockback);
        }
    }

    void HandleAttackRotation()
    {
        // 1 — Se estiver em lock-on → sempre olha para o alvo
        if (isLockedOn && lockTarget != null)
        {
            Vector3 dir = lockTarget.position - transform.position;
            dir.y = 0;
            transform.rotation = Quaternion.LookRotation(dir);
            return;
        }

        // 2 — Se estiver segurando botão direito AGORA → mira com a câmera
        bool aimPressedNow = InputManager.Instance != null && InputManager.Instance.IsAimPressed();
        if (aimPressedNow)
        {
            Vector3 camForward = (cameraTransform != null ? cameraTransform.forward : Vector3.forward);
            camForward.y = 0;
            transform.rotation = Quaternion.LookRotation(camForward);
            return;
        }

        // 3 — Se NÃO estiver se movendo → vira instantaneamente para a câmera
        Vector2 input = InputManager.Instance.GetMoveVector();
        if (input.magnitude < 0.1f)
        {
            Vector3 camForward = (cameraTransform != null ? cameraTransform.forward : Vector3.forward);
            camForward.y = 0;
            transform.rotation = Quaternion.LookRotation(camForward);
        }
    }

    private void TryToggleLockFromCrosshair()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            ToggleLock(null);
            return;
        }

        Ray ray = cam.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f));
        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            var enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                ToggleLock(enemy.transform);
                return;
            }
        }

        // se não houver inimigo no centro, desativa lock
        ToggleLock(null);
    }

    #endregion

    public void UpdateHealthUI(bool instant = false)
    {
        if (healthBar == null) return;

        float fill = (float)currentHealth / maxHealth;

        healthText.text = $"{currentHealth} / {maxHealth}";

        if (instant)
            healthBar.fillAmount = fill;
        else
            healthBar.DOFillAmount(fill, 0.25f).SetEase(Ease.OutQuad);
    }

    public void UpdateStaminaUI(bool instant = false)
    {
        if (staminaBar == null) return;

        float fill = currentStamina / maxStamina;

        staminaText.text = $"{Mathf.RoundToInt(currentStamina)} / {Mathf.RoundToInt(maxStamina)}";

        if (instant)
            staminaBar.fillAmount = fill;
        else
            staminaBar.DOFillAmount(fill, 0.2f).SetEase(Ease.OutQuad);
    }
}