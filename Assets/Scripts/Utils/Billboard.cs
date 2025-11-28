using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        // Garante que o canvas esteja sempre virado para a câmera
        transform.LookAt(transform.position + cam.forward);
    }
}
