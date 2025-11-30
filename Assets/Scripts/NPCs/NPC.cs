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
    Idle,
    Patrol,
    Sit,
    Sleep,
    Work,
    TalkToOtherNPC
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
    public float socialAngerDuration = 15f; // tempo que ele permanece bravo socialmente após se acalmar
    private Coroutine _socialAngerCoroutine;

    public bool canCalmWhileDetecting = true; // para guardas, false
    public float calmSpeedWhilePlayerNearby = 1f; // normal
    public float calmSpeedWhenPlayerAway = 2f;    // acelera

    [Header("Dialogue Settings")]
    public DialogueLine[] linearDialogue;

    [Header("Player Insert Lines (opcional)")]
    [TextArea(2, 5)]
    public string[] playerLines;

    [Header("Dialogue Progression")]
    public int interactionCount = 0;
    public DialogueSet[] dialogueStages;

    [Header("Dialogue Type")]
    public bool interactiveDialogue = false; // true = opções de player, false = diálogo linear

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

    [Header("Routine Points")]
    public Transform workPoint;
    public Transform sleepPoint;
    public Transform idlePoint;

    [Header("Movement Settings")]
    public bool patrol = false;
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    private int currentPatrolIndex = 0;

    [Header("NavMesh Settings")]
    public NavMeshAgent agent;
    private Vector3 originalPosition;

    [Header("Quest Settings")]
    public List<QuestData> questDataList;
    public string questID;
    public string questName;
    public string questDescription;
    public QuestState questState;
    public int rewardGold;

    [Header("Follower / Escaper Settings")]
    public Transform targetPlayer;
    public float followDistance = 2f;
    public float escapeDistance = 3f;

    [Header("Reactions to Player")]
    public bool reactsToPlayer = true;
    public float lookRange = 5f;
    public float lookSpeed = 2f;

    #endregion

    [Header("Hostility")]
    public bool isHostile = false;
    private Enemy enemyBehaviour;
    [Header("Configurações de Combate")]
    public bool isInvincible = false;


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

    private void Start()
    {
        SaveSystem.Instance.RegisterSavable(this);
        if (agent == null)
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
        }

        agent.speed = moveSpeed;

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
        if (!string.IsNullOrEmpty(factionId))
            ReputationSystem.Instance.Subscribe(factionId, OnReputationChanged);

        if (!string.IsNullOrEmpty(characterId))
            ReputationSystem.Instance.Subscribe(characterId, OnReputationChanged);

        OnReputationChanged(0);
    }

    private void OnDestroy()
    {
        if (ReputationSystem.Instance == null) return;

        if (!string.IsNullOrEmpty(factionId))
            ReputationSystem.Instance.Unsubscribe(factionId, OnReputationChanged);

        if (!string.IsNullOrEmpty(characterId))
            ReputationSystem.Instance.Unsubscribe(characterId, OnReputationChanged);
    }

    private void Update()
    {
        HandleRoutine();
        HandleSpecialBehavior();
    }

    #region NPC BUILD
    public void InitializeAfterNavmesh()
    {
        if (agent == null)
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
    }

    #region Routine Logic
    void HandleRoutine()
    {
        switch (currentRoutine)
        {
            case NPCRoutine.Sleep:
                agent.enabled = false;
                break;

            case NPCRoutine.Work:
                agent.enabled = true;
                agent.SetDestination(workPoint.position);
                break;

            case NPCRoutine.Patrol:
                HandlePatrol();
                break;
        }
    }
    #endregion

    #region Movement
    private void HandlePatrol()
    {
        if (!patrol || patrolPoints.Length == 0 || isInteracting) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);

        if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.2f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void HandleSpecialBehavior()
    {
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

    public void HandlePlayerReaction(Quest quest)
    {
        if (quest == null)
        {
            Debug.LogWarning($"{npcName} recebeu reação de player, mas quest é NULL.");
            return;
        }

        // Se a quest está completa
        if (quest.state == QuestState.Completed)
        {
            Debug.Log($"{npcName} reage à conclusão da quest '{quest.questName}'!");

            // Reações visuais
            ShowEmoji(happyEmoji);
            BounceEffect();

            // Reações de fala (opcional)
            if (speechCanvas && speechText)
            {
                speechText.text = "Muito obrigado pela ajuda!";
                SpeakAmbient();
            }

            // Se quiser forçar um diálogo novo
            interactionCount = 999; // desbloqueia diálogos avançados

            return;
        }

        // Caso o jogador ainda esteja fazendo a quest
        if (quest.state == QuestState.InProgress)
        {
            Debug.Log($"{npcName} comenta sobre a quest '{quest.questName}'.");

            ShowEmoji(questionEmoji);
            return;
        }
    }

    private void HandleFollow()
    {
        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist > followDistance)
            agent.SetDestination(targetPlayer.position);
        else
            agent.ResetPath();
    }

    private void HandleEscape()
    {
        float dist = Vector3.Distance(transform.position, targetPlayer.position);

        if (dist < escapeDistance)
        {
            Vector3 dir = (transform.position - targetPlayer.position).normalized;
            Vector3 escapePoint = transform.position + dir * escapeDistance;
            agent.SetDestination(escapePoint);
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
        DialogueSet selectedStage = null;

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
        Transform player = targetPlayer ?? GameObject.FindWithTag("Player")?.transform;
        if (player == null) return;

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        transform.DORotateQuaternion(Quaternion.LookRotation(dir), 0.4f);
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

        public DialogueCondition[] conditions;// Fecha diálogo se true
    }

    [System.Serializable]
    public class PlayerDialogue
    {
        [TextArea(2, 5)]
        public string text;
    }

    #endregion

    private string _previousTag = null;


    public void BecomeHostile(float delay = 0f)
    {
        if (isHostile) return;
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

        // Ativar NavMeshAgent
        if (agent == null)
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = true;

        // Trocar tag
        _previousTag = gameObject.tag;
        gameObject.tag = "Enemy";
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
                // comportamento simples: passa a "morto" — aqui apenas destrói o objeto
                Debug.Log($"{npcName} (NPC) morreu.");
                Destroy(gameObject);
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
                // caso contrário, não aumenta elapsed
            }
            else
            {
                elapsed += Time.deltaTime * calmSpeedWhenPlayerAway; // acelera calm down
            }

            yield return null;
        }

        BecomeFriendly();
        _calmCoroutine = null;
    }

    private IEnumerator SocialAngerTimer()
    {
        yield return new WaitForSeconds(socialAngerDuration);
        isSociallyAngry = false;
        _socialAngerCoroutine = null;
    }

    private bool IsPlayerInRange()
    {
        if (targetPlayer == null) return false;
        float dist = Vector3.Distance(transform.position, targetPlayer.position);
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

        // Volta para a posição original registrada (se houver)
        if (originalPosition.sqrMagnitude > 0.01f && agent != null && agent.isOnNavMesh)
        {
            agent.Warp(originalPosition);
        }
        else if (originalPosition.sqrMagnitude > 0.01f)
        {
            transform.position = originalPosition;
        }

        // Restaura rotina (ex.: voltar a patrulhar)
        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, (patrolPoints != null && patrolPoints.Length > 0) ? patrolPoints.Length - 1 : 0);
        // garante que o NPC comece a seguir sua rotina novamente
        HandleRoutine();

        // Restaura tag anterior, se houver
        if (!string.IsNullOrEmpty(_previousTag))
        {
            try { gameObject.tag = _previousTag; } catch { /* ignora se inválida */ }
            _previousTag = null;
        }

        Debug.Log($"{npcName} voltou a ser NPC amigável e retornou à rotina.");
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

    public string GetSaveKey() => $"NPC_{npcName}";

    public string SaveData()
    {
        NPCSaveData data = new NPCSaveData()
        {
            npcName = npcName,
            position = transform.position,
            interactionCount = interactionCount,
            currentRoutine = currentRoutine.ToString(),
            hasQuest = questID != null,
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

#if UNITY_EDITOR
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
#endif

}
