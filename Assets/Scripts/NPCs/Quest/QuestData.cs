using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/New Quest")]
public class QuestData : ScriptableObject
{
    public string questName;
    public int rewardGold;

    // PRÉ REQUISITO OPCIONAL
    public string requirementVariable;
    public string requirementOperator = "=";
    public int requirementValue;

    // STEPS
    public List<QuestStep> steps = new List<QuestStep>();
}
