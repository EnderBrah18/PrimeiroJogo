using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public enum CharacterState
{
    IDLE,
    WALKING,
    JUMPING,
    FALLING,
    DASHING,
    SPRINTING,
    CLIMB_JUMP_AIMING,
    CLIMBING
}

public class Player : MonoBehaviour
{

    public CharacterState currentState = CharacterState.IDLE;

    [Header("Inputs")]
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction sprintAction;
    private InputAction interactAction; 

    public CharacterController _characterController;
    public Transform cameraTransform;

    [Header("Movimentação")]
    public float turnSpeed = 10f;
    public float jumpForce = 8f;
    public float moveSpeed = 10f;
    public float vSpeed = 0f;
    public float gravity = -9.8f;

    private float groundedGraceTime = 0.15f;
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
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f;
    public float staminaSprintCost = 20f; // por segundo
    public float staminaClimbCost = 10f;  // por segundo
    public float staminaJumpCost = 15f;
    public float staminaMinToSprint = 5f;
    public float staminaMinToClimb = 5f;
    public float sprintMultiplier = 1.75f;
    public bool isSprinting = false;

    [Header("Collect")]
    public float collectRange = 2f;
    public Tools equipedTool;

    private bool isGrabbingWall = false;
    private Vector3 finalMovement;
    private Coroutine dashRoutine;
    private Vector3 currentHorizontalVelocity = Vector3.zero;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInput.actions.Enable();

        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
        dashAction = playerInput.actions.FindAction("Dash");
        sprintAction = playerInput.actions.FindAction("Sprint");
        interactAction = playerInput.actions.FindAction("Interact");

        CollectableManager.Instance.SetInteractAction(interactAction);

        currentStamina = maxStamina;

        if (_characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
            vSpeed = -2f;
            currentState = CharacterState.IDLE;
        }
        else
        {
            currentState = CharacterState.FALLING;
        }
    }

    private void Update()
    {

        finalMovement = Vector3.zero;

        #region CollectUpdate

        if (interactAction.WasPressedThisFrame())
        {
            CollectableManager.Instance.TryCollect();
        }
        #endregion

        if (_characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }
        if (currentState == CharacterState.CLIMBING && jumpAction.WasPressedThisFrame())
        {
            isGrabbingWall = false;
            currentState = CharacterState.FALLING;
            vSpeed = 0f;

            return;
        }

            if (CheckClimbableWall() && jumpAction.WasPressedThisFrame())
        {
            isGrabbingWall = true;
            currentState = CharacterState.CLIMBING;
            currentHorizontalVelocity = Vector3.zero;
        }


            switch (currentState)
        {
            case CharacterState.IDLE:
            case CharacterState.WALKING:
                HandleMovement();
                HandleJump();
                HandleDash();
                break;

            case CharacterState.JUMPING:
            case CharacterState.FALLING:
                HandleMovement();
                HandleGravity();
                break;

            case CharacterState.CLIMBING:
                HandleClimb();
                break;

            case CharacterState.DASHING:
                // Durante o dash, o movimento é tratado pela coroutine
                break;
        }

        Vector3 horizontalVelocity = currentHorizontalVelocity;
        _characterController.Move(horizontalVelocity * Time.deltaTime);

        Vector3 verticalVelocity = Vector3.up * vSpeed;
        _characterController.Move(verticalVelocity * Time.deltaTime);

        UpdateStamina();
        UpdateState();
    }

    void UpdateState()
    {
        if (currentState == CharacterState.CLIMBING && (!CheckClimbableWall() || !isGrabbingWall))
        {
            isGrabbingWall = false;
            currentState = CharacterState.FALLING;
            return;
        }

        if (currentState != CharacterState.CLIMBING && currentState != CharacterState.DASHING)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();

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
    }
    #region Movement
    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

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
        if (isReallyGrounded && jumpAction.WasPressedThisFrame() && currentStamina >= staminaJumpCost)
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

    #region DASH
    void HandleDash()
    {
        if (dashAction.WasPressedThisFrame() && Time.time >= lastDashTime + dashCooldown && dashRoutine == null)
        {
            dashRoutine = StartCoroutine(PerformDash());
        }
    }

    IEnumerator PerformDash()
    {
        currentState = CharacterState.DASHING;
        float elapsedTime = 0f;
        Vector3 dashDirection = transform.forward;

        while (elapsedTime < dashDuration)
        {
            float step = (dashDistance / dashDuration) * Time.deltaTime;
            if (!Physics.CapsuleCast(_characterController.transform.position + _characterController.center, _characterController.transform.position + _characterController.center, _characterController.radius, dashDirection, out RaycastHit hit, step, dashCollisionMask))
            {
                _characterController.Move(dashDirection * step);
            }
            else
            {
                break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        lastDashTime = Time.time;
        dashRoutine = null;
        currentState = CharacterState.FALLING;
    }
    #endregion
    #region Climb
    void HandleClimb()
    {
        if (currentStamina < staminaMinToClimb)
        {
            isGrabbingWall = false;
            currentState = CharacterState.FALLING;
            vSpeed = 0f;
            return;
        }

        if (!CheckClimbableWall())
        {
            isGrabbingWall = false;
            currentState = CharacterState.FALLING;
            vSpeed = 0f;
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();

        if (Input.GetKey(KeyCode.LeftControl))
        {
            Vector3 launchDirection = Vector3.zero;

            if (input.y < -0.5f)
            {
                launchDirection = -transform.forward + Vector3.up * 0.75f;
            }
            else if (Mathf.Abs(input.x) > 0.5f)
            {
                launchDirection = transform.right * Mathf.Sign(input.x) + Vector3.up * 0.75f;
            }

            if (launchDirection != Vector3.zero)
            {
                launchDirection.Normalize();
                vSpeed = jumpForce * 0.75f;
                currentHorizontalVelocity = launchDirection * moveSpeed * 1.2f;
                currentState = CharacterState.JUMPING;
                return;
            }

        }


        Vector3 upward = Vector3.up * input.y * climbSpeed;
        Vector3 sideways = transform.right * input.x * lateralClimbSpeed;
        Vector3 climbMovement = upward + sideways;
        _characterController.Move(climbMovement * Time.deltaTime);
        vSpeed = 0f;

        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, climbCheckDistance, climbableMask))
        {
            Vector3 forwardOnWall = -hit.normal;
            forwardOnWall.y = 0;
            transform.rotation = Quaternion.LookRotation(forwardOnWall);
        }

    }

    bool CheckClimbableWall()
    {
        Vector3 origin = transform.position + Vector3.up;
        Vector3 direction = transform.forward;
        return Physics.Raycast(origin, direction, climbCheckDistance, climbableMask);
    }

    void UpdateStamina()
    {
        // Corrida
        isSprinting = sprintAction.IsPressed() && currentState == CharacterState.WALKING && currentStamina > staminaMinToSprint;

        if (isSprinting)
        {
            currentStamina -= staminaSprintCost * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0);
            currentState = CharacterState.SPRINTING;
        }
        else if (currentState != CharacterState.CLIMBING)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
        }

        // Escalada
        if (currentState == CharacterState.CLIMBING)
        {
            if (currentStamina <= 0)
            {
                isGrabbingWall = false;
                currentState = CharacterState.FALLING;
                return;
            }

            currentStamina -= staminaClimbCost * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0);
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }
    #endregion
    #endregion

    public void EquiparFerramenta(Tools newTool)
    {
        equipedTool = newTool;
        Debug.Log($"Ferramenta equipada: {newTool.name}");
    }
}


