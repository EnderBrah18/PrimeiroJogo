using UnityEngine;
using UnityEngine.AI;

public enum EnemyPersonality
{
    Hostile,
    Neutral,
    Friendly
}

public class Enemy : MonoBehaviour
{
    [Header("Basic Settings")]
    public string enemyName;
    public EnemyPersonality personality;
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 10;
    public float detectionRange = 5f;
    public float attackRange = 1.5f;

    [Header("Movement Settings")]
    public bool patrol = false;
    public Transform[] patrolPoints;
    public float moveSpeed = 3f;
    private int currentPatrolIndex = 0;
    private NavMeshAgent agent;

    [Header("Quest Settings")]
    public bool isQuestTarget = false; // Ex: precisa ser morto para completar quest
    public string questName;

    private Transform player;

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed = moveSpeed;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        switch (personality)
        {
            case EnemyPersonality.Hostile:
                HandleHostileBehavior();
                break;
            case EnemyPersonality.Neutral:
                HandleNeutralBehavior();
                break;
            case EnemyPersonality.Friendly:
                HandleFriendlyBehavior();
                break;
        }

        if (patrol && personality != EnemyPersonality.Hostile)
            HandlePatrol();
    }

    #region Behavior
    private void HandleHostileBehavior()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < detectionRange)
        {
            if (agent != null)
                agent.SetDestination(player.position);

            if (dist <= attackRange)
            {
                AttackPlayer();
            }
        }
    }

    private void HandleNeutralBehavior()
    {
        // Por padrão, neutros não atacam e só patrulham
        HandlePatrol();
    }

    private void HandleFriendlyBehavior()
    {
        // Amigáveis podem seguir o jogador ou dar buffs, etc.
    }

    private void HandlePatrol()
    {
        if (patrol && patrolPoints.Length > 0 && agent != null)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);

            if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.2f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }

    private void AttackPlayer()
    {
        // Placeholder para ataque
        Debug.Log($"{enemyName} atacou o jogador causando {attackDamage} de dano!");
    }
    #endregion

    #region Combat
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{enemyName} morreu!");
        if (isQuestTarget)
            QuestSystem.Instance.CompleteQuest(questName);
        Destroy(gameObject);
    }
    #endregion
}
