using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSpawnManager : MonoBehaviour
{
    private static TileSpawnManager _instance;
    public static TileSpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("TileSpawnManager");
                _instance = go.AddComponent<TileSpawnManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Queue<IEnumerator> taskQueue = new Queue<IEnumerator>();
    private bool isProcessing = false;

    /// <summary>
    /// Registra uma tarefa (Coroutine) para execução sequencial.
    /// </summary>
    public void EnqueueTask(IEnumerator task)
    {
        taskQueue.Enqueue(task);

        if (!isProcessing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (taskQueue.Count > 0)
        {
            IEnumerator currentTask = taskQueue.Dequeue();
            yield return StartCoroutine(currentTask);
        }

        isProcessing = false;
        Debug.Log("✅ Todas as tarefas de geração foram concluídas!");
    }
}
