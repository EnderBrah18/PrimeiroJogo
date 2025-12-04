using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static NPC;
using static QuestSystem;
using static ReputationSystem;
using System.Linq;
using static QuestStep;
using Unity.VisualScripting;






#if UNITY_EDITOR
using UnityEditor;
#endif

public enum NPCType
{
    Dialogue,
    Movement,
    Shop,
    Quest,
    Follower,
    Escaper
}

public enum NPCRoutine
{
    None,
    Idle,
    Patrol,
    Sit,
    Sleep,
    Work,
    TalkToOtherNPC,
    WakeUp
}

public enum ValueComparison
{
    Equal,
    Greater,
    Less
}

public class NPC : InteractableBase, ISavable
{
    [System.Serializable]
    public class NPCSaveData
    {
        public string npcName;
        public Vector3 position;
        public int interactionCount;
        public string currentRoutine;
        public bool hasQuest;
        public string questID;
        public string questState;
        public int currentPatrolIndex;
        public int maxHealth;
        public int currentHealth;
    }

    #region NPC Settings
    [Header("Base Settings")]
    public string npcName;
    public NPCType npcType;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    [Tooltip("Tempo em segundos para o NPC 'acalmar' e voltar a ser amigável")]
    public float calmDownDelay = 20f;

    [Header("Daily Routine")]
    private Coroutine _calmCoroutine;
    public NPCRoutine currentRoutine;

    [Header("Social Anger Settings")]
    public bool isSociallyAngry = false;   // controla se o NPC bloqueia loja/diálogo
    public float socialAngerDuration = 666f; // tempo que ele permanece bravo socialmente após se acalmar
    private Coroutine _socialAngerCoroutine;

    public bool canCalmWhileDetecting = true; // para guardas, false
    public float calmSpeedWhilePlayerNearby = 1f; // normal
    public float calmSpeedWhenPlayerAway = 2f;    // acelera

    [Header("Dialogue Settings")]
    public DialogueLine[] linearDialogue;

    [Header("Dialogue Progression")]
    public int interactionCount = 0;
    public DialogueSet[] dialogueStages;


    [Header("Reactions (World Canvas)")]
    public Canvas emojiCanvas;
    public Image emojiImage;
    public Sprite happyEmoji;
    public Sprite angryEmoji;
    public Sprite loveEmoji;
    public Sprite questionEmoji;
    public Sprite surpriseEmoji;

    [Header("Ambient Speech")]
    public Canvas speechCanvas;
    public TMP_Text speechText;
    public string[] ambientLines;
    public float randomSpeechInterval = 8f;
    private float lastSpokeTime = 0f;
    public AmbientSpeechSet[] ambientStages;

    private bool isInteracting = false;

    [Header("Daily Schedule")]
    public NPCSchedule schedule;
    public float currentHour;  // puxado do sistema de tempo global
    private NPCRoutine routineBeforeHostile;

    [Header("Movement Settings")]
    public bool patrol = false;
    public float moveSpeed = 2f;

    [Header("NavMesh Settings")]
    private Vector3 originalPosition;
    //public NavMeshAgent agent;
    

    [Header("Quest Settings")]
    public List<QuestData> questDataList;

    [Header("Follower / Escaper Settings")]
    public float followDistance = 2f;
    public float escapeDistance = 3f;

    [Header("Reactions to Player")]
    public bool reactsToPlayer = true;
    private Quaternion lookAwayRotation;
    public float lookRange = 5f;
    public float lookSpeed = 2f;

    #endregion

    [Header("Hostility")]
    public bool isHostile = false;
    private Enemy enemyBehaviour;
    [Header("Configurações de Combate")]
    public bool isInvincible = false;

    [Header("Behavior: position & respawn")]
    public bool returnToPositionOnCalm = false;   // se true: volta à posição salva quando virou hostil
    private Vector3 positionWhenBecameHostile = Vector3.zero;
    private Vector3 originalSpawnPosition = Vector3.zero; // posição inicial para respawn

    [Header("Regeneration (friendly)")]
    public bool enableRegenWhenFriendly = true;
    public float regenAmountPerTick = 1f;
    public float regenTickInterval = 1f; // segundos entre ticks
    public float regenDelayAfterCombat = 3f; // espera após acalmar/ser atacado antes de regen (opcional)
    private Coroutine _regenCoroutine;

    [Header("Death & Respawn")]
    public bool canRespawn = false;
    public float respawnDelay = 30f;
    [SerializeField] private GameObject visualModel;

    private bool isDead = false;
    private bool socialAngerPaused = false;


    // Adições / alterações dentro da classe NPC:

    public enum ReputationMode
    {
        CharacterPriority,  // a reputação individual tem prioridade
        FactionPriority,    // a reputação da facção tem prioridade
        Average              // média entre as duas
    }

    // no NPC
    public ReputationMode reputationMode = ReputationMode.CharacterPriority;

    // Identificador único para mapear reputação neste personagem
    [Header("Reputation")]
    [Tooltip("Identificador único usado pelo ReputationSystem (ex: 'NPC_Bob')")]
    public string characterId;
    public string factionId;
    public bool useFactionReputation = true;
    [Tooltip("Thresholds para reagir à reputação")]
    public int reputationBecomeHostileThreshold = -40;
    public int reputationBecomeFriendlyThreshold = 40;

    // Valor de reputação aplicado quando o player ataca este NPC (por instância de ataque)
    public int reputationChangeOnAttack = -10;

    void Awake()
    {
        float neighborRadius = 5f;
        float maxRadius = 20f;

        foreach (var wp in waypoints)
        {
            wp.neighbors = new List<Waypoint>();
            float radius = neighborRadius;

            while (wp.neighbors.Count == 0 && radius <= maxRadius)
            {
                foreach (var other in waypoints)
                {
                    if (wp == other) continue;
                    if (Vector3.Distance(wp.Position, other.Position) <= radius)
                        wp.neighbors.Add(other);
                }
                radius += 1f; // aumenta o raio
            }
        }
    }

