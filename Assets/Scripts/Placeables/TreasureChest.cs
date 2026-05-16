using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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

    [Space(10)]
    [SerializeField] private TMP_Text goldText;

    [Header("Animation")]
    [SerializeField] private GameObject lidObject;
    [SerializeField] private List<GameObject> vfxGold;
    [SerializeField] private Vector3 lidOpenRotation = new Vector3(-90f, 90f, -90f);
    [SerializeField] private Ease lidOpenEase = Ease.OutBack;
    [SerializeField] private float lidOpenDuration = 0.4f;
    [SerializeField] private float scaleDownDuration = 0.3f;

    private bool isPlayerInRange = false;
    private bool isChestOpened = false;

    public PlaceableSettings PlaceableSettings => placeableSettings;

    public void SetGoldAmount(int amount)
    {
        goldAmount = Mathf.Max(0, amount);
    }

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

        goldText.gameObject.SetActive(true);
        goldText.text = $"${goldAmount}";

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
        goldText.gameObject.SetActive(false);

        OnPlayerExitedChestArea?.Invoke();
    }

    private void OnTryToOpenChest(InputAction.CallbackContext context)
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isPlayerInRange == false) return;
        if (isChestOpened) return;

        isChestOpened = true;
        foreach (var vfx in vfxGold)
            vfx.SetActive(true);
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
        openSequence.OnComplete(() =>
        {
            foreach (var vfx in vfxGold)
                vfx.SetActive(false);
            gameObject.SetActive(false);
        });
    }
}
