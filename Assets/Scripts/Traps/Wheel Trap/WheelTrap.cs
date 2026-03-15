using DG.Tweening;
using UnityEngine;

public class WheelTrap : TrapBase
{
    [Header("Wheel Trap Settings")]
    [SerializeField] private GameObject wheelObject;
    [SerializeField] private GameObject wheelTriggerObject;
    [SerializeField] private float rollSpeed = 5f;
    [SerializeField] private float rollBackSpeed = 5f;
    [SerializeField] private float rollDistance = 5f;
    [SerializeField] private Ease rollEase = Ease.InOutSine;

    private bool rollingForward = true;
    private bool isRolling = false;

    protected override void Awake()
    {
        base.Awake();
        actionTrigger.OnEnter += HandleReturnOnHit;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        actionTrigger.OnEnter -= HandleReturnOnHit;
    }

    protected override void OnAlwaysActive()
    {
        if (!isRolling)
            isRolling = true;

        if (!isRolling) return;
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;

        Vector3 moveDir = rollingForward ? Vector3.left : Vector3.right;
        wheelObject.transform.localPosition += moveDir * rollSpeed * Time.fixedDeltaTime;
        wheelTriggerObject.transform.localPosition += moveDir * rollSpeed * Time.fixedDeltaTime;

        float rotationAmount = (rollSpeed / 1f) * 360f * Time.fixedDeltaTime / (2f * Mathf.PI);
        wheelObject.transform.Rotate(-Vector3.forward, rotationAmount * (rollingForward ? 1f : -1f), Space.Self);
        wheelTriggerObject.transform.Rotate(-Vector3.forward, rotationAmount * (rollingForward ? 1f : -1f), Space.Self);
    }

    private void HandleReturnOnHit(Collider other)
    {
        if (!isRolling) return;
        rollingForward = !rollingForward;
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by wheel trap");
        var controller = player.GetComponent<PlayerDeathIdentifier>();
        if (controller != null)
            controller.Death();
    }

    protected override void OnAction(Collider player, float totalDuration)
    {
    }

    protected override void OnReactivate(float totalDuration)
    {
    }
}
