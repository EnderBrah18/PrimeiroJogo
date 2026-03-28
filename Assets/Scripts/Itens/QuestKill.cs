using UnityEngine;

public class QuestKill : MonoBehaviour
{
    [Header("Variável da Quest")]
    public string variableName; // Ex: "slimes_derrotados"
    public int amount = 1;

    [Header("Filtros de Inimigo (opcional)")]
    public string requiredEnemyName;          // deixa vazio para aceitar qualquer nome
    public string requiredEnemyType;          // deixa vazio para aceitar qualquer tipo
    public EnemySizeType? requiredSizeType;   // deixa null para aceitar qualquer tamanho
    public EnemyFightType? requiredFightType; // deixa null para aceitar qualquer estilo de luta
    public EnemyPersonality? requiredPersonality; // deixa null para aceitar qualquer personalidade


    // Chamado quando o inimigo morre
    public void OnKilled(Enemy killedEnemy)
    {
        if (killedEnemy == null) return;

        if (!string.IsNullOrEmpty(requiredEnemyName) && killedEnemy.enemyName != requiredEnemyName) return;
        if (!string.IsNullOrEmpty(requiredEnemyType) && killedEnemy.enemyType != requiredEnemyType) return;
        if (requiredSizeType.HasValue && killedEnemy.enemySizeType != requiredSizeType.Value) return;
        if (requiredFightType.HasValue && killedEnemy.enemyFightType != requiredFightType.Value) return;
        if (requiredPersonality.HasValue && killedEnemy.personality != requiredPersonality.Value) return;

        int current = GlobalVariableSystem.Instance.GetValue(variableName);
        GlobalVariableSystem.Instance.SetValue(variableName, current + amount);

        QuestSystem.Instance.CheckQuestProgress(variableName, current + amount);
    }
}
