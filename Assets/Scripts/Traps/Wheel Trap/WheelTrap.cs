using DG.Tweening;
using UnityEngine;

public class WheelTrap : TrapBase
{
    [Header("Wheel Trap Settings")]
    [SerializeField] private GameObject wheelObject;
    [SerializeField] private float rollSpeed = 5f;
    [SerializeField] private float rollBackSpeed = 5f;
    [SerializeField] private float rollDistance = 5f;
    [SerializeField] private Ease rollEase = Ease.InOutSine;

    private Vector3 leftDirection;
    private bool rollingForward = true;
    private bool isRolling = false;

    protected override void Awake()
    {
        base.Awake();

        if (wheelObject == null)
            wheelObject = gameObject;

        leftDirection = wheelObject.transform.right * -1;

        hitTrigger.OnEnter += HandleHitTrigger;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        hitTrigger.OnEnter -= HandleHitTrigger;
    }

    protected override void OnAlwaysActive()
    {
        if (!isRolling)
        {
            isRolling = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isRolling) return;
        Vector3 moveDir = rollingForward ? leftDirection : -leftDirection;
        wheelObject.transform.localPosition += moveDir * rollSpeed * Time.fixedDeltaTime;

        float rotationAmount = (rollSpeed / 1f) * 360f * Time.fixedDeltaTime / (2f * Mathf.PI);
        wheelObject.transform.Rotate(Vector3.forward, rotationAmount * (rollingForward ? 1f : -1f), Space.Self);
    }

    private void HandleHitTrigger(Collider other)
    {
        if (!isRolling) return;

        if (other.CompareTag("Wall"))
        {
            rollingForward = !rollingForward;
        }
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
