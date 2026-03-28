using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum QuestStepType
{
    Kill,
    Collect,
    Craft,
    TalkToNPC,
    SetVariable,
    WaitForVariable,
    CustomEvent
}

[System.Serializable]
public class QuestStep
{
    public string stepID;
    public string stepName;
    public string targetID;
    public int requiredAmount = 1;
    public bool countProgressBeforeStart = false;


    public string completeVariableName;
    public int completeVariableValue;
    [System.Serializable]
    public class DeliverableItem
    {
        public ItemSO delvItem;  // qual recurso entregar
        public int amount = 1;       // quantidade necessária
        public string targetVariable; // variável de progresso específica (pode ser a mesma de targetID ou diferente)
    }

    public List<DeliverableItem> deliverItems = new List<DeliverableItem>();

    public List<BranchCondition> nextBranches = new List<BranchCondition>();



    [System.Serializable]
    public class BranchCondition
    {
        public enum BranchType { VariableCheck, DialogueChoice, EventTrigger }
        public BranchType branchType;
        public string targetStepID;

        public string variableName;
        public string comparison;
        public int value;

        public string dialogueChoiceID;
        public string eventID;

        public string nextStepID;
    }
}
