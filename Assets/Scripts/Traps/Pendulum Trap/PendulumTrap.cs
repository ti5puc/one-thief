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
    [SerializeField] private GameObject swingObject;

    private bool hasStartedSwinging = false;
    private Sequence swingSequence;

    protected override void OnAlwaysActive()
    {
        if (GameManager.CurrentGameState == GameState.Exploring)
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
        controller.Death();
    }

    protected override void Initialize()
    {
        base.Initialize();
        actionTrigger.gameObject.SetActive(false);
    }

    protected override void OnAction(Collider player, float totalDuration) => throw new NotImplementedException();
    protected override void OnReactivate(float totalDuration) => throw new NotImplementedException();

    [Button]
    private void ResetAnimation()
    {
        hasStartedSwinging = false;
        if (swingSequence != null)
        {
            swingSequence.Kill();
            swingSequence = null;
        }
    }
}
