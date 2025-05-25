using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CollectableManager : MonoBehaviour
{
    public static CollectableManager Instance { get; private set; }

    public float detectionRadius = 2f;
    public LayerMask collectableLayer;
    public TextMeshProUGUI promptUI;
    [SerializeField] private InputAction interactAction;
    [SerializeField] private Transform playerTransform;

    private CollectableObject current;
    private Player player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        player = playerTransform.GetComponent<Player>();
    }

    void Update()
    {
        DetectCollectable();
        UpdateUI();
    }

    void DetectCollectable()
    {
        current = null;
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, detectionRadius, collectableLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out CollectableObject col))
            {
                current = col;
                break;
            }
        }
    }

    void UpdateUI()
    {
        if (promptUI == null) return;

        if (current != null)
        {
            string inputDisplay = InputDisplayHelper.GetDisplayString(interactAction);
            promptUI.text = $"Pressione <b>{inputDisplay}</b> para coletar {current.itemName}";
            promptUI.gameObject.SetActive(true);
        }
        else
        {
            promptUI.gameObject.SetActive(false);
        }

    }

    public void TryCollect()
    {
        if (current != null)
        {
            if (current.CanBeCollected(player.equippedTool))
            {
                current.StartCollect(player.equippedTool);
            }
            else
            {
                Debug.Log("Ferramenta inadequada ou nível muito baixo.");
                // Aqui você pode mostrar uma mensagem na UI também
            }
        }
    }

    public void SetInteractAction(InputAction action)
    {
        interactAction = action;
    }

}
