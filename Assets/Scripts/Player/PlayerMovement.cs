using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    private PlayerInput playerInput;
    private InputAction moveAction;
    public CharacterController _characterController;

    public Transform cameraTransform;

    public float turnSpeed = 1f;
    public float moveSpeed = 1f;


    public float vSpeed = 0f;
    public float gravity = -9.81f;

    private void Start()
    {

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
    }

    private void Update()
    {
        Move();
    }

    void Move()
    {
        // Obter os valores de entrada usando o novo sistema InputAction
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(input.x, 0, input.y).normalized;

        // Converte a direção da entrada para a direção baseada na câmera
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x);

        // Calcula a velocidade final de movimento
        Vector3 speedVector = moveDirection * moveSpeed;

        // Atualizar a velocidade vertical com gravidade
        if (_characterController.isGrounded)
        {
            vSpeed = -1f; // Mantém o personagem colado no chão
        }
        else
        {
            vSpeed += gravity * Time.deltaTime;
        }
        speedVector.y = vSpeed;

        // Se houver alguma entrada, rotaciona o personagem na direção da entrada
        if (inputDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Agora sim, aplica o movimento completo (x, y, z)
        _characterController.Move(speedVector * Time.deltaTime);

    }
}

