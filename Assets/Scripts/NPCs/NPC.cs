using UnityEngine;
using UnityEngine.AI;

public enum NPCType
{
    Dialogue,
    Movement,
    Shop,
    Quest,
    Follower,
    Escaper
}

public class NPC : InteractableBase
{
    [Header("Base Settings")]
    public string npcName;
    public NPCType npcType;

    [Header("Dialogue Settings")]
    [TextArea(2, 5)]
    public string[] dialogueLines;
    public float interactDistance = 2f;
    private bool isInteracting = false;

    [Header("Movement Settings")]
    public bool patrol = false;
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    private int currentPatrolIndex = 0;

    [Header("NavMesh Settings")]
    public NavMeshAgent agent;

    [Header("Quest Settings")]
    public bool givesQuest = false;
    public string questName;
    public int rewardGold = 0;

    [Header("Follower / Escaper Settings")]
    public Transform targetPlayer;
    public float followDistance = 2f;
    public float escapeDistance = 3f;

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed = moveSpeed;
    }

    private void Update()
    {
        // Patrulha se estiver marcada
        if (patrol && npcType != NPCType.Follower && npcType != NPCType.Escaper)
        {
            HandlePatrol();
        }

        // Comportamentos especiais
        switch (npcType)
        {
            case NPCType.Follower:
                HandleFollow();
                break;
            case NPCType.Escaper:
                HandleEscape();
                break;
        }
    }

    #region Movement
    private void HandlePatrol()
    {
        if (!patrol || patrolPoints.Length == 0 || agent == null || isInteracting) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.2f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void HandleFollow()
    {
        if (targetPlayer == null || agent == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist > followDistance)
            agent.SetDestination(targetPlayer.position);
        else
            agent.ResetPath();
    }

    private void HandleEscape()
    {
        if (targetPlayer == null || agent == null) return;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist < escapeDistance)
        {
            Vector3 dir = (transform.position - targetPlayer.position).normalized;
            Vector3 escapePoint = transform.position + dir * escapeDistance;
            agent.SetDestination(escapePoint);
        }
    }
    #endregion



    public override void Interact()
    {
        isInteracting = true; // pausa patrulha
        switch (npcType)
        {
            case NPCType.Dialogue:
                Talk();
                break;
            case NPCType.Quest:
                Talk();
                GiveQuest();
                break;
            case NPCType.Shop:
                OpenShop();
                break;
        }
    }

    // Chamado quando o diálogo termina
    public void EndInteraction()
    {
        isInteracting = false; // retoma patrulha
    }

    public void Talk()
    {
        DialogueUI.Instance.ShowDialogue(npcName, dialogueLines);
    }

    public void GiveQuest()
    {
        QuestSystem.Instance.AddQuest("Quest do " + npcName, 100);
    }

    public void OpenShop()
    {
        ShopManager.Instance.ShopUI.SetActive(true);
    }

}
