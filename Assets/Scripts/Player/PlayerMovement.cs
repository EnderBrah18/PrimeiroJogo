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
    CLIMBING
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
    public float airControlMultiplier = 0.5f; // Adicionado

    private float groundedGraceTime = 0.15f;
    private float lastGroundedTime;
    private bool isReallyGrounded => Time.time - lastGroundedTime <= groundedGraceTime;

    [Header("Dash")]
    public float dashDistance = 10f;
    public float dashCooldown = 1f;
    public float lastDashTime = -Mathf.Infinity;

    [Header("Escalada")]
    public float climbSpeed = 3f;
    public float climbCheckDistance = 1f;
    public LayerMask climbableLayer;
    private bool isClimbing = false;
    private float climbCooldown = 0.5f;
    private float lastClimbExitTime = -Mathf.Infinity;

    private void Start()
    {
        if (_characterController.isGrounded)
        {
            vSpeed = -10f;
        }

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
        jumpAction = playerInput.actions.FindAction("Jump");
        dashAction = playerInput.actions.FindAction("Dash");
    }

    private void Update()
    {
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
                CheckClimb();
                break;

            case CharacterState.JUMPING:
            case CharacterState.FALLING:
                HandleMovement();
                HandleGravity();
                CheckClimb();
                break;

            case CharacterState.DASHING:
                HandleDash();
                break;

            case CharacterState.CLIMBING:
                HandleClimb();
                break;
        }

        UpdateState();
    }

    void UpdateState()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool isMovingOnWall = input.y > 0.1f || Mathf.Abs(input.x) > 0.1f || input.y < -0.1f;

        if (currentState == CharacterState.CLIMBING)
        {
            if (IsTouchingClimbable(out RaycastHit hit))
            {
                transform.rotation = Quaternion.LookRotation(-hit.normal);
                return;
            }
            else
            {
                isClimbing = false;
                currentState = CharacterState.FALLING;
                return;
            }
        }

        if (IsTouchingClimbable(out RaycastHit climbHit) && isMovingOnWall)
        {
            isClimbing = true;
            transform.rotation = Quaternion.LookRotation(-climbHit.normal);
            currentState = CharacterState.CLIMBING;
        }
        else if (!isReallyGrounded)
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

        Vector3 moveDirection = Vector3.zero;
        if (inputDirection != Vector3.zero)
        {
            moveDirection = (cameraForward * input.y + cameraRight * input.x);

            // Só rotaciona se estiver no chão
            if (currentState != CharacterState.JUMPING && currentState != CharacterState.FALLING)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }

        float controlMultiplier = (currentState == CharacterState.JUMPING || currentState == CharacterState.FALLING)
            ? airControlMultiplier : 1f;

        Vector3 speedVector = moveDirection * moveSpeed * controlMultiplier;
        speedVector.y = vSpeed;

        _characterController.Move(speedVector * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isReallyGrounded && jumpAction.WasPressedThisFrame())
        {
            vSpeed = jumpForce;
            currentState = CharacterState.JUMPING;
            lastGroundedTime = -1;
        }
    }

    void HandleGravity()
    {
        if (currentState == CharacterState.CLIMBING) return;

        if (!_characterController.isGrounded)
        {
            vSpeed += gravity * Time.deltaTime;
        }
        else if (vSpeed < 0)
        {
            vSpeed = gravity * Time.deltaTime;
        }
    }

    void HandleDash()
    {
        if (dashAction.WasPressedThisFrame() && Time.time >= lastDashTime + dashCooldown)
        {
            Vector3 dashDirection = transform.forward;
            _characterController.Move(dashDirection * dashDistance);
            lastDashTime = Time.time;
        }
    }

    void CheckClimb()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, climbCheckDistance, climbableLayer))
        {
            if (Mathf.Abs(input.x) > 0.1f || Mathf.Abs(input.y) > 0.1f)
            {
                isClimbing = true;
            }
        }
        else
        {
            isClimbing = false;
        }
    }

    void HandleClimb()
    {
        if (!IsTouchingClimbable(out RaycastHit hit))
        {
            isClimbing = false;
            currentState = CharacterState.FALLING;
            return;
        }

        Vector3 wallNormal = hit.normal;
        Vector3 lookDirection = -wallNormal;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        float adjustedY = input.y;
        if (Mathf.Abs(input.x) > 0.1f && Mathf.Abs(input.y) < 0.1f)
            adjustedY = 0.3f;

        Vector3 climbDir = new Vector3(input.x, adjustedY, 0);
        Vector3 localClimb = transform.TransformDirection(climbDir);
        _characterController.Move(localClimb * climbSpeed * Time.deltaTime);

        if (jumpAction.WasPressedThisFrame())
        {
            isClimbing = false;
            vSpeed = jumpForce;
            currentState = CharacterState.JUMPING;

            Vector3 pushBack = -transform.forward * 0.3f;
            _characterController.Move(pushBack);
            lastClimbExitTime = Time.time;
        }
    }

    bool IsTouchingClimbable(out RaycastHit hit)
    {
        Ray ray = new Ray(transform.position + Vector3.up * 1.0f, transform.forward);
        return Physics.Raycast(ray, out hit, climbCheckDistance + 0.1f, climbableLayer);
    }
}

