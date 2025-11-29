using System;
using System.Collections;
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

    private NPC currentNPC;
    private string[] npcLines;
    private Sprite[] npcEmojis;
    private int currentLine = -1;

    private DialogueOption[] currentOptions;
    private DialogueOption selectedPlayerOption;
    private DialogueLine[] currentLinearDialogue;

    private bool waitingForPlayerDialogue = false;

    private ThirdPersonCamera cameraScript;

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
    public void StartDialogue(NPC npc, string[] dialogueLines, Sprite[] lineEmojis)
    {
        if (DialogueUIManager.IsDialogueOpen)
        {
            CloseDialogueUI();
            return;
        }

        currentNPC = npc;
        npcLines = dialogueLines;
        npcEmojis = lineEmojis;
        currentLine = 0;
        // Não zere currentOptions aqui: StartInteractiveDialogue já configura as opções
        selectedPlayerOption = null;

        dialoguePanel.SetActive(true);
        nameText.text = npc.npcName;
        dialogueText.text = npcLines[currentLine];

        Player.Instance?.SetMovementBlocked(true);
        Player.Instance?.SetAttackBlocked(true);
        InventoryManager.Instance?.PlayerHud(false);
        cameraScript?.HandleInventoryToggled(true);
        DialogueUIManager.IsDialogueOpen = true;

        TriggerLineEffects();

        // Se houver opções configuradas (diálogo interativo), mostre os botões
        if (currentOptions != null && currentOptions.Length > 0)
        {
            ShowPlayerOptions(currentNPC, currentOptions);
        }
    }
    public void StartLinearDialogue(NPC npc, DialogueLine[] lines)
    {
        currentNPC = npc;
        currentLinearDialogue = lines;
        currentLine = -1;

        NextLine(); // inicia a primeira linha
    }

    public void NextLine()
    {
        // Se estamos esperando o player terminar a fala, apenas fecha o painel
        if (waitingForPlayerDialogue)
        {
            playerDialoguePanel.SetActive(false);
            waitingForPlayerDialogue = false;
            currentLine++; // avança para a próxima linha do NPC
        }
        else
        {
            // Se não estamos esperando player, avançamos normalmente
            currentLine++;
        }

        // Checa se acabou o diálogo
        if (currentLine >= npcLines.Length)
        {
            CloseDialogueUI();
            return;
        }

        // Mostra fala do NPC
        dialoguePanel.SetActive(true);
        playerDialoguePanel.SetActive(false);
        dialogueText.text = npcLines[currentLine];
        TriggerLineEffects();

        // Checa se existe fala do player **logo após esta linha**
        if (currentNPC.playerLines != null &&
            currentNPC.playerLines.Length > currentLine &&
            !string.IsNullOrEmpty(currentNPC.playerLines[currentLine]))
        {
            waitingForPlayerDialogue = true;
            playerDialoguePanel.SetActive(true);
            dialoguePanel.SetActive(false);
            playerNameText.text = "Você";
            playerDialogueText.text = currentNPC.playerLines[currentLine];
        }
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
    public void StartInteractiveDialogue(NPC npc, DialogueSet stage)
    {
        currentOptions = stage.playerOptions;
        StartDialogue(npc, stage.lines, stage.lineEmojis);
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

        if (option.playerDialogue != null && option.playerDialogue.Length > 0 && !string.IsNullOrEmpty(option.playerDialogue[0].text))
        {
            ShowPlayerDialogue(option.playerDialogue[0].text);
        }
        else
        {
            ProcessPlayerOption(npc, option);
        }
    }


    private void ProcessPlayerOption(NPC npc, DialogueOption option)
    {
        option.onSelect?.Invoke();

        if (option.endsConversation)
        {
            CloseDialogueUI();
            return;
        }

        if (option.nextDialogue != null && option.nextDialogue.Length > 0)
        {
            DialogueSet nextStage = option.nextDialogue[0];
            StartInteractiveDialogue(npc, nextStage);
        }
        else
        {
            CloseDialogueUI();
        }
    }

    public void ShowPlayerDialogue(string text)
    {
        waitingForPlayerDialogue = true;
        playerDialoguePanel.SetActive(true);
        playerNameText.text = "Você";
        playerDialogueText.text = text;
    }
    #endregion

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

        Player.Instance?.SetMovementBlocked(false);
        Player.Instance?.SetAttackBlocked(false);
        InventoryManager.Instance?.PlayerHud(true);

        cameraScript?.HandleInventoryToggled(false);
        DialogueUIManager.IsDialogueOpen = false;

        currentOptions = null;
        selectedPlayerOption = null;
        currentNPC = null;
        npcLines = null;
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
