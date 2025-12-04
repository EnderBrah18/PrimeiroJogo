using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static QuestStep;

public class QuestSystem : MonoBehaviour, ISOSavable
{

    public static QuestSystem Instance;

    public List<QuestData> allQuestData = new List<QuestData>();
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

        public int currentStepIndex = 0;

        // Esses são NECESSÁRIOS porque o Quest runtime usa eles
        public string startVariableName;
        public int startVariableValue;
        public string completeVariableName;
        public int completeVariableValue;

        // Pré-requisito
        public string requirementVariable;
        public string requirementOperator;
        public int requirementValue;

        // Referência de quem deu a quest
        public NPC questGiver;

        public QuestData data => QuestSystem.Instance.allQuestData
            .Find(q => q.questName == questName);

        public Quest(string name, int reward, NPC giver)
        {
            questName = name;
            rewardGold = reward;
            state = QuestState.NotStarted;
            questGiver = giver;
        }
    }

    public Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    public Dictionary<string, Quest> completedQuests = new Dictionary<string, Quest>();
    private Dictionary<string, int> bufferedProgress = new Dictionary<string, int>();

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

        // Pré-requisito simples (opcional, você decide manter ou migrar para step)
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

        Quest newQuest = new Quest(data.questName, data.rewardGold, giver);
        newQuest.state = QuestState.InProgress;
        activeQuests.Add(data.questName, newQuest);

        Debug.Log($"Quest iniciada: {data.questName}");

        // Aplicar progresso retroativo se existir
        QuestStep firstStep = data.steps[0];

        if (data.steps == null || data.steps.Count == 0)
        {
            Debug.LogError($"Quest '{data.questName}' não tem nenhum Step configurado!");
            return;
        }
        if (bufferedProgress.ContainsKey(firstStep.targetID))
        {
            ApplyStepProgress(newQuest, firstStep, bufferedProgress[firstStep.targetID]);
            bufferedProgress.Remove(firstStep.targetID);
        }
    }

    public void RegisterProgress(string targetID, int amount)
{
    // Faz uma cópia da lista de quests ativas
    foreach (var quest in activeQuests.Values.ToList())
    {
        QuestStep step = quest.data.steps[quest.currentStepIndex];

        if (step.targetID != targetID) continue;

        // Buffer retroativo
        if (quest.state != QuestState.InProgress)
        {
            if (step.countProgressBeforeStart)
            {
                if (!bufferedProgress.ContainsKey(targetID))
                    bufferedProgress[targetID] = 0;

                bufferedProgress[targetID] += amount;
            }
            continue;
        }

        ApplyStepProgress(quest, step, amount);
    }
}

    private void ApplyStepProgress(Quest quest, QuestStep step, int amount)
    {
        string varName = $"{quest.questName}_{step.stepName}_progress";

        int current = GlobalVariableSystem.Instance.GetValue(varName);
        GlobalVariableSystem.Instance.SetValue(varName, current + amount);

        if (current + amount >= step.requiredAmount)
        {
            CompleteStep(quest, step);
        }
    }

    private void CompleteStep(Quest quest, QuestStep step)
    {
        Debug.Log($"Step Concluído: {step.stepName}");

        // dispara variável pós step
        if (!string.IsNullOrEmpty(step.completeVariableName))
            GlobalVariableSystem.Instance.SetValue(step.completeVariableName, step.completeVariableValue);

        // -------------- BRANCHES --------------
        if (step.nextBranches != null && step.nextBranches.Count > 0)
        {
            foreach (var branch in step.nextBranches)
            {
                if (IsBranchConditionMet(branch))
                {
                    quest.currentStepIndex = quest.data.steps.FindIndex(s => s.stepID == branch.nextStepID);
                    return;
                }
            }
        }

        // Se não disparou branch  vai normal
        quest.currentStepIndex++;

        if (quest.currentStepIndex >= quest.data.steps.Count)
        {
            CompleteQuest(quest.questName);
        }
    }

    private bool IsBranchConditionMet(BranchCondition b)
    {
        switch (b.branchType)
        {
            case BranchCondition.BranchType.VariableCheck:
                return GlobalVariableSystem.Instance.Compare(b.variableName, b.comparison, b.value);

            case BranchCondition.BranchType.DialogueChoice:
                if (string.IsNullOrEmpty(b.dialogueChoiceID)) return false;
                return GlobalVariableSystem.Instance.GetValue(b.dialogueChoiceID) == 1;

            case BranchCondition.BranchType.EventTrigger:
                return GlobalVariableSystem.Instance.GetValue(b.eventID) == 1;

            default:
                return false;
        }
    }

    public void CheckQuestProgress(string variableName, int newValue)
    {
        foreach (var quest in activeQuests.Values.ToList()) // .ToList() para não modificar o dicionário durante iteração
        {
            // Se a quest usa essa variável como requisito
            if (!string.IsNullOrEmpty(quest.requirementVariable) && quest.requirementVariable == variableName)
            {
                // Usa o sistema de comparação que você já tem
                if (GlobalVariableSystem.Instance.Compare(variableName, quest.requirementOperator, quest.requirementValue))
                {
                    CompleteQuest(quest.questName);
                }
            }


            if (quest != null)
            {
                Debug.Log($"Quest {quest.questName}, Step atual: {quest.currentStepIndex}");
            }

        }
        
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
            currentStepIndex = q.currentStepIndex,
            questGiverName = q.questGiver != null ? q.questGiver.npcName : ""
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
        public int currentStepIndex;
        public string questGiverName;
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
                currentStepIndex = qData.currentStepIndex
            };
            if (q.state == QuestState.InProgress) activeQuests.Add(q.questName, q);
            else if (q.state == QuestState.Completed) completedQuests.Add(q.questName, q);
        }
    }
}
