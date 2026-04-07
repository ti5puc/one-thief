using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class TreasureChest : MonoBehaviour, IPlaceable
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

    [Header("Animation")]
    [SerializeField] private GameObject lidObject;
    [SerializeField] private Vector3 lidOpenRotation = new Vector3(-90f, 90f, -90f);
    [SerializeField] private Ease lidOpenEase = Ease.OutBack;
    [SerializeField] private float lidOpenDuration = 0.4f;
    [SerializeField] private float scaleDownDuration = 0.3f;

    private bool isPlayerInRange = false;
    private bool isChestOpened = false;

    public PlaceableSettings PlaceableSettings => placeableSettings;

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
        // openChestAction.action.Disable();
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
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isPlayerInRange == false) return;
        if (isChestOpened) return;

        isChestOpened = true;
        SoundManager.PlaySound(SoundType.COIN);

        Sequence openSequence = DOTween.Sequence();

        // Lid opens with overshoot bounce for juiciness
        openSequence.Append(
            lidObject.transform.DOLocalRotate(lidOpenRotation, lidOpenDuration)
                .SetEase(lidOpenEase)
        );

        // Brief pause then the whole chest scales down and disappears
        openSequence.AppendCallback(() =>
        {
            OnAnyChestOpened?.Invoke(goldAmount);
            OnChestOpened?.Invoke(goldAmount);
        });

        openSequence.AppendInterval(0.15f);
        openSequence.Append(
            transform.DOScale(Vector3.zero, scaleDownDuration)
                .SetEase(Ease.InBack)
        );
        openSequence.OnComplete(() => gameObject.SetActive(false));
    }
}
