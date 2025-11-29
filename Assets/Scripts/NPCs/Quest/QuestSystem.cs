using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestSystem : MonoBehaviour, ISOSavable
{

    public static QuestSystem Instance;

    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed
    }

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        SaveSystem.Instance.RegisterSOSavable(this);
    }

    [System.Serializable]
    public class Quest
    {
        public string questName;
        public int rewardGold;
        public QuestState state;
        public NPC questGiver;

        // --- Variable Effects (RPG Maker Style) ---
        public string startVariableName;
        public int startVariableValue;

        public string completeVariableName;
        public int completeVariableValue;

        // --- Requirements ---
        public string requirementVariable;
        public string requirementOperator = "=";
        public int requirementValue;

        public Quest(string name, int reward, NPC giver)
        {
            questName = name;
            rewardGold = reward;
            questGiver = giver;
            state = QuestState.NotStarted;
        }
    }

    public Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    public Dictionary<string, Quest> completedQuests = new Dictionary<string, Quest>();


    // ----------------------------------------------------------
    // Add Quest (Now checks variable requirement)
    // ----------------------------------------------------------
    public void AddQuest(QuestData data, NPC giver)
    {
        if (activeQuests.ContainsKey(data.questName))
        {
            Debug.LogWarning("Quest já ativa.");
            return;
        }

        // Requisito
        if (!string.IsNullOrEmpty(data.requirementVariable))
        {
            if (!GlobalVariableSystem.Instance.Compare(
                data.requirementVariable,
                data.requirementOperator,
                data.requirementValue))
            {
                Debug.Log($"Pré-requisito para '{data.questName}' não atendido.");
                return;
            }
        }

        // Cria quest runtime
        Quest newQuest = new Quest(data.questName, data.rewardGold, giver)
        {
            startVariableName = data.startVariableName,
            startVariableValue = data.startVariableValue,
            completeVariableName = data.completeVariableName,
            completeVariableValue = data.completeVariableValue
        };

        newQuest.state = QuestState.InProgress;
        activeQuests.Add(data.questName, newQuest);

        if (!string.IsNullOrEmpty(newQuest.startVariableName))
            GlobalVariableSystem.Instance.SetValue(newQuest.startVariableName, newQuest.startVariableValue);

        Debug.Log($"Quest iniciada: {data.questName}");
        DebugGlobalVariables();
    }

    public void DebugGlobalVariables()
    {
        foreach (var kvp in GlobalVariableSystem.Instance.GetAllVariables())
        {
            Debug.Log($"Variável {kvp.Key} = {kvp.Value}");
        }
    }

    // ----------------------------------------------------------
    // Complete Quest (Now sets complete variable)
    // ----------------------------------------------------------
    public void CompleteQuest(string name)
    {
        if (!activeQuests.ContainsKey(name))
        {
            Debug.LogWarning("Quest não ativa.");
            return;
        }

        Quest q = activeQuests[name];
        q.state = QuestState.Completed;

        // variável ao concluir
        if (!string.IsNullOrEmpty(q.completeVariableName))
            GlobalVariableSystem.Instance.SetValue(q.completeVariableName, q.completeVariableValue);

        // mover para completed
        activeQuests.Remove(name);
        completedQuests.Add(name, q);

        // notificar NPC
        q.questGiver?.ReactToCompletedQuest(q);

        Debug.Log($"Quest concluída: {name}");
    }


    // Query helpers
    public bool HasQuest(string name) => activeQuests.ContainsKey(name);
    public bool IsCompleted(string name) => completedQuests.ContainsKey(name);
    public Quest GetActiveQuest(string name) => activeQuests.TryGetValue(name, out var q) ? q : null;
    public Quest GetCompletedQuest(string name) => completedQuests.TryGetValue(name, out var q) ? q : null;



    private NPC FindNPCByName(string name)
    {
        NPC[] allNPCs = FindFirstObjectByType<NPC>().GetComponentsInChildren<NPC>(true);
        foreach (var npc in allNPCs)
        {
            if (npc.npcName == name)
                return npc;
        }
        return null;
    }

    private QuestSaveData QuestToSave(Quest q)
    {
        return new QuestSaveData
        {
            questName = q.questName,
            rewardGold = q.rewardGold,
            state = q.state.ToString(),
            questGiverName = q.questGiver != null ? q.questGiver.npcName : "",
            startVariableName = q.startVariableName,
            startVariableValue = q.startVariableValue,
            completeVariableName = q.completeVariableName,
            completeVariableValue = q.completeVariableValue
        };
    }

    [System.Serializable]
    public class QuestWrapper
    {
        public List<QuestSaveData> quests;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public string questName;
        public int rewardGold;
        public string state;
        public string questGiverName;
        public string startVariableName;
        public int startVariableValue;
        public string completeVariableName;
        public int completeVariableValue;
    }

    public string GetSaveKey() => "QuestSystem";

    public string SaveData()
    {
        var allQuests = new List<QuestSaveData>();
        foreach (var q in activeQuests.Values)
            allQuests.Add(QuestToSave(q));
        foreach (var q in completedQuests.Values)
            allQuests.Add(QuestToSave(q));
        return JsonUtility.ToJson(new QuestWrapper { quests = allQuests }, true);
    }

    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<QuestWrapper>(json);
        if (wrapper == null || wrapper.quests == null) return;

        activeQuests.Clear();
        completedQuests.Clear();

        foreach (var qData in wrapper.quests)
        {
            var npc = FindNPCByName(qData.questGiverName);
            var q = new Quest(qData.questName, qData.rewardGold, npc)
            {
                state = (QuestState)System.Enum.Parse(typeof(QuestState), qData.state),
                startVariableName = qData.startVariableName,
                startVariableValue = qData.startVariableValue,
                completeVariableName = qData.completeVariableName,
                completeVariableValue = qData.completeVariableValue
            };
            if (q.state == QuestState.InProgress) activeQuests.Add(q.questName, q);
            else if (q.state == QuestState.Completed) completedQuests.Add(q.questName, q);
        }
    }
}
