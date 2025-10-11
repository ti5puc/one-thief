using System;
using UnityEngine;
using DG.Tweening;

public class ArrowTrapPart : MonoBehaviour
{
    private event Action OnDisappear;

    [SerializeField] private TriggerEventSender hitTrigger;

    private Tween appearTween;
    private Tween disappearTween;
    private bool isActive = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private Transform parentTransform;

    private bool isMoving = false;
    private bool isPaused = false;
    private Vector3 moveDirection;
    private WallArrowTrap.ArrowSettings currentSettings;

    private void Awake()
    {
        hitTrigger.OnEnter += HandleHit;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        parentTransform = transform.parent;

        gameObject.SetActive(false);
        DisableTrigger();
    }

    private void OnDestroy()
    {
        hitTrigger.OnEnter -= HandleHit;
    }

    private void FixedUpdate()
    {
        if (currentSettings == null) return;

        if (isActive && isMoving && !isPaused)
            transform.localPosition += moveDirection * currentSettings.ArrowSpeed * Time.deltaTime;
    }

    public void PauseMovement() => isPaused = true;
    public void ResumeMovement() => isPaused = false;

    public void EnableTrigger() => hitTrigger.Collider.enabled = true;
    public void DisableTrigger() => hitTrigger.Collider.enabled = false;

    public void LaunchArrow(WallArrowTrap.ArrowSettings arrowSettings, Action onDisappear)
    {
        isActive = true;
        appearTween?.Kill();
        disappearTween?.Kill();

        transform.SetParent(parentTransform);
        transform.localScale = initialScale;
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;

        gameObject.SetActive(true);
        DisableTrigger();
        OnDisappear = onDisappear;

        isMoving = false;
        moveDirection = transform.parent.forward.normalized;
        currentSettings = arrowSettings;

        var seq = DOTween.Sequence();
        Vector3 behindPos = initialPosition - (transform.parent.forward.normalized * currentSettings.ArrowAppearPosition);

        transform.localPosition = behindPos;

        seq.Append(transform.DOLocalMove(initialPosition, currentSettings.ArrowAppearDuration).SetEase(currentSettings.ArrowAppearEase));
        seq.OnComplete(() =>
        {
            DOVirtual.DelayedCall(currentSettings.DelayToActivateArrow, EnableTrigger, false);
            isMoving = true;
        });

        appearTween = seq;
    }

    private void Disappear()
    {
        isActive = false;
        isMoving = false;

        if (currentSettings == null)
        {
            gameObject.SetActive(false);
            DisableTrigger();
            OnDisappear?.Invoke();

            return;
        }

        disappearTween = transform.DOScale(Vector3.zero, currentSettings.ArrowDisappearDuration)
            .SetEase(currentSettings.ArrowDisappearEase).OnComplete(() =>
            {
                gameObject.SetActive(false);
                DisableTrigger();
                OnDisappear?.Invoke();
            });
    }

    private void HandleHit(Collider collider)
    {
        if (GameManager.CurrentGameState != GameState.Exploring) return;
        if (!isActive) return;

        isActive = false;
        isMoving = false;

        transform.SetParent(collider.transform);
        Disappear();

        if (GameManager.CurrentGameState == GameState.Building) return;
        if (!collider.CompareTag(GameManager.PlayerTag)) return;

        var deathIdentifier = collider.GetComponent<PlayerDeathIdentifier>();
        if (deathIdentifier == null || deathIdentifier.IsDead) return;

        deathIdentifier.Death();
        Debug.Log("Player hit by arrow trap");
    }
}
