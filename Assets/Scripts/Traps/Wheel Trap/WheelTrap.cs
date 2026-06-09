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

    [Header("Proximity Shake")]
    [SerializeField] private float shakeMediumDistance = 3f;
    [SerializeField] private float shakeLightDistance = 6f;
    [SerializeField] private float shakeMediumCooldown = 0.8f;
    [SerializeField] private float shakeLightCooldown = 1.2f;

    private bool rollingForward = true;
    private bool isRolling = false;
    private Transform _playerTransform;
    private float _shakeCooldownTimer;

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

        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;

        Vector3 moveDir = rollingForward ? Vector3.left : Vector3.right;
        wheelObject.transform.localPosition += moveDir * rollSpeed * Time.deltaTime;
        wheelTriggerObject.transform.localPosition += moveDir * rollSpeed * Time.deltaTime;

        float rotationAmount = (rollSpeed / 1f) * 360f * Time.deltaTime / (2f * Mathf.PI);
        wheelObject.transform.Rotate(-Vector3.forward, rotationAmount * (rollingForward ? 1f : -1f), Space.Self);
        wheelTriggerObject.transform.Rotate(-Vector3.forward, rotationAmount * (rollingForward ? 1f : -1f), Space.Self);

        TryProximityShake();
    }

    private void TryProximityShake()
    {
        _shakeCooldownTimer -= Time.deltaTime;
        if (_shakeCooldownTimer > 0f) return;

        if (_playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag(GameManager.PlayerTag);
            if (playerObj == null) return;
            _playerTransform = playerObj.transform;
        }

        float dist = Vector3.Distance(wheelObject.transform.position, _playerTransform.position);

        if (dist <= shakeMediumDistance)
        {
            GameManager.ShakeMedium();
            _shakeCooldownTimer = shakeMediumCooldown;
        }
        else if (dist <= shakeLightDistance)
        {
            GameManager.ShakeLight();
            _shakeCooldownTimer = shakeLightCooldown;
        }
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
