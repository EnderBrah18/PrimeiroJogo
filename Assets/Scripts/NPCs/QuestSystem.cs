using System.Collections.Generic;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance;
    public List<string> activeQuests = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    public void AddQuest(string questName, int rewardGold)
    {
        activeQuests.Add(questName);
        Debug.Log($"Nova quest recebida: {questName}. Recompensa: {rewardGold} gold");
    }

    public void CompleteQuest(string questName)
    {
        if (activeQuests.Contains(questName))
        {
            activeQuests.Remove(questName);
            Debug.Log($"Quest {questName} concluída!");
        }
    }
}
