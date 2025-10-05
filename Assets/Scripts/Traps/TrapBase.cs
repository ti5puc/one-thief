using System;
using DG.Tweening;
using DG.Tweening.Plugins.Options;
using NaughtyAttributes;
using UnityEngine;

public abstract class TrapBase : MonoBehaviour
{
    [SerializeField] protected TrapSettings trapSettings;

    [Header("Base Colliders")]
    [SerializeField] protected TriggerEventSender actionTrigger;
    [SerializeField] protected TriggerEventSender hitTrigger;

    protected bool foundNearestGround = false;
    protected bool hasActionTriggerStayed = false;
    protected bool hasHitTriggerStayed = false;

    public TrapSettings TrapSettings => trapSettings;

    protected virtual void Awake()
    {
        Initialize();

        actionTrigger.OnEnter += OnActionTriggerEnter;
        actionTrigger.OnStay += OnActionTriggerStay;
        hitTrigger.OnEnter += OnHitTriggerEnter;
        hitTrigger.OnStay += OnHitTriggerStay;

        // if on ground, check for nearest ground tile to disable it and use the trap one instead
        if (trapSettings.TrapSurface == GameManager.GroundLayerMask)
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

    protected virtual void Update()
    {
        if (trapSettings.IsAlwaysActive)
            OnAlwaysActive();
    }

    protected virtual void OnActionTriggerEnter(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;
        if (other.GetComponent<PlayerDeathIdentifier>().IsDead) return;

        actionTrigger.gameObject.SetActive(false);

        OnAction(other, trapSettings.DelayToHit);

        DOVirtual.DelayedCall(trapSettings.DelayToHit, () =>
        {
            hitTrigger.gameObject.SetActive(true);

            if (trapSettings.KeepHitTrigger == false)
            {
                DOVirtual.DelayedCall(trapSettings.HitTriggerActiveTime, () =>
                {
                    hitTrigger.gameObject.SetActive(false);
                });
            }

            if (trapSettings.CanReactive)
            {
                OnReactivate(trapSettings.DelayBeforeReactivate);
                DOVirtual.DelayedCall(trapSettings.DelayBeforeReactivate, Initialize);
            }
        });
    }

    protected virtual void OnActionTriggerStay(Collider other)
    {
        if (hasActionTriggerStayed) return;

        OnActionTriggerEnter(other);
        hasActionTriggerStayed = true;
    }

    protected virtual void OnHitTriggerEnter(Collider other)
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (other.CompareTag(GameManager.PlayerTag) == false) return;
        if (other.GetComponent<PlayerDeathIdentifier>().IsDead) return;
        OnHit(other);
    }

    protected virtual void OnHitTriggerStay(Collider other)
    {
        if (hasHitTriggerStayed) return;

        OnHitTriggerEnter(other);
        hasHitTriggerStayed = true;
    }

    protected virtual void Initialize()
    {
        actionTrigger.gameObject.SetActive(true);
        hitTrigger.gameObject.SetActive(trapSettings.IsAlwaysActive);

        if (trapSettings == null)
        {
            Debug.LogError("TrapSettings not assigned in " + gameObject.name);
            return;
        }
    }

    protected abstract void OnAction(Collider player, float totalDuration);
    protected abstract void OnHit(Collider player);
    protected abstract void OnReactivate(float totalDuration);
    protected abstract void OnAlwaysActive();
}
