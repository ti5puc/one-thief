using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected string playerTag = "Player";
    [SerializeField] protected float delayToHit = 0.7f;

    [Space(10)]
    [SerializeField] protected bool keepHitTrigger;
    [SerializeField, HideIf(nameof(keepHitTrigger))] protected float hitTriggerActiveTime = .2f;

    [Space(10)]
    [SerializeField] protected bool canReactive;
    [SerializeField, ShowIf(nameof(canReactive))] protected float activatedDurationTime = 4f;

    [Header("Base Colliders")]
    [SerializeField] protected TriggerEventSender actionTrigger;
    [SerializeField] protected TriggerEventSender hitTrigger;

    protected virtual void Awake()
    {
        Setup();

        actionTrigger.OnEnter += OnActionTriggerEnter;
        hitTrigger.OnEnter += OnHitTriggerEnter;
    }

    protected virtual void OnDestroy()
    {
        actionTrigger.OnEnter -= OnActionTriggerEnter;
        hitTrigger.OnEnter -= OnHitTriggerEnter;
    }

    protected virtual void OnActionTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) == false) return;
        if (other.GetComponent<PlayerDeathIdentifier>().IsDead) return;

        actionTrigger.gameObject.SetActive(false);

        OnAction(delayToHit);

        DOVirtual.DelayedCall(delayToHit, () =>
        {
            hitTrigger.gameObject.SetActive(true);

            if (keepHitTrigger == false)
            {
                DOVirtual.DelayedCall(hitTriggerActiveTime, () =>
                {
                    hitTrigger.gameObject.SetActive(false);
                });
            }

            if (canReactive)
            {
                OnReactivate(activatedDurationTime);
                DOVirtual.DelayedCall(activatedDurationTime, Setup);
            }
        });
    }

    protected virtual void OnHitTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) == false) return;
        if (other.GetComponent<PlayerDeathIdentifier>().IsDead) return;
        OnHit(other);
    }

    protected virtual void Setup()
    {
        actionTrigger.gameObject.SetActive(true);
        hitTrigger.gameObject.SetActive(false);
    }

    protected abstract void OnAction(float totalDuration);
    protected abstract void OnHit(Collider player);
    protected abstract void OnReactivate(float totalDuration);
}
