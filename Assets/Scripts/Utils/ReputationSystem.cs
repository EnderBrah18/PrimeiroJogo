using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReputationSystem : MonoBehaviour, ISavable
{
    public static ReputationSystem Instance { get; private set; }

    // reputations por id de personagem
    private readonly Dictionary<string, int> reputations = new Dictionary<string, int>();
    // listeners por id (notifica somente quem está interessado naquele id)
    private readonly Dictionary<string, Action<int>> listeners = new Dictionary<string, Action<int>>();
    // quando foi a última modificação (Time.time)
    private readonly Dictionary<string, float> lastModified = new Dictionary<string, float>();

    [Header("Recuperação")]
    [Tooltip("Tempo (s) após a última alteração antes de começar a recuperação")]
    public float recoveryDelay = 30f;
    [Tooltip("Unidades de reputação por segundo movendo-se em direção a 0")]
    public float recoverySpeedPerSecond = 2f;
    [Tooltip("Valor padrão se objeto não existir")]
    public int defaultReputation = 0;

    [Header("Database de reputações iniciais")]
    public ReputationDatabase database;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SaveSystem.Instance.RegisterSavable(this);

        if (database != null)
        {
            foreach (var entry in database.entries)
            {
                reputations[entry.id] = entry.startValue;
            }
        }

        StartCoroutine(RecoveryLoop());
    }

    public int GetReputation(string id)
    {
        if (string.IsNullOrEmpty(id)) return defaultReputation;
        reputations.TryGetValue(id, out var v);
        return v;
    }

    public void SetReputation(string id, int value)
    {
        if (string.IsNullOrEmpty(id)) return;
        reputations[id] = value;
        lastModified[id] = Time.time;
        Notify(id, value);
    }

    public void AdjustReputation(string id, int delta)
    {
        if (string.IsNullOrEmpty(id)) return;
        int cur = GetReputation(id);
        int next = cur + delta;
        SetReputation(id, next);
    }

    public int GetEffectiveReputation(string characterId, string factionId)
    {
        int charRep = GetReputation(characterId);
        int facRep = GetReputation(factionId);

        return Mathf.RoundToInt((charRep + facRep) * 0.5f); // média
    }

    private Dictionary<string, float> repCooldown = new();

    public bool CanModify(string id, float cooldown = 1f)
    {
        if (!repCooldown.ContainsKey(id) || Time.time - repCooldown[id] >= cooldown)
        {
            repCooldown[id] = Time.time;
            return true;
        }
        return false;
    }

    public void AddReputation(string id, int amount) => AdjustReputation(id, Math.Abs(amount));
    public void RemoveReputation(string id, int amount) => AdjustReputation(id, -Math.Abs(amount));

    // Subscrição por id (permite unsubscribe facilmente)
    public void Subscribe(string id, Action<int> callback)
    {
        if (string.IsNullOrEmpty(id) || callback == null) return;
        if (!listeners.ContainsKey(id)) listeners[id] = null;
        listeners[id] += callback;
    }

    public void Unsubscribe(string id, Action<int> callback)
    {
        if (string.IsNullOrEmpty(id) || callback == null) return;
        if (listeners.ContainsKey(id))
            listeners[id] -= callback;
    }

    private void Notify(string id, int value)
    {
        if (listeners.ContainsKey(id))
            listeners[id]?.Invoke(value);
    }

    private IEnumerator RecoveryLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            var keys = new List<string>(reputations.Keys);
            float now = Time.time;
            foreach (var id in keys)
            {
                if (!lastModified.TryGetValue(id, out float t)) t = 0f;
                if (now - t < recoveryDelay) continue;

                int cur = GetReputation(id);
                if (cur == 0) continue;

                // move em direção a zero
                float step = recoverySpeedPerSecond * 1f;
                int next;
                if (cur > 0) next = (int)Mathf.Max(0, cur - step);
                else next = (int)Mathf.Min(0, cur + step);

                if (next != cur)
                {
                    reputations[id] = next;
                    lastModified[id] = now;
                    Notify(id, next);
                }
            }
        }
    }

    public string GetSaveKey() => "REPUTATION_SYSTEM";

    public string SaveData()
    {
        ReputationSaveData data = new ReputationSaveData();
        data.reputations = reputations;
        return JsonUtility.ToJson(data);
    }

    [System.Serializable]
    public class ReputationSaveData
    {
        public Dictionary<string, int> reputations;
    }

    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<ReputationSaveData>(json);

        reputations.Clear();

        foreach (var kvp in data.reputations)
            reputations[kvp.Key] = kvp.Value;
    }
}