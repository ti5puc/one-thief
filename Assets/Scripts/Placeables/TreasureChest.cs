using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureChest : MonoBehaviour
{
    public static event Action OnPlayerEnteredChestArea;
    public static event Action OnPlayerExitedChestArea;
    public static event Action<int> OnAnyChestOpened; // int goldAmount
    public event Action<int> OnChestOpened; // int goldAmount

    [SerializeField] private int goldAmount = 150;

    [Header("References")]
    [SerializeField] private PlaceableSettings placeableSettings;
    [SerializeField] private InputActionReference openChestAction;
    [SerializeField] private TriggerEventSender actionTrigger;

    private bool isPlayerInRange = false;
    private bool isChestOpened = false;

    private void Awake()
    {
        openChestAction.action.Enable();
        openChestAction.action.performed += OnTryToOpenChest;

        actionTrigger.OnEnter += OnActionTriggerEnter;
        actionTrigger.OnStay += OnActionTriggerStay;
        actionTrigger.OnExit += OnActionTriggerExit;
    }

    private void OnDestroy()
    {
        openChestAction.action.Disable();
        openChestAction.action.performed -= OnTryToOpenChest;

        actionTrigger.OnEnter -= OnActionTriggerEnter;
        actionTrigger.OnStay -= OnActionTriggerStay;
        actionTrigger.OnExit -= OnActionTriggerExit;
    }

    private void OnActionTriggerEnter(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        isPlayerInRange = true;
        OnPlayerEnteredChestArea?.Invoke();
    }

    private void OnActionTriggerStay(Collider collider)
    {
        if (isPlayerInRange == false)
            OnActionTriggerEnter(collider);
    }

    private void OnActionTriggerExit(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        isPlayerInRange = false;
        OnPlayerExitedChestArea?.Invoke();
    }

    private void OnTryToOpenChest(InputAction.CallbackContext context)
    {
        if (isPlayerInRange == false) return;
        if (isChestOpened) return;

        isChestOpened = true;
        OnAnyChestOpened?.Invoke(goldAmount);
        OnChestOpened?.Invoke(goldAmount);

        gameObject.SetActive(false);
    }
}
