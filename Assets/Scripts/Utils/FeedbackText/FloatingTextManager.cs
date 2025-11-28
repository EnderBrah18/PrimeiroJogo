using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance;

    public FloatingText floatingTextPrefab;
    public Transform targetCanvas;

    private void Awake()
    {
        Instance = this;
    }

    public void CreateText(string text, Vector3 worldPos, Color color)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        var obj = Instantiate(floatingTextPrefab, targetCanvas);
        obj.transform.position = screenPos;

        obj.GetComponent<FloatingText>().Show(text, color);
    }
}