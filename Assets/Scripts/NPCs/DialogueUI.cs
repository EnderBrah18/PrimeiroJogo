using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NPC;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

[Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    public TMP_Text playerNameText;
    public TMP_Text playerDialogueText;
    public GameObject playerDialoguePanel;

    public GameObject npcNextButton;
    public GameObject playerNextButton;

    private NPC currentNPC;
    private Sprite[] npcEmojis;
    private int currentLine = -1;

    private DialogueOption[] currentOptions;
    private DialogueOption selectedPlayerOption;

    private bool waitingForPlayerDialogue = false;

    private ThirdPersonCamera cameraScript;

    public enum DialogueMode
    {
        None,
        Normal,
        Linear,
        Interactive
    }

    private DialogueMode mode = DialogueMode.None;

    private int currentIndex = 0;
    private int playerDialogueIndex = 0;
    private string[] currentPlayerLines;

    private string[] activeLines;
    private Sprite[] activeEmojis;

    private DialogueLine[] activeLinearLines;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        dialoguePanel.SetActive(false);
        playerDialoguePanel.SetActive(false);
    }

    private void Start()
    {
        cameraScript = FindAnyObjectByType<ThirdPersonCamera>();
    }

    #region Dialogue Flow
    public void StartDialogue(NPC npc, string[] lines, Sprite[] emojis)
    {
        mode = DialogueMode.Normal;

        currentNPC = npc;
        activeLines = lines;
        activeEmojis = emojis;
        currentIndex = 0;

        SetupDialogueUI();
        ShowCurrentLine();
    }

    public void StartLinearDialogue(NPC npc, DialogueLine[] lines)
    {
        mode = DialogueMode.Linear;

        currentNPC = npc;
        activeLinearLines = lines;
        currentIndex = 0;

        SetupDialogueUI();
        ShowCurrentLine();
    }

    public void NextLine()
    {
        // PLAYER ESTÁ FALANDO:
        if (waitingForPlayerDialogue)
        {
            playerDialogueIndex++;

            // ainda existem linhas do player?
            if (playerDialogueIndex < currentPlayerLines.Length)
            {
                playerDialogueText.text = currentPlayerLines[playerDialogueIndex];
                return; // NÃO processa a opção ainda
            }

            // acabou as falas do player
            waitingForPlayerDialogue = false;
            playerDialoguePanel.SetActive(false);

            // agora sim, processa a opção
            if (selectedPlayerOption != null)
            {
                ProcessPlayerOption(currentNPC, selectedPlayerOption);
                return;
            }

            // fallback
            currentIndex++;
            CheckEndOrContinue();
            return;
        }

        // NPC ESTÁ FALANDO
        currentIndex++;
        UpdateNextButtons();
        CheckEndOrContinue();
    }

    private void CheckEndOrContinue()
    {
        bool ended = false;

        if (mode == DialogueMode.Linear)
            ended = currentIndex >= activeLinearLines.Length;
        else
            ended = currentIndex >= activeLines.Length;

        if (ended)
        {
            // DIÁLOGO INTERATIVO  MOSTRA OPÇÕES
            if (mode == DialogueMode.Interactive && currentOptions != null && currentOptions.Length > 0)
            {
                playerDialoguePanel.SetActive(false);
                optionsContainer.gameObject.SetActive(true);

                ShowPlayerOptions(currentNPC, currentOptions);

                npcNextButton?.SetActive(false);
                playerNextButton?.SetActive(false);
                return;
            }

            CloseDialogueUI();
            return;
        }

        ShowCurrentLine();
    }

    private void UpdateNextButtons()
    {
        if (waitingForPlayerDialogue)
        {
            // Jogador está falando  botão do jogador ativo
            npcNextButton?.SetActive(false);
            playerNextButton?.SetActive(true);
            return;
        }

        // NPC está falando
        npcNextButton?.SetActive(true);
        playerNextButton?.SetActive(false);

        
    }
        


    private void TriggerLineEffects()
    {
        if (currentNPC != null)
        {
            Sprite emoji = null;
            if (npcEmojis != null && npcEmojis.Length > currentLine)
                emoji = npcEmojis[currentLine];

            currentNPC.OnDialogueLine(currentLine, emoji);
        }
    }
    #endregion

    #region Interactive Dialogue
    public void StartInteractiveDialogue(NPC npc, DialogueSet set)
    {
        Debug.Log("START INTERACTIVE Options: " + set.playerOptions?.Length);

        mode = DialogueMode.Interactive;

        currentNPC = npc;
        activeLines = set.lines;
        activeEmojis = set.lineEmojis;
        currentOptions = set.playerOptions;

        currentIndex = 0;

        SetupDialogueUI();
        ShowCurrentLine();
    }

    public void ShowPlayerOptions(NPC npc, DialogueOption[] options)
    {
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        foreach (var option in options)
        {   
            if (!ConditionsMet(option.conditions))
                continue;

            GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = option.playerText;

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => OnPlayerSelectOption(npc, option));
        }
    }

    public void OnPlayerSelectOption(NPC npc, DialogueOption option)
    {
        selectedPlayerOption = option;

        if (option.endsConversation)
        {
            CloseDialogueUI();
            return;
        }


        if (option.playerDialogue != null && option.playerDialogue.Length > 0 && !string.IsNullOrEmpty(option.playerDialogue[0].text))
        {
            ShowPlayerDialogue(option.playerDialogue.Select(p => p.text).ToArray());
        }
        else
        {
            ProcessPlayerOption(npc, option);
        }
    }


    private void ProcessPlayerOption(NPC npc, DialogueOption option)
    {
        selectedPlayerOption = null; //  EVITA LOOP LÓGICO

        option.onSelect?.Invoke();

        if (option.sendToQuestSystem && !string.IsNullOrEmpty(option.targetID))
        {
            // Registra variável no sistema global
            GlobalVariableSystem.Instance.SetValue(option.targetID, option.progressAmount);

            // Notifica progresso de quest
            QuestSystem.Instance.RegisterProgress(option.targetID, option.progressAmount);

            Debug.Log($"[Dialogue] Enviando progresso: ({option.targetID}) +{option.progressAmount}");
        }

        if (option.nextDialogue != null && option.nextDialogue.Length > 0)
        {
            StartInteractiveDialogue(npc, option.nextDialogue[0]);
            return;
        }

        if (option.returnToThisDialogue)
        {
            ReturnToLastDialogue(npc);
            return;
        }

        CloseDialogueUI();
    }

    private void ReturnToLastDialogue(NPC npc)
    {
        currentIndex = activeLines.Length - 1;

        dialoguePanel.SetActive(true);
        playerDialoguePanel.SetActive(false);

        dialogueText.text = activeLines[currentIndex];

        optionsContainer.gameObject.SetActive(true);
        ShowPlayerOptions(npc, currentOptions);

        npcNextButton?.SetActive(false);
        playerNextButton?.SetActive(false);
    }

    public void ShowPlayerDialogue(string[] lines)
    {
        waitingForPlayerDialogue = true;

        currentPlayerLines = lines;
        playerDialogueIndex = 0;

        dialoguePanel.SetActive(false);
        playerDialoguePanel.SetActive(true);
        optionsContainer.gameObject.SetActive(false);

        playerNameText.text = "Você";
        playerDialogueText.text = lines[playerDialogueIndex];

        UpdateNextButtons();
    }

    #endregion

    private void ShowCurrentLine()
    {
        optionsContainer.gameObject.SetActive(false);

        // Se estamos em diálogo linear
        if (mode == DialogueMode.Linear)
        {
            var line = activeLinearLines[currentIndex];

            if (line.speaker == SpeakerType.NPC)
            {
                dialoguePanel.SetActive(true);
                playerDialoguePanel.SetActive(false);

                dialogueText.text = line.text;
                nameText.text = currentNPC.npcName;

                waitingForPlayerDialogue = false;
            }
            else // SpeakerType.Player
            {
                dialoguePanel.SetActive(false);
                playerDialoguePanel.SetActive(true);

                playerDialogueText.text = line.text;
                playerNameText.text = "Você";

                waitingForPlayerDialogue = true;
            }

            UpdateNextButtons();
            return;
        }

        // --- DIÁLOGO NORMAL OU INTERATIVO ---
        dialoguePanel.SetActive(true);
        playerDialoguePanel.SetActive(false);

        dialogueText.text = activeLines[currentIndex];

        UpdateNextButtons();
    }

    private void SetupDialogueUI()
    {
        dialoguePanel.SetActive(true);
        playerDialoguePanel.SetActive(false);
        optionsContainer.gameObject.SetActive(false);

        nameText.text = currentNPC.npcName;

        Player.Instance?.SetMovementBlocked(true);
        Player.Instance?.SetAttackBlocked(true);
        InventoryManager.Instance?.PlayerHud(false);
        cameraScript?.HandleInventoryToggled(true);

        DialogueUIManager.IsDialogueOpen = true;
    }

    #region Conditions
    private bool ConditionsMet(DialogueCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0) return true;

        foreach (var cond in conditions)
        {
            switch (cond.type)
            {
                case "FIRST_INTERACTION":
                    if (currentNPC.interactionCount != 1) return false;
                    break;
                case "AFTER_INTERACTIONS":
                    if (currentNPC.interactionCount < cond.variableValue) return false;
                    break;
                case "VARIABLE":
                    if (!GlobalVariableSystem.Instance.Compare(
                        cond.variableName,
                        cond.variableOperator,
                        cond.variableValue)) return false;
                    break;
                case "QUEST":
                    if (cond.questCondition == "ACTIVE" && !QuestSystem.Instance.HasQuest(cond.questName))
                        return false;
                    if (cond.questCondition == "COMPLETED" && !QuestSystem.Instance.IsCompleted(cond.questName))
                        return false;
                    break;
            }
        }
        return true;
    }
    #endregion

    #region Close
    public void CloseDialogueUI()
    {
        dialoguePanel.SetActive(false);
        playerDialoguePanel.SetActive(false);
        optionsContainer.gameObject.SetActive(false);

        Player.Instance?.SetMovementBlocked(false);
        Player.Instance?.SetAttackBlocked(false);
        InventoryManager.Instance?.PlayerHud(true);

        cameraScript?.HandleInventoryToggled(false);
        DialogueUIManager.IsDialogueOpen = false;

        currentOptions = null;
        selectedPlayerOption = null;
        currentNPC = null;
        npcEmojis = null;
        currentLine = -1;
        waitingForPlayerDialogue = false;
    }
    #endregion

    public static class DialogueUIManager
    {
        public static bool IsDialogueOpen = false;
    }

}
