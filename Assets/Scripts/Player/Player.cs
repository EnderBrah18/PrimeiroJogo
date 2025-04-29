using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movimentação")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    PlayerInput playerInputs;

    private InputAction moveAction;
    private InputAction jumpAction;

    private CharacterController _characterController;
    private Vector3 velocity;
    private bool isGrounded;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        playerInputs = GetComponent<PlayerInput>();
        moveAction = playerInputs.actions.FindAction("Move");
        jumpAction = playerInputs.actions.FindAction("Jump");
    }

    private void Update()
    {
        isGrounded = _characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        MoveAction();
        JumpAction();
    }

    void MoveAction()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        _characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    void JumpAction()
    {
        if (jumpAction.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Aplicar gravidade
        velocity.y += gravity * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}