    private void Start()
    {
        SaveSystem.Instance.RegisterSavable(this);
        /*if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            agent.enabled = true;
        }
        else
        {
            Debug.LogWarning("NPC não consegui encontrar posição válida na NavMesh!");
        }*/

        originalSpawnPosition = transform.position;
        //agent.speed = moveSpeed;

        // Inicializa vida do NPC (não sobrescreve se já definido)
        if (maxHealth <= 0) maxHealth = 100;
        if (currentHealth <= 0) currentHealth = maxHealth;

        // Canvas sempre desligados por padrão
        if (emojiCanvas) emojiCanvas.gameObject.SetActive(false);
        if (speechCanvas) speechCanvas.gameObject.SetActive(false);

        // Fala ambiente automática
        if (ambientLines != null && ambientLines.Length > 0)
            StartCoroutine(RandomSpeechRoutine());

        enemyBehaviour = GetComponent<Enemy>();

        if (enemyBehaviour != null)
            enemyBehaviour.enabled = false; // NPC é pacífico por padrão

        // Inscrever-se no sistema de reputação (se existir e tiver id)
        if (ReputationSystem.Instance == null) return;

        

        // Registrar reputação da FACÇÃO
        /*if (!string.IsNullOrEmpty(factionId))
            ReputationSystem.Instance.Subscribe(factionId, OnReputationChanged);

        if (!string.IsNullOrEmpty(characterId))
            ReputationSystem.Instance.Subscribe(characterId, OnReputationChanged);

        OnReputationChanged(0);*/
    }

    /*private void OnDestroy()
    {
        if (ReputationSystem.Instance == null) return;

        if (!string.IsNullOrEmpty(factionId))
            ReputationSystem.Instance.Unsubscribe(factionId, OnReputationChanged);

        if (!string.IsNullOrEmpty(characterId))
            ReputationSystem.Instance.Unsubscribe(characterId, OnReputationChanged);
    }*/
    public float speed = 2f;
    private void Update()
    {
        if (!isDead)
            currentHour = TimeSystem.Instance.GetHour();

        UpdateRoutineFromTime();
        HandleRoutine();
        HandleSpecialBehavior();
        //LookBack();
    }

    private Transform GetPlayer()
    {
        return GameObject.FindWithTag("Player")?.transform;
    }

    #region NPC BUILD
    /*public void InitializeAfterNavmesh()
    {
        /if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning($"{npcName}: não possui NavMeshAgent!");
            return;
        }

        // Tenta encontrar a posição válida mais próxima no NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            originalPosition = hit.position;

            Debug.Log($"{npcName}: Posicionamento corrigido após navmesh: {originalPosition}");
        }
        else
        {
            originalPosition = transform.position;
            Debug.LogWarning($"{npcName}: NÃO encontrou posição no NavMesh após build!");
        }
    }*/
    #region Routine

    [Header("Movement Settings")]
    public float maxSpeed = 2f;
    public float rotationSpeed = 5f;
    public float waypointThreshold = 0.2f;
    public float avoidanceRadius = 1f;
    public LayerMask npcLayer;

    [Header("Routine Points")]
    public Transform workPoint;
    public Transform sleepPoint;
    public Transform patrolPoint;

    [Header("Routine Connections")]
    public List<Waypoint> workConnections;   // múltiplas cadeias de work
    public List<Waypoint> sleepConnections;  // múltiplas cadeias de sleep
    public List<Waypoint> patrolConnections; // múltiplas cadeias de patrol

    [Header("Waypoints Graph")]
    public List<Waypoint> waypoints; // Todos os waypoints disponíveis

    private Transform currentTarget;
    private bool routineInProgress = false;
    private Coroutine routineCoroutine;
    private bool isPerformingRoutineAction = false;

    // Rotina principal
    void HandleRoutine()
    {
        if (isDead || isHostile) return;
        if (routineInProgress) return;
        if (isPerformingRoutineAction) return;

        if (routineCoroutine != null)
            StopCoroutine(routineCoroutine);

        Debug.Log($"[HANDLE] Tentando rodar rotina {currentRoutine}. routineInProgress={routineInProgress} | isPerformingRoutineAction={isPerformingRoutineAction}");

        switch (currentRoutine)
        {
            case NPCRoutine.Sleep:
                routineCoroutine = StartCoroutine(TransitionToSleep());
                break;

            case NPCRoutine.Work:
                routineCoroutine = StartCoroutine(TransitionToWork());
                break;

            case NPCRoutine.Idle:
                Debug.Log("[HANDLE] Entrou no transition Idle");
                routineCoroutine = StartCoroutine(TransitionToIdle());
                break;

            case NPCRoutine.WakeUp:
                routineCoroutine = StartCoroutine(TransitionToWakeUp());
                break;

            default:
                Debug.LogWarning($"Rotina {currentRoutine} não tem transition!");
                break;
        }
    }

    #endregion

    #region Movement Helper

    // Encontrar o caminho pelo grafo de waypoints
    private List<Waypoint> FindPathDynamic(Waypoint start, Vector3 targetPosition, float initialRadius = 5f, float maxRadius = 50f, float increment = 5f)
    {
        if (start == null) return null;

        // Destino final convertido para waypoint
        Waypoint targetWP = FindClosestWaypointDynamic(targetPosition, initialRadius, maxRadius, increment);
        if (targetWP == null) return null;

        List<Waypoint> path = new List<Waypoint>();
        HashSet<Waypoint> visited = new HashSet<Waypoint>();
        Waypoint current = start;

        while (current != null && current != targetWP)
        {
            path.Add(current);
            visited.Add(current);

            // Procura neighbor mais próximo do destino dentro do raio
            Waypoint next = null;
            float radius = initialRadius;

            while (next == null && radius <= maxRadius)
            {
                foreach (var neighbor in current.neighbors)
                {
                    if (visited.Contains(neighbor)) continue;
                    float dist = Vector3.Distance(neighbor.Position, targetWP.Position);
                    if (dist <= radius)
                    {
                        next = neighbor;
                        break;
                    }
                }
                radius += increment;
            }

            // Se não encontrar neighbor, tenta pegar o waypoint mais próximo do destino ainda não visitado
            if (next == null)
            {
                next = FindClosestWaypointDynamic(targetWP.Position, initialRadius, maxRadius, increment);
                if (next != null && visited.Contains(next))
                    next = null; // já visitado, evita loop infinito
            }

            if (next == null)
            {
                Debug.LogWarning("Caminho dinâmico não encontrado, NPC indo direto.");
                break;
            }

            current = next;
        }

        // Adiciona waypoint do destino final se ainda não estiver no caminho
        if (!path.Contains(targetWP)) path.Add(targetWP);

        return path;
    }

