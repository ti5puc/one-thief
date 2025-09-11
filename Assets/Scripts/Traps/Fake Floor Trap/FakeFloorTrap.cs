using DG.Tweening;
using UnityEngine;

public class FakeFloorTrap : TrapBase
{
    [Header("Fake Floor Settings")]
    [SerializeField] private float fallDuration = 1f;

    [Header("Custom Death Cam")]
    [SerializeField] private float customCameraDeathRotationX = 40f;
    [SerializeField] private float customCameraDeathOffsetY = 3f;
    [SerializeField] private float customCameraDeathOffsetZ = -2f;

    [Header("References")]
    [SerializeField] private GameObject fakeFloorVisual;
    [SerializeField] private DeathTrigger deathTrigger;

    protected override void Awake()
    {
        base.Awake();
        deathTrigger.SetCustomDeathCam(customCameraDeathRotationX, customCameraDeathOffsetY, customCameraDeathOffsetZ);
    }

    protected override void OnAction(float totalDuration) { }

    protected override void OnHit(Collider player)
    {
        Debug.Log("Player hit by fake floor trap");

        fakeFloorVisual.SetActive(false);

        var collider = player.GetComponent<Collider>();
        collider.isTrigger = true;

        DOVirtual.DelayedCall(fallDuration, () =>
        {
            collider.isTrigger = false;
        });
    }

    protected override void OnReactivate(float totalDuration) { }
}
