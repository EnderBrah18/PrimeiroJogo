using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    [Tooltip("Prefab do player (usado apenas se não houver um Player já existente).")]
    public GameObject playerPrefab;

    private void Awake()
    {
        // Opcional: garantir que este manager persista (coloque se o Bootstrap deve manter tudo)
        DontDestroyOnLoad(gameObject);

        // Subcrever evento de cena carregada
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Callback chamado quando qualquer cena termina de carregar
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Use coroutine para garantir que todos os objetos da cena estejam "prontos"
        StartCoroutine(HandleSpawnNextFrame());
    }

    private IEnumerator HandleSpawnNextFrame()
    {
        // Espera um frame para garantir que todos os objetos Awake/Start da cena já rodaram
        yield return null;

        // Encontra todos os spawn points na cena carregada
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        // Escolhe o spawn com base no ID salvo
        SpawnPoint chosen = null;
        foreach (var sp in spawnPoints)
        {
            if (sp.spawnID == PlayerSpawnInfo.targetSpawnPoint)
            {
                chosen = sp;
                break;
            }
        }

        // Se não achar, tenta usar qualquer Default ou o primeiro disponível
        if (chosen == null && spawnPoints.Length > 0)
        {
            // tenta achar "Default"
            foreach (var sp in spawnPoints)
                if (sp.spawnID == "Default")
                {
                    chosen = sp;
                    break;
                }

            if (chosen == null)
                chosen = spawnPoints[0];
        }

        // Tenta encontrar um player já existente (tag "Player")
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            // Move o player para o spawn escolhido (se existir)
            if (chosen != null)
            {
                player.transform.position = chosen.transform.position;
                player.transform.rotation = chosen.transform.rotation;
            }
            // Caso queira resetar velocity / rigidbody:
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            // Se não houver player na cena, instancia a partir do prefab (se atribuído)
            if (playerPrefab != null)
            {
                Vector3 pos = Vector3.zero;
                Quaternion rot = Quaternion.identity;

                if (chosen != null)
                {
                    pos = chosen.transform.position;
                    rot = chosen.transform.rotation;
                }

                GameObject instantiated = Instantiate(playerPrefab, pos, rot);

                // (Opcional) marque o player para não ser destruído ao trocar de cenas, se desejar:
                DontDestroyOnLoad(instantiated);
            }
            else
            {
                Debug.LogWarning("SpawnManager: Nenhum Player encontrado e playerPrefab não atribuído.");
            }
        }
    }
}
