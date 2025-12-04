using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance;

    [Header("Time Settings")]
    [Range(0, 24f)]
    public float currentHour = 6f; // começa às 6 da manhã

    [Tooltip("Quantas horas do jogo passam por 1 segundo real")]
    public float timeScale = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Avança o relógio
        currentHour += Time.deltaTime * timeScale;

        // Loop no relógio 0-24
        if (currentHour >= 24f)
            currentHour -= 24f;
    }

    public float GetHour()
    {
        return currentHour;
    }
}
