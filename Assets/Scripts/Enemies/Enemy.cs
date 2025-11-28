using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyPersonality
{
    Hostile,
    Neutral,
    Friendly
}

public class Enemy : MonoBehaviour
{
    private Vector3 originalPosition;   

    [Header("Basic Settings")]
    public string enemyName;
    public EnemyPersonality personality;
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 10;
    public float detectionRange = 5f;
    public float attackRange = 1.5f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

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

        // Ajuste para o NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);

            // POSIÇÃO ORIGINAL ATUALIZADA AQUI!
            originalPosition = hit.position;

            Debug.Log($"{enemyName}: originalPosition corrigido para {originalPosition}");
        }
        else
        {
            // se nem assim encontrou, usa a posição atual mesmo
            originalPosition = transform.position;
            Debug.LogWarning($"{enemyName}: NÃO foi possível encaixar no NavMesh!");
        }
    }

    private void Update()
    {
        if (agent != null)
            agent.speed = moveSpeed;

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
        if (player == null || agent == null) return;

        if (agent == null || !agent.isOnNavMesh || !agent.enabled)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Jogador dentro da área de detecção
        if (dist < detectionRange)
        {
            agent.SetDestination(player.position);

            // Atacar
            if (dist <= attackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Jogador fugiu -> voltar para posição original
            agent.SetDestination(originalPosition);

            // Opcional: rotacionar lentamente enquanto volta
            Vector3 dir = originalPosition - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5);
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
    #endregion

    #region Combat
    private void AttackPlayer()
    {
        // Cooldown
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent == null) return;

        // Virar para o jogador
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        // Atacar
        playerComponent.TakeDamage(Mathf.RoundToInt(attackDamage));

        Debug.Log($"{enemyName} atacou o jogador causando {attackDamage} de dano!");
    }

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

    public void InitializeAfterNavmesh()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // tenta encontrar a posição válida mais próxima no navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            originalPosition = hit.position;

            Debug.Log($"{enemyName}: Posicionamento corrigido após navmesh: {originalPosition}");
        }
        else
        {
            originalPosition = transform.position;
            Debug.LogWarning($"{enemyName}: NÃO encontrou posição no navmesh após build!");
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        StartCoroutine(KnockbackCoroutine(direction, force));
    }

    private IEnumerator KnockbackCoroutine(Vector3 dir, float force)
    {
        if (agent == null) yield break;

        // desativa o navmesh agent temporariamente
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        float duration = 0.15f; // tempo do empurrão

        Vector3 start = transform.position;
        Vector3 end = start + dir.normalized * force;

        // anima o deslocamento manualmente
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // reativa o navmesh agent
        agent.Warp(transform.position); // atualiza a posição no navmesh
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
    }
}
