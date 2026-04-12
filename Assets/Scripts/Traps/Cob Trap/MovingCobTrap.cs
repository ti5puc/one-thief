using DG.Tweening;
using UnityEngine;

public class MovingCobTrap : TrapBase
{
    [Header("Cob Trap Settings")]
    [SerializeField] private GameObject cobObject;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float sineAmplitude = 1f;
    [SerializeField] private float sineFrequency = 1f;
    [SerializeField] private Vector3 sineAxis = Vector3.up;

    private Vector3 _startLocalPosition;
    private float _sineTime;

    protected override void Awake()
    {
        base.Awake();
        _startLocalPosition = cobObject.transform.localPosition;
    }

    protected override void OnAlwaysActive()
    {
        if (GameManager.CurrentGameState == GameState.Building) return;
        if (GameManager.IsGamePaused) return;

        _sineTime += Time.deltaTime;
        cobObject.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime, Space.Self);
        cobObject.transform.localPosition = _startLocalPosition + sineAxis.normalized * (Mathf.Sin(_sineTime * sineFrequency) * sineAmplitude);
    }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by cob trap");
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
