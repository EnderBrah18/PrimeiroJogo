using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public enum CharacterState
{
    IDLE,
    WALKING,
    JUMPING,
    FALLING,
    DASHING,
}

public class PlayerMovement : MonoBehaviour
{
    public CharacterState currentState = CharacterState.IDLE;

    [Header("Inputs")]
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

    public CharacterController _characterController;
    public Transform cameraTransform;

    [Header("Movimentação")]
    public float turnSpeed = 1f;
    public float jumpForce = 12f;
    public float moveSpeed = 1f;
    public float vSpeed = 0f;
    public float gravity = -30f;

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
    public float airControlMultiplier = 0.5f;
    public float airAcceleration = 5f;
    public float airDrag = 2f;

    private Vector3 finalMovement;
    private Coroutine dashRoutine;
    private Vector3 currentHorizontalVelocity = Vector3.zero;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
        dashAction = playerInput.actions.FindAction("Dash");

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

        if (_characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
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
        }

        // Aplicar movimento horizontal primeiro
        Vector3 horizontalVelocity = currentHorizontalVelocity;
        _characterController.Move(horizontalVelocity * Time.deltaTime);

        // Aplicar movimento vertical depois
        Vector3 verticalVelocity = Vector3.up * vSpeed;
        _characterController.Move(verticalVelocity * Time.deltaTime);

        UpdateState();
    }

    void UpdateState()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        if (!isReallyGrounded)
        {
            currentState = vSpeed > 0 ? CharacterState.JUMPING : CharacterState.FALLING;
        }
        else if (IsMoving())
        {
            currentState = CharacterState.WALKING;
        }
        else
        {
            currentState = CharacterState.IDLE;
        }
    }

    bool IsMoving()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        return input.magnitude > 0.1f;
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;

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
            currentHorizontalVelocity = moveDirection * moveSpeed;
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
        if (isReallyGrounded && jumpAction.WasPressedThisFrame())
        {
            vSpeed = jumpForce;
            currentState = CharacterState.JUMPING;
            lastGroundedTime = -1;
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
                break; // colisão detectada
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        lastDashTime = Time.time;
        dashRoutine = null;
        currentState = CharacterState.FALLING;
    }
    #endregion
}

