using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    private int currentLine = 0;
    private string[] lines;

    private ThirdPersonCamera cameraScript;
    private bool blocked = false;

    private void Awake()
    {
        // --- Singleton seguro ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        dialoguePanel.SetActive(false);
    }

    private void Start()
    {
        // Busca a câmera quando a cena inicializa
        cameraScript = FindAnyObjectByType<ThirdPersonCamera>();
    }

    // Chamado automaticamente quando muda de cena
    private void OnLevelWasLoaded(int level)
    {
        // Realoca referências perdidas
        cameraScript = FindAnyObjectByType<ThirdPersonCamera>();
    }

    public void ShowDialogue(string npcName, string[] dialogueLines)
    {
        lines = dialogueLines;
        currentLine = 0;
        dialoguePanel.SetActive(true);
        nameText.text = npcName;
        dialogueText.text = lines[currentLine];
        blocked = !blocked;

        Player.Instance?.SetMovementBlocked(blocked);
        Player.Instance?.SetAttackBlocked(blocked);
        cameraScript?.HandleInventoryToggled(true);
        DialogueUIManager.IsDialogueOpen = true;
    }

    public void NextLine()
    {
        currentLine++;
        if (currentLine < lines.Length)
        {
            dialogueText.text = lines[currentLine];
        }
        else
        {
            dialoguePanel.SetActive(false);
            Player.Instance?.SetMovementBlocked(false);
            cameraScript?.HandleInventoryToggled(false);
            DialogueUIManager.IsDialogueOpen = false;
        }
    }

    public static class DialogueUIManager
    { 
        public static bool IsDialogueOpen = false;
    }
}

