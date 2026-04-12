using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExitPortal : MonoBehaviour, IPlaceable
{
    public static event Action OnPlayerEnteredPortalArea;
    public static event Action OnPlayerExitedPortalArea;
    public static event Action OnAnyPortalActivated;
    public event Action OnPortalActivated;

    [Header("References")]
    [SerializeField] private PlaceableSettings placeableSettings;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private TriggerEventSender actionTrigger;

    private bool isPlayerInRange = false;

    public PlaceableSettings PlaceableSettings => placeableSettings;

    private void Awake()
    {
        interactAction.action.Enable();
        interactAction.action.performed += OnTryToExitPortal;

        actionTrigger.OnEnter += OnActionTriggerEnter;
        actionTrigger.OnStay += OnActionTriggerStay;
        actionTrigger.OnExit += OnActionTriggerExit;
    }

    private void OnDestroy()
    {
        // openChestAction.action.Disable();
        interactAction.action.performed -= OnTryToExitPortal;

        actionTrigger.OnEnter -= OnActionTriggerEnter;
        actionTrigger.OnStay -= OnActionTriggerStay;
        actionTrigger.OnExit -= OnActionTriggerExit;
    }

    private void OnActionTriggerEnter(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        isPlayerInRange = true;
        OnPlayerEnteredPortalArea?.Invoke();
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
        OnPlayerExitedPortalArea?.Invoke();
    }

    private void OnTryToExitPortal(InputAction.CallbackContext context)
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isPlayerInRange == false) return;

        OnPortalActivated?.Invoke();
        OnAnyPortalActivated?.Invoke();
    }
}
