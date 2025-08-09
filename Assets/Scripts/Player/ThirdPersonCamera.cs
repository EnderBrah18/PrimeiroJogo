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

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // Leitura do mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotX -= mouseY;
        rotY += mouseX;

        // Limita a rotação vertical
        rotX = Mathf.Clamp(rotX, minY, maxY);

        // Aplica rotação no pivot
        transform.rotation = Quaternion.Euler(rotX, rotY, 0);

        // Posiciona a câmera atrás do pivot
        cam.position = transform.position - transform.forward * distance;
        cam.LookAt(target.position + Vector3.up * 1.7f); // Ajusta para olhar a cabeça do player
    }
}
