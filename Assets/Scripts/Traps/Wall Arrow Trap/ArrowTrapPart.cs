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
            // Move in local space along the part's local up (Vector3.up in local coordinates)
            transform.localPosition += moveDirection * currentSettings.ArrowSpeed * Time.fixedDeltaTime;
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
        moveDirection = Vector3.forward;
        currentSettings = arrowSettings;

        var seq = DOTween.Sequence();
        Vector3 behindPos = initialPosition - (moveDirection * currentSettings.ArrowAppearPosition);

        transform.localPosition = behindPos;

        seq.Append(transform.DOLocalMove(initialPosition, currentSettings.ArrowAppearDuration).SetEase(currentSettings.ArrowAppearEase));
        seq.OnComplete(() =>
        {
            DOVirtual.DelayedCall(currentSettings.DelayToActivateArrow, EnableTrigger, false);
            isMoving = true;
        });

        appearTween = seq;
    }

    private void Disappear(bool hasHitPlayer = false)
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

        float duration = hasHitPlayer ? 0f : currentSettings.ArrowDisappearDuration;

        disappearTween = transform.DOScale(Vector3.zero, duration)
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

        bool hasHitPlayer = collider.CompareTag(GameManager.PlayerTag);
        Disappear(hasHitPlayer);

        if (hasHitPlayer)
        {
            var deathIdentifier = collider.GetComponent<PlayerDeathIdentifier>();
            if (deathIdentifier == null || deathIdentifier.IsDead) return;

            deathIdentifier.Death();
            Debug.Log("Player hit by arrow trap");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw local axes
        var origin = transform.position;
        float length = 0.5f;

        // Right - red
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + transform.right * length);

        // Up - green
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + transform.up * length);

        // Forward - blue
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + transform.forward * length);

        // If has parent, draw parent's axes as dimmer/longer lines
        if (transform.parent != null)
        {
            var pOrigin = transform.parent.position;
            float pLength = 0.7f;

            // Parent Right - red (dashed look by drawing short segments)
            Gizmos.color = new Color(1f, 0.4f, 0.4f);
            Gizmos.DrawLine(pOrigin, pOrigin + transform.parent.right * pLength);

            // Parent Up - green
            Gizmos.color = new Color(0.4f, 1f, 0.4f);
            Gizmos.DrawLine(pOrigin, pOrigin + transform.parent.up * pLength);

            // Parent Forward - blue
            Gizmos.color = new Color(0.4f, 0.4f, 1f);
            Gizmos.DrawLine(pOrigin, pOrigin + transform.parent.forward * pLength);
        }
    }
}
