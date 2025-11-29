using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static NPC;
using static QuestSystem;

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
    }

    [Header("Base Settings")]
    public string npcName;
    public NPCType npcType;

    [Header("Daily Routine")]
    public NPCRoutine currentRoutine;

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
            Debug.LogWarning("NPC não conseguiu encontrar posição válida na NavMesh!");
        }

        agent.speed = moveSpeed;

        // Canvas sempre desligados por padrão
        if (emojiCanvas) emojiCanvas.gameObject.SetActive(false);
        if (speechCanvas) speechCanvas.gameObject.SetActive(false);

        // Fala ambiente automática
        if (ambientLines != null && ambientLines.Length > 0)
            StartCoroutine(RandomSpeechRoutine());
    }

    private void Update()
    {
        HandleRoutine();
        HandleSpecialBehavior();
    }

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

    public void OpenShop()
    {
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


    public string GetSaveKey() => $"NPC_{npcName}";

    public string SaveData()
    {
        NPCSaveData data = new NPCSaveData()
        {
            npcName = npcName,
            position = transform.position,
            interactionCount = interactionCount,
            currentRoutine = currentRoutine.ToString(),
            hasQuest = questID != null
        };
        return JsonUtility.ToJson(data);
    }

    public void LoadData(string json)
    {
        NPCSaveData data = JsonUtility.FromJson<NPCSaveData>(json);
        transform.position = data.position;
        interactionCount = data.interactionCount;
        currentRoutine = (NPCRoutine)System.Enum.Parse(typeof(NPCRoutine), data.currentRoutine);
    }

}