    // Seguir caminho entre waypoints
    private IEnumerator FollowWaypointsToTarget(Waypoint start, Waypoint endWP, Transform finalTarget)
    {
        List<Waypoint> path = FindPathDynamic(start, finalTarget.position);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Caminho não encontrado, NPC indo direto.");
            yield return StartCoroutine(MoveToWaypointCoroutine(finalTarget));
            yield break;
        }

        // Segue o caminho pelos waypoints
        for (int i = 0; i < path.Count; i++)
        {
            yield return StartCoroutine(MoveToWaypointCoroutine(path[i].point, finalTarget));
        }

        // Por segurança, ainda pode mover para o finalTarget se não estiver exatamente lá
        if (finalTarget != null && Vector3.Distance(transform.position, finalTarget.position) > waypointThreshold)
            yield return StartCoroutine(MoveToWaypointCoroutine(finalTarget));


    }

    // Escolhe o waypoint de conexão mais próximo do NPC
    private Waypoint GetConnectionForCurrentRoutine()
    {
        return currentRoutine switch
        {
            NPCRoutine.Work => workConnections?.OrderBy(w => Vector3.Distance(transform.position, w.Position)).FirstOrDefault(),
            NPCRoutine.Sleep => sleepConnections?.OrderBy(w => Vector3.Distance(transform.position, w.Position)).FirstOrDefault(),
            _ => null
        };
    }

    // Encontra o waypoint mais próximo
    private Waypoint FindClosestWaypoint(Vector3 position)
    {
        Waypoint closest = null;
        float minDist = Mathf.Infinity;

        foreach (var wp in waypoints)
        {
            float dist = Vector3.Distance(position, wp.Position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = wp;
            }
        }

        return closest;
    }

    // Procura waypoint dentro de um raio crescente (para NPCs fora do caminho)
    private Waypoint FindClosestWaypointDynamic(Vector3 position, float initialRadius = 5f, float maxRadius = 50f, float increment = 5f)
    {
        float radius = initialRadius;
        while (radius <= maxRadius)
        {
            Waypoint closest = null;
            float minDist = Mathf.Infinity;

            foreach (var wp in waypoints)
            {
                float dist = Vector3.Distance(position, wp.Position);
                if (dist < radius && dist < minDist)
                {
                    minDist = dist;
                    closest = wp;
                }
            }

            if (closest != null)
                return closest;

            radius += increment;
        }

        return null;
    }

    private IEnumerator MoveToWaypointCoroutine(Transform target, Transform finalTarget = null)
    {
        while (Vector3.Distance(transform.position, target.position) > waypointThreshold)
        {
            MoveToWaypoint(target);

            // Checa se atingiu o destino final
            if (finalTarget != null && Vector3.Distance(transform.position, finalTarget.position) <= waypointThreshold)
            {
                Debug.Log($"NPC atingiu o destino final: {finalTarget.name}, corrotina encerrada.");
                yield break;
            }

            yield return null;
        }

        transform.position = target.position;
    }

    private void MoveToWaypoint(Transform target, float currentDistance = -1f)
    {
        if (currentDistance < 0f)
            currentDistance = Vector3.Distance(transform.position, target.position);

        if (currentDistance <= waypointThreshold)
            return; // Já está suficientemente próximo, não move

        Vector3 dir = (target.position - transform.position).normalized;

        // Steering local para evitar NPCs próximos
        Collider[] nearby = Physics.OverlapSphere(transform.position, avoidanceRadius, npcLayer);
        Vector3 avoidance = Vector3.zero;

        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 away = transform.position - col.transform.position;
            if (away.magnitude > 0.01f) avoidance += away.normalized / away.magnitude;
        }

        if (avoidance.sqrMagnitude > 0.001f)
            dir += avoidance.normalized * 0.5f;

        dir = Vector3.ClampMagnitude(dir, 1f);
        transform.position += dir * maxSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    private Transform GetRoutineDestination()
    {
        return currentRoutine switch
        {
            NPCRoutine.Work => workPoint,
            NPCRoutine.Sleep => sleepPoint,
            NPCRoutine.Patrol => patrolPoint,
            _ => null
        };
    }

    private IEnumerator MoveToCurrentTarget()
    {
        if (currentTarget == null) yield break;

        while (Vector3.Distance(transform.position, currentTarget.position) > waypointThreshold)
        {
            MoveToWaypoint(currentTarget);
            yield return null;
        }

        transform.position = currentTarget.position;
        Debug.Log($"Chegou ao destino: {currentTarget.name}");
    }


    #endregion

    #region Routine Transitions

    private IEnumerator TransitionToIdle()
    {
        Debug.Log("[IDLE] TransitionToIdle INICIADO");

        routineInProgress = true;
        StopCurrentRoutineAnimation();

        // animação idle (respiração leve)
        transform.DOScale(new Vector3(1.02f, 1.02f, 1.02f), 0.6f)
                 .SetEase(Ease.InOutSine)
                 .SetLoops(-1, LoopType.Yoyo);

        routineInProgress = false;
        isPerformingRoutineAction = false;
        yield break;
    }

    private IEnumerator TransitionToWakeUp()
    {
        routineInProgress = true;
        StopCurrentRoutineAnimation();

        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutQuad);
        transform.DORotate(new Vector3(0, transform.eulerAngles.y + 15f, 0), 0.3f)
                 .SetLoops(2, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(0.7f);

        routineInProgress = false;
        isPerformingRoutineAction = false;

        NPCRoutine nextRoutine = GetRoutineForTime(currentHour);

        if (nextRoutine != NPCRoutine.Sleep && nextRoutine != NPCRoutine.WakeUp)
        {
            currentRoutine = nextRoutine;
            HandleRoutine();
        }
    }


    private IEnumerator TransitionToWork()
    {
        routineInProgress = true;
        isPerformingRoutineAction = false;
        StopCurrentRoutineAnimation();

        Waypoint startWP = FindClosestWaypointDynamic(transform.position);
        if (startWP == null) yield break;

        Waypoint endWP = GetConnectionForCurrentRoutine();
        if (endWP == null)
            endWP = FindClosestWaypointDynamic(workPoint.position);

        // segue caminho até o ponto de trabalho
        yield return FollowWaypointsToTarget(startWP, endWP, workPoint);

        // chegou
        routineInProgress = false;
        isPerformingRoutineAction = true;

        // animação de “trabalhando”
        transform.DOShakePosition(1.5f, 0.15f, 15, 90)
                 .SetLoops(-1, LoopType.Yoyo);
    }

    private IEnumerator TransitionToSleep()
    {
        routineInProgress = true;
        isPerformingRoutineAction = false;

        StopCurrentRoutineAnimation();

        Waypoint startWP = FindClosestWaypointDynamic(transform.position);
        if (startWP == null) yield break;

        Waypoint endWP = GetConnectionForCurrentRoutine();
        if (endWP == null)
            endWP = FindClosestWaypointDynamic(sleepPoint.position);

        yield return FollowWaypointsToTarget(startWP, endWP, sleepPoint);

        // animação de dormir (diminuir altura)
        transform.DOScale(new Vector3(1f, 0.55f, 1f), 0.5f)
                 .SetEase(Ease.InOutQuad)
                 .SetLoops(-1, LoopType.Yoyo);

        routineInProgress = false;
        isPerformingRoutineAction = true;
    }

    private void StopCurrentRoutineAnimation()
    {
        transform.DOKill();
    }


    #endregion

    #region Routine Time Management

    private void UpdateRoutineFromTime()
    {
        if (isHostile || isDead) return;

        NPCRoutine correctRoutine = GetRoutineForTime(currentHour);

        if (currentRoutine != correctRoutine)
        {
            NPCRoutine oldRoutine = currentRoutine; // <— pega o valor correto ANTES de mudar

            StopCurrentRoutineAnimation();

            currentRoutine = correctRoutine;
            routineInProgress = false;
            isPerformingRoutineAction = false;

            Debug.Log($"[TIME] Mudando rotina: {oldRoutine} → {correctRoutine}");

            HandleRoutine();
        }
    }

    private NPCRoutine GetRoutineForTime(float hour)
    {
        if (schedule == null || schedule.entries.Length == 0)
            return currentRoutine;

        RoutineEntry chosen = schedule.entries[0];
        foreach (var r in schedule.entries)
        {
            if (hour >= r.startHour)
                chosen = r;
        }

        return chosen.routine;
    }

    #endregion
    #region Movement


    private void HandleSpecialBehavior()
    {
        switch (npcType)
        {
            case NPCType.Follower:
                //HandleFollow();
                break;

            case NPCType.Escaper:
                HandleEscape();
                break;
        }
    }

    /*private void HandleFollow()
    {
        Transform player = GetPlayer();

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > followDistance)
            agent.SetDestination(player.position);
        else
            agent.ResetPath();
    }*/

    private void HandleEscape()
    {
        Transform player = GetPlayer();

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < escapeDistance)
        {
            Vector3 dir = (transform.position - player.position).normalized;
            Vector3 escapePoint = transform.position + dir * escapeDistance;
        }
    }
    #endregion

    #region Interactions
    public override void Interact()
    {
        if (isInteracting)
        {
            // Se já está em diálogo, clicou de novo, fecha o diálogo
            DialogueUI.Instance.CloseDialogueUI(); // ou seu método de fechar
            isInteracting = false;
            return;
        }


        isInteracting = true;


        switch (npcType)
        {
            case NPCType.Dialogue:
                interactionCount++; // incrementa só na primeira vez que abre
                Talk();
                break;

            case NPCType.Quest:
                interactionCount++; // incrementa só na primeira vez que abre
                Talk();
                GiveNextQuest();
                DebugActiveQuests();
                break;

            case NPCType.Shop:
                OpenShop();
                break;
        }
    }

    public void DebugActiveQuests()
    {
        foreach (var q in QuestSystem.Instance.activeQuests.Values)
        {
            Debug.Log($"Quest ativa: {q.questName}, Estado: {q.state}");
        }
    }

    public void Talk()
    {
        Debug.Log("Chamando Talk() no NPC: " + npcName);
        DialogueSet selectedStage = null;
        LookAtPlayer();

        // Escolhe o stage que atende as condições
        foreach (var stage in dialogueStages)
        {
            if (DialogueConditionMet(stage))
            {
                selectedStage = stage;
                break;
            }
        }

        if (selectedStage != null)
        {
            Debug.Log("Selecionou DialogueSet: " + selectedStage);
            // Se tiver opções do player, mostra menu interativo
            if (selectedStage.playerOptions != null && selectedStage.playerOptions.Length > 0)
            {
                DialogueUI.Instance.StartInteractiveDialogue(this, selectedStage);
            }
            else
            {
                DialogueUI.Instance.StartDialogue(this, selectedStage.lines, selectedStage.lineEmojis);
            }
        }
        else
        {
            // Se não houver DialogueSet válido, usa o linearDialogue
            if (linearDialogue != null && linearDialogue.Length > 0)
            {
                DialogueUI.Instance.StartLinearDialogue(this, linearDialogue);
            }
            else
            {
                // fallback antigo
                DialogueUI.Instance.StartDialogue(this, new string[] { "..." }, null);
            }
        }
    }

    public void OnDialogueLine(int lineIndex, Sprite emoji)
    {
        // Sempre aplica efeito geral
        TalkEffect();

        // Se houver emoji na linha, mostra emoji e aplica bounce
        if (emoji != null)
        {
            ShowEmoji(emoji);
            BounceEffect();
        }

        LookAtPlayer();
    }

    private bool DialogueConditionMet(DialogueSet stage)
    {
        foreach (var cond in stage.conditions)
        {
            if (!CheckSingleCondition(cond))
                return false; // se UMA falhar, todo o conjunto falha
        }

        return true; // todas passaram
    }

    private bool CheckSingleCondition(DialogueCondition cond)
    {
        if (string.IsNullOrEmpty(cond.type))
            return true; // tipo vazio significa "sempre verdadeiro"

        switch (cond.type)
        {
            case "FIRST_INTERACTION":
                return interactionCount == 1;

            case "AFTER_INTERACTIONS":
                return interactionCount >= cond.variableValue;

            case "VARIABLE":
                return GlobalVariableSystem.Instance.Compare(
                    cond.variableName,
                    cond.variableOperator,
                    cond.variableValue);

            case "SOCIAL_ANGER":
                return isSociallyAngry == true;
            case "NOT_SOCIAL_ANGER":
                return isSociallyAngry == false;

            case "QUEST":
                if (cond.questCondition == "ACTIVE")
                    return QuestSystem.Instance.HasQuest(cond.questName);
                if (cond.questCondition == "COMPLETED")
                    return QuestSystem.Instance.IsCompleted(cond.questName);
                return false;

            default:
                return false;
        }
    }
    #endregion

    #region DOTween Animations (No Animator)
    void BounceEffect()
    {
        transform.DOComplete();
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.3f, 8, 0.5f);
    }

    void TalkEffect()
    {
        transform.DOComplete();

        // Movimento leve para cima e para baixo
        transform.DOPunchPosition(new Vector3(0, 0.1f, 0), 0.3f, 5, 0.5f);

        // Rotação leve para frente e para trás (em Z ou X dependendo da direção)
        transform.DOPunchRotation(new Vector3(5f, 0f, 0f), 0.3f, 5, 0.5f);
    }

    void LookAtPlayer()
    {
        Transform player = GetPlayer();
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        transform.DORotateQuaternion(Quaternion.LookRotation(dir), 0.4f);

        lookAwayRotation = transform.rotation;
    }

    void LookBack()
    {
        if (isInteracting) return;

        // Retorna à última direção “olhando para frente” antes da interação
        transform.DORotateQuaternion(lookAwayRotation, 0.4f);
    }

    #endregion

    #region Emoji / World Canvas
    public void ShowEmoji(Sprite sprite, float duration = 2f)
    {
        if (!emojiCanvas || !emojiImage) return;

        emojiCanvas.gameObject.SetActive(true);
        emojiImage.sprite = sprite;

        // Fade + move
        CanvasGroup cg = emojiCanvas.GetComponent<CanvasGroup>();
        if (!cg) cg = emojiCanvas.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        cg.DOFade(1, 0.25f);

        emojiCanvas.transform.localScale = Vector3.zero;
        emojiCanvas.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // Exit
        cg.DOFade(0, 0.4f).SetDelay(duration).OnComplete(() =>
        {
            emojiCanvas.gameObject.SetActive(false);
        });
    }
    #endregion

    #region Ambient Speech
    IEnumerator RandomSpeechRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(randomSpeechInterval * 0.5f, randomSpeechInterval * 1.5f));
            if (isInteracting || Time.time - lastSpokeTime < 5f) continue;
            SpeakAmbient();
            lastSpokeTime = Time.time;
        }
    }

    public void SpeakAmbient()
    {
        if (!speechCanvas) return;

        string[] availableLines = ambientLines; // default

        foreach (var stage in ambientStages)
        {
            if (ConditionsMet(stage.conditions))
            {
                availableLines = stage.lines;
                break;
            }
        }

        string selectedLine = availableLines[Random.Range(0, availableLines.Length)];
        ShowSpeech(selectedLine);
    }

    private void ShowSpeech(string text)
    {
        speechText.text = text;
        speechCanvas.gameObject.SetActive(true);

        CanvasGroup cg = speechCanvas.GetComponent<CanvasGroup>();
        if (!cg) cg = speechCanvas.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        cg.DOFade(1, 0.25f);
        speechCanvas.transform.localScale = Vector3.zero;
        speechCanvas.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        cg.DOFade(0, 0.4f).SetDelay(3f).OnComplete(() =>
        {
            speechCanvas.gameObject.SetActive(false);
        });
    }

    private bool ConditionsMet(DialogueCondition[] conditions)
    {
        foreach (var cond in conditions)
        {
            if (!CheckSingleCondition(cond))
                return false; // se UMA falhar, todo o conjunto falha
        }
        return true; // todas passaram
    }
    #endregion

    #region Quest
    public void GiveNextQuest()
    {
        foreach (var quest in questDataList)
        {
            // Checa se já está ativa ou concluída
            if (QuestSystem.Instance.HasQuest(quest.questName) ||
                QuestSystem.Instance.IsCompleted(quest.questName))
                continue;

            // Condições adicionais opcionais
            bool canGive = true;

            if (!string.IsNullOrEmpty(quest.requirementVariable))
            {
                canGive = GlobalVariableSystem.Instance.Compare(
                    quest.requirementVariable,
                    quest.requirementOperator,
                    quest.requirementValue);
            }

            if (!canGive)
                continue;

            // Adiciona a quest
            QuestSystem.Instance.AddQuest(quest, this);
            ShowEmoji(questionEmoji);
            return;
        }

        Debug.Log($"{npcName} não tem novas quests para oferecer.");
    }


    public void DeliverQuest()
    {
        // Pega a quest ativa deste NPC
        foreach (var questData in questDataList)
        {
            PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
            Inventory inventory = playerInventory.inventory;

            var quest = QuestSystem.Instance.GetActiveQuest(questData.questName);
            if (quest == null) continue; // só pros ativos

            // Gera a lista de itens que precisam ser entregues neste step
            var step = questData.steps[quest.currentStepIndex];
            if (step.deliverItems == null || step.deliverItems.Count == 0) continue;

            // Chama a função que vai remover do inventário e atualizar progresso
            DeliverQuestItems(inventory, step.deliverItems);
        }
    }

    private void DeliverQuestItems(Inventory inventory, List<DeliverableItem> items)
    {
        foreach (var item in items)
        {
            if (item.delvItem == null) continue;

            bool removed = inventory.RemoveItem(item.delvItem, item.amount);

            if (removed && !string.IsNullOrEmpty(item.targetVariable))
            {
                int current = GlobalVariableSystem.Instance.GetValue(item.targetVariable);
                GlobalVariableSystem.Instance.SetValue(item.targetVariable, current + item.amount);

                QuestSystem.Instance.CheckQuestProgress(item.targetVariable, current + item.amount);
            }
        }
    }


    public void ReactToCompletedQuest(QuestSystem.Quest quest)
    {
        ShowEmoji(happyEmoji);
        Debug.Log($"{npcName} reagiu à conclusão da quest {quest.questName}");
    }

    public bool CanInteract()
    {
        return !isHostile && !isSociallyAngry;
    }

    public void OpenShop()
    {
        if (!CanInteract()) return;
        ShopManager.Instance.ShopUI.SetActive(true);
        ShowEmoji(surpriseEmoji);
    }
    #endregion

    public enum SpeakerType { NPC, Player }

    [System.Serializable]
    public class DialogueLine
    {
        public SpeakerType speaker;
        [TextArea(2, 5)]
        public string text;
        public Sprite emoji; // opcional
    }


    [System.Serializable]
    public class DialogueCondition
    {
        public string type;              // FIRST_INTERACTION, VARIABLE, QUEST
        public string variableName;
        public string variableOperator = "=";
        public int variableValue;
        public string questName;
        public string questCondition;    // ACTIVE, COMPLETED
    }

    [System.Serializable]
    public class DialogueSet
    {
        public DialogueCondition[] conditions;  // condições para este stage
        [TextArea(2, 5)]
        public string[] lines;                  // falas do NPC
        public Sprite[] lineEmojis;             // emojis do NPC

        public DialogueOption[] playerOptions;  // novas opções de resposta do player
    }

    [System.Serializable]
    public class AmbientSpeechSet
    {
        public DialogueCondition[] conditions;  // mesmas condições que o NPC já usa
        [TextArea(1, 3)]
        public string[] lines;                  // falas possíveis nesse estágio
    }

    [System.Serializable]
    public class DialogueOption
    {
        public string playerText;
        public PlayerDialogue[] playerDialogue;             // Texto que o player vai escolher

        public DialogueSet[] nextDialogue;         // Próximo stage/branch
        public UnityEngine.Events.UnityEvent onSelect; // Métodos do NPC a chamar (OpenShop, GiveNextQuest)
        public bool endsConversation = false;
        public bool returnToThisDialogue = false;

        public DialogueCondition[] conditions;// Fecha diálogo se true

        // NOVO: ID para conectar com Quest e GlobalVariableSystem
        public string targetID;
        public bool sendToQuestSystem = false;
        public int progressAmount = 1;
    }

    [System.Serializable]
    public class PlayerDialogue
    {
        [TextArea(2, 5)]
        public string text;
    }

    #endregion

    #region NAVMESH
    /*
    public void BecomeHostile(float delay = 0f)
    {
        if (isHostile) return;
        routineBeforeHostile = currentRoutine;
        StartCoroutine(BecomeHostileRoutine(delay));
    }

    private IEnumerator BecomeHostileRoutine(float delay)
    {
        if (isHostile) yield break;
        isHostile = true;

        Debug.Log($"{npcName} iniciando transição para hostilidade...");

        // Não force fechamento do diálogo — espere o jogador fechá-lo
        isInteracting = false;

        if (DialogueUI.Instance != null && DialogueUI.DialogueUIManager.IsDialogueOpen)
        {
            Debug.Log($"{npcName} aguardando o jogador fechar o diálogo...");
            yield return new WaitUntil(() => !DialogueUI.DialogueUIManager.IsDialogueOpen);
            Debug.Log($"{npcName} detectou que o diálogo foi fechado, continuando...");
        }

        // Animação de virar inimigo (DOTween)
        if (transform)
        {
            transform.DOShakePosition(0.4f, 0.3f, 20);
            transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);
        }

        // Esperar delay configurado
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // Só agora ativamos o comportamento Enemy
        ActivateEnemyBehaviour();

        Debug.Log($"{npcName} tornou-se hostil!");
    }

    private void ActivateEnemyBehaviour()
    {
        if (enemyBehaviour == null)
            enemyBehaviour = GetComponent<Enemy>() ?? gameObject.AddComponent<Enemy>();

        // Ajusta stats do Enemy com base no NPC (não resetar vida)
        enemyBehaviour.enabled = true;
        enemyBehaviour.personality = EnemyPersonality.Hostile;

        enemyBehaviour.enemyName = npcName;

        // manter o mesmo maxHealth/curHealth
        enemyBehaviour.maxHealth = Mathf.Max(1, maxHealth);
        enemyBehaviour.currentHealth = Mathf.Clamp(currentHealth, 0, enemyBehaviour.maxHealth);

        enemyBehaviour.moveSpeed = moveSpeed;
        positionWhenBecameHostile = transform.position;

        // Ativar NavMeshAgent
        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = true;

    }

    // Recebe dano direcionado ao NPC. Se atacado, fica hostil e encaminha o dano ao Enemy.
    public void ReceiveDamage(int damage, bool fromPlayer = true)
    {

        if (isInvincible)
        {
            // NPC ignora dano, mas reputação pode cair
            if (fromPlayer && !string.IsNullOrEmpty(characterId) && ReputationSystem.Instance != null)
            {
                if (ReputationSystem.Instance.CanModify(characterId))
                    ReputationSystem.Instance.AdjustReputation(characterId, reputationChangeOnAttack);
            }

            if (fromPlayer && !string.IsNullOrEmpty(factionId) && ReputationSystem.Instance != null)
            {
                if (ReputationSystem.Instance.CanModify(factionId))
                    ReputationSystem.Instance.AdjustReputation(factionId, reputationChangeOnAttack);
            }

            return;
        }

        if (damage <= 0) return;

        // cancelar timer de acalmar se houver
        CancelCalmDownTimer();

        // se ataque foi do jogador (flag), ajustar reputação negativa
        if (fromPlayer && !string.IsNullOrEmpty(characterId) && ReputationSystem.Instance != null)
        {
            if (ReputationSystem.Instance.CanModify(characterId))
                ReputationSystem.Instance.AdjustReputation(characterId, reputationChangeOnAttack);
        }

        if (fromPlayer && !string.IsNullOrEmpty(factionId) && ReputationSystem.Instance != null)
        {
            if (ReputationSystem.Instance.CanModify(factionId))
                ReputationSystem.Instance.AdjustReputation(factionId, reputationChangeOnAttack);
        }

        // Se não for hostil ainda, torna hostil imediatamente (vai ativar Enemy)
        if (!isHostile)
        {
            StopRegeneration();
            BecomeHostile(0f); // sem delay, ou passe um pequeno delay se quiser
        }

        // Fica socialmente bravo
        if (_socialAngerCoroutine != null) StopCoroutine(_socialAngerCoroutine);
        isSociallyAngry = true;
        _socialAngerCoroutine = StartCoroutine(SocialAngerTimer());

        // garante que o Enemy esteja ativo para processar dano
        if (enemyBehaviour == null)
            enemyBehaviour = GetComponent<Enemy>() ?? gameObject.GetComponent<Enemy>();

        // Aplica dano:
        if (enemyBehaviour != null && enemyBehaviour.enabled)
        {
            // usa método do Enemy para efeitos e morte
            enemyBehaviour.TakeDamage(damage);
            // sincroniza vida de NPC com Enemy
            currentHealth = enemyBehaviour.currentHealth;
        }
        else
        {
            // fallback: reduz diretamente a vida do NPC
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            if (currentHealth <= 0)
            {
                Debug.Log($"{npcName} (NPC) morreu.");
                HandleDeath();
                return;
            }
        }

        // Inicia/renova timer para voltar a ser amigável após calmDownDelay
        if (calmDownDelay > 0f)
            _calmCoroutine = StartCoroutine(CalmDownCoroutine(calmDownDelay));
    }

    private void CancelCalmDownTimer()
    {
        if (_calmCoroutine != null)
        {
            StopCoroutine(_calmCoroutine);
            _calmCoroutine = null;
        }
    }

    private IEnumerator CalmDownCoroutine(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            // Se player estiver no range de detecção
            if (reactsToPlayer && IsPlayerInRange())
            {
                if (canCalmWhileDetecting)
                    elapsed += Time.deltaTime * calmSpeedWhilePlayerNearby;
                Debug.Log($"Player detectado, calmando devagar. Elapsed: {elapsed:F2}");
                // caso contrário, não aumenta elapsed
            }
            else
            {
                elapsed += Time.deltaTime * calmSpeedWhenPlayerAway; // acelera calm down
                Debug.Log($"Player ausente, calmando rápido. Elapsed: {elapsed:F2}");
            }

            yield return null;
        }

        BecomeFriendly();
        _calmCoroutine = null;
    }

    private IEnumerator SocialAngerTimer()
    {
        float elapsed = 0f;

        while (elapsed < socialAngerDuration)
        {
            // SE MORREU pausa e espera até ressuscitar
            if (isDead || socialAngerPaused)
            {
                yield return null;
                continue;
            }

            float delta = 0f;

            if (reactsToPlayer && IsPlayerInRange())
            {
                delta = canCalmWhileDetecting ? Time.deltaTime * calmSpeedWhilePlayerNearby : 0f;
                Debug.Log($"Player detectado, calmando devagar. Elapsed: {elapsed:F2}");
            }
            else
            {
                delta = Time.deltaTime * calmSpeedWhenPlayerAway;
                Debug.Log($"Player ausente, calmando rápido. Elapsed: {elapsed:F2}");
            }

            elapsed += delta;

            yield return null;
        }

        isSociallyAngry = false;
        _socialAngerCoroutine = null;
    }

    private bool IsPlayerInRange()
    {
        Transform player = GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("Player não encontrado na cena!");
            return false;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        Debug.Log($"Distância até player: {dist:F2} | LookRange: {lookRange}");
        return dist <= lookRange;
    }

    public void BecomeFriendly()
    {
        if (!isHostile) return;

        // Marca como não hostil
        isHostile = false;

        // Fecha comportamentos de combate e retorna controle de interação
        if (enemyBehaviour != null)
        {
            // sincroniza vida do Enemy de volta para o NPC antes de desligar
            currentHealth = Mathf.Clamp(enemyBehaviour.currentHealth, 0, Mathf.Max(1, enemyBehaviour.maxHealth));
            enemyBehaviour.enabled = false;
            enemyBehaviour.personality = EnemyPersonality.Neutral;
        }

        // Reativa o script NPC (lógica de diálogo/rotina)
        this.enabled = true;
        isInteracting = false;

        // Reativa componentes InteractableBase para permitir interações novamente
        foreach (var interactable in GetComponents<InteractableBase>())
        {
            if (interactable != null)
                interactable.enabled = true;
        }

        // Reativa UIs de NPC (mantém escondidas até necessário)
        if (emojiCanvas) emojiCanvas.gameObject.SetActive(false);
        if (speechCanvas) speechCanvas.gameObject.SetActive(false);

        // Garante que o NavMeshAgent exista e pare qualquer caminho atual
        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.ResetPath();
            agent.enabled = true;
            agent.speed = moveSpeed;
        }

        // Move o NPC de volta para a posição salva quando virou hostil, se configurado
        if (returnToPositionOnCalm)
        {
            // volta para a posição que estava quando virou hostil
            if (agent != null && agent.isOnNavMesh)
                agent.Warp(positionWhenBecameHostile);
            else
                transform.position = positionWhenBecameHostile;
        }
        else
        {
            // fica onde está (não fazer nada)
        }

        // Restaura rotina (ex.: voltar a patrulhar)
        //currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, (patrolPoints != null && patrolPoints.Length > 0) ? patrolPoints.Length - 1 : 0);
        // garante que o NPC comece a seguir sua rotina novamente
        currentRoutine = GetRoutineForTime(currentHour);
        HandleRoutine();
        StartRegeneration(regenDelayAfterCombat);


        Debug.Log($"{npcName} voltou a ser NPC amigável e retornou à rotina.");
    }

    private void StartRegeneration(float delay = 0f)
    {
        if (!enableRegenWhenFriendly || _regenCoroutine != null) return;
        _regenCoroutine = StartCoroutine(RegenCoroutine(delay));
    }

    private void StopRegeneration()
    {
        if (_regenCoroutine != null)
        {
            StopCoroutine(_regenCoroutine);
            _regenCoroutine = null;
        }
    }

    private IEnumerator RegenCoroutine(float initialDelay)
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (!isHostile && currentHealth > 0)
        {
            // Se estiver com Enemy ativo, atualize lá também
            currentHealth = Mathf.Min(maxHealth, currentHealth + (int)regenAmountPerTick);

            if (enemyBehaviour != null)
                enemyBehaviour.currentHealth = Mathf.RoundToInt(Mathf.Clamp(enemyBehaviour.currentHealth + regenAmountPerTick, 0, enemyBehaviour.maxHealth));

            yield return new WaitForSeconds(regenTickInterval);
        }

        _regenCoroutine = null;
    }

    private void OnReputationChanged(int _)
    {
        int charRep = ReputationSystem.Instance.GetReputation(characterId);
        int facRep = ReputationSystem.Instance.GetReputation(factionId);

        int effectiveRep;

        switch (reputationMode)
        {
            case ReputationMode.CharacterPriority:
                effectiveRep = charRep != 0 ? charRep : facRep;
                break;

            case ReputationMode.FactionPriority:
                effectiveRep = facRep != 0 ? facRep : charRep;
                break;

            default: // média
                effectiveRep = Mathf.RoundToInt((charRep + facRep) * 0.5f);
                break;
        }

        ReactToReputationValue(effectiveRep);
    }

    private void ReactToReputationValue(int rep)
    {
        if (rep <= reputationBecomeHostileThreshold && !isHostile)
        {
            BecomeHostile();
        }
        else if (rep >= reputationBecomeFriendlyThreshold && isHostile)
        {
            BecomeFriendly();
        }
        // entre thresholds -> estado neutro (não força mudança)
    }

    private void HandleDeath()
    {
        BecomeFriendly();
        isDead = true;
        socialAngerPaused = true;

        // ex.: tocar animação, desativar interações, etc.
        isHostile = false;
        isInteracting = false;

        // desativa componentes úteis
        if (enemyBehaviour != null) enemyBehaviour.enabled = false;
        foreach (var interactable in GetComponents<InteractableBase>())
            if (interactable) interactable.enabled = false;

        // esconder UI se estiver aberto
        if (DialogueUI.Instance != null && DialogueUI.DialogueUIManager.IsDialogueOpen)
        {
            DialogueUI.Instance.CloseDialogueUI();
        }

        // caso respawn
        if (canRespawn)
        {
            visualModel.SetActive(false);
            StartCoroutine(RespawnCoroutine()); 
        }
        else
        {
            // morte permanente: destrói ou desativa para sempre
            Destroy(gameObject); // ou SetActive(false) e salvar estado
        }
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // recriar / reativar o NPC no spawn position
        // Simples: reativar o gameobject e resetar stats
        visualModel.SetActive(true);

        // resetar vida
        currentHealth = maxHealth;


        // resetar flags
        isHostile = false;
        isInteracting = false;
        isSociallyAngry = false;
        isDead = false;
        socialAngerPaused = false;

        // Se ainda estava bravo socialmente, retoma o timer
        if (isSociallyAngry && _socialAngerCoroutine == null)
            _socialAngerCoroutine = StartCoroutine(SocialAngerTimer());

        // reposicionar no spawn original (ou numa posição designada)
        if (agent == null) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.Warp(originalSpawnPosition);
            agent.ResetPath();
        }
        else
        {
            transform.position = originalSpawnPosition;
        }

        // reativar componentes
        foreach (var interactable in GetComponents<InteractableBase>())
            if (interactable) interactable.enabled = true;

        // reset Enemy component if existia
        if (enemyBehaviour == null) enemyBehaviour = GetComponent<Enemy>();
        if (enemyBehaviour != null)
        {
            enemyBehaviour.enabled = false;
            enemyBehaviour.currentHealth = enemyBehaviour.maxHealth;
        }

        // delay curto antes de voltar ao comportamento normal
        yield return null;

        // opcional: reiniciar rotina do NPC
        currentRoutine = GetRoutineForTime(currentHour);
        HandleRoutine();
    }

    public void OnEnemyDied()
    {
        HandleDeath();
    }
    */
    #endregion

    public string GetSaveKey() => $"NPC_{npcName}";

    public string SaveData()
    {
        NPCSaveData data = new NPCSaveData()
        {
            npcName = npcName,
            position = transform.position,
            interactionCount = interactionCount,
            currentRoutine = currentRoutine.ToString(),
            maxHealth = maxHealth,
            currentHealth = currentHealth
        };
        return JsonUtility.ToJson(data);
    }

    public void LoadData(string json)
    {
        NPCSaveData data = JsonUtility.FromJson<NPCSaveData>(json);
        transform.position = data.position;
        interactionCount = data.interactionCount;
        currentRoutine = (NPCRoutine)System.Enum.Parse(typeof(NPCRoutine), data.currentRoutine);

        // Carregar vida, se presente
        if (data.maxHealth > 0) maxHealth = data.maxHealth;
        if (data.currentHealth > 0) currentHealth = data.currentHealth;
    }

/*#if UNITY_EDITOR
    [CustomEditor(typeof(NPC))]
    public class NPCEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            NPC npc = (NPC)target;

            if (GUILayout.Button("Testar: Virar Inimigo"))
            {
                npc.BecomeHostile();
            }

            if (GUILayout.Button("Testar: Virar Amigo"))
            {
                npc.BecomeFriendly();
            }
        }
    }
#endif*/

}
