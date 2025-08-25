using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;       // Player
    public Transform cam;          // MainCamera
    public float distance = 5f;    // Distância da câmera
    public float mouseSensitivity = 2f;
    public float minY = -30f;      // Limite vertical
    public float maxY = 60f;

    private float rotX = 0f;
    private float rotY = 0f;

    public InputActionReference lookAction;

    public InventoryToggle inventoryToggle;

    private bool blockCamera = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        InventoryToggle.OnInventoryToggled += HandleInventoryToggled;
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        InventoryToggle.OnInventoryToggled -= HandleInventoryToggled;
        lookAction.action.Disable();
    }

    private void HandleInventoryToggled(bool isOpen)
    {
        blockCamera = isOpen;
    }

    void LateUpdate()
    {
        if (blockCamera) return; // bloqueia câmera enquanto inventário aberto

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        // Leitura do mouse
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime * 100f;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime * 100f;

        Vector2 scaledDelta = Vector2.Scale(lookInput, new Vector2(0.1f, 0.1f));
        rotY += scaledDelta.x;
        rotX -= scaledDelta.y;

        // Limita a rotação vertical
        rotX = Mathf.Clamp(rotX, minY, maxY);

        // Aplica rotação no pivot
        transform.rotation = Quaternion.Euler(rotX, rotY, 0);

        // Posiciona a câmera atrás do pivot
        cam.position = transform.position - transform.forward * distance;
        cam.LookAt(target.position + Vector3.up * 1.7f); // Ajusta para olhar a cabeça do player
    }
}
