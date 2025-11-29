using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    public string questName;
    public int rewardGold;

    [Header("Variable Effects")]
    public string startVariableName;
    public int startVariableValue;
    public string completeVariableName;
    public int completeVariableValue;

    [Header("Requirements")]
    public string requirementVariable;
    public string requirementOperator = "=";
    public int requirementValue;
}
