using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class PendulumTrap : TrapBase
{
    [Header("Pendulum Settings")]
    [SerializeField] private float swingAngle = 45f;
    [SerializeField] private float swingDuration = 1f;
    [SerializeField] private Ease swingEase = Ease.InOutSine;

    [Space(10)]
    [SerializeField] private bool onlyKnockback = false;
    [SerializeField, ShowIf(nameof(onlyKnockback))] private float knockbackForce = 10f;
    [SerializeField, ShowIf(nameof(onlyKnockback))] private float knockbackVerticalForce = 2f;

    [Space(10)]
    [SerializeField] private GameObject swingObject;

    private bool hasStartedSwinging = false;
    private Sequence swingSequence;
    private Quaternion initialRotation;
    private bool canKnockback = true;

    protected override void Awake()
    {
        base.Awake();
        initialRotation = swingObject.transform.localRotation;
    }

    protected override void OnAlwaysActive()
    {
        if (GameManager.CurrentGameState != GameState.Building)
            swingSequence.Play();
        else
            swingSequence.Pause();

        if (hasStartedSwinging == false)
        {
            var localRotY = swingObject.transform.localEulerAngles.y;
            var localRotZ = swingObject.transform.localEulerAngles.z;

            swingSequence = DOTween.Sequence();

            swingObject.transform.localRotation = Quaternion.Euler(new Vector3(-swingAngle, localRotY, localRotZ));

            swingSequence.Append(swingObject.transform.DOLocalRotate(new Vector3(swingAngle, localRotY, localRotZ), swingDuration).SetEase(swingEase));
            swingSequence.Append(swingObject.transform.DOLocalRotate(new Vector3(-swingAngle, localRotY, localRotZ), swingDuration).SetEase(swingEase));
            swingSequence.SetLoops(-1);

            hasStartedSwinging = true;
        }
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by pendulum trap");

        var controller = player.GetComponent<PlayerDeathIdentifier>();
        if (onlyKnockback)
        {
            if (!canKnockback)
                return;

            canKnockback = false;

            Vector3 swingDir = swingObject.transform.right;
            swingDir.y = 0f;
            swingDir.Normalize();

            Vector3 pendulumToPlayer = player.transform.position - swingObject.transform.position;
            float side = Vector3.Dot(pendulumToPlayer, swingDir);
            if (side < 0f)
                swingDir = -swingDir;

            Vector3 direction = swingDir;
            direction.y = knockbackVerticalForce;
            direction.Normalize();

            controller.Knockback(direction, knockbackForce);

            Invoke(nameof(ResetKnockback), 1f);
            return;
        }

        controller.Death();
    }

    protected override void Initialize()
    {
        base.Initialize();
        actionTrigger.gameObject.SetActive(false);
    }

    protected override void OnAction(Collider player, float totalDuration) => throw new NotImplementedException();
    protected override void OnReactivate(float totalDuration) => throw new NotImplementedException();

    private void ResetKnockback()
    {
        canKnockback = true;
    }

    [Button]
    private void ResetAnimation()
    {
        hasStartedSwinging = false;
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
        swingObject.transform.localRotation = initialRotation;
    }
}
