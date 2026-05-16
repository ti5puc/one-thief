using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FakeChestTrap : TrapBase
{
    public static event Action OnPlayerEnteredChestArea;
    public static event Action OnPlayerExitedChestArea;
    public static event Action OnAnyChestOpened;

    [Header("References")]
    [SerializeField] private InputActionReference openChestAction;

    [Space(10)]
    [SerializeField] private TMP_Text fakeGoldText;

    [Header("Animation")]
    [SerializeField] private GameObject lidObject;
    [SerializeField] private Vector3 lidOpenRotation = new Vector3(-90f, 90f, -90f);
    [SerializeField] private Ease lidOpenEase = Ease.OutBack;
    [SerializeField] private float lidOpenDuration = 0.4f;

    [Space(10)]
    [SerializeField] private GameObject wallSpears;
    [SerializeField] private float spearsExtendDistance = 1f;
    [SerializeField] private float spearsExtendDuration = 0.3f;
    [SerializeField] private Ease spearsExtendEase = Ease.OutBack;
    [SerializeField] private float spearsRetractDuration = 0.4f;

    private bool isPlayerInRange = false;
    private bool isChestOpened = false;
    private int fakeGoldAmount;

    private Vector3 lidOriginalLocalRotation;
    private Vector3 spearsStartWorldPos;

    protected override void Awake()
    {
        base.Awake();

        lidOriginalLocalRotation = lidObject.transform.localEulerAngles;
        spearsStartWorldPos = wallSpears.transform.position;

        openChestAction.action.Enable();
        openChestAction.action.performed += OnTryToOpenChest;

        // Only Exit needs to be registered here; TrapBase.Awake already handles Enter/Stay via virtual dispatch
        actionTrigger.OnExit += OnActionTriggerExit;

        fakeGoldAmount = UnityEngine.Random.Range(SaveSystem.NextLevelTotalGold / 4, SaveSystem.NextLevelTotalGold);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        openChestAction.action.performed -= OnTryToOpenChest;
        actionTrigger.OnExit -= OnActionTriggerExit;
    }

    protected override void Initialize()
    {
        base.Initialize();
        isChestOpened = false;
    }

    protected override void OnActionTriggerEnter(Collider other)
    {
        // Do NOT call base — the fake chest only triggers on button press, not on area enter
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        isPlayerInRange = true;

        fakeGoldText.gameObject.SetActive(true);
        fakeGoldText.text = $"${fakeGoldAmount}";

        OnPlayerEnteredChestArea?.Invoke();
    }

    protected override void OnActionTriggerStay(Collider collider)
    {
        if (isPlayerInRange == false)
            OnActionTriggerEnter(collider);
    }

    private void OnActionTriggerExit(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;

        isPlayerInRange = false;
        fakeGoldText.gameObject.SetActive(false);

        OnPlayerExitedChestArea?.Invoke();
    }

    private void OnTryToOpenChest(InputAction.CallbackContext context)
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (isPlayerInRange == false) return;
        if (isChestOpened) return;

        isChestOpened = true;

        Vector3 spearsTargetPos = spearsStartWorldPos + wallSpears.transform.right * spearsExtendDistance;

        Sequence openSequence = DOTween.Sequence();

        // Lid opens with overshoot bounce for juiciness
        openSequence.Append(
            lidObject.transform.DOLocalRotate(lidOpenRotation, lidOpenDuration)
                .SetEase(lidOpenEase)
        );

        // Brief pause then the whole chest scales down and disappears
        openSequence.AppendCallback(() =>
        {
            OnAnyChestOpened?.Invoke();
        });

        // Spears shoot out and hit trigger activates in sync the moment lid finishes
        openSequence.AppendCallback(() =>
        {
            wallSpears.transform.DOMove(spearsTargetPos, spearsExtendDuration).SetEase(spearsExtendEase);

            hitTrigger.gameObject.SetActive(true);

            if (!trapSettings.KeepHitTrigger)
            {
                DOVirtual.DelayedCall(trapSettings.HitTriggerActiveTime, () =>
                    hitTrigger.gameObject.SetActive(false));
            }

            if (trapSettings.CanReactive)
            {
                OnReactivate(trapSettings.DelayBeforeReactivate);
                DOVirtual.DelayedCall(trapSettings.DelayBeforeReactivate, Initialize);
            }
        });
    }

    protected override void OnHit(Collider player)
    {
        var controller = player.GetComponent<PlayerDeathIdentifier>();
        controller.Death();
    }

    protected override void OnReactivate(float totalDuration)
    {
        // Retract spears then close lid, timed so both animations finish just before Initialize runs
        float retractStart = Mathf.Max(0f, totalDuration - spearsRetractDuration - lidOpenDuration - 0.15f);

        DOVirtual.DelayedCall(retractStart, () =>
        {
            wallSpears.transform.DOMove(spearsStartWorldPos, spearsRetractDuration).SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    lidObject.transform.DOLocalRotate(lidOriginalLocalRotation, lidOpenDuration).SetEase(Ease.InBack);
                });
        });
    }

    protected override void OnAction(Collider player, float totalDuration)
    {
        // Handled directly in OnTryToOpenChest
    }

    protected override void OnAlwaysActive()
    {
    }
}
