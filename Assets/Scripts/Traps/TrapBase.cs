using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    public enum TrapSurface { Floor, Wall, Ceiling }

    [Header("Base Settings")]
    [SerializeField] protected string playerTag = "Player";
    [SerializeField] protected float delayToHit = 0.7f;
    [SerializeField] protected TrapSurface trapSurface;

    [Space(10)]
    [SerializeField] protected bool keepHitTrigger;
    [SerializeField, HideIf(nameof(keepHitTrigger))] protected float hitTriggerActiveTime = .2f;

    [Space(10)]
    [SerializeField] protected bool canReactive;
    [SerializeField, ShowIf(nameof(canReactive))] protected float activatedDurationTime = 4f;

    [Header("Base Colliders")]
    [SerializeField] protected TriggerEventSender actionTrigger;
    [SerializeField] protected TriggerEventSender hitTrigger;

    protected bool foundNearestGround = false;

    protected virtual void Awake()
    {
        Setup();

        actionTrigger.OnEnter += OnActionTriggerEnter;
        hitTrigger.OnEnter += OnHitTriggerEnter;

        if (trapSurface == TrapSurface.Floor)
        {
            if (!foundNearestGround)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Ground"));
                if (colliders.Length > 0)
                {
                    Collider nearestGround = colliders[0];
                    float minDistance = Vector3.Distance(transform.position, nearestGround.transform.position);
                    foreach (var collider in colliders)
                    {
                        float distance = Vector3.Distance(transform.position, collider.transform.position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestGround = collider;
                        }
                    }

                    nearestGround.gameObject.SetActive(false);
                    foundNearestGround = true;
                    Debug.Log("Nearest ground object found: " + nearestGround.name);
                }
            }
        }
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

        OnAction(other, delayToHit);

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

    protected abstract void OnAction(Collider player, float totalDuration);
    protected abstract void OnHit(Collider player);
    protected abstract void OnReactivate(float totalDuration);
}
