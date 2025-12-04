using UnityEngine;

public class QuestTriggerArea : MonoBehaviour
{
    [Header("Variável da Quest")]
    public string variableName; // Ex: "visitou_clareira"
    public int amount = 1;
    public bool triggerOnce = true;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        int current = GlobalVariableSystem.Instance.GetValue(variableName);
        GlobalVariableSystem.Instance.SetValue(variableName, current + amount);

        QuestSystem.Instance.CheckQuestProgress(variableName, current + amount);
    }
}
